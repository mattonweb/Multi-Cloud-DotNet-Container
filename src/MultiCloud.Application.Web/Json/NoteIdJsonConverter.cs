namespace MultiCloud.Application.Web.Json;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using MultiCloud.Adapter.Notes.Contracts;

/// <summary>
/// Writes a <see cref="NoteId"/> to JSON as a bare number, and reads it back. The wire format is the web
/// application's, so the converter lives here rather than on the adapter's contract.
/// </summary>
internal sealed class NoteIdJsonConverter : JsonConverter<NoteId>
{
    /// <inheritdoc/>
    public override NoteId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        long value = reader.GetInt64();
        return new NoteId(value);
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, NoteId value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteNumberValue(value.Value);
    }
}
