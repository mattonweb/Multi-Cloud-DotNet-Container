namespace MultiCloud.Application.Web.DTOs;

using System;

/// <summary>
/// How many entries of <c>X-Forwarded-For</c>, counted from the right, the application trusts: one per proxy the
/// platform puts in front of it. The caller's own address is the last trusted entry.
/// </summary>
/// <remarks>
/// A caller can write anything at the start of the header, so only the entries the platform's own proxies appended are
/// trustworthy, and the application cannot tell how many those are from the header alone. Google's front end on Cloud
/// Run appends one; Google's HTTP load balancer on GKE appends two, the caller's address and then its own.
/// </remarks>
internal readonly record struct ForwardLimit
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ForwardLimit"/> class.
    /// </summary>
    /// <param name="value">The number of entries to trust; 1 or more.</param>
    public ForwardLimit(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
        this.Value = value;
    }

    /// <summary>
    /// Gets the number of entries to trust.
    /// </summary>
    public int Value { get; }
}
