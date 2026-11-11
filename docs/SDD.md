# Solution Design: Multi-Cloud DotNet Container

Status: phase 1 (runs locally and in a local container) is built. Written 2026-11-10; its open questions were decided
the same day (section 15). The solution builds, its unit tests pass, and the image passes every acceptance check in
section 13. Section 14 says what the first build confirmed.

## 1. Purpose

One small ASP.NET Core MVC application, built into one container image, that is later deployed unchanged to Google
Cloud Run, Google Kubernetes Engine and AWS ECS. It is a placeholder application: it does nothing useful on its own,
and exists so that a person can check that a platform is wired correctly.

It answers five questions about any environment it is deployed to:

1. Is the application up, and can the platform's health probes reach it?
2. Are the application's log lines arriving in the log system (for example Splunk), and from which run of which
   instance?
3. Do errors raised by the application reach the log system, and can a caller tell an application error from one
   returned by a firewall or proxy in front of it?
4. Does the network path allow every HTTP verb through?
5. Where does the application think it is running?

The rule that shapes the design: **the code and the image are identical everywhere. Everything that differs between
environments is an environment variable or a platform setting.** A change to `Program.cs` to suit one cloud is a
defect in the design, and is raised before it is made.

## 2. Phases

| Phase | Deliverable | State |
|---|---|---|
| 1 | The solution, the Dockerfile, and `docs/INSTALL-local.md`. Runs with `dotnet run` and with `docker run`. | This document |
| 2 | Google Cloud Run, with `docs/INSTALL-gcp-cloud-run.md` | In progress: deployed by hand and by its workflow on 2026-11-11; not designed here |
| 3 | Google Kubernetes Engine, with `docs/INSTALL-gcp-gke.md` | In progress: deployed by hand and by its workflow on 2026-11-11 (section 17) |
| 4 | AWS ECS on Fargate, with `docs/INSTALL-aws-ecs.md` | Later; not designed here |

Each later phase adds an install file and whatever platform settings it needs. Each install file is written as
plain command-line steps a person can follow by hand, and ends with a section recording what was different on that
platform. A cloud's GitHub Actions deploy workflow comes after its manual steps work, and automates exactly those
steps (section 16); infrastructure-as-code, if it comes at all, comes after that.

**Out of scope for phase 1:** any cloud resource, Terraform, a GitHub Actions pipeline, a log sink other than the
console, the OpenTelemetry SDK, sign-in, cookies, and any storage that outlives the process.

## 3. Solution layout

```
Dockerfile                                 the one image definition
.dockerignore
docs/
  SDD.md                                   this document
  INSTALL-local.md                         phase 1: run it with dotnet, run it with Docker
src/
  Multi-Cloud Container Solution.slnx      the one solution file
  Directory.Build.props                    house build settings (already in the repository)
  .editorconfig                            house C# style (already in the repository)
  MultiCloud.Application.Web/              the MVC host: composition root, controllers, logging setup
  MultiCloud.Adapter.Platform/             detects the hosting platform and describes it
  MultiCloud.Adapter.Notes/                the in-memory Notes database and its create, read, update, delete
  MultiCloud.Orchestrator.Heartbeat/       the background heartbeat
  MultiCloud.Test.Unit/                    MSTest unit tests
```

The root namespace is `MultiCloud`. Project kinds, naming, dependency directions and what each class library may
make public follow the `dotnet-solution-architecture` skill; the adoption state is `conforming` from the first
commit.

| Project | Kind | Why it is its own project | May reference |
|---|---|---|---|
| `MultiCloud.Application.Web` | Application | The host. Loads configuration, builds each options object, calls each registration, holds the controllers. No application logic. | The three below |
| `MultiCloud.Adapter.Platform` | Adapter | The hosting platform is an external system. Each platform announces itself differently; this project translates all of them into one contract. | Nothing in the solution |
| `MultiCloud.Adapter.Notes` | Adapter | Owns one data store, the in-memory `Notes` database. | Nothing in the solution |
| `MultiCloud.Orchestrator.Heartbeat` | Orchestrator | A scheduled task is an entry point, and entry points live in Orchestrators. | Nothing in the solution |
| `MultiCloud.Test.Unit` | Test | | Any |

> **Decision D1 (section 15): five projects.** The shape first agreed was one Application, one Adapter for the
> database, and one test project. This design adds `MultiCloud.Adapter.Platform` and
> `MultiCloud.Orchestrator.Heartbeat`, because the architecture skill puts a scheduled task in an Orchestrator and an
> external system behind an Adapter. The alternative keeps both inside the Application as hosting concerns, under
> `Platform/` and `Heartbeat/` folders: two fewer projects, and a recorded departure from the skill. It is revisited
> if a cloud phase shows the split costs more than it gives.

### Precedent to read before writing code

The repository `Razor-Pages-With-HTMX` (same owner, public, cloned beside this one at `C:\Source\HTMX`) is the
existing solution of the same shape. Read these before creating the matching piece here, and build on their form:

