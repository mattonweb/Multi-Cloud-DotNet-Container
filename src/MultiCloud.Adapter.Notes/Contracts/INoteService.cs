namespace MultiCloud.Adapter.Notes.Contracts;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Creates, reads, replaces, changes and deletes notes. Every answer a caller expects, a note that is not there
/// among them, is a return value, never an exception.
/// </summary>
public interface INoteService
{
    /// <summary>
    /// Lists every note, oldest first.
    /// </summary>
    /// <param name="cancellationToken">Signals that the caller no longer wants the answer.</param>
    /// <returns>The notes; empty when there are none.</returns>
    Task<NoteSingleResponse[]> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds one note.
    /// </summary>
    /// <param name="id">The note's identifier.</param>
    /// <param name="cancellationToken">Signals that the caller no longer wants the answer.</param>
    /// <returns>The note, or <see langword="null"/> when no note has the identifier.</returns>
    Task<NoteSingleResponse?> FindAsync(NoteId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a note. The database assigns its identifier.
    /// </summary>
    /// <param name="request">The new note's values.</param>
    /// <param name="cancellationToken">Signals that the caller no longer wants the answer.</param>
    /// <returns>The new note.</returns>
    Task<NoteSingleResponse> CreateAsync(NoteCreateRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces a note's text and done flag, and marks it changed now.
    /// </summary>
    /// <param name="request">The note's identifier and its new values.</param>
    /// <param name="cancellationToken">Signals that the caller no longer wants the answer.</param>
    /// <returns>The note as saved, or <see langword="null"/> when no note has the identifier.</returns>
    Task<NoteSingleResponse?> ReplaceAsync(NoteReplaceRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes whichever of a note's text and done flag the request gives, and marks it changed now.
    /// </summary>
    /// <param name="request">The note's identifier and the values to change.</param>
    /// <param name="cancellationToken">Signals that the caller no longer wants the answer.</param>
    /// <returns>The note as saved, or <see langword="null"/> when no note has the identifier.</returns>
    Task<NoteSingleResponse?> ChangeAsync(NoteChangeRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a note.
    /// </summary>
    /// <param name="id">The note's identifier.</param>
    /// <param name="cancellationToken">Signals that the caller no longer wants the answer.</param>
    /// <returns>Whether the note was deleted or was not there.</returns>
    Task<DeleteOutcome> DeleteAsync(NoteId id, CancellationToken cancellationToken = default);
}
