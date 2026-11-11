namespace MultiCloud.Application.Web.Models;

using System;
using System.Text.Json.Serialization;

/// <summary>
/// One run of one instance of the application, as <c>GET /</c> reports it.
/// </summary>
public sealed record SessionResource
{
    /// <summary>
    /// Gets the session's identifier, as the <c>X-Session-Id</c> header and <c>service.instance.id</c> show it.
    /// </summary>
    [JsonPropertyName("id")]
    required public string Id { get; init; }

    /// <summary>
    /// Gets when the session started, in UTC.
    /// </summary>
    [JsonPropertyName("startedOn")]
    required public DateTimeOffset StartedOn { get; init; }
}
