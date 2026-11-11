namespace MultiCloud.Adapter.Notes.Configuration;

/// <summary>
/// What the host supplies to register the Notes adapter. The adapter reads no configuration of its own.
/// </summary>
public sealed record NotesOptions
{
    /// <summary>
    /// Gets the name of the in-memory database the adapter creates when the application starts.
    /// </summary>
    required public DatabaseName DatabaseName { get; init; }
}
