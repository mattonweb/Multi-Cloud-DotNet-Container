namespace MultiCloud.Application.Web;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MultiCloud.Adapter.Notes;
using MultiCloud.Adapter.Notes.Configuration;
using MultiCloud.Adapter.Notes.Contracts;
using MultiCloud.Adapter.Platform;
using MultiCloud.Adapter.Platform.Contracts;
using MultiCloud.Application.Web.DTOs;
using MultiCloud.Application.Web.Errors;
using MultiCloud.Application.Web.Hosting;
using MultiCloud.Application.Web.Json;
using MultiCloud.Application.Web.Logging;
using MultiCloud.Application.Web.Models;
using MultiCloud.Orchestrator.Heartbeat;
using MultiCloud.Orchestrator.Heartbeat.Configuration;
using Serilog;
using Serilog.AspNetCore;
using Serilog.Core;
using Serilog.Extensions.Hosting;

/// <summary>
/// The application's entry point and composition root: it starts the session, reads the settings, registers every
/// project, builds the request pipeline, and creates the Notes database before the first request.
/// </summary>
/// <remarks>
/// The code and the image are identical in every environment. Everything that differs between a developer machine, a
/// local container and each cloud is an environment variable or a platform setting; nothing here branches on where the
/// application runs.
/// </remarks>
public static class Program
{
    /// <summary>
    /// Runs the application until it is stopped.
    /// </summary>
    /// <param name="args">The command-line arguments, which override configuration.</param>
    /// <returns>0 after a polite shutdown; 1 when the application could not start or stopped on an exception.</returns>
    public static async Task<int> Main(string[] args)
    {
        // The bootstrap logger exists before the host is built, so a failure while starting is logged too.
        Log.Logger = CreateBootstrapLogger();
        try
        {
            WebApplication application = Build(args);

            // The host is the application's, so it decides when the adapter's database is made: here, once, before
            // the application runs and so before the first request can arrive.
            INotesDatabase notes = application.Services.GetRequiredService<INotesDatabase>();
            await notes.CreateAsync().ConfigureAwait(false);
            await application.RunAsync().ConfigureAwait(false);
            return 0;
        }
#pragma warning disable CA1031 // Boundary: the process's entry point; the exception is logged, then the process exits with 1.
        catch (Exception exception)
        {
            Log.Fatal(exception, "The application stopped on an exception it did not handle");
            return 1;
        }
#pragma warning restore CA1031
        finally
        {
            await Log.CloseAndFlushAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Creates the logger used until the host's logging is configured: the console, one JSON object per line, in the
    /// same shape as every later line.
    /// </summary>
    /// <returns>The bootstrap logger, which the host's configuration replaces.</returns>
    private static ReloadableLogger CreateBootstrapLogger()
    {
        LoggerConfiguration configuration = new LoggerConfiguration();
        configuration.WriteTo.Console(JsonLogFormat.Formatter);
        return configuration.CreateBootstrapLogger();
    }

    /// <summary>
    /// Starts this run's session and builds the application around it.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>The application, built and ready to run.</returns>
    private static WebApplication Build(string[] args)
    {
        ApplicationSession session = StartSession();
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        ApplicationSettings settings = new ApplicationSettings(builder.Configuration);
        ListenOnPlatformPort(builder.WebHost, settings);
        AddHosting(builder.Services, settings, session);
        AddWeb(builder.Services, session);
        AddCapabilities(builder.Services, settings, session);

        WebApplication application = builder.Build();
        UsePipeline(application);
        return application;
    }

    /// <summary>
    /// Starts the session: one run of one instance of the application, identified by a version 7 UUID.
    /// </summary>
    /// <returns>The session.</returns>
    private static ApplicationSession StartSession()
    {
        DateTimeOffset startedOn = TimeProvider.System.GetUtcNow();
        return new ApplicationSession
        {
            Id = new SessionId(Guid.CreateVersion7(startedOn)),
            StartedOn = startedOn,
        };
    }

    /// <summary>
    /// Listens on the port a platform names in <c>PORT</c>, on every interface. When <c>PORT</c> is not set, the
    /// framework's default applies: 8080 in the image, the launch profile's port under <c>dotnet run</c>.
    /// </summary>
    /// <param name="webHost">The web host being built.</param>
    /// <param name="settings">The application's settings.</param>
    private static void ListenOnPlatformPort(IWebHostBuilder webHost, ApplicationSettings settings)
    {
        ListenPort? port = settings.ReadPort();
        if (port is not ListenPort platformPort)
        {
            return;
        }

        // The image already says its port through ASPNETCORE_HTTP_PORTS; replacing that same setting binds every
        // interface on the platform's port, without the warning that binding by URL would add.
        webHost.UseSetting(WebHostDefaults.HttpPortsKey, platformPort.ToString());
    }

    /// <summary>
    /// Registers the session and what describes this run, the logging, and the host's shutdown timeout.
    /// </summary>
    /// <param name="services">The application's service collection.</param>
    /// <param name="settings">The application's settings.</param>
    /// <param name="session">This run of the application.</param>
    private static void AddHosting(IServiceCollection services, ApplicationSettings settings, ApplicationSession session)
    {
        services.AddSingleton(session);
        services.AddSingleton(ReadIdentity());
        services.AddSingleton(settings.ReadDiagnostics());
        services.AddSingleton<HostReadiness>();

        // Every log line carries the session, version, environment, platform and host, through one enricher.
        services.AddSingleton<ILogEventEnricher, TelemetryEnricher>();
        services.AddSerilog(ConfigureLogging);

        TimeSpan shutdownTimeout = settings.ReadShutdownTimeout();
        services.Configure<HostOptions>(options => options.ShutdownTimeout = shutdownTimeout);
    }

    /// <summary>
    /// Registers MVC controllers without views, with lower-case URLs, the notes API's JSON converters, problem details
    /// that carry the session, and the one global exception handler.
    /// </summary>
    /// <param name="services">The application's service collection.</param>
    /// <param name="session">This run of the application.</param>
    /// <remarks>
    /// The application is a JSON API and returns no HTML page (docs/SDD.md, decision D7). Without views, MVC registers
    /// no anti-forgery and no data protection, so no key is created at startup.
    /// </remarks>
    private static void AddWeb(IServiceCollection services, ApplicationSession session)
    {
        IMvcBuilder mvc = services.AddControllers();
        mvc.AddJsonOptions(ConfigureJson);
        services.AddRouting(static options => options.LowercaseUrls = true);

        SessionProblemDetails sessionProblemDetails = new SessionProblemDetails(session);
        services.AddProblemDetails(options => options.CustomizeProblemDetails = sessionProblemDetails.Customize);
        services.AddExceptionHandler<ApplicationExceptionHandler>();
    }

    /// <summary>
    /// Registers the platform adapter, the Notes adapter and the heartbeat, each with the options the host builds.
    /// </summary>
    /// <param name="services">The application's service collection.</param>
    /// <param name="settings">The application's settings.</param>
    /// <param name="session">This run of the application.</param>
    private static void AddCapabilities(
        IServiceCollection services,
        ApplicationSettings settings,
        ApplicationSession session)
    {
        services.AddPlatformAdapter();

        // The platform cannot change while the process runs, so it is detected once and shared.
        services.AddSingleton<PlatformDescription>(DetectPlatform);

        NotesOptions notes = new NotesOptions
        {
            DatabaseName = settings.ReadNotesDatabaseName(),
        };
        services.AddNotesAdapter(notes);

        HeartbeatOptions heartbeat = new HeartbeatOptions
        {
            Wait = settings.ReadHeartbeatWait(),
            SessionStartedOn = session.StartedOn,
        };
        services.AddHeartbeatOrchestrator(heartbeat);
    }

    /// <summary>
    /// Builds the request pipeline. The order is the point: the response headers go on everything, the request log
    /// sees the status the exception handler chose, the exception handler catches whatever the controllers throw, and
    /// an error answered with no body gets one.
    /// </summary>
    /// <param name="application">The application being built.</param>
    /// <remarks>
    /// Forwarded headers are switched on by <c>ASPNETCORE_FORWARDEDHEADERS_ENABLED</c>, which the image sets, and the
    /// host then puts them first; there is no HTTPS redirection and no HSTS, because the platform owns HTTPS.
    /// </remarks>
    private static void UsePipeline(WebApplication application)
    {
        application.UseMiddleware<ResponseHeadersMiddleware>();
        application.UseWhen(RequestLogFilter.IsLogged, UseRequestLogging);
        application.UseExceptionHandler();

        // The framework answers some errors itself with no body: 404 for a route that does not exist, 405 for a verb a
        // route does not take. With problem details registered, this writes Problem Details into each of them, so a
        // client that expects JSON gets JSON for every error, not an empty body its browser dresses up as a page.
        application.UseStatusCodePages();
        application.MapControllers();
    }

    /// <summary>
    /// Writes one log line per request, after its response, with verb, path, status and duration.
    /// </summary>
    /// <param name="branch">The branch of the pipeline that requests outside the health probes take.</param>
    private static void UseRequestLogging(IApplicationBuilder branch)
    {
        branch.UseSerilogRequestLogging(ConfigureRequestLog);
    }

    /// <summary>
    /// Writes the verb and path in the request log's message as they are, without the quotes Serilog puts around text.
    /// </summary>
    /// <param name="options">The request log's options.</param>
    private static void ConfigureRequestLog(RequestLoggingOptions options)
    {
        options.MessageTemplate =
            "HTTP {RequestMethod:l} {RequestPath:l} responded {StatusCode} in {Elapsed:0.0000} ms";
    }

    /// <summary>
    /// Configures Serilog from the <c>Serilog</c> section of configuration, so a level or a sink changes without a code
    /// change, and adds the enrichers registered as services.
    /// </summary>
    /// <param name="services">The application's built service provider.</param>
    /// <param name="logging">The logger configuration.</param>
    private static void ConfigureLogging(IServiceProvider services, LoggerConfiguration logging)
    {
        IConfiguration configuration = services.GetRequiredService<IConfiguration>();
        logging.ReadFrom.Configuration(configuration);
        logging.ReadFrom.Services(services);
    }

    /// <summary>
    /// Adds the converters that write a note's identifier as a number and its text as a string.
    /// </summary>
    /// <param name="options">MVC's JSON options.</param>
    private static void ConfigureJson(JsonOptions options)
    {
        IList<JsonConverter> converters = options.JsonSerializerOptions.Converters;
        converters.Add(new NoteIdJsonConverter());
        converters.Add(new NoteTextJsonConverter());
    }

    /// <summary>
    /// Asks the platform adapter where the application is running.
    /// </summary>
    /// <param name="services">The application's built service provider.</param>
    /// <returns>The platform's description.</returns>
    private static PlatformDescription DetectPlatform(IServiceProvider services)
    {
        IPlatformDetector detector = services.GetRequiredService<IPlatformDetector>();
        return detector.Detect();
    }

    /// <summary>
    /// Reads the version the project file sets, with no source control suffix (docs/SDD.md, decision D4), and the
    /// machine's name.
    /// </summary>
    /// <returns>What the application is and where it runs.</returns>
    private static ApplicationIdentity ReadIdentity()
    {
        Assembly assembly = typeof(Program).Assembly;
        AssemblyInformationalVersionAttribute? attribute =
            assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        if (attribute is null)
        {
            throw new InvalidOperationException("The application's assembly carries no informational version.");
        }

        return new ApplicationIdentity
        {
            Version = new ApplicationVersion(attribute.InformationalVersion),
            HostName = new HostName(Environment.MachineName),
        };
    }
}
