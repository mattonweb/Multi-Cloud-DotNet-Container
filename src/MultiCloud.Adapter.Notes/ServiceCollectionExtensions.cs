namespace MultiCloud.Adapter.Notes;

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MultiCloud.Adapter.Notes.Configuration;
using MultiCloud.Adapter.Notes.Contracts;
using MultiCloud.Adapter.Notes.Data;
using MultiCloud.Adapter.Notes.Services;

/// <summary>
/// Registers the Notes adapter: the one public way into it, besides its options and its contracts.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the in-memory database, the context that reads and writes it, and the services behind the contracts,
    /// among them <see cref="INotesDatabase"/>, which creates the table and writes the seed when the application calls
    /// it. Registering starts nothing: the host is the application's.
    /// </summary>
    /// <param name="services">The host's service collection.</param>
    /// <param name="options">What the host supplies: the database's name.</param>
    /// <returns>The same service collection, so registrations can follow one another.</returns>
    public static IServiceCollection AddNotesAdapter(this IServiceCollection services, NotesOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        // The system clock, unless the host or a test has registered another one first.
        services.TryAddSingleton(TimeProvider.System);

        // A singleton made by a factory, so the container disposes it, and so the database, when the application stops.
        services.AddSingleton(_ => new InMemoryDatabase(options.DatabaseName));

        // The factory for the creator, which outlives any request; the factory also registers the context itself, one
        // per request, for the note service.
        services.AddDbContextFactory<NotesDatabaseContext>(ConfigureContext);
        services.AddSingleton<INotesDatabase, DatabaseCreator>();
        services.AddScoped<INoteService, NoteService>();
        return services;
    }

    /// <summary>
    /// Points a new context at the in-memory database.
    /// </summary>
    /// <param name="serviceProvider">The provider the context is being created from.</param>
    /// <param name="options">The options of the context being configured.</param>
    private static void ConfigureContext(IServiceProvider serviceProvider, DbContextOptionsBuilder options)
    {
        InMemoryDatabase database = serviceProvider.GetRequiredService<InMemoryDatabase>();
        database.Configure(options);
    }
}
