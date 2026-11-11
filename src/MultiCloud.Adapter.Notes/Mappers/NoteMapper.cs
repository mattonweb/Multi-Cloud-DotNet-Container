namespace MultiCloud.Adapter.Notes.Mappers;

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MultiCloud.Adapter.Notes.Contracts;
using MultiCloud.Adapter.Notes.Entities;

/// <summary>
/// Maps <see cref="Note"/> to the Notes table.
/// </summary>
internal sealed class NoteMapper : IEntityTypeConfiguration<Note>
{
    /// <summary>
    /// Names the table, makes the identifier the key, and bounds the text's length.
    /// </summary>
    /// <param name="builder">The builder for the note entity.</param>
    public void Configure(EntityTypeBuilder<Note> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Notes");
        builder.HasKey(static note => note.Id);

        // AUTOINCREMENT, which Entity Framework leaves off a key it converts: a deleted note's identifier is never
        // given out again, so a later GET of a deleted note answers 404 rather than another note.
        builder.Property(static note => note.Id).UseAutoincrement();
        builder.Property(static note => note.Text).HasMaxLength(NoteText.MaximumLength);
    }
}
