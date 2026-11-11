namespace MultiCloud.Adapter.Notes.Data.Seed;

using System;
using MultiCloud.Adapter.Notes.Contracts;
using MultiCloud.Adapter.Notes.Entities;

/// <summary>
/// The three invented notes the database starts with, so every verb has something to read, replace, change and delete
/// from the first request.
/// </summary>
internal static class NoteSeed
{
    /// <summary>
    /// Creates the seed notes, each created and last changed at the moment given. The database assigns their
    /// identifiers, 1 to 3, in this order.
    /// </summary>
    /// <param name="now">When the notes are created, in UTC.</param>
    /// <returns>The seed notes, not yet saved.</returns>
    internal static Note[] Create(DateTime now)
    {
        return
        [
            CreateNote("Check that the platform's health probe reaches /healthz/ping.", done: false, now),
            CreateNote("Find this run's heartbeat in the log system by its session identifier.", done: false, now),
            CreateNote("Send every HTTP verb to /api/notes through the platform's front door.", done: true, now),
        ];
    }

    /// <summary>
    /// Creates one seed note.
    /// </summary>
    /// <param name="text">What the note says.</param>
    /// <param name="done">Whether the note starts out done.</param>
    /// <param name="now">When the note is created, in UTC.</param>
    /// <returns>The note, not yet saved.</returns>
    private static Note CreateNote(string text, bool done, DateTime now)
    {
        return new Note
        {
            Text = new NoteText(text),
            Done = done,
            CreatedOn = now,
            ChangedOn = now,
        };
    }
}
