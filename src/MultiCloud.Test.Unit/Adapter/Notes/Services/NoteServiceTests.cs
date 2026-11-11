namespace MultiCloud.Test.Unit.Adapter.Notes.Services;

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiCloud.Adapter.Notes;
using MultiCloud.Adapter.Notes.Configuration;
using MultiCloud.Adapter.Notes.Contracts;
using MultiCloud.Adapter.Notes.Services;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="NoteService"/> class, through the adapter's
/// contracts as the web application reaches it.
/// </summary>
/// <remarks>
/// Each test registers the adapter as the host does, over its own in-memory database under a name no other test uses,
/// with the three seed notes in it. Each call runs in its own scope, as each request does.
/// </remarks>
[TestClass]
public sealed class NoteServiceTests
{
    /// <summary>
    /// The moment the seed notes are created.
    /// </summary>
    private static readonly DateTimeOffset SeededOn = new DateTimeOffset(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);

    private FakeTimeProvider? _clock;
    private ServiceProvider? _provider;

    /// <summary>
    /// Registers the adapter over a new database, creates it, and writes the seed.
    /// </summary>
    [TestInitialize]
    public async Task TestInitialize()
    {
        this._clock = new FakeTimeProvider(SeededOn);
        ServiceCollection services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(this._clock);
        NotesOptions options = new NotesOptions
        {
            DatabaseName = new DatabaseName(Guid.NewGuid().ToString("N")),
        };
        services.AddNotesAdapter(options);
        this._provider = services.BuildServiceProvider();
        INotesDatabase database = this._provider.GetRequiredService<INotesDatabase>();
        await database.CreateAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Disposes the provider, and with it the database.
    /// </summary>
    [TestCleanup]
    public void TestCleanup()
    {
        this._provider?.Dispose();
    }

    #region Public Methods

    /// <summary>
    /// Unit test to verify that the database starts with the three seed notes, oldest first, numbered 1 to 3.
    /// </summary>
    [TestMethod]
    public async Task ListAsync_SeededDatabase_ReturnsThreeNotesInOrder()
    {
        // Arrange (Given)

        // Act (When)
        NoteSingleResponse[] notes = await this.CallAsync(static service => service.ListAsync()).ConfigureAwait(false);

        // Assert (Then)
        Assert.HasCount(
            3,
            notes,
            "The database should start with the three seed notes.");
        Assert.AreEqual(
            new NoteId(1),
            notes[0].Id,
            "The oldest note should come first.");
        Assert.AreEqual(
            DateTimeKind.Utc,
            notes[0].CreatedOn.Kind,
            "Dates should be read back as UTC.");
    }

    /// <summary>
    /// Unit test to verify that a seed note can be read by its identifier.
    /// </summary>
    [TestMethod]
    public async Task FindAsync_SeededIdentifier_ReturnsTheNote()
    {
        // Arrange (Given)
        NoteId id = new NoteId(2);

        // Act (When)
        NoteSingleResponse? note = await this.CallAsync(service => service.FindAsync(id)).ConfigureAwait(false);

        // Assert (Then)
        Assert.IsNotNull(
            note,
            "A seed note should be found.");
        Assert.AreEqual(
            id,
            note.Id,
            "The note found should be the one asked for.");
    }

    /// <summary>
    /// Unit test to verify that an identifier no note has is not found, as a return value rather than an exception.
    /// </summary>
    [TestMethod]
    public async Task FindAsync_UnknownIdentifier_ReturnsNull()
    {
        // Arrange (Given)
        NoteId id = new NoteId(999);

        // Act (When)
        NoteSingleResponse? note = await this.CallAsync(service => service.FindAsync(id)).ConfigureAwait(false);

        // Assert (Then)
        Assert.IsNull(
            note,
            "An identifier no note has should not be found.");
    }

    /// <summary>
    /// Unit test to verify that a new note takes the next identifier, and is created and last changed now.
    /// </summary>
    [TestMethod]
    public async Task CreateAsync_ValidRequest_SavesWithNextIdentifierAndNow()
    {
        // Arrange (Given)
        this._clock!.Advance(TimeSpan.FromMinutes(5));
        NoteCreateRequest request = new NoteCreateRequest
        {
            Text = new NoteText("Created by a test."),
            Done = false,
        };

        // Act (When)
        NoteSingleResponse created = await this.CallAsync(service => service.CreateAsync(request)).ConfigureAwait(false);
        NoteSingleResponse? found = await this.CallAsync(service => service.FindAsync(created.Id)).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            new NoteId(4),
            created.Id,
            "The new note should take the identifier after the seed's.");
        Assert.AreEqual(
            this._clock.GetUtcNow().UtcDateTime,
            created.CreatedOn,
            "The new note should be created now.");
        Assert.AreEqual(
            created,
            found,
            "The new note should be read back as it was saved.");
    }

