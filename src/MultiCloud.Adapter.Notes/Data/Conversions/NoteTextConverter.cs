namespace MultiCloud.Adapter.Notes.Data.Conversions;

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MultiCloud.Adapter.Notes.Contracts;

/// <summary>
/// Stores a <see cref="NoteText"/> as text, and wraps it again when it is read.
/// </summary>
internal sealed class NoteTextConverter : ValueConverter<NoteText, string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoteTextConverter"/> class.
    /// </summary>
    public NoteTextConverter()
        : base(static wrapped => wrapped.Value, static stored => new NoteText(stored))
    {
    }
}
