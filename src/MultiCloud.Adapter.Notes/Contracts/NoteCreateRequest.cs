namespace MultiCloud.Adapter.Notes.Contracts;

/// <summary>
/// A new note: its text, and whether it starts out done.
/// </summary>
public sealed record NoteCreateRequest
{
    /// <summary>
    /// Gets what the note says.
    /// </summary>
    required public NoteText Text { get; init; }

    /// <summary>
    /// Gets a value indicating whether the note starts out done.
    /// </summary>
    required public bool Done { get; init; }
}
