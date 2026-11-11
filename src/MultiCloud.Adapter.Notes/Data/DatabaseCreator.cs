namespace MultiCloud.Adapter.Notes.Data;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MultiCloud.Adapter.Notes.Contracts;
using MultiCloud.Adapter.Notes.Data.Seed;
using MultiCloud.Adapter.Notes.Entities;

/// <summary>
/// Creates the Notes table from the entity model, and writes the seed into it, when the application asks.
/// </summary>
/// <remarks>
/// The application calls it once, as it starts and before it runs, so the seed is in place before the first request
/// arrives. It is an ordinary service, not a hosted one: the host is the application's, and so is the decision of when
/// the database is made.
/// </remarks>
internal sealed class DatabaseCreator : INotesDatabase
{
    private readonly IDbContextFactory<NotesDatabaseContext> _contexts;
    private readonly TimeProvider _clock;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseCreator"/> class.
    /// </summary>
    /// <param name="contexts">Makes the context the table is created through.</param>
    /// <param name="clock">Says when the seed notes are created.</param>
    public DatabaseCreator(IDbContextFactory<NotesDatabaseContext> contexts, TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(contexts);
        ArgumentNullException.ThrowIfNull(clock);
        this._contexts = contexts;
        this._clock = clock;
    }

    /// <inheritdoc/>
    public async Task CreateAsync(CancellationToken cancellationToken = default)
    {
        NotesDatabaseContext context = await this._contexts.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        using (context)
        {
            DatabaseFacade database = context.Database;
            bool created = await database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
            if (!created)
            {
                return;
            }

            DateTimeOffset now = this._clock.GetUtcNow();
            DbSet<Note> notes = context.Set<Note>();
            notes.AddRange(NoteSeed.Create(now.UtcDateTime));
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