    /// <summary>
    /// Unit test to verify that a replace sets both values, keeps the creation time, and marks the note changed now.
    /// </summary>
    [TestMethod]
    public async Task ReplaceAsync_ExistingNote_ReplacesValuesAndKeepsCreatedOn()
    {
        // Arrange (Given)
        this._clock!.Advance(TimeSpan.FromMinutes(5));
        NoteReplaceRequest request = new NoteReplaceRequest
        {
            Id = new NoteId(1),
            Text = new NoteText("Replaced by a test."),
            Done = true,
        };

        // Act (When)
        NoteSingleResponse? replaced = await this.CallAsync(service => service.ReplaceAsync(request)).ConfigureAwait(false);

        // Assert (Then)
        Assert.IsNotNull(
            replaced,
            "An existing note should be replaced.");
        Assert.AreEqual(
            new NoteText("Replaced by a test."),
            replaced.Text,
            "The text should be replaced.");
        Assert.IsTrue(
            replaced.Done,
            "The done flag should be replaced.");
        Assert.AreEqual(
            SeededOn.UtcDateTime,
            replaced.CreatedOn,
            "The creation time should be kept.");
        Assert.AreEqual(
            this._clock.GetUtcNow().UtcDateTime,
            replaced.ChangedOn,
            "The note should be marked changed now.");
    }

    /// <summary>
    /// Unit test to verify that replacing a note that is not there changes nothing and says so.
    /// </summary>
    [TestMethod]
    public async Task ReplaceAsync_UnknownIdentifier_ReturnsNull()
    {
        // Arrange (Given)
        NoteReplaceRequest request = new NoteReplaceRequest
        {
            Id = new NoteId(999),
            Text = new NoteText("Nobody reads this."),
            Done = false,
        };

        // Act (When)
        NoteSingleResponse? replaced = await this.CallAsync(service => service.ReplaceAsync(request)).ConfigureAwait(false);

        // Assert (Then)
        Assert.IsNull(
            replaced,
            "A note that is not there should not be replaced.");
    }

    /// <summary>
    /// Unit test to verify that a change that gives only the done flag keeps the note's text.
    /// </summary>
    [TestMethod]
    public async Task ChangeAsync_OnlyDoneGiven_KeepsTheText()
    {
        // Arrange (Given)
        NoteSingleResponse? before = await this.CallAsync(static service => service.FindAsync(new NoteId(1))).ConfigureAwait(false);
        NoteChangeRequest request = new NoteChangeRequest
        {
            Id = new NoteId(1),
            Done = true,
        };

        // Act (When)
        NoteSingleResponse? changed = await this.CallAsync(service => service.ChangeAsync(request)).ConfigureAwait(false);

        // Assert (Then)
        Assert.IsNotNull(
            changed,
            "An existing note should be changed.");
        Assert.IsTrue(
            changed.Done,
            "The done flag given should be changed.");
        Assert.AreEqual(
            before!.Text,
            changed.Text,
            "The text not given should be kept.");
    }

