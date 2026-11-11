namespace MultiCloud.Adapter.Notes.Contracts;

/// <summary>
/// What became of a request to delete a note. Both are answers the caller expects; neither is an error.
/// </summary>
public enum DeleteOutcome
{
    /// <summary>
    /// The note was deleted.
    /// </summary>
    Deleted,

    /// <summary>
    /// No note has the identifier given, so nothing was deleted.
    /// </summary>
    NotFound,
}
