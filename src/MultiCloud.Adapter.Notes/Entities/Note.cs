namespace MultiCloud.Adapter.Notes.Entities;

using System;
using MultiCloud.Adapter.Notes.Contracts;

/// <summary>
/// A note as the Notes table holds it. Every date is in UTC.
/// </summary>
internal sealed record Note
{
    /// <summary>
    /// Gets the identifier, which the database assigns when the note is first saved.
    /// </summary>
    public NoteId Id { get; init; }

    /// <summary>
    /// Gets what the note says.
    /// </summary>
    required public NoteText Text { get; init; }

    /// <summary>
    /// Gets a value indicating whether the note is done.
    /// </summary>
    required public bool Done { get; init; }

    /// <summary>
    /// Gets when the note was created.
    /// </summary>
    required public DateTime CreatedOn { get; init; }

    /// <summary>
    /// Gets when the note was last changed, which is when it was created until its first change.
    /// </summary>
    required public DateTime ChangedOn { get; init; }
}
