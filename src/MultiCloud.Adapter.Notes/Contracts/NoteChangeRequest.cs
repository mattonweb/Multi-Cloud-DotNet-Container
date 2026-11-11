namespace MultiCloud.Adapter.Notes.Contracts;

/// <summary>
/// Some of the values of a note, to change only those. A value left out is kept as it is.
/// </summary>
public sealed record NoteChangeRequest
{
    /// <summary>
    /// Gets the identifier of the note to change.
    /// </summary>
    required public NoteId Id { get; init; }

    /// <summary>
    /// Gets the note's new text, or <see langword="null"/> to keep the current text.
    /// </summary>
    public NoteText? Text { get; init; }

    /// <summary>
    /// Gets a value indicating whether the note is now done, or <see langword="null"/> to keep the current value.
    /// </summary>
    public bool? Done { get; init; }
}
