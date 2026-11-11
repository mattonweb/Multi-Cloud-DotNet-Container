namespace MultiCloud.Application.Web.Mapping;

using System;
using MultiCloud.Adapter.Notes.Contracts;
using MultiCloud.Application.Web.Models;

/// <summary>
/// Shapes a note from the Notes adapter's contract into the notes API's wire format.
/// </summary>
internal static class NoteResourceMapper
{
    /// <summary>
    /// Shapes one note.
    /// </summary>
    /// <param name="note">The note as the adapter answers it.</param>
    /// <returns>The note as the API sends it.</returns>
    internal static NoteResource ToResource(NoteSingleResponse note)
    {
        ArgumentNullException.ThrowIfNull(note);
        return new NoteResource
        {
            Id = note.Id,
            Text = note.Text,
            Done = note.Done,
            CreatedOn = note.CreatedOn,
            ChangedOn = note.ChangedOn,
        };
    }
}
