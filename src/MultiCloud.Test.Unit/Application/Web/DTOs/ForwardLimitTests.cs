namespace MultiCloud.Test.Unit.Application.Web.DTOs;

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiCloud.Application.Web.DTOs;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="ForwardLimit"/> struct, and what each limit does
/// to the caller's address when ASP.NET Core's own forwarded headers middleware applies it, as it does in the image.
/// </summary>
/// <remarks>
/// The addresses follow GKE, where Google's HTTP load balancer connects from one of its own addresses and sends
/// <c>X-Forwarded-For</c> with the caller's address, then the load balancer's public address (docs/SDD.md, decision
/// D11). All are documentation addresses or Google's published load balancer ranges, none anyone's own.
/// </remarks>
[TestClass]
public sealed class ForwardLimitTests
{
    private const string Caller = "203.0.113.7";
    private const string LoadBalancer = "198.51.100.20";
    private const string Forged = "192.0.2.99";

    #region Public Methods

    /// <summary>
    /// Unit test to verify that a limit below one is refused, since there is always at least the one proxy that
    /// connected.
    /// </summary>
    [TestMethod]
    public void Constructor_Zero_ThrowsArgumentOutOfRangeException()
    {
        // Arrange (Given)
        bool exceptionThrown = false;

        // Act (When)
        try
        {
            _ = new ForwardLimit(0);
        }
        catch (ArgumentOutOfRangeException)
        {
            exceptionThrown = true;
        }

        // Assert (Then)
        Assert.IsTrue(
            exceptionThrown,
            "A limit of 0 should be refused with ArgumentOutOfRangeException.");
    }

    /// <summary>
    /// Unit test to verify the finding on GKE: with the default limit of one, behind two proxies, the application
    /// reports the load balancer's own address as the caller's.
    /// </summary>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    public async Task Apply_LimitOneBehindTwoProxies_ReportsTheLoadBalancer()
    {
        // Arrange (Given)
        HttpContext context = CreateRequest($"{Caller}, {LoadBalancer}");

        // Act (When)
        await ApplyAsync(new ForwardLimit(1), context).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            IPAddress.Parse(LoadBalancer),
            context.Connection.RemoteIpAddress,
            "With one entry trusted, the last entry, the load balancer's, should become the remote address.");
    }

    /// <summary>
    /// Unit test to verify the fix: with a limit of two, behind two proxies, the application reports the caller's
    /// address.
    /// </summary>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    public async Task Apply_LimitTwoBehindTwoProxies_ReportsTheCaller()
    {
        // Arrange (Given)
        HttpContext context = CreateRequest($"{Caller}, {LoadBalancer}");

        // Act (When)
        await ApplyAsync(new ForwardLimit(2), context).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            IPAddress.Parse(Caller),
            context.Connection.RemoteIpAddress,
            "With two entries trusted, the caller's address should become the remote address.");
    }

    /// <summary>
    /// Unit test to verify why the limit is a setting rather than "take the first entry": an address the caller wrote
    /// at the start of the header is ignored, because only the entries the platform's proxies appended are trusted.
    /// </summary>
    /// <returns>A task that completes when the test has run.</returns>
    [TestMethod]
    public async Task Apply_LimitTwoWithForgedFirstEntry_IgnoresTheForgery()
    {
        // Arrange (Given)
        HttpContext context = CreateRequest($"{Forged}, {Caller}, {LoadBalancer}");

        // Act (When)
        await ApplyAsync(new ForwardLimit(2), context).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            IPAddress.Parse(Caller),
            context.Connection.RemoteIpAddress,
            "A forged first entry should not become the remote address.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates a request as it reaches the pod on GKE: from one of Google's load balancer addresses, with the header
    /// given.
    /// </summary>
    /// <param name="forwardedFor">The <c>X-Forwarded-For</c> header.</param>
    /// <returns>The request's context.</returns>
    private static HttpContext CreateRequest(string forwardedFor)
    {
        DefaultHttpContext context = new DefaultHttpContext();
        ConnectionInfo connection = context.Connection;
        connection.RemoteIpAddress = IPAddress.Parse("35.191.10.1");
        HttpRequest request = context.Request;
        request.Headers["X-Forwarded-For"] = forwardedFor;
        request.Headers["X-Forwarded-Proto"] = "http";
        return context;
    }

    /// <summary>
    /// Runs the forwarded headers middleware as the image configures it: X-Forwarded-For and X-Forwarded-Proto, every
    /// proxy trusted (what <c>ASPNETCORE_FORWARDEDHEADERS_ENABLED</c> does), and the limit given.
    /// </summary>
    /// <param name="limit">The number of entries to trust.</param>
    /// <param name="context">The request.</param>
    /// <returns>A task that completes when the middleware has run.</returns>
    private static Task ApplyAsync(ForwardLimit limit, HttpContext context)
    {
        ForwardedHeadersOptions options = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            ForwardLimit = limit.Value,
        };
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
        ForwardedHeadersMiddleware middleware = new ForwardedHeadersMiddleware(
            static _ => Task.CompletedTask,
            NullLoggerFactory.Instance,
            Options.Create(options));
        return middleware.Invoke(context);
    }

    #endregion Private Methods
}
