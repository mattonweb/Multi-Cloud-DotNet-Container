namespace MultiCloud.Application.Web.Models;

using System;
using System.Text.Json.Serialization;
using MultiCloud.Adapter.Notes.Contracts;

/// <summary>
/// A note as the notes API sends it. The JSON names are set here, so the wire format holds still if the adapter's
/// contract changes.
/// </summary>
public sealed record NoteResource
{
    /// <summary>
    /// Gets the note's identifier.
    /// </summary>
    [JsonPropertyName("id")]
    required public NoteId Id { get; init; }

    /// <summary>
    /// Gets what the note says.
    /// </summary>
    [JsonPropertyName("text")]
    required public NoteText Text { get; init; }

    /// <summary>
    /// Gets a value indicating whether the note is done.
    /// </summary>
    [JsonPropertyName("done")]
    required public bool Done { get; init; }

    /// <summary>
    /// Gets when the note was created, in UTC.
    /// </summary>
    [JsonPropertyName("createdOn")]
    required public DateTime CreatedOn { get; init; }

    /// <summary>
    /// Gets when the note was last changed, in UTC.
    /// </summary>
    [JsonPropertyName("changedOn")]
    required public DateTime ChangedOn { get; init; }
}