- `src/HTMX.Adapter.Customers/Data/InMemoryDatabase.cs`: the SQLite in-memory database kept alive by one open
  connection. `MultiCloud.Adapter.Notes` uses the same pattern.
- `src/HTMX.Adapter.Customers/ServiceCollectionExtensions.cs` and its `Configuration/` and `Contracts/` folders: the
  public surface of an Adapter.
- The three `.csproj` files and `src/Directory.Build.props`: how little a project file needs to state.
- `src/HTMX.Test.Unit/`: test naming and structure, and the use of a fake `TimeProvider`.

## 4. The web application

MVC controllers without views: the application is a JSON API and returns no HTML page (decision D7). Every answer
is JSON, an error as Problem Details, except the two probes' plain text. No views, no Razor Pages, no minimal-API
endpoints. Every controller carries `[ApiController]`.

### 4.1 Routes

| Verb | Route | Action | Answer |
|---|---|---|---|
| GET | `/` | `HomeController.Index` | 200, JSON, section 4.2 |
| GET | `/healthz/ping` | `HealthController.Ping` | 200, `text/plain`, body `up` |
| GET | `/healthz/ready` | `HealthController.Ready` | 200 `ready`, or 503 `not ready` |
| GET | `/healthz/error/500` | `DiagnosticsController.Error500` | Throws. 500 problem details |
| GET | `/healthz/error/403` | `DiagnosticsController.Error403` | Throws. 403 problem details |
| GET | `/api/notes` | `NotesController.List` | 200, JSON array |
| GET | `/api/notes/{id}` | `NotesController.Read` | 200, or 404 |
| HEAD | `/api/notes/{id}` | `NotesController.Exists` | 200 or 404, no body |
| OPTIONS | `/api/notes` | `NotesController.Options` | 204 with an `Allow` header |
| POST | `/api/notes` | `NotesController.Create` | 201 with a `Location` header |
| PUT | `/api/notes/{id}` | `NotesController.Replace` | 200, or 404 |
| PATCH | `/api/notes/{id}` | `NotesController.Change` | 200, or 404 |
| DELETE | `/api/notes/{id}` | `NotesController.Delete` | 204, or 404 |

Every URL is lower case.

The probes and the error routes share the `/healthz` prefix but are two controllers. As one, SonarAnalyzer's S6960
found two responsibilities in it, and the build failed: the probes depend on the host's state, the error routes on
the `Diagnostics` setting. The routes are unchanged.

### 4.2 The root route

One JSON document, `application/json`, that says where the application thinks it is running. It holds:

- `service`, `version` and `environment`: the application's name, as `service.name` gives it, its version, and the
  host environment's name.
- `session`: the session identifier and when the session started (section 7).
- `platform`: the platform as section 5 describes it. A value the platform does not supply is `null`.
- `request`: the request as the application sees it, its scheme, host and remote address. Behind a platform's proxy
  these show whether forwarded headers are being honored.
- `routes`: every GET route above.

### 4.3 Health routes

- **`/healthz/ping` has no dependencies and never will.** It returns the constant text `up`. It is what an outside
  monitor such as Dynatrace calls. If it checked a dependency, one failing dependency would make every instance look
  dead at once. Say so in a comment on the action.
- **`/healthz/ready`** answers whether this instance should receive traffic. In phase 1 it returns `ready` once the
  host has finished starting and `not ready` with 503 once the host has begun stopping. The 503 is expected to be
  brief: soon after the stop signal the server stops accepting connections, and from then on a probe fails to
  connect instead. Either answer takes the instance out of rotation. A platform that needs a longer drain gets it
  from a platform setting, such as a pre-stop hook or a deregistration delay, decided in that platform's phase; never
  from a delay in code (decision D5). The action carries a comment marking where dependency checks are added later.
- **The two error routes exist to prove that an error comes back as JSON a client can read**: Problem Details, as RFC
  9457 (formerly RFC 7807) defines it, with the media type `application/problem+json`. A client that expects JSON
  gets JSON, from the application, through whatever stands in front of it. They also prove error handling and error
  logging end to end. `Error500` throws an ordinary exception. `Error403` throws the application's own exception type
  that carries a status code (ASP.NET Core has no built-in one). Both are caught by one global exception handler
  (section 4.4).
- **The error routes are on by default.** `Diagnostics:ErrorRoutesEnabled` defaults to `true`, in every environment.
  A setting can turn them off: when `false`, both routes answer 404, itself Problem Details, and throw nothing.

### 4.4 Cross-cutting behavior

- **Exception handling.** One `IExceptionHandler` with problem details. An exception carrying a status code is
  answered with that status and logged as a warning; anything else is answered with 500 and logged as an error with
  the exception. The problem details body includes the session identifier.
- **Errors the framework answers itself are Problem Details too.** A route that does not exist (404) and a verb a
  route does not take (405) would otherwise go out with no body, which a browser dresses up as its own HTML page. The
  status code pages middleware writes Problem Details into every error response that has no body, so every error is
  `application/problem+json`. Found by Matt under `dotnet run`, after decision D7.
