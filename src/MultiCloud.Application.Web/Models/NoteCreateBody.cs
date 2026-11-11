namespace MultiCloud.Application.Web.Models;

using System.Text.Json.Serialization;
using MultiCloud.Adapter.Notes.Contracts;

/// <summary>
/// The body of POST <c>/api/notes</c>: the text is required, and done is false when left out.
/// </summary>
public sealed record NoteCreateBody
{
    /// <summary>
    /// Gets what the note says.
    /// </summary>
    [JsonPropertyName("text")]
    required public NoteText Text { get; init; }

    /// <summary>
    /// Gets a value indicating whether the note starts out done, or <see langword="null"/> when the body leaves it out,
    /// which means not done.
    /// </summary>
    [JsonPropertyName("done")]
    public bool? Done { get; init; }
}
