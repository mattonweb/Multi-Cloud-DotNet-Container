namespace MultiCloud.Adapter.Notes.Services;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MultiCloud.Adapter.Notes.Contracts;
using MultiCloud.Adapter.Notes.Data;
using MultiCloud.Adapter.Notes.Entities;

/// <summary>
/// The rules over notes, behind <see cref="INoteService"/>, read and written through one request's context.
/// </summary>
/// <remarks>
/// A note is read without tracking and saved as a new record with the same identifier, so a record, once read, is
/// never changed in place. Every replace or change marks the note changed now, even when the values sent are the ones
/// it already holds: the changed time says when it was last written.
/// </remarks>
internal sealed class NoteService : INoteService
{
    private readonly NotesDatabaseContext _context;
    private readonly TimeProvider _clock;

    /// <summary>
    /// Initializes a new instance of the <see cref="NoteService"/> class.
    /// </summary>
    /// <param name="context">The request's context on the Notes database.</param>
    /// <param name="clock">Says when a note is created or changed.</param>
    public NoteService(NotesDatabaseContext context, TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(clock);
        this._context = context;
        this._clock = clock;
    }

    /// <inheritdoc/>
    public async Task<NoteSingleResponse[]> ListAsync(CancellationToken cancellationToken = default)
    {
        DbSet<Note> notes = this._context.Set<Note>();
        Note[] read = await notes
            .AsNoTracking()
            .OrderBy(static note => note.Id)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        return [.. read.Select(ToResponse)];
    }

    /// <inheritdoc/>
    public async Task<NoteSingleResponse?> FindAsync(NoteId id, CancellationToken cancellationToken = default)
    {
        Note? note = await this.ReadAsync(id, cancellationToken).ConfigureAwait(false);
        if (note is null)
        {
            return null;
        }

        return ToResponse(note);
    }

    /// <inheritdoc/>
    public async Task<NoteSingleResponse> CreateAsync(
        NoteCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        DateTime now = this.ReadNow();
        Note note = new Note
        {
            Text = request.Text,
            Done = request.Done,
            CreatedOn = now,
            ChangedOn = now,
        };
        DbSet<Note> notes = this._context.Set<Note>();
        notes.Add(note);
        await this._context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Entity Framework has written the identifier the database assigned into the record it saved.
        return ToResponse(note);
    }

    /// <inheritdoc/>
    public async Task<NoteSingleResponse?> ReplaceAsync(
        NoteReplaceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Note? current = await this.ReadAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (current is null)
        {
            return null;
        }

        Note replaced = new Note
        {
            Id = current.Id,
            Text = request.Text,
            Done = request.Done,
            CreatedOn = current.CreatedOn,
            ChangedOn = this.ReadNow(),
        };
        return await this.SaveAsync(replaced, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<NoteSingleResponse?> ChangeAsync(
        NoteChangeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Note? current = await this.ReadAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (current is null)
        {
            return null;
        }

        Note changed = new Note
        {
            Id = current.Id,
            Text = request.Text ?? current.Text,
            Done = request.Done ?? current.Done,
            CreatedOn = current.CreatedOn,
            ChangedOn = this.ReadNow(),
        };
        return await this.SaveAsync(changed, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<DeleteOutcome> DeleteAsync(NoteId id, CancellationToken cancellationToken = default)
    {
        DbSet<Note> notes = this._context.Set<Note>();
        int deleted = await notes
            .Where(note => note.Id == id)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        return deleted == 0 ? DeleteOutcome.NotFound : DeleteOutcome.Deleted;
    }

    /// <summary>
    /// Shapes a note for the contract.
    /// </summary>
    /// <param name="note">The note as the table holds it.</param>
    /// <returns>The note as callers see it.</returns>
    private static NoteSingleResponse ToResponse(Note note)
    {
        return new NoteSingleResponse
        {
            Id = note.Id,
            Text = note.Text,
            Done = note.Done,
            CreatedOn = note.CreatedOn,
            ChangedOn = note.ChangedOn,
        };
    }

    /// <summary>
    /// Reads one note without tracking it.
    /// </summary>
    /// <param name="id">The note's identifier.</param>
    /// <param name="cancellationToken">Signals that the caller no longer wants the answer.</param>
    /// <returns>The note, or <see langword="null"/> when no note has the identifier.</returns>
    private Task<Note?> ReadAsync(NoteId id, CancellationToken cancellationToken)
    {
        DbSet<Note> notes = this._context.Set<Note>();
        return notes
            .AsNoTracking()
            .FirstOrDefaultAsync(note => note.Id == id, cancellationToken);
    }

    /// <summary>
    /// Saves a note over the row with the same identifier.
    /// </summary>
    /// <param name="note">The note's new values, with its existing identifier.</param>
    /// <param name="cancellationToken">Signals that the caller no longer wants the answer.</param>
    /// <returns>The note as saved.</returns>
    private async Task<NoteSingleResponse> SaveAsync(Note note, CancellationToken cancellationToken)
    {
        this._context.Update(note);
        await this._context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ToResponse(note);
    }

    /// <summary>
    /// Reads the clock, in UTC.
    /// </summary>
    /// <returns>Now, in UTC.</returns>
    private DateTime ReadNow()
    {
        DateTimeOffset now = this._clock.GetUtcNow();
        return now.UtcDateTime;
    }
}
