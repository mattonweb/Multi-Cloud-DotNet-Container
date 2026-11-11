namespace MultiCloud.Adapter.Notes.Contracts;

using System;

/// <summary>
/// One note, with every value it holds.
/// </summary>
public sealed record NoteSingleResponse
{
    /// <summary>
    /// Gets the note's identifier.
    /// </summary>
    required public NoteId Id { get; init; }

    /// <summary>
    /// Gets what the note says.
    /// </summary>
    required public NoteText Text { get; init; }

    /// <summary>
    /// Gets a value indicating whether the note is done.
    /// </summary>
    required public bool Done { get; init; }

    /// <summary>
    /// Gets when the note was created, in UTC.
    /// </summary>
    required public DateTime CreatedOn { get; init; }

    /// <summary>
    /// Gets when the note was last changed, in UTC; when it was created until its first change.
    /// </summary>
    required public DateTime ChangedOn { get; init; }
}
