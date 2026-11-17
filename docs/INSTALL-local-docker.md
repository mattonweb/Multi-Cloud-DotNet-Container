# Install: local, with Docker

Runs the application on one machine in two ways: with `dotnet run`, for working on the code, and in a container built and run by Docker, which is the deliverable. Every cloud's install file repeats these headings.

Docker is the tool these steps were run with. [INSTALL-local-podman.md](INSTALL-local-podman.md) builds and runs the same image with Podman, from the same `Dockerfile`, and records what differs.

## Prerequisites

- **.NET SDK 10.0** (10.0.402 or later in the 10.0 line). Check with `dotnet --list-sdks`.
- **Docker** (Docker Desktop on Windows or macOS, Docker Engine on Linux) that builds `linux/amd64` images. Check with `docker version`: both a client and a server should answer.
- **curl**. In Windows PowerShell 5.1, `curl` is an alias for `Invoke-WebRequest`, so call `curl.exe`, or run the commands below from Git Bash.

The commands below are written for a bash shell (Git Bash on Windows works) and run from the repository root.

## One-time setup

```bash
git clone https://github.com/mattonweb/Multi-Cloud-DotNet-Container.git
```

```bash
cd Multi-Cloud-DotNet-Container
```

There is nothing else to set up: no database to install, no secret to create, no account to sign in to. The Notes database is created in memory each time the application starts.

## Build

Build and test the solution. Expect 0 warnings, 0 errors and every test passing; warnings are errors in this solution.

```bash
dotnet build "src/Multi-Cloud Container Solution.slnx"
```

```bash
dotnet test "src/Multi-Cloud Container Solution.slnx"
```

Build the image from the `Dockerfile` at the repository root. The tests do not run inside the image build.

```bash
docker build --platform linux/amd64 --tag multi-cloud-dotnet-container:local .
```

## Run

### With dotnet run

```bash
Heartbeat__WaitInSeconds=15 dotnet run --project src/MultiCloud.Application.Web
```

The application listens on <http://localhost:5080>, the launch profile's port, in the Development environment. `Heartbeat__WaitInSeconds=15` makes a heartbeat appear every 15 seconds instead of every 10 minutes. Stop it with Ctrl+C.

### In a Docker container

```bash
docker run --detach --name multi-cloud --publish 8080:8080 --env Heartbeat__WaitInSeconds=15 multi-cloud-dotnet-container:local
```

The application listens on <http://localhost:8080> in the Production environment, as the image's non-root user.

### Settings

Every setting has its default in `src/MultiCloud.Application.Web/appsettings.json` and is overridden by an environment variable, with `__` standing for `:`. None is a secret.

| Environment variable | Default | Meaning |
|---|---|---|
| `Heartbeat__WaitInSeconds` | `600` | Seconds between `Running` heartbeats; 1 or more |
| `Diagnostics__ErrorRoutesEnabled` | `true` | Whether the two error routes throw, or answer 404 |
| `Host__ShutdownTimeoutInSeconds` | `8` | How long the host waits for work to finish on shutdown |
| `ForwardedHeaders__ForwardLimit` | `1` | How many entries of `X-Forwarded-For`, counted from the right, to trust: one per proxy in front; 1 or more |
| `Notes__DatabaseName` | `Notes` | Name of the in-memory database |
| `Serilog__MinimumLevel__Default` | `Information` | The lowest log level written; any `Serilog` setting works the same way |
| `PORT` | not set | The port to listen on, when a platform says which |

A setting that is missing or out of range stops the application at startup, with a log line that names the setting.


---

## What is different here

- **The platform** is "Local process" under `dotnet run` and "Container, platform not identified" in Docker. Neither has a cloud, so the log lines carry no `cloud.provider`, `cloud.platform` or `cloud.region`.
- **No proxy is in front.** The image honors forwarded headers, but nothing sends them unless you add them to a request yourself. The scheme is `http`, and the remote address in Docker is the Docker network's gateway, not your machine.
- **The port** is 5080 under `dotnet run`, from the launch profile, and 8080 in the container, the base image's default. `PORT` is not set. Under `dotnet run`, the launch profile's address wins over `PORT`; run with `--no-launch-profile` to try `PORT` locally.
- **The environment** is Development under `dotnet run` and Production in the container. Nothing in the code behaves differently between them; only the `deployment.environment.name` on each log line changes.
- **The logs** go to the console: the terminal under `dotnet run`, `docker logs` in the container.
- **Docker is a client and a daemon.** `docker build` and `docker run` ask a background service, which runs as root inside Docker's Linux virtual machine on Windows and macOS, to do the work. Podman has no such service; [INSTALL-local-podman.md](INSTALL-local-podman.md) says what that changes, and what it does not.
