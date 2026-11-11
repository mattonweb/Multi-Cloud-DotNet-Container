namespace MultiCloud.Application.Web.Models;

using System.Text.Json.Serialization;
using MultiCloud.Adapter.Notes.Contracts;

/// <summary>
/// The body of PATCH <c>/api/notes/{id}</c>: whichever values it gives are changed, and the rest kept.
/// </summary>
public sealed record NoteChangeBody
{
    /// <summary>
    /// Gets the note's new text, or <see langword="null"/> to keep the current text.
    /// </summary>
    [JsonPropertyName("text")]
    public NoteText? Text { get; init; }

    /// <summary>
    /// Gets a value indicating whether the note is now done, or <see langword="null"/> to keep the current value.
    /// </summary>
    [JsonPropertyName("done")]
    public bool? Done { get; init; }
}