- **Response headers on every response.** `X-Session-Id` (the session identifier) and `X-Platform` (the platform
  name). A 403 or a 500 without these headers did not come from the application.
- **Request logging.** One log line per request, written after the response, with verb, path, status and duration.
  `/healthz/ping` and `/healthz/ready` are left out, because probes call them every few seconds. The error routes
  are not left out.
- **Forwarded headers.** Every target platform ends HTTPS at a proxy in front of the container. The application
  honors `X-Forwarded-For` and `X-Forwarded-Proto`. In phase 1 this is switched on by an environment variable set in
  the image, not by code. How many entries of `X-Forwarded-For` to trust, counted from the right, is the setting
  `ForwardedHeaders:ForwardLimit`, one per proxy the platform puts in front: 1 by default, 2 on GKE (decision D11).
- **No HTTPS redirection and no HSTS in the application.** The platform owns HTTPS.
- **Port.** When the environment variable `PORT` is set, the application listens on that port on all interfaces.
  When it is not, the framework default applies: 8080 in the container image, the launch profile on a developer
  machine.
- **Shutdown.** The host's shutdown timeout is read from `Host:ShutdownTimeoutInSeconds`, default 8. Platforms send
  a stop signal and then kill the process after a grace period; the shortest of the three is about 10 seconds.
- **Nothing that needs shared state between instances:** no cookies, no anti-forgery tokens, no session state, no
  data protection keys.

> **No views, and so no data protection key (decisions D6 and D7, section 15).** The first container run found that
> MVC's views register ASP.NET Core data protection, and that the host then created a key at startup that nothing
> used: it was written to `/home/app/.aspnet/DataProtection-Keys`, with two warnings that broke acceptance check 8.
> Quieting them in configuration (D6) was decided, then withdrawn the same day when the views went (D7): MVC without
> views registers no anti-forgery and no data protection, and the container writes no key and logs no warning.

## 5. Platform detection (`MultiCloud.Adapter.Platform`)

One contract: read the platform description. The description holds the platform name, the cloud provider, the
cloud platform, the region, the name of the deployed service, its revision, and the name of this instance. A value
the platform does not supply is absent, never guessed.

The platform name is for people: the root route and the `X-Platform` header show it. The cloud provider and cloud
platform are for log systems: they hold OpenTelemetry's well-known values for the `cloud.provider` and
`cloud.platform` attributes (section 7), and are absent on a row that has no well-known value (decision D3). A
cluster found through `KUBERNETES_SERVICE_HOST` may be anyone's Kubernetes, not only Google's, so that row sets
neither; phase 3 decides whether Google Kubernetes Engine can be told apart without a network call.

Detection reads environment variables only. No network call in phase 1. Environment variables are read through an
injected abstraction so tests supply their own.

| Checked in this order | Variable that identifies it | Platform name | Cloud provider / cloud platform | Other values, where the platform supplies them |
|---|---|---|---|---|
| 1 | `K_SERVICE` | Google Cloud Run | `gcp` / `gcp_cloud_run` | Service `K_SERVICE`, revision `K_REVISION` |
| 2 | `ECS_CONTAINER_METADATA_URI_V4` | AWS ECS | `aws` / `aws_ecs` | Region `AWS_REGION` |
| 3 | `KUBERNETES_SERVICE_HOST` | Kubernetes | absent | Instance `HOSTNAME` (the pod name) |
| 4 | `DOTNET_RUNNING_IN_CONTAINER` | Container, platform not identified | absent | Instance `HOSTNAME` |
| 5 | none of the above | Local process | absent | Instance: the machine name |

**These variable names are from memory and are not verified.** Phase 1 can only prove rows 4 and 5. Each cloud phase
checks its own row against what the platform actually sets and corrects this table and the code together.

Public surface: `Configuration/`, `Contracts/` and `ServiceCollectionExtensions`, nothing else.

## 6. The heartbeat (`MultiCloud.Orchestrator.Heartbeat`)

A hosted background service that writes a heartbeat log message through `ILogger`. It is how a person confirms that
log lines are reaching a log system, and tells one run of the application from the next.

| Kind | When | Sequence |
|---|---|---|
| `Started` | Once, as soon as the service starts. Nobody waits a full interval for the first line. | 0 |
| `Running` | Every `Heartbeat:WaitInSeconds` seconds after that | 1, 2, 3 and so on |
| `Stopping` | Once, when the host signals shutdown | The next number |

All three kinds have the same shape, so one search finds the whole life of a session. Each carries the kind, the
sequence number, the seconds since the session started, the configured wait, the .NET runtime version and the
operating system description. The session, version, environment and platform values are already on every log line
(section 7), so the heartbeat does not repeat them in its own message.

- **`Heartbeat:WaitInSeconds`** defaults to 600. A value below 1 stops the application at startup with a message
  that names the setting.
- **The sequence number is how a dropped message is noticed:** a gap in the numbers for one session.
- **A `Stopping` line is only written on a polite shutdown.** A container that is killed writes none. A `Started`
  line from a new session with no `Stopping` line from the one before is the sign of that.
