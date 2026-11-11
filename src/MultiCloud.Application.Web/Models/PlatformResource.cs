namespace MultiCloud.Application.Web.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Where the application thinks it is running, as <c>GET /</c> reports it. A value the platform does not supply is
/// <see langword="null"/>, never guessed.
/// </summary>
public sealed record PlatformResource
{
    /// <summary>
    /// Gets the platform's name for people, as the <c>X-Platform</c> header shows it.
    /// </summary>
    [JsonPropertyName("name")]
    required public string Name { get; init; }

    /// <summary>
    /// Gets OpenTelemetry's well-known value for the cloud provider, or <see langword="null"/>.
    /// </summary>
    [JsonPropertyName("cloudProvider")]
    public string? CloudProvider { get; init; }

    /// <summary>
    /// Gets OpenTelemetry's well-known value for the cloud platform, or <see langword="null"/>.
    /// </summary>
    [JsonPropertyName("cloudPlatform")]
    public string? CloudPlatform { get; init; }

    /// <summary>
    /// Gets the region, or <see langword="null"/>.
    /// </summary>
    [JsonPropertyName("region")]
    public string? Region { get; init; }

    /// <summary>
    /// Gets the name of the deployed service, or <see langword="null"/>.
    /// </summary>
    [JsonPropertyName("service")]
    public string? Service { get; init; }

    /// <summary>
    /// Gets the name of the deployed revision, or <see langword="null"/>.
    /// </summary>
    [JsonPropertyName("revision")]
    public string? Revision { get; init; }

    /// <summary>
    /// Gets the name of this instance, or <see langword="null"/>.
    /// </summary>
    [JsonPropertyName("instance")]
    public string? Instance { get; init; }
}
