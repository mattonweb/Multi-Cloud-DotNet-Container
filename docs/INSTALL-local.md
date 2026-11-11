# Install: local

Runs the application on one machine in two ways: with `dotnet run`, for working on the code, and in a container,
which is the deliverable. Every cloud's install file repeats these headings.

## Prerequisites

- **.NET SDK 10.0** (10.0.402 or later in the 10.0 line). Check with `dotnet --list-sdks`.
- **Docker** (Docker Desktop on Windows or macOS, Docker Engine on Linux) that builds `linux/amd64` images. Check
  with `docker version`: both a client and a server should answer.
- **curl**. In Windows PowerShell 5.1, `curl` is an alias for `Invoke-WebRequest`, so call `curl.exe`, or run the
  commands below from Git Bash.

The commands below are written for a bash shell (Git Bash on Windows works) and run from the repository root.

## One-time setup

```bash
git clone https://github.com/mattonweb/Multi-Cloud-DotNet-Container.git
```

```bash
cd Multi-Cloud-DotNet-Container
```

There is nothing else to set up: no database to install, no secret to create, no account to sign in to. The Notes
database is created in memory each time the application starts.

## Build

Build and test the solution. Expect 0 warnings, 0 errors and every test passing; warnings are errors in this
solution.

```bash
dotnet build "src/Multi-Cloud Container Solution.slnx"
```

```bash
dotnet test "src/Multi-Cloud Container Solution.slnx"
```

Build the image. The tests do not run inside the image build.

```bash
docker build --platform linux/amd64 --tag multi-cloud-dotnet-container:local .
```

## Run

### With dotnet run

```bash
Heartbeat__WaitInSeconds=15 dotnet run --project src/MultiCloud.Application.Web
```

The application listens on <http://localhost:5080>, the launch profile's port, in the Development environment.
`Heartbeat__WaitInSeconds=15` makes a heartbeat appear every 15 seconds instead of every 10 minutes. Stop it with
Ctrl+C.

### In a container

```bash
docker run --detach --name multi-cloud --publish 8080:8080 --env Heartbeat__WaitInSeconds=15 multi-cloud-dotnet-container:local
```

The application listens on <http://localhost:8080> in the Production environment, as the image's non-root user.

### Settings

Every setting has its default in `src/MultiCloud.Application.Web/appsettings.json` and is overridden by an
environment variable, with `__` standing for `:`. None is a secret.

| Environment variable | Default | Meaning |
|---|---|---|
| `Heartbeat__WaitInSeconds` | `600` | Seconds between `Running` heartbeats; 1 or more |
| `Diagnostics__ErrorRoutesEnabled` | `true` | Whether the two error routes throw, or answer 404 |
| `Host__ShutdownTimeoutInSeconds` | `8` | How long the host waits for work to finish on shutdown |
| `Notes__DatabaseName` | `Notes` | Name of the in-memory database |
| `Serilog__MinimumLevel__Default` | `Information` | The lowest log level written; any `Serilog` setting works the same way |
| `PORT` | not set | The port to listen on, when a platform says which |

A setting that is missing or out of range stops the application at startup, with a log line that names the setting.

## Verify

Run these against the container. The same calls work against `dotnet run` with `BASE=http://localhost:5080`.

```bash
BASE=http://localhost:8080
```

1. **The process is not root.** Expect `uid=1654(app)`.

   ```bash
   docker exec multi-cloud id
   ```

2. **Liveness.** Expect `200`, the body exactly `up`, and the headers `X-Session-Id` and `X-Platform`.

   ```bash
   curl -i $BASE/healthz/ping
   ```

3. **Readiness.** Expect `200` and `ready`.

   ```bash
   curl -i $BASE/healthz/ready
   ```

4. **The session, the heartbeat and the header are one run.** The first heartbeat is one JSON object whose
   `HeartbeatKind` is `Started`, and its `service.instance.id` equals the `X-Session-Id` header from step 2.

   ```bash
   docker logs multi-cloud | grep '"HeartbeatKind":"Started"'
   ```

5. **Running heartbeats.** After 30 seconds, expect `Running` heartbeats numbered 1 and 2, about 15 seconds apart. A
   gap in the numbers for one session is a dropped log line.

   ```bash
   docker logs multi-cloud | grep '"HeartbeatKind"'
   ```

6. **A polite stop, then a new run.** The old logs end with a `Stopping` heartbeat. The new container has a different
   `X-Session-Id`.

   ```bash
   docker stop multi-cloud && docker logs multi-cloud | tail -n 3
   ```

   ```bash
   docker rm multi-cloud && docker run --detach --name multi-cloud --publish 8080:8080 --env Heartbeat__WaitInSeconds=15 multi-cloud-dotnet-container:local
   ```

