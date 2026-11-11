namespace MultiCloud.Application.Web.Models;

using System.Text.Json.Serialization;

/// <summary>
/// What <c>GET /</c> answers, as JSON: which application this is, which run, where it thinks it is running, the
/// request as it sees it, and the GET routes to try. The application returns no HTML page.
/// </summary>
/// <remarks>
/// The JSON names are set here, so the wire format holds still if the types behind it change. Each value is the text
/// of the wrapped value it reports.
/// </remarks>
public sealed record HomeResource
{
    /// <summary>
    /// Gets the application's name, the same as <c>service.name</c> on every log line.
    /// </summary>
    [JsonPropertyName("service")]
    required public string Service { get; init; }

    /// <summary>
    /// Gets the application's version.
    /// </summary>
    [JsonPropertyName("version")]
    required public string Version { get; init; }

    /// <summary>
    /// Gets the host environment's name, such as Development or Production.
    /// </summary>
    [JsonPropertyName("environment")]
    required public string Environment { get; init; }

    /// <summary>
    /// Gets this run of the application.
    /// </summary>
    [JsonPropertyName("session")]
    required public SessionResource Session { get; init; }

    /// <summary>
    /// Gets the platform, as the platform adapter describes it.
    /// </summary>
    [JsonPropertyName("platform")]
    required public PlatformResource Platform { get; init; }

    /// <summary>
    /// Gets the request as the application sees it.
    /// </summary>
    [JsonPropertyName("request")]
    required public RequestResource Request { get; init; }

    /// <summary>
    /// Gets the GET routes to try, this one among them.
    /// </summary>
    [JsonPropertyName("routes")]
    required public string[] Routes { get; init; }
}