- **The heartbeat never writes environment variables or configuration values wholesale.** They can hold secrets.
- **Testability.** The timing uses the injected `TimeProvider`, so a test advances a fake clock. The code that
  composes and writes one heartbeat is separate from the loop that decides when.
- The host constructs the options object, including the time the session started, and passes it to the
  registration. The project does not read configuration itself.
- **The project references nothing else in the solution** (decision D2). It needs the session's start time for the
  seconds-since-start value and nothing more: the session identifier and the platform reach every log line through
  the enricher (section 7). The session type belongs to `MultiCloud.Application.Web`, and only its start time
  crosses into this project.

## 7. Logging and the session

- **Serilog is the logging provider.** Application code logs through `ILogger<T>` only. Serilog's own static logger
  appears in `Program.cs` alone, for the bootstrap logger and the final flush.
- **One sink by default: the console, one JSON object per line.** Every target platform collects standard output.
  Sending logs to Splunk or anywhere else is a platform setting in a later phase, or a Serilog sink added through
  configuration; no log system's package is referenced in phase 1.

- **Every line carries a `severity` a log system can read** (decision D8). One line is one JSON object: `time`,
  `severity` (`DEBUG`, `INFO`, `WARNING`, `ERROR` or `CRITICAL`, from Serilog's Verbose and Debug, Information,
  Warning, Error and Fatal), the rendered `message`, and, when they exist, the `exception`, `traceId` and `spanId`;
  then every property of the event. The names are generic, so no log system is named in the code; Cloud Logging, among
  others, reads `severity` and shows `message` as the entry's summary.

> **Decision D8 (section 15), found by the first Cloud Run deploy.** Cloud Logging received every line, but none
> carried a severity: `RenderedCompactJsonFormatter` writes the level as `@l` and leaves it out for Information, and
> Cloud Logging reads it from `severity`, so a severity filter or an alert on errors missed the application's errors.
> The shape above is a Serilog.Expressions template, written once in `Logging/JsonLogFormat.cs` so it can be tested.
> The `Serilog` configuration section names it for the console, and the bootstrap logger uses it too, so a failure
> while starting is `CRITICAL` with its exception. A Google-specific formatter package was not an option: the design
> references no log system's package, and the code must not know which cloud it is in.
- **Serilog is configured from configuration** (`Serilog` section), so a level or a sink changes without a code
  change.
- **A bootstrap logger** exists before the host is built, so a failure during startup is logged. The log is flushed
  in a `finally` block as the process exits.
- **The session.** `Program.cs` creates one session when the process starts: a version 7 UUID and the start time.
  A session is one run of one instance of the application. It is not a user's session. The session type lives in
  `MultiCloud.Application.Web`; the heartbeat receives only its start time (section 6).
- **Every log line is enriched** with the properties below. The names are OpenTelemetry's semantic convention names,
  so a log system that knows those conventions recognizes them.

| Property | Value |
|---|---|
| `service.name` | `multi-cloud-dotnet-container` |
| `service.version` | The application's informational version, the same under `dotnet run` and in the image |
| `service.instance.id` | The session identifier |
| `deployment.environment.name` | The host environment name |
| `cloud.provider` | OpenTelemetry's well-known value from platform detection (section 5), when known |
| `cloud.platform` | OpenTelemetry's well-known value from platform detection (section 5), when known |
| `cloud.region` | From platform detection, when known |
| `host.name` | The machine name, which in a container is the container's host name |

These are set by an enricher, not through message templates, because a message template property name cannot
contain a dot. Confirm the names against the published conventions when writing the enricher, and record any that
differ.

- **`service.version` is the same in every build** (decision D4). The .NET SDK appends the source control commit to
  the informational version by default. The image is built without `.git` in its context, so it would report a
  different version from `dotnet run` on the same tree. `MultiCloud.Application.Web` turns that suffix off, so the
  version is the one its project file sets. Recording the commit in the version belongs to a pipeline, and there is
  none.

## 8. The Notes database (`MultiCloud.Adapter.Notes`)

Exists so that every HTTP verb has something real to do.

- Entity Framework Core over SQLite in memory, held open by one connection for the life of the process, as the
  precedent does it. Created and seeded with three invented notes at startup. Nothing is written to disk.
- A note has an identifier assigned by the database, a text (required, 1 to 200 characters), a done flag, and the
  times it was created and last changed, in UTC.
- PUT replaces the text and the done flag and requires both. PATCH changes whichever of the two it is given.
- POST requires the text; the done flag is false when left out. Every PUT and PATCH marks the note changed now, even
  when the values sent are the ones it holds. OPTIONS `/api/notes` answers `Allow: GET, POST, OPTIONS`, the verbs that
  route itself answers. (Written by the first build where the design was silent; to be confirmed.)
- A request that fails validation is answered 400 with problem details.
- **The data lives inside one process.** With two instances behind a load balancer, a note created on one is absent
  on the other, and a restart empties it. That is expected; the `X-Session-Id` header on each response shows which
  instance answered. Say so in the install files.
