namespace MultiCloud.Application.Web.Controllers;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MultiCloud.Adapter.Notes.Contracts;
using MultiCloud.Application.Web.Mapping;
using MultiCloud.Application.Web.Models;

/// <summary>
/// The notes API, which exists so that every HTTP verb has something real to do: GET, HEAD, OPTIONS, POST, PUT, PATCH
/// and DELETE.
/// </summary>
/// <remarks>
/// The notes live in one process. With two instances behind a load balancer, a note created on one is absent on the
/// other, and a restart empties both; each response's <c>X-Session-Id</c> header shows which instance answered.
/// </remarks>
[ApiController]
[Route("api/notes")]
public sealed class NotesController : ControllerBase
{
    /// <summary>
    /// The verbs <c>/api/notes</c> itself answers, for the <c>Allow</c> header.
    /// </summary>
    private const string CollectionVerbs = "GET, POST, OPTIONS";

    private readonly INoteService _notes;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotesController"/> class.
    /// </summary>
    /// <param name="notes">The Notes adapter's service.</param>
    public NotesController(INoteService notes)
    {
        ArgumentNullException.ThrowIfNull(notes);
        this._notes = notes;
    }

    /// <summary>
    /// Lists every note, oldest first.
    /// </summary>
    /// <param name="cancellationToken">Signals that the caller has gone.</param>
    /// <returns>200 with a JSON array.</returns>
    [HttpGet]
    public async Task<ActionResult<NoteResource[]>> List(CancellationToken cancellationToken)
    {
        NoteSingleResponse[] notes = await this._notes.ListAsync(cancellationToken).ConfigureAwait(false);
        return this.Ok(notes.Select(NoteResourceMapper.ToResource).ToArray());
    }

    /// <summary>
    /// Reads one note.
    /// </summary>
    /// <param name="id">The note's identifier.</param>
    /// <param name="cancellationToken">Signals that the caller has gone.</param>
    /// <returns>200 with the note, or 404.</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<NoteResource>> Read(NoteId id, CancellationToken cancellationToken)
    {
        NoteSingleResponse? note = await this._notes.FindAsync(id, cancellationToken).ConfigureAwait(false);
        if (note is null)
        {
            return this.NotFound();
        }

        return this.Ok(NoteResourceMapper.ToResource(note));
    }

    /// <summary>
    /// Says whether a note exists, with no body.
    /// </summary>
    /// <param name="id">The note's identifier.</param>
    /// <param name="cancellationToken">Signals that the caller has gone.</param>
    /// <returns>200 or 404, with no body.</returns>
    [HttpHead("{id}")]
    public async Task<IActionResult> Exists(NoteId id, CancellationToken cancellationToken)
    {
        NoteSingleResponse? note = await this._notes.FindAsync(id, cancellationToken).ConfigureAwait(false);
        if (note is null)
        {
            return this.NotFound();
        }

        return this.Ok();
    }

    /// <summary>
    /// Says which verbs <c>/api/notes</c> answers.
    /// </summary>
    /// <returns>204 with an <c>Allow</c> header.</returns>
    [HttpOptions]
    public NoContentResult Options()
    {
        IHeaderDictionary headers = this.Response.Headers;
        headers.Allow = CollectionVerbs;
        return this.NoContent();
    }

    /// <summary>
    /// Creates a note.
    /// </summary>
    /// <param name="body">The new note's text, and whether it starts out done.</param>
    /// <param name="cancellationToken">Signals that the caller has gone.</param>
    /// <returns>201 with the note and a <c>Location</c> header, or 400 when the body is invalid.</returns>
    [HttpPost]
    public async Task<ActionResult<NoteResource>> Create(NoteCreateBody body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        NoteCreateRequest request = new NoteCreateRequest
        {
            Text = body.Text,
            Done = body.Done ?? false,
        };
        NoteSingleResponse note = await this._notes.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        NoteResource resource = NoteResourceMapper.ToResource(note);
        return this.CreatedAtAction(nameof(this.Read), new { id = resource.Id.Value }, resource);
    }

    /// <summary>
    /// Replaces a note's text and done flag; both are required.
    /// </summary>
    /// <param name="id">The note's identifier.</param>
    /// <param name="body">The note's new values.</param>
    /// <param name="cancellationToken">Signals that the caller has gone.</param>
    /// <returns>200 with the note, 404, or 400 when the body is invalid.</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<NoteResource>> Replace(
        NoteId id,
        NoteReplaceBody body,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        NoteReplaceRequest request = new NoteReplaceRequest
        {
            Id = id,
            Text = body.Text,
            Done = body.Done,
        };
        NoteSingleResponse? note = await this._notes.ReplaceAsync(request, cancellationToken).ConfigureAwait(false);
        if (note is null)
        {
            return this.NotFound();
        }

        return this.Ok(NoteResourceMapper.ToResource(note));
    }

    /// <summary>
    /// Changes whichever of a note's text and done flag the body gives.
    /// </summary>
    /// <param name="id">The note's identifier.</param>
    /// <param name="body">The values to change.</param>
    /// <param name="cancellationToken">Signals that the caller has gone.</param>
    /// <returns>200 with the note, 404, or 400 when the body is invalid.</returns>
    [HttpPatch("{id}")]
    public async Task<ActionResult<NoteResource>> Change(
        NoteId id,
        NoteChangeBody body,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        NoteChangeRequest request = new NoteChangeRequest
        {
            Id = id,
            Text = body.Text,
            Done = body.Done,
        };
        NoteSingleResponse? note = await this._notes.ChangeAsync(request, cancellationToken).ConfigureAwait(false);
        if (note is null)
        {
            return this.NotFound();
        }

        return this.Ok(NoteResourceMapper.ToResource(note));
    }

    /// <summary>
    /// Deletes a note.
    /// </summary>
    /// <param name="id">The note's identifier.</param>
    /// <param name="cancellationToken">Signals that the caller has gone.</param>
    /// <returns>204, or 404.</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(NoteId id, CancellationToken cancellationToken)
    {
        DeleteOutcome outcome = await this._notes.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        if (outcome == DeleteOutcome.NotFound)
        {
            return this.NotFound();
        }

        return this.NoContent();
    }
}
