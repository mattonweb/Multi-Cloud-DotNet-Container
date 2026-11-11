namespace MultiCloud.Adapter.Platform.Services;

using MultiCloud.Adapter.Platform.DTOs;

/// <summary>
/// Reads this process's environment: its environment variables and the machine's name. Tests supply their own.
/// </summary>
internal interface IProcessEnvironment
{
    /// <summary>
    /// Reads one environment variable.
    /// </summary>
    /// <param name="name">The variable's name.</param>
    /// <returns>The variable's value, or <see langword="null"/> when it is not set.</returns>
    string? Read(EnvironmentVariableName name);

    /// <summary>
    /// Reads the machine's name, which in a container is the container's host name.
    /// </summary>
    /// <returns>The machine's name.</returns>
    string ReadMachineName();
}
