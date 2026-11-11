namespace MultiCloud.Application.Web.Models;

using System.Text.Json.Serialization;

/// <summary>
/// The request as the application sees it, as <c>GET /</c> reports it. Behind a platform's proxy, the scheme and the
/// remote address are the caller's only when forwarded headers are honored.
/// </summary>
public sealed record RequestResource
{
    /// <summary>
    /// Gets the request's scheme, <c>http</c> or <c>https</c>.
    /// </summary>
    [JsonPropertyName("scheme")]
    required public string Scheme { get; init; }

    /// <summary>
    /// Gets the request's host, with its port when it has one.
    /// </summary>
    [JsonPropertyName("host")]
    required public string Host { get; init; }

    /// <summary>
    /// Gets the caller's address, or <see langword="null"/> when the connection has none.
    /// </summary>
    [JsonPropertyName("remoteAddress")]
    public string? RemoteAddress { get; init; }
}
