namespace MultiCloud.Adapter.Notes.Data.Conversions;

using System;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

/// <summary>
/// Marks every date read from the database as UTC. SQLite stores a date as text with no time zone, so without this a
/// date read back would be of unspecified kind.
/// </summary>
internal sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UtcDateTimeConverter"/> class.
    /// </summary>
    public UtcDateTimeConverter()
        : base(static written => written, static stored => DateTime.SpecifyKind(stored, DateTimeKind.Utc))
    {
    }
}