7. **The error routes.** Expect `500` with problem details and an error log line holding the exception, then `403`
   with problem details and a warning log line. Both responses carry `X-Session-Id`, and both bodies carry
   `sessionId`: a 403 or 500 without them did not come from the application.

   ```bash
   curl -i $BASE/healthz/error/500
   ```

   ```bash
   curl -i $BASE/healthz/error/403
   ```

   ```bash
   docker logs multi-cloud | grep -E '"@l":"(Error|Warning)"'
   ```

8. **The error routes turned off.** Restart with the setting off. Expect `404` for both and no error or warning log
   line.

   ```bash
   docker rm --force multi-cloud && docker run --detach --name multi-cloud --publish 8080:8080 --env Diagnostics__ErrorRoutesEnabled=false multi-cloud-dotnet-container:local
   ```

   ```bash
   curl -s -o /dev/null -w '%{http_code}\n' $BASE/healthz/error/500 && curl -s -o /dev/null -w '%{http_code}\n' $BASE/healthz/error/403
   ```

9. **Every verb.** Expect `201` with a `Location` header, then `200`, `200`, `200`, `200`, `204` with an `Allow`
   header, `204`, and finally `404` for the deleted note.

   ```bash
   curl -i -X POST $BASE/api/notes -H 'Content-Type: application/json' -d '{"text":"Sent through every verb.","done":false}'
   ```

   ```bash
   curl -i $BASE/api/notes/4
   ```

   ```bash
   curl -I $BASE/api/notes/4
   ```

   ```bash
   curl -i -X PUT $BASE/api/notes/4 -H 'Content-Type: application/json' -d '{"text":"Replaced.","done":true}'
   ```

   ```bash
   curl -i -X PATCH $BASE/api/notes/4 -H 'Content-Type: application/json' -d '{"done":false}'
   ```

   ```bash
   curl -i -X OPTIONS $BASE/api/notes
   ```

   ```bash
   curl -i -X DELETE $BASE/api/notes/4
   ```

   ```bash
   curl -i $BASE/api/notes/4
   ```

   An invalid note is answered `400` with problem details that name the field:

   ```bash
   curl -i -X POST $BASE/api/notes -H 'Content-Type: application/json' -d '{"text":""}'
   ```

10. **The request log.** Expect one line for each notes and error request, and none for `/healthz/ping` or
    `/healthz/ready`.

    ```bash
    docker logs multi-cloud | grep '"@m":"HTTP '
    ```

11. **Where it thinks it is running.** Open <http://localhost:8080/> in a browser: the platform is "Container,
    platform not identified". Under `dotnet run`, <http://localhost:5080/> says "Local process".

12. **Forwarded headers.** Send the headers a platform's proxy would add. Expect the home page to show the scheme
    `https` and the remote address `203.0.113.7`.

    ```bash
    curl -s -H 'X-Forwarded-Proto: https' -H 'X-Forwarded-For: 203.0.113.7' $BASE/ | grep -A1 -E '<dt>(Scheme|Remote address)</dt>'
    ```

The notes live inside one process. With two instances behind a load balancer, a note created on one is absent on
the other, and a restart empties them; the `X-Session-Id` header on each response shows which instance answered.

## Tear down

```bash
docker rm --force multi-cloud
```

```bash
docker image rm multi-cloud-dotnet-container:local
```

Stop `dotnet run` with Ctrl+C. Nothing was written to disk, so there is nothing else to remove.

## What is different here

- **The platform** is "Local process" under `dotnet run` and "Container, platform not identified" in Docker. Neither
  has a cloud, so the log lines carry no `cloud.provider`, `cloud.platform` or `cloud.region`.
- **No proxy is in front.** The image honors forwarded headers, but nothing sends them unless you do, as in step 12.
  The scheme is `http`, and the remote address in Docker is the Docker network's gateway, not your machine.
- **The port** is 5080 under `dotnet run`, from the launch profile, and 8080 in the container, the base image's
  default. `PORT` is not set. Under `dotnet run`, the launch profile's address wins over `PORT`; run with
  `--no-launch-profile` to try `PORT` locally.
- **The environment** is Development under `dotnet run` and Production in the container. Nothing in the code behaves
  differently between them; only the `deployment.environment.name` on each log line changes.
- **The logs** go to the console: the terminal under `dotnet run`, `docker logs` in the container.
