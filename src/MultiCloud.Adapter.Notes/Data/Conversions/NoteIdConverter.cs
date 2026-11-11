namespace MultiCloud.Adapter.Notes.Data.Conversions;

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MultiCloud.Adapter.Notes.Contracts;

/// <summary>
/// Stores a <see cref="NoteId"/> as an integer, and wraps it again when it is read.
/// </summary>
internal sealed class NoteIdConverter : ValueConverter<NoteId, long>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoteIdConverter"/> class.
    /// </summary>
    public NoteIdConverter()
        : base(static wrapped => wrapped.Value, static stored => new NoteId(stored))
    {
    }
}
