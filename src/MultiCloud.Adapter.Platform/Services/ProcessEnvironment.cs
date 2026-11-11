namespace MultiCloud.Adapter.Platform.Services;

using System;
using MultiCloud.Adapter.Platform.DTOs;

/// <summary>
/// Reads the real process environment.
/// </summary>
internal sealed class ProcessEnvironment : IProcessEnvironment
{
    /// <inheritdoc/>
    public string? Read(EnvironmentVariableName name)
    {
        return Environment.GetEnvironmentVariable(name.Value);
    }

    /// <inheritdoc/>
    public string ReadMachineName()
    {
        return Environment.MachineName;
    }
}
