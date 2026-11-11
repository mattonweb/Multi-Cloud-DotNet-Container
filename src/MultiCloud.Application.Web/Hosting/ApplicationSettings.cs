namespace MultiCloud.Application.Web.Hosting;

using System;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using MultiCloud.Adapter.Notes.Configuration;
using MultiCloud.Application.Web.DTOs;
using MultiCloud.Application.Web.Models;
using MultiCloud.Orchestrator.Heartbeat.Configuration;

/// <summary>
/// Reads the settings in docs/SDD.md section 9 from configuration, and stops the application at startup, with a
/// message that names the setting, when one is missing or out of range.
/// </summary>
/// <remarks>
/// Every setting has its default in appsettings.json and is overridden by an environment variable, with <c>__</c>
/// standing for <c>:</c>. The defaults live there and nowhere else, so a setting with no value is an error here rather
/// than a second, silent default.
/// </remarks>
internal sealed class ApplicationSettings
{
    private const string HeartbeatWaitKey = "Heartbeat:WaitInSeconds";
    private const string ErrorRoutesKey = "Diagnostics:ErrorRoutesEnabled";
    private const string ShutdownTimeoutKey = "Host:ShutdownTimeoutInSeconds";
    private const string NotesDatabaseKey = "Notes:DatabaseName";
    private const string ForwardLimitKey = "ForwardedHeaders:ForwardLimit";
    private const string PortKey = "PORT";

    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationSettings"/> class.
    /// </summary>
    /// <param name="configuration">The application's configuration.</param>
    public ApplicationSettings(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        this._configuration = configuration;
    }

    /// <summary>
    /// Reads <c>Heartbeat:WaitInSeconds</c>, which must be 1 or more.
    /// </summary>
    /// <returns>The wait between heartbeats.</returns>
    public HeartbeatWait ReadHeartbeatWait()
    {
        int seconds = this.ReadWholeNumber(HeartbeatWaitKey);
        if (seconds < 1)
        {
            throw new InvalidOperationException(
                $"The setting {HeartbeatWaitKey} is {seconds}; it must be 1 or more.");
        }

        return new HeartbeatWait(seconds);
    }

    /// <summary>
    /// Reads <c>Diagnostics:ErrorRoutesEnabled</c>, which must be true or false.
    /// </summary>
    /// <returns>The diagnostics options.</returns>
    public DiagnosticsOptions ReadDiagnostics()
    {
        string text = this.ReadText(ErrorRoutesKey);
        if (!bool.TryParse(text, out bool enabled))
        {
            throw new InvalidOperationException(
                $"The setting {ErrorRoutesKey} is '{text}'; it must be true or false.");
        }

        return new DiagnosticsOptions { ErrorRoutesEnabled = enabled };
    }

    /// <summary>
    /// Reads <c>Host:ShutdownTimeoutInSeconds</c>, which must be 0 or more.
    /// </summary>
    /// <returns>How long the host waits for work to finish on shutdown.</returns>
    public TimeSpan ReadShutdownTimeout()
    {
        int seconds = this.ReadWholeNumber(ShutdownTimeoutKey);
        if (seconds < 0)
        {
            throw new InvalidOperationException(
                $"The setting {ShutdownTimeoutKey} is {seconds}; it must be 0 or more.");
        }

        return TimeSpan.FromSeconds(seconds);
    }

    /// <summary>
    /// Reads <c>Notes:DatabaseName</c>.
    /// </summary>
    /// <returns>The name of the in-memory Notes database.</returns>
    public DatabaseName ReadNotesDatabaseName()
    {
        string name = this.ReadText(NotesDatabaseKey);
        return new DatabaseName(name);
    }

    /// <summary>
    /// Reads <c>ForwardedHeaders:ForwardLimit</c>, which must be 1 or more: how many proxies the platform puts in front
    /// of the application, and so how many entries of <c>X-Forwarded-For</c> to trust.
    /// </summary>
    /// <returns>The number of entries to trust.</returns>
    public ForwardLimit ReadForwardLimit()
    {
        int entries = this.ReadWholeNumber(ForwardLimitKey);
        if (entries < 1)
        {
            throw new InvalidOperationException(
                $"The setting {ForwardLimitKey} is {entries}; it must be 1 or more.");
        }

        return new ForwardLimit(entries);
    }

    /// <summary>
    /// Reads <c>PORT</c>, which a platform sets when it decides the port, and which has no default.
    /// </summary>
    /// <returns>The port, or <see langword="null"/> when <c>PORT</c> is not set.</returns>
    public ListenPort? ReadPort()
    {
        string? text = this._configuration[PortKey];
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        bool parsed = int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int port);
        if (!parsed || port < 1 || port > 65535)
        {
            throw new InvalidOperationException($"The setting {PortKey} is '{text}'; it must be a port from 1 to 65535.");
        }

        return new ListenPort(port);
    }

    /// <summary>
    /// Reads a setting that must have a value.
    /// </summary>
    /// <param name="key">The setting's name, with <c>:</c> between its sections.</param>
    /// <returns>The setting's value.</returns>
    private string ReadText(string key)
    {
        string? text = this._configuration[key];
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException(
                $"The setting {key} has no value; appsettings.json gives every setting its default.");
        }

        return text;
    }

    /// <summary>
    /// Reads a setting that must be a whole number.
    /// </summary>
    /// <param name="key">The setting's name, with <c>:</c> between its sections.</param>
    /// <returns>The setting's value.</returns>
    private int ReadWholeNumber(string key)
    {
        string text = this.ReadText(key);
        if (!int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int value))
        {
            throw new InvalidOperationException($"The setting {key} is '{text}'; it must be a whole number.");
        }

        return value;
    }
}