- Public surface: `Configuration/`, `Contracts/` and `ServiceCollectionExtensions`. The database context, the
  entity and the service are internal.

## 9. Configuration

Every setting has a default in `appsettings.json` and is overridden by an environment variable, with `__` standing
for `:`.

| Setting | Environment variable | Default | Meaning |
|---|---|---|---|
| `Heartbeat:WaitInSeconds` | `Heartbeat__WaitInSeconds` | `600` | Seconds between `Running` heartbeats |
| `Diagnostics:ErrorRoutesEnabled` | `Diagnostics__ErrorRoutesEnabled` | `true` | Whether the two error routes throw or answer 404 |
| `Host:ShutdownTimeoutInSeconds` | `Host__ShutdownTimeoutInSeconds` | `8` | How long the host waits for work to finish on shutdown |
| `ForwardedHeaders:ForwardLimit` | `ForwardedHeaders__ForwardLimit` | `1` | How many entries of `X-Forwarded-For`, counted from the right, to trust: one per proxy in front (decision D11) |
| `Notes:DatabaseName` | `Notes__DatabaseName` | `Notes` | Name of the in-memory database |
| `Serilog:*` | `Serilog__*` | Console, JSON, `Information` | Levels and sinks |
| (none) | `PORT` | not set | Port to listen on, when the platform supplies one |

No setting is a secret, and no secret is needed in phase 1.

## 10. The container image

- `Dockerfile` at the repository root, two stages: the .NET 10 SDK image builds and publishes
  `MultiCloud.Application.Web` in Release; the ASP.NET Core 10 runtime image runs it.
- Project files and `Directory.Build.props` are copied and restored before the source, so a source change does not
  repeat the restore.
- `src/.editorconfig` is in the build context, so the analyzers behave in the image build as they do on a developer
  machine. Warnings are errors in both.
- The process runs as the image's non-root user.
- Port 8080 is exposed.
- The image sets the environment variable that switches forwarded headers on.
- `.dockerignore` keeps `bin`, `obj`, `.git`, `.vs` and `.claude` out of the build context.
- The image is built for `linux/amd64`. Tests are not run in the image build.

## 11. Tests

MSTest, written as the `code-csharp-mstest` skill describes. Unit tests only in phase 1.

- Platform detection: one test per row of the table in section 5, including its cloud provider and cloud platform
  values, the order of the checks, and absent values.
- Heartbeat: the three kinds, the sequence, the first line without waiting, the interval under a fake clock, and
  the rejected setting.
- Notes: each operation, the validation rules, and the not-found answers.
- Health controller: ping, and ready in each host state. Diagnostics controller: the error routes with the setting
  on and off. Home controller: the application, session, platform and request the root route reports.
- Exception handler: the status-carrying exception and the ordinary one.

`MultiCloud.Application.Web` also holds a `.http` file with one request for every route in section 4.1, so every
verb can be sent from Visual Studio.

## 12. Documents the first session writes

- `docs/INSTALL-local.md`, with these headings, which every later install file repeats: Prerequisites, One-time
  setup, Build, Run, Verify, Tear down, What is different here. The Verify section gives a `curl` command for every
  route and verb.
- `README.md`: replace the placeholder sections with the routes, the settings, and the exact version of every
  package and base image chosen.

## 13. Acceptance for phase 1

Run by Matt, against the container, not against `dotnet run`, because the image is the deliverable. Start it with
`Heartbeat__WaitInSeconds=15`.

If the image does not build, the container does not start, or the port cannot be reached, the checks below did not
run. That is a build or setup failure to fix first, not a failed check.

| # | Check | Passes when |
|---|---|---|
| 1 | Build and test the solution | No warnings, no errors, every test passes |
| 2 | Inspect the running container's user | It is not root |
| 3 | GET `/healthz/ping` | 200 and the body is exactly `up` |
| 4 | Compare the `X-Session-Id` header from check 3 with the first heartbeat in `docker logs` | The log line is one JSON object, its kind is `Started`, and its `service.instance.id` equals the header |
| 5 | Wait 30 seconds and read the logs | `Running` heartbeats with sequence 1 and 2, about 15 seconds apart |
| 6 | `docker stop`, then start a new container | The old logs end with a `Stopping` heartbeat; the new container has a different session identifier |
| 7 | GET both error routes | 500 with an error log line holding the exception; 403 with a warning log line; both are `application/problem+json` with `type`, `title`, `status` and `sessionId`, and carry `X-Session-Id` |
| 8 | Restart with `Diagnostics__ErrorRoutesEnabled=false` and GET both error routes | 404 for both, and no error or warning log line |
| 9 | Send POST, GET, HEAD, PUT, PATCH, OPTIONS and DELETE to the notes routes, then GET the deleted note | 201, 200, 200, 200, 200, 204, 204, then 404 |
| 10 | Read the request log after checks 3 to 9 | The notes and error requests are logged; no ping or ready request is |
| 11 | GET `/` in the container, then under `dotnet run` | JSON whose `platform.name` is "Container, platform not identified" in the first and "Local process" in the second |
| 12 | GET a route that does not exist, and DELETE `/healthz/ping` | 404 and 405, each `application/problem+json` with `sessionId`; never an empty body |

