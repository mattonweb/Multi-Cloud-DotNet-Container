namespace MultiCloud.Adapter.Notes.Contracts;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// The Notes database the adapter owns, as the application that hosts it sees it: something to create before the
/// first request.
/// </summary>
/// <remarks>
/// The host is the application's, so the application decides when the database is created; the adapter decides only
/// how. Registering the adapter starts nothing.
/// </remarks>
public interface INotesDatabase
{
    /// <summary>
    /// Creates the Notes table and writes the three seed notes into it. A database that already has the table is left
    /// as it is, so calling this twice does no harm.
    /// </summary>
    /// <param name="cancellationToken">Signals that the application is stopping before it has started.</param>
    /// <returns>A task that completes when the seed is saved.</returns>
    Task CreateAsync(CancellationToken cancellationToken = default);
}
