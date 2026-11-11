namespace MultiCloud.Test.Unit.Adapter.Platform.Services;

using System;
using System.Collections.Generic;
using MultiCloud.Adapter.Platform.DTOs;
using MultiCloud.Adapter.Platform.Services;

/// <summary>
/// A process environment that holds only the variables a test gives it, on a machine with a known name.
/// </summary>
internal sealed class StubProcessEnvironment : IProcessEnvironment
{
    /// <summary>
    /// The machine name every stub reports.
    /// </summary>
    internal const string MachineName = "developer-laptop";

    private readonly Dictionary<string, string> _variables;

    /// <summary>
    /// Initializes a new instance of the <see cref="StubProcessEnvironment"/> class.
    /// </summary>
    /// <param name="variables">The variables that are set, by name.</param>
    public StubProcessEnvironment(Dictionary<string, string> variables)
    {
        ArgumentNullException.ThrowIfNull(variables);
        this._variables = variables;
    }

    /// <inheritdoc/>
    public string? Read(EnvironmentVariableName name)
    {
        return this._variables.GetValueOrDefault(name.Value);
    }

    /// <inheritdoc/>
    public string ReadMachineName()
    {
        return MachineName;
    }
}