Check 4 is the one that matters most: it proves in one step that the session identifier, the enrichment, the
heartbeat and the response header all come from the same run.

## 14. Not verified when this was written

Written from memory or from design, and to be confirmed by the session that builds it. Anything in this list that
turns out different is corrected here in the same change that discovers it.

**Confirmed by the first build, 2026-11-10:**

- **Serilog accepts dotted property names from an enricher** and the JSON formatter writes them as plain
  top-level JSON keys, such as `"service.instance.id":"…"`.
- **The OpenTelemetry names and values exist** in the semantic conventions registry (`open-telemetry/semantic-conventions`,
  `model/`, main branch): `service.name`, `service.version`, `service.instance.id` and `deployment.environment.name` are
  stable; `cloud.provider`, `cloud.platform`, `cloud.region` and `host.name` are still marked development. The values
  `gcp`, `aws`, `gcp_cloud_run` and `aws_ecs` are all listed.
- **The SDK appends the commit to the informational version by default** (an assembly built from this tree without
  the setting reports `1.0.0+61cc46e…`), and `IncludeSourceRevisionInInformationalVersion` set to `false` turns it off
  (`MultiCloud.Application.Web` reports `0.1.0`).
- **Rows 4 and 5 of the platform table**: under `dotnet run` the platform is "Local process", named by the machine;
  in a local container it is "Container, platform not identified", named by the container's host name.
- **A build with no `.git` in its context raises no warning**: the image builds with warnings as errors and prints
  none.
- **`ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` switches forwarded headers on.** In the container, a request from
  Docker's gateway with `X-Forwarded-Proto: https` and `X-Forwarded-For: 203.0.113.7` is seen as https from
  203.0.113.7, so it is not limited to a proxy on the loopback address.
- **The SQLite native library loads in the ASP.NET Core 10.0.14 runtime image as the non-root user** (uid 1654): the
  Notes database is created and every verb answers.
- **The 503 window on shutdown is too short to see.** Probing `/healthz/ready` about every 0.2 seconds through Docker's
  port forward, the answers went from 200 straight to refused connections; `docker stop` took 1.3 seconds and the
  `Stopping` heartbeat was written. A platform's probe sees the instance go away either way (decision D5).

**Confirmed on Google Cloud Run, 2026-11-11:**

- **Row 1 of the platform table.** Cloud Run sets `K_SERVICE` and `K_REVISION`; the root route reported "Google Cloud
  Run", `gcp`, `gcp_cloud_run`, the service and the revision, and no region or instance, as the table says.
- **Forwarded headers are trusted behind Google's front end.** The root route reported the scheme as `https`, though
  the container speaks plain HTTP, and the caller's public address as the remote address.
- **`PORT` works**: Cloud Run sets it and the application listened on it.
- **Google's front end passes the application's error bodies through**: 500, 403 and 404 arrived as
  `application/problem+json` with `X-Session-Id`.
