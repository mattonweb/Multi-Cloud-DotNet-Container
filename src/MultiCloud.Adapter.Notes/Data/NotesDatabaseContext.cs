namespace MultiCloud.Adapter.Notes.Data;

using System;
using Microsoft.EntityFrameworkCore;
using MultiCloud.Adapter.Notes.Contracts;
using MultiCloud.Adapter.Notes.Data.Conversions;
using MultiCloud.Adapter.Notes.Mappers;

/// <summary>
/// The Notes database's Entity Framework context. One instance serves one request and is then discarded, so nothing it
/// has read outlives the request that read it.
/// </summary>
/// <remarks>
/// The table is reached with <c>Set&lt;Note&gt;()</c> rather than a property, so the context has no public state;
/// <see cref="NoteMapper"/> names the table instead.
/// </remarks>
internal sealed class NotesDatabaseContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotesDatabaseContext"/> class.
    /// </summary>
    /// <param name="options">The options that point the context at its database.</param>
    public NotesDatabaseContext(DbContextOptions<NotesDatabaseContext> options)
        : base(options)
    {
        ArgumentNullException.ThrowIfNull(options);
    }

    /// <summary>
    /// Says, once for every entity, how each wrapped value and every date is stored.
    /// </summary>
    /// <param name="configurationBuilder">The builder for the model's conventions.</param>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);
        configurationBuilder.Properties<NoteId>().HaveConversion<NoteIdConverter>();
        configurationBuilder.Properties<NoteText>().HaveConversion<NoteTextConverter>();
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }

    /// <summary>
    /// Maps each entity to its table.
    /// </summary>
    /// <param name="modelBuilder">The builder for the model.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfiguration(new NoteMapper());
    }
}
