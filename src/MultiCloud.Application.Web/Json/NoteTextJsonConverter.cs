namespace MultiCloud.Application.Web.Json;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using MultiCloud.Adapter.Notes.Contracts;

/// <summary>
/// Writes a <see cref="NoteText"/> to JSON as a string, and reads it back, refusing text a note cannot hold.
/// </summary>
/// <remarks>
/// A refusal is a <see cref="JsonException"/>, which the API controller turns into a 400 with problem details naming
/// the field. The rule itself, 1 to 200 characters and not only white space, is the note text's own.
/// </remarks>
internal sealed class NoteTextJsonConverter : JsonConverter<NoteText>
{
    /// <inheritdoc/>
    public override NoteText Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? text = reader.GetString();
        if (!NoteText.TryParse(text, provider: null, out NoteText noteText))
        {
            throw new JsonException(
                $"A note's text must be 1 to {NoteText.MaximumLength} characters, and not only white space.");
        }

        return noteText;
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, NoteText value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStringValue(value.Value);
    }
}