    /// <summary>
    /// Unit test to verify that a change that gives only the text keeps the note's done flag.
    /// </summary>
    [TestMethod]
    public async Task ChangeAsync_OnlyTextGiven_KeepsTheDoneFlag()
    {
        // Arrange (Given)
        NoteChangeRequest request = new NoteChangeRequest
        {
            Id = new NoteId(3),
            Text = new NoteText("Changed by a test."),
        };

        // Act (When)
        NoteSingleResponse? changed = await this.CallAsync(service => service.ChangeAsync(request)).ConfigureAwait(false);

        // Assert (Then)
        Assert.IsNotNull(
            changed,
            "An existing note should be changed.");
        Assert.AreEqual(
            new NoteText("Changed by a test."),
            changed.Text,
            "The text given should be changed.");
        Assert.IsTrue(
            changed.Done,
            "The third seed note's done flag, not given, should be kept.");
    }

    /// <summary>
    /// Unit test to verify that changing a note that is not there changes nothing and says so.
    /// </summary>
    [TestMethod]
    public async Task ChangeAsync_UnknownIdentifier_ReturnsNull()
    {
        // Arrange (Given)
        NoteChangeRequest request = new NoteChangeRequest
        {
            Id = new NoteId(999),
            Done = true,
        };

        // Act (When)
        NoteSingleResponse? changed = await this.CallAsync(service => service.ChangeAsync(request)).ConfigureAwait(false);

        // Assert (Then)
        Assert.IsNull(
            changed,
            "A note that is not there should not be changed.");
    }

    /// <summary>
    /// Unit test to verify that a deleted note is gone.
    /// </summary>
    [TestMethod]
    public async Task DeleteAsync_ExistingNote_DeletesIt()
    {
        // Arrange (Given)
        NoteId id = new NoteId(2);

        // Act (When)
        DeleteOutcome outcome = await this.CallAsync(service => service.DeleteAsync(id)).ConfigureAwait(false);
        NoteSingleResponse? found = await this.CallAsync(service => service.FindAsync(id)).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            DeleteOutcome.Deleted,
            outcome,
            "An existing note should be deleted.");
        Assert.IsNull(
            found,
            "A deleted note should no longer be found.");
    }

    /// <summary>
    /// Unit test to verify that deleting a note that is not there says so, as a return value.
    /// </summary>
    [TestMethod]
    public async Task DeleteAsync_UnknownIdentifier_ReturnsNotFound()
    {
        // Arrange (Given)
        NoteId id = new NoteId(999);

        // Act (When)
        DeleteOutcome outcome = await this.CallAsync(service => service.DeleteAsync(id)).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            DeleteOutcome.NotFound,
            outcome,
            "A note that is not there should not be found to delete.");
    }

    /// <summary>
    /// Unit test to verify that a deleted note's identifier is never given out again, so an old link cannot open a
    /// different note.
    /// </summary>
    [TestMethod]
    public async Task CreateAsync_AfterLastNoteDeleted_DoesNotReuseItsIdentifier()
    {
        // Arrange (Given)
        await this.CallAsync(static service => service.DeleteAsync(new NoteId(3))).ConfigureAwait(false);
        NoteCreateRequest request = new NoteCreateRequest
        {
            Text = new NoteText("Created after a delete."),
            Done = false,
        };

        // Act (When)
        NoteSingleResponse created = await this.CallAsync(service => service.CreateAsync(request)).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            new NoteId(4),
            created.Id,
            "The deleted note's identifier, 3, should not be given out again.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Makes one call to the note service in a scope of its own, as one request would.
    /// </summary>
    /// <typeparam name="TResult">What the call answers.</typeparam>
    /// <param name="call">The call.</param>
    /// <returns>The call's answer.</returns>
    private async Task<TResult> CallAsync<TResult>(Func<INoteService, Task<TResult>> call)
    {
        using (IServiceScope scope = this._provider!.CreateScope())
        {
            IServiceProvider services = scope.ServiceProvider;
            INoteService service = services.GetRequiredService<INoteService>();
            return await call(service).ConfigureAwait(false);
        }
    }

    #endregion Private Methods
}
