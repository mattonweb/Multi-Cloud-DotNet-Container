namespace MultiCloud.Adapter.Notes.Data;

using System;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MultiCloud.Adapter.Notes.Configuration;

/// <summary>
/// Keeps a named SQLite in-memory database alive for as long as this object lives, and points each
/// <see cref="DbContext"/> at it.
/// </summary>
/// <remarks>
/// SQLite deletes an in-memory database when its last connection closes, so this class holds one connection open
/// from construction until disposal. The adapter registers one instance as a singleton, which makes the database last
/// exactly as long as the process. Every context configured here opens its own connection to the same database: a
/// note one request saves is read by the next request, and two requests in flight never share a connection. Nothing is
/// written to disk.
/// </remarks>
internal sealed class InMemoryDatabase : IDisposable
{
    private readonly SqliteConnection _keepAlive;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryDatabase"/> class, which creates the database, empty.
    /// </summary>
    /// <param name="name">The database's name; another instance with the same name reaches the same database.</param>
    public InMemoryDatabase(DatabaseName name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name.Value);
        SqliteConnectionStringBuilder connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = name.Value,
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared,
        };
        this._keepAlive = new SqliteConnection(connectionString.ToString());
        this._keepAlive.Open();
    }

    /// <summary>
    /// Points a context at this database.
    /// </summary>
    /// <param name="options">The options of the context being configured.</param>
    public void Configure(DbContextOptionsBuilder options)
    {
        ObjectDisposedException.ThrowIf(this._disposed, this);
        ArgumentNullException.ThrowIfNull(options);
        options.UseSqlite(this._keepAlive.ConnectionString);
    }

    /// <summary>
    /// Closes the connection that keeps the database alive. SQLite deletes the database once no other connection to it
    /// is open.
    /// </summary>
    public void Dispose()
    {
        if (this._disposed)
        {
            return;
        }

        this._keepAlive.Dispose();
        this._disposed = true;
    }
}
