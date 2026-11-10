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
- **A home page** that says where the application thinks it is running.

## Status

**Phase 1 of 4. The repository is set up and designed; the application is not built yet.**

| Phase | What | State |
|---|---|---|
| 1 | The application, the `Dockerfile`, and running it locally | Designed in `docs/SDD.md`, not built |
| 2 | Google Cloud Run | Not started |
| 3 | AWS ECS on Fargate | Not started |
| 4 | Google Kubernetes Engine | Not started |

## What is here

- `docs/SDD.md` — the solution design: what is built, how it is laid out, and how phase 1 is accepted.
- `CLAUDE.md` — the working agreement for AI coding sessions in this repository.
- `src/Directory.Build.props` — build settings shared by every project.
- `src/.editorconfig` — the house C# style, a verbatim copy of the one in
  [Signalbox](https://github.com/mattonweb/Signalbox).

## Running it

Not yet. The first build adds this section and `docs/INSTALL-local.md`.

## Versions and choices

- **.NET 10** and **C# 14**.
- **ASP.NET Core MVC**, controllers with views.
- **Serilog**, writing one JSON object per line to the console, with OpenTelemetry's names for the session and
  platform properties on every line.
- **Entity Framework Core over SQLite, in memory.** Nothing is written to disk; a restart empties it.
- **MSTest** for the tests.

Exact package and base-image versions are recorded here when the first build chooses them.

## License

MIT, see `LICENSE`.
