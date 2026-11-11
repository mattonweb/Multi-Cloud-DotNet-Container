namespace MultiCloud.Adapter.Notes.Contracts;

/// <summary>
/// Every value of a note that a caller may set, to replace the note's current values. Both are required.
/// </summary>
public sealed record NoteReplaceRequest
{
    /// <summary>
    /// Gets the identifier of the note to replace.
    /// </summary>
    required public NoteId Id { get; init; }

    /// <summary>
    /// Gets the note's new text.
    /// </summary>
    required public NoteText Text { get; init; }

    /// <summary>
    /// Gets a value indicating whether the note is now done.
    /// </summary>
    required public bool Done { get; init; }
}