- **The heartbeat keeps time while idle, with CPU given only during requests** (Cloud Run's default). One instance
  wrote `Running` 1 to 17 a minute apart, with no gap, though no request arrived after its first three minutes.
- **An instance Cloud Run stops writes its `Stopping` line.** About 15 minutes after its last request, Cloud Run
  stopped the idle instance, and its last line was `Stopping` with the next number: Cloud Run sends a polite stop
  signal, and the 8-second shutdown timeout is long enough.
- **Cloud Logging reads decision D8's `severity` and `message`.** On the first revision deployed with them, the 500's
  lines arrived as `ERROR`, the 403's refusal as `WARNING`, and the heartbeats and startup lines as `INFO`, each with
  its message as the entry's summary.
- **Kestrel warns when Cloud Run withholds CPU.** Between startup and the first request, Kestrel's own one-second
  timer, which it calls its heartbeat and which is not the application's, ran 3.5 seconds late, and Kestrel logged
  a `WARNING` about thread pool starvation when the request arrived. It is the effect of CPU given only during
  requests, harmless, and a platform setting (startup CPU boost, or CPU always allocated) would remove it.

**Confirmed by the workflows, 2026-11-11** (section 16):

- **Publish image publishes one image manifest.** Its first run built and tested everything, then `ghcr.io` refused
  the push with `denied`: the runner's Docker (Ubuntu 26.04) adds a provenance attestation by default, which turns the
  push into a manifest list, and `ghcr.io` reads the source label that links a new package to its repository from the
  image itself. Built with no attestation, the second run published `2026.1111.2`, the digest it read back matching
  the one the registry reported.
- **Deploy to Google Cloud Run deploys the published bytes.** Its first run, with the tag left blank, chose
  `2026.1111.2` from `ghcr.io`, signed in through Workload Identity Federation, copied the image to Artifact Registry
  with its digest unchanged (`docker buildx imagetools create --prefer-index=false`), deployed that digest as a new
  revision, and checked that the revision answered. The project's ID and number showed masked throughout the log.
- **Deploy to Google Kubernetes Engine deploys the published bytes, and the caller is reported.** Its first run, with
  the tag left blank, chose `2026.1111.5`, the first image that reads `ForwardedHeaders:ForwardLimit`, and copied it
  to Artifact Registry with its digest unchanged. Applying `deploy/gke/` changed only the deployment; the rollout
  replaced the pod in about 20 seconds, and the new pod ran the copied digest. Through the load balancer, the new pod
  answered the first request, reporting "Kubernetes" and the runner's own public address as the caller's. The
  project's ID and the load balancer's address showed masked throughout the log.

**Confirmed on Google Kubernetes Engine, deployed by hand, 2026-11-11** (section 17):

- **Row 3 of the platform table.** The root route reported "Kubernetes", the pod's name as the instance, and no cloud
  provider, platform, region, service or revision, as the table says.
- **Google's load balancer passes the application's answers through**: `/healthz/ping` answered `up` with
  `X-Session-Id`, `X-Platform: Kubernetes` and `Via: 1.1 google`; 500, 403 and 404 arrived as
  `application/problem+json` with `X-Session-Id`; every verb answered as in local Docker.
- **Cloud Logging reads the pod's console as it reads Cloud Run's**: the 500 as `ERROR`, the 403's refusal as
  `WARNING`, the rest as `INFO`. The load balancer's and Kubernetes's probes left no line in the request log.
- **A pod Kubernetes replaces stops politely.** After `kubectl rollout restart`, the old pod wrote `Running` up to 7,
  a minute apart, then `Stopping` 8; the new pod wrote `Started` with a new session.
- **Finding: behind two proxies, the default trusts only one.** The root route reported the load balancer's own public
  address as the caller's. Google's load balancer appends two entries to `X-Forwarded-For`, the caller's address and
  then its own, where Cloud Run's front end appends one, and the framework's default limit trusts only the rightmost
  entry. Decision D11 makes the limit a setting, which GKE's deployment sets to 2; the first run of Deploy to Google
  Kubernetes Engine confirmed the caller's address is reported with it.

**Still to confirm:**

- Row 2 of the platform table, in phase 4.
- That forwarded headers are trusted from the proxy in front of the container on ECS, in phase 4.
- That the default of 1 leaves Cloud Run reporting the caller's address, on the first Cloud Run deploy of an image
  that reads `ForwardedHeaders:ForwardLimit` (`2026.1111.5` or later).

## 15. Decisions

Decided by Matt: on 2026-11-10, D1 to D5 taking the recommendations from the first review of this design, and D6 and
D7 after the first container run; on 2026-11-11, D8 after the first Cloud Run deploy, D9 when the first workflows
were written, D10 for phase 3, and D11 after the first GKE deploy. Each one is provisional: the cloud phases deploy
the image for real, and a decision that does not survive that is changed here in the same change that finds it.

| # | Decision | Applied in |
|---|---|---|
| D1 | Five projects, not three: the platform Adapter and the heartbeat Orchestrator are their own projects | Section 3 |
| D2 | The session type lives in the Application. The heartbeat receives only the session's start time and references no other project in the solution | Sections 3, 6, 7 |
| D3 | `cloud.provider` and `cloud.platform` hold OpenTelemetry's well-known values, or are absent. The platform name for people appears in the root route's JSON and in `X-Platform` | Sections 5, 7 |
| D4 | `service.version` carries no commit suffix, so `dotnet run` and the image report the same version | Section 7 |
| D5 | `/healthz/ready` answers 503 once stopping begins, and no delay is added in code; a longer drain is a platform setting, decided per platform | Section 4.3 |
| D6 | Withdrawn by D7. The data protection log category was to be set to `Error` in `appsettings.json`, to quiet the two startup warnings of a key nothing used; without views no key is created | Section 4.4 |
| D7 | No views: the application is a JSON API and returns no HTML page. `GET /` answers JSON; MVC is registered without views, which also removes data protection | Sections 4, 4.2, 4.4 |
| D8 | Every log line carries `severity` and `message`: one Serilog.Expressions template, in code so it is tested, named by configuration and used by the bootstrap logger. Decided after the first Cloud Run deploy | Section 7 |
| D9 | GitHub Actions, started only by hand: one workflow publishes the image once to GitHub Container Registry; one workflow per cloud copies it, digest unchanged, into that cloud's registry and deploys it. Each cloud's workflow follows its manual steps | Sections 2, 16 |
| D10 | Phase 3 runs on a GKE Autopilot cluster, behind Google's HTTP load balancer from an Ingress, from plain manifests in `deploy/gke/`. GKE comes before AWS: Google Cloud is finished first | Sections 2, 17 |
| D11 | How many entries of `X-Forwarded-For` to trust is the setting `ForwardedHeaders:ForwardLimit`, default 1, read by `Program.cs` like every other setting; GKE's deployment sets 2. A setting, not detection: the entries left of the platform's own are whatever the caller sent, so counting them cannot tell a proxy from a forgery. Decided after the first GKE deploy | Sections 4.4, 9, 17 |

## 16. Workflows

GitHub Actions publishes the image and deploys it, and **every workflow runs only when Matt starts it by hand**
(`workflow_dispatch`), never on a push (decision D9). Each one is a cloud's install file, automated: a cloud gets its
deploy workflow once its manual steps have been run and pass.

| Workflow | File | What it does |
|---|---|---|
| Publish image | `.github/workflows/publish-image.yml` | Builds and unit-tests the solution through Signalbox's reusable pull request checks, pinned to a version tag; builds the image once for `linux/amd64`; pushes it to GitHub Container Registry as `ghcr.io/<owner>/multi-cloud-dotnet-container:<tag>`. Needs no cloud credential |
| Deploy to Google Cloud Run | `.github/workflows/deploy-gcp-cloud-run.yml` | Takes a published tag, or, left blank, the newest tag on `ghcr.io`; signs in to Google through Workload Identity Federation; copies the image into Artifact Registry and checks the digest is unchanged; deploys that digest to the existing service; checks the new revision answers |
| Deploy to Google Kubernetes Engine | `.github/workflows/deploy-gcp-gke.yml` | The same first three steps, with the same service account; then applies `deploy/gke/` with the image's digest, waits for the rollout, checks every new pod runs that digest, and checks through the load balancer that a new pod answers `up`, reports "Kubernetes", and reports the caller's address rather than the load balancer's. The cluster must already exist |
| Deploy to AWS ECS | to come, in phase 4 | The same shape: sign in to the cloud without a stored key, copy by digest, deploy, check |

- **The image is built once.** Every cloud runs the same digest: the publish run's digest is the digest each deploy
  copies, checks and deploys. That is the design's one rule, shown rather than assumed.
- **The tag is `<YYYY.MMdd>.<run number>`**, in the America/Chicago time zone, the scheme of Signalbox's reusable build
  release. The application's own version stays `0.1.0` (decision D4); writing the tag into it is a later decision.
- **Nothing private is in the repository or the logs.** No cloud key exists: each cloud trusts GitHub's short-lived
  token for this repository's `main` branch. A cloud's identifiers, such as the Google project's ID and number, are
  repository secrets, which GitHub masks in the run logs that a public repository shows to everyone.
- **Every action is pinned to a commit**, and Signalbox's reusable workflow to a version tag, as in Matt's other
  repositories.

## 17. Phase 3: Google Kubernetes Engine

The same image on GKE, deployed by hand first from `docs/INSTALL-gcp-gke.md`, then by a deploy workflow once those
steps pass (section 16). Decided by Matt on 2026-11-11 (decision D10).

- **An Autopilot cluster**, `multi-cloud`, in `us-central1`, the region of the image repository. Google runs, sizes and
  patches the nodes; the pod's requests, 0.1 vCPU and 256 MiB, are what is billed. Standard mode, with nodes to choose
  and patch, was the alternative.
- **Google's external HTTP load balancer in front, from an Ingress**, so a proxy sits in front of the container as on
  Cloud Run, and forwarded headers and Problem Details are tested through it again. A LoadBalancer Service, with
  nothing in front to rewrite the request, was the alternative. Plain HTTP on a public address; HTTPS needs a domain
  and a certificate.
- **The manifests are plain Kubernetes YAML in `deploy/gke/`**: a namespace, a deployment, a service and an ingress.
  The deployment's image is `${IMAGE}`, filled in by `envsubst` when it is applied, because the image's address holds
  the project's ID, which stays out of this repository.
- **Probes**: startup and liveness on `/healthz/ping`, which has no dependencies, so a failing dependency never
  restarts the pod; readiness on `/healthz/ready`, which the load balancer's health check follows. The grace period
  on a stop is 30 seconds, longer than the host's 8-second shutdown.
- **Settings are environment variables in the deployment**, as on Cloud Run: `Heartbeat__WaitInSeconds` is 60, and
  `ForwardedHeaders__ForwardLimit` is 2, because Google's load balancer appends two entries to `X-Forwarded-For`
  where Cloud Run's front end appends one (decision D11, found on the first deploy).
- **The deploy workflow applies the same manifests** as the manual steps, with the image named by digest, and acts as
  the service account Deploy to Google Cloud Run uses, given one more role: `roles/container.developer` on the
  project, which reaches the Kubernetes objects in the project's clusters. That is one cluster here; a project with
  others would narrow it with Kubernetes RBAC.
- **Row 3 of the platform table** applies: "Kubernetes", the pod's name as the instance, no cloud provider. Nothing in
  a pod's environment says the cluster is Google's, and the design makes no network call, so GKE reports itself as
  any Kubernetes. Whether to change that, for example with a variable the deployment sets, is open until the first
  deploy shows what the environment holds.
