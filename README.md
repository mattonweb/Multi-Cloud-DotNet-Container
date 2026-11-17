# Multi-Cloud DotNet Container

A single .NET MVC application, packaged as one container image that deploys unchanged to Google Cloud Run and Google Kubernetes Engine (GKE). Amazon ECS on Fargate is in progress. Each platform has a step-by-step install guide.

This is a lightweight deployment verification application, designed to validate cloud infrastructure and container configuration before deploying a production workload. It provides a set of endpoints and logging behaviors that let you verify connectivity, health checks, HTTP traffic, and error handling without introducing the complexity of a real application.

The application includes:

- **Health check endpoints** to support platform health probes and external uptime monitoring.
- **Periodic heartbeat logging** with a unique session identifier, making it easy to verify log delivery and distinguish between application instances or successive runs.
- **Intentional failure endpoints** to test error handling, logging, and platform monitoring of application failures.
- **An in-memory Notes API** that accepts all supported HTTP methods, allowing you to verify that firewalls, ingress rules, and network policies permit the expected HTTP traffic.
- **A JSON root endpoint** that reports the application's view of its current runtime environment. No HTML pages are served; the application is entirely API-driven.

## Application stack

- **.NET 10 and C# 14** — Built using .NET SDK `10.0.402` on the development machine.
- **ASP.NET Core MVC** — Controllers without views. Responses are JSON or Problem Details, except for the plain-text health probes.
- **Serilog** — Writes structured JSON log events to the console, one JSON object per line. Each event includes a timestamp, severity, rendered message, and OpenTelemetry-compatible session and platform property names.
- **Entity Framework Core with SQLite in-memory mode** — Provides a relational data store without writing to disk. All data is lost when the application restarts.
- **MSTest** — Unit testing framework.

The container base images, Entity Framework Core, and the Microsoft.Extensions abstractions use the .NET `10.0.14` servicing release.

## HTTP API

All routes use lowercase URLs. The request collection in `src/MultiCloud.Application.Web/MultiCloud.Application.Web.http` contains an example request for every supported route and HTTP method.

| Method  | Route                | Response                                                                                                                                   |
| ------- | -------------------- | ------------------------------------------------------------------------------------------------------------------------------------------ |
| GET     | `/`                  | JSON containing the detected platform, session identifier, application version, environment, request information, and available GET routes |
| GET     | `/healthz/ping`      | `200 OK` with `up`; always succeeds and has no dependencies                                                                                |
| GET     | `/healthz/ready`     | `200 OK` with `ready` once started; `503 Service Unavailable` while starting or stopping                                                   |
| GET     | `/healthz/error/500` | `500 Internal Server Error` with Problem Details and an error log containing the exception                                                 |
| GET     | `/healthz/error/403` | `403 Forbidden` with Problem Details and a warning log                                                                                     |
| GET     | `/api/notes`         | `200 OK` with all notes as a JSON array                                                                                                    |
| GET     | `/api/notes/{id}`    | `200 OK` with the note, or `404 Not Found`                                                                                                 |
| HEAD    | `/api/notes/{id}`    | `200 OK` or `404 Not Found`, without a response body                                                                                       |
| OPTIONS | `/api/notes`         | `204 No Content` with `Allow: GET, POST, OPTIONS`                                                                                          |
| POST    | `/api/notes`         | `201 Created` with a `Location` header; `text` is required and `done` defaults to `false`                                                  |
| PUT     | `/api/notes/{id}`    | `200 OK` or `404 Not Found`; both `text` and `done` are required                                                                           |
| PATCH   | `/api/notes/{id}`    | `200 OK` or `404 Not Found`; updates whichever of `text` and `done` is supplied                                                            |
| DELETE  | `/api/notes/{id}`    | `204 No Content` or `404 Not Found`                                                                                                        |

### API behavior

- Note text must be 1 to 200 characters long and cannot be only whitespace.
- Invalid note data returns `400 Bad Request` with Problem Details.
- Every response includes `X-Session-Id` and `X-Platform` headers.
- Every Problem Details response includes a `sessionId` property.
- Errors generated by the application use Problem Details (RFC 9457, `application/problem+json`), including unknown routes (`404`) and unsupported HTTP methods (`405`).

The session and platform headers help distinguish application-generated responses from errors generated by upstream proxies, load balancers, or other infrastructure components.

The two health probes return plain text rather than JSON. All other application responses are JSON or Problem Details; the application does not serve HTML pages.

## Configuration

Application settings are defined in `appsettings.json` and can be overridden using environment variables. The double underscore (`__`) represents the configuration hierarchy separator (`:`).

|Environment variable|Default|Description|
|---|---|---|
|`Heartbeat__WaitInSeconds`|`600`|Interval, in seconds, between `Running` heartbeat messages. Minimum: 1|
|`Diagnostics__ErrorRoutesEnabled`|`true`|Enables or disables the two intentional error endpoints. When disabled, they return `404`|
|`Host__ShutdownTimeoutInSeconds`|`8`|Maximum time the host waits for outstanding work during shutdown|
|`ForwardedHeaders__ForwardLimit`|`1`|Number of rightmost `X-Forwarded-For` entries to trust. Normally one per trusted proxy; GKE uses `2`|
|`Notes__DatabaseName`|`Notes`|Name of the in-memory database|
|`Serilog__*`|Console, JSON, `Information`|Logging configuration, including minimum levels and sinks|
|`PORT`|Not set|Listening port supplied by the hosting platform, when applicable|


---
## Project status

**Three of the four deployment phases are complete.** The same container image runs locally, on Google Cloud Run, and on Google Kubernetes Engine (GKE). Both cloud deployments work by hand and through GitHub Actions.

AWS ECS on Fargate is the remaining phase. Its deployment design and manual installation guide are written, but deployment has not yet been completed.

|Phase|Platform|Status|
|---|---|---|
|1|Local development and container execution|Complete|
|2|Google Cloud Run|Complete|
|3|Google Kubernetes Engine|Complete|
|4|AWS ECS on Fargate|In progress|

## Repository contents

### Install guides

- [Local development with Docker](docs/INSTALL-local-docker.md)
- [Local development with Podman](docs/INSTALL-local-podman.md) (written, not yet tested)
- [Google Cloud Run](docs/INSTALL-gcp-cloud-run.md)
- [Google Kubernetes Engine](docs/INSTALL-gcp-gke.md)
- [AWS ECS on Fargate](docs/INSTALL-aws-ecs.md) (deployment not yet tested)

### Source, design, and license

- [`src/`](src/) — The .NET solution: the web application, two adapters, the heartbeat, and the unit tests.
- [Solution design](docs/SDD.md) — What was built, and why.
- [LICENSE](LICENSE) — MIT.

### Container and deployment assets

- `Dockerfile` and `.dockerignore` — The container image definition and build exclusions.
- `deploy/gke/` — Kubernetes manifests for Google Kubernetes Engine.
- `deploy/aws-ecs/` — ECS task definition for AWS Fargate.
- [`.github/workflows/`](https://github.com/mattonweb/Multi-Cloud-DotNet-Container/tree/main/.github/workflows) — GitHub Actions workflows:
    - **Publish image** — Builds the application once and publishes the container image to GitHub Container Registry (GHCR).
    - **Deploy to Google Cloud Run** — Deploys the published image to Cloud Run.
    - **Deploy to Google Kubernetes Engine** — Deploys the published image to GKE.

All three workflows are currently started manually from the GitHub Actions tab. The deployment workflows consume the image published by the build workflow rather than rebuilding it.
