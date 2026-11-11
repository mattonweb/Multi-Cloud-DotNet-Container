# Multi-Cloud DotNet Container

One .NET MVC application in one container image, deployed unchanged to Google Cloud Run, Google Kubernetes Engine
and AWS ECS, with a step-by-step install guide for each.

It is a placeholder application. It does nothing useful on its own; it exists to check that a platform is wired
correctly before a real application goes there:

- **Health routes** for the platform's probes and for an outside monitor.
- **A heartbeat log message** on a timer, stamped with a session identifier, to confirm that log lines reach a log
  system and to tell one run of the application from the next.
- **Two routes that fail on purpose**, to confirm that errors are handled and logged.
- **A small in-memory API** that accepts every HTTP verb, to confirm a firewall lets them all through.
- **A root route** that says, as JSON, where the application thinks it is running. The application is a JSON API and
  returns no HTML page.

## Status

**Phase 1 of 4. The application and its image are built and run locally.**

| Phase | What | State |
|---|---|---|
| 1 | The application, the `Dockerfile`, and running it locally | Built; [docs/INSTALL-local.md](docs/INSTALL-local.md) |
| 2 | Google Cloud Run | Not started |
| 3 | AWS ECS on Fargate | Not started |
| 4 | Google Kubernetes Engine | Not started |

## What is here

- `docs/SDD.md`: the solution design, what is built, how it is laid out, and how phase 1 is accepted.
- `docs/INSTALL-local.md`: build, run and verify it with `dotnet` and with Docker.
- `Dockerfile` and `.dockerignore`: the one image.
- `src/`: the solution, five projects.

| Project | What it holds |
|---|---|
| `MultiCloud.Application.Web` | The MVC host: settings, logging, the session and the controllers |
| `MultiCloud.Adapter.Platform` | Works out which platform the application runs on, from environment variables |
| `MultiCloud.Adapter.Notes` | The in-memory Notes database, and creating, reading, replacing, changing and deleting notes |
| `MultiCloud.Orchestrator.Heartbeat` | The background heartbeat |
| `MultiCloud.Test.Unit` | The unit tests |

- `CLAUDE.md`: the working agreement for AI coding sessions in this repository.
- `src/Directory.Build.props` and `src/.editorconfig`: build settings and the house C# style, the latter a verbatim
  copy of the one in [Signalbox](https://github.com/mattonweb/Signalbox).

## Running it

```bash
dotnet run --project src/MultiCloud.Application.Web
```

```bash
docker build --platform linux/amd64 --tag multi-cloud-dotnet-container:local .
```

```bash
docker run --rm --publish 8080:8080 multi-cloud-dotnet-container:local
```

[docs/INSTALL-local.md](docs/INSTALL-local.md) has the full steps and a check for every route and verb.

### Routes

Every URL is lower case. `src/MultiCloud.Application.Web/MultiCloud.Application.Web.http` holds one request for each.

| Verb | Route | Answer |
|---|---|---|
| GET | `/` | JSON: the platform, session, version, environment, the request as the application sees it, and the GET routes |
| GET | `/healthz/ping` | 200 `up`, always; no dependencies |
| GET | `/healthz/ready` | 200 `ready` once started; 503 `not ready` while starting or stopping |
| GET | `/healthz/error/500` | 500 problem details, and an error log line with the exception |
| GET | `/healthz/error/403` | 403 problem details, and a warning log line |
| GET | `/api/notes` | 200, every note as a JSON array |
| GET | `/api/notes/{id}` | 200, or 404 |
| HEAD | `/api/notes/{id}` | 200 or 404, no body |
| OPTIONS | `/api/notes` | 204 with `Allow: GET, POST, OPTIONS` |
| POST | `/api/notes` | 201 with a `Location` header; `text` required, `done` false when left out |
| PUT | `/api/notes/{id}` | 200, or 404; `text` and `done` both required |
| PATCH | `/api/notes/{id}` | 200, or 404; changes whichever of `text` and `done` it is given |
| DELETE | `/api/notes/{id}` | 204, or 404 |

A note's text is 1 to 200 characters and not only white space; anything else is a 400 with problem details. Every
response carries `X-Session-Id` and `X-Platform`, and every problem details body carries `sessionId`: a 403 or a
500 without them did not come from the application.

### Settings

Each has its default in `appsettings.json` and is overridden by an environment variable, `__` standing for `:`.

| Environment variable | Default | Meaning |
|---|---|---|
| `Heartbeat__WaitInSeconds` | `600` | Seconds between `Running` heartbeats; 1 or more |
| `Diagnostics__ErrorRoutesEnabled` | `true` | Whether the two error routes throw, or answer 404 |
| `Host__ShutdownTimeoutInSeconds` | `8` | How long the host waits for work to finish on shutdown |
| `Notes__DatabaseName` | `Notes` | Name of the in-memory database |
| `Serilog__*` | Console, JSON, `Information` | Log levels and sinks |
| `PORT` | not set | The port to listen on, when a platform says which |

## Versions and choices

- **.NET 10** and **C# 14**, built with the .NET SDK 10.0.402 on the development machine.
- **ASP.NET Core MVC**, controllers without views: every answer is JSON, an error as Problem Details (RFC 9457),
  except the two probes' plain text.
- **Serilog**, writing one JSON object per line to the console, with OpenTelemetry's names for the session and
  platform properties on every line.
- **Entity Framework Core over SQLite, in memory.** Nothing is written to disk; a restart empties it.
- **MSTest** for the tests.

Every package and base image is pinned to one exact version:

| Package or image | Version | Used by |
|---|---|---|
| `mcr.microsoft.com/dotnet/sdk` | `10.0.403-noble` | The image's build stage |
| `mcr.microsoft.com/dotnet/aspnet` | `10.0.14-noble` | The image's run stage |
| `Serilog.AspNetCore` | 10.0.0 | `MultiCloud.Application.Web`; brings Serilog 4.3.0, Serilog.Settings.Configuration 10.0.0, Serilog.Sinks.Console 6.1.1 and Serilog.Formatting.Compact 3.0.0 |
| `Microsoft.EntityFrameworkCore.Sqlite` | 10.0.14 | `MultiCloud.Adapter.Notes` |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | 10.0.14 | `MultiCloud.Adapter.Platform` |
| `Microsoft.Extensions.Hosting.Abstractions` | 10.0.14 | `MultiCloud.Orchestrator.Heartbeat` |
| `MSTest` | 4.5.2 | `MultiCloud.Test.Unit` |
| `Microsoft.Extensions.TimeProvider.Testing` | 10.11.0 | `MultiCloud.Test.Unit` |
| `Microsoft.Extensions.Diagnostics.Testing` | 10.11.0 | `MultiCloud.Test.Unit` |
| `StyleCop.Analyzers` | 1.1.118 | Every project, from `src/Directory.Build.props` |
| `SonarAnalyzer.CSharp` | 10.35.0.4138 | Every project, from `src/Directory.Build.props` |
| `Microsoft.CodeAnalysis.NetAnalyzers` | 10.0.402 | Every project, from `src/Directory.Build.props` |

The two images, Entity Framework Core and the two `Microsoft.Extensions` abstractions are all from the .NET 10.0.14
servicing release.

## License

MIT, see `LICENSE`.
