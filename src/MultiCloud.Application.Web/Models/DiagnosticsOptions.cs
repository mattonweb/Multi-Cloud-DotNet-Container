namespace MultiCloud.Application.Web.Models;

/// <summary>
/// The settings under <c>Diagnostics</c>, which govern the routes that fail on purpose.
/// </summary>
public sealed record DiagnosticsOptions
{
    /// <summary>
    /// Gets a value indicating whether the two error routes throw. When <see langword="false"/>, both answer 404 and
    /// throw nothing.
    /// </summary>
    required public bool ErrorRoutesEnabled { get; init; }
}
