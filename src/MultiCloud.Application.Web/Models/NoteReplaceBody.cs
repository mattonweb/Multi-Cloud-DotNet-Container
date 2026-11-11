namespace MultiCloud.Application.Web.Models;

using System.Text.Json.Serialization;
using MultiCloud.Adapter.Notes.Contracts;

/// <summary>
/// The body of PUT <c>/api/notes/{id}</c>: both values are required, because PUT replaces the note.
/// </summary>
public sealed record NoteReplaceBody
{
    /// <summary>
    /// Gets the note's new text.
    /// </summary>
    [JsonPropertyName("text")]
    required public NoteText Text { get; init; }

    /// <summary>
    /// Gets a value indicating whether the note is now done.
    /// </summary>
    [JsonPropertyName("done")]
    required public bool Done { get; init; }
}
