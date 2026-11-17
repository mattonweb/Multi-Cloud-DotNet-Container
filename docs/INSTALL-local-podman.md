# Install: local, with Podman

Builds and runs the same image as [INSTALL-local-docker.md](INSTALL-local-docker.md), with Podman in place of Docker, and records what differs. Nothing in this repository changes for Podman: the same `Dockerfile`, the same `.dockerignore`, the same code.

**These steps have not been run yet.** They were written on 2026-11-16 from Podman's documentation and from reading this repository's files; Podman was not installed on the machine that wrote them. The first run confirms them, and corrects this file and section 14 of [SDD.md](SDD.md) where it differs.

## The image definition, the image and the container

Three things are often called by one name. Only the first is a text file.

| | What it is | In this repository |
|---|---|---|
| Image definition | A text file of build instructions: which base image to start from, what to copy in, what to run | `Dockerfile`, with `.dockerignore` beside it |
| Image | What a build produces from the definition: the application's files as compressed layers, a configuration document (user, port, environment variables, entry point) and a manifest that lists them. A digest, such as `sha256:1632bb0b…`, names it | Not in the repository. It is in the tool's own storage after a build, and in a registry after a push |
| Container | A running process, started from an image and isolated from the rest of the machine | `multi-cloud`, while it runs |

The image format and the way registries exchange images are open standards, from the Open Container Initiative (OCI). That is why two tools can build from one definition, and why Cloud Run, Kubernetes and ECS, none of which runs Docker or Podman, can all run the result. Docker and Podman are two tools for the same definition and the same kind of image.

## Prerequisites

- **.NET SDK 10.0** and **curl**, as in [INSTALL-local-docker.md](INSTALL-local-docker.md).
- **Podman** that builds `linux/amd64` images. On Windows and macOS, Podman Desktop installs the Podman engine and creates the Linux virtual machine it runs in, the "Podman machine"; on Linux, the `podman` package needs no virtual machine. Check with `podman version`: both a client and a server should answer.
- **On Windows**: WSL 2 turned on, administrator rights for the setup, and 6 GB of memory for the Podman machine (Podman Desktop's documentation). Podman Desktop's own page gives the install command:

  ```bash
  winget install RedHat.Podman-Desktop
  ```

  Without Podman Desktop, the Podman installer alone needs two commands before the first build:

  ```bash
  podman machine init
  ```

  ```bash
  podman machine start
  ```

The commands below are written for a bash shell (Git Bash on Windows works) and run from the repository root.

## One-time setup

The clone, as in [INSTALL-local-docker.md](INSTALL-local-docker.md). There is nothing else to set up.

## Build

Build and test the solution first; that part does not involve a container tool.

```bash
dotnet build "src/Multi-Cloud Container Solution.slnx"
```

```bash
dotnet test "src/Multi-Cloud Container Solution.slnx"
```

Build the image from the same `Dockerfile`. The command differs from Docker's by one word.

```bash
podman build --platform linux/amd64 --tag multi-cloud-dotnet-container:local .
```

Podman stores an image whose tag names no registry as `localhost/multi-cloud-dotnet-container:local`. Expect the short name to work in the commands below; `podman images` shows the full one.

## Run

```bash
podman run --detach --name multi-cloud --publish 8080:8080 --env Heartbeat__WaitInSeconds=15 multi-cloud-dotnet-container:local
```

The application listens on <http://localhost:8080> in the Production environment, as the image's non-root user. The settings table in [INSTALL-local-docker.md](INSTALL-local-docker.md) applies unchanged.

## What is different here

**Nothing in the image definition changes.** File by file:

| File | Change under Podman |
|---|---|
| `src/**`, all the C# | None. The application decides it is in a container from `DOTNET_RUNNING_IN_CONTAINER`, which Microsoft's base image sets, not Docker |
| `Dockerfile` | None. Podman builds a file named `Dockerfile` as it is. Its own name for the file is `Containerfile`; it reads either, so there is nothing to rename |
| `.dockerignore` | None. Podman reads `.containerignore`, or `.dockerignore` when there is no `.containerignore`, with the same syntax |

Why this `Dockerfile` needs no change:

- **Both base images are named in full**, `mcr.microsoft.com/dotnet/sdk` and `mcr.microsoft.com/dotnet/aspnet`. Docker completes a name that has no registry, such as `ubuntu`, with Docker Hub. Podman completes it from its own configuration, and depending on that configuration may ask which registry, or refuse. A full name means the same thing to both.
- **It uses only instructions both image formats store**: `FROM`, `WORKDIR`, `COPY`, `RUN`, `ENV`, `USER`, `EXPOSE` and `ENTRYPOINT`. Podman builds in the OCI image format by default, and that format has no place for `HEALTHCHECK` or `ONBUILD`; Podman warns and leaves them out unless the build is run with `--format docker`. This image has neither: each platform's own probe checks `/healthz/ready`.
- **It uses nothing particular to Docker's builder**: no `# syntax=` line and no `RUN --mount`. Podman builds with Buildah, not Docker's BuildKit.
- **It already runs as a non-root user** and listens on a port above 1023, so it needs nothing from a root daemon.

What differs is around the image, not in it:

- **There is no daemon.** `podman build` and `podman run` do the work themselves, as you, and the container is a child of that command. Docker's client asks a background service that runs as root. On Windows and macOS both tools still run Linux containers inside a Linux virtual machine.
- **The image format on disk is OCI, not Docker's.** The same source built by each tool has a different digest. That changes nothing here: no file in this repository holds a digest the build must match, and every build has a new digest anyway.
- **The local image's name gains `localhost/`**, as the Build section says.
- **The remote address the application sees** is expected to differ. Under Docker it is the Docker network's gateway, and Podman's network path is not Docker's.
- **The workflows are out of scope here.** They copy the image between registries with `docker buildx imagetools`, which Podman does not have. Building and testing locally with Podman does not touch them.

## Choosing between them

For building and testing this image on one machine, either works, and the choice changes no file here.

- **License.** Docker Desktop is free for personal use, education, non-commercial open source and small businesses, which Docker defines as fewer than 250 employees and less than $10 million in annual revenue; a larger organization needs a paid subscription for each user (Docker's license page, read 2026-11-16; check it before relying on it). Podman and Podman Desktop are open source under the Apache License 2.0, with no such terms.
- **Security posture.** Podman runs without a root daemon by default. Docker can run rootless too, but it is not the default.
- **What the instructions you will find assume.** Most tutorials, and this repository's other files, are written for `docker`. The commands used here are the same word for word after the first, and Podman can also answer Docker's API for tools that expect it.
- **What the team already runs elsewhere.** A build pipeline that uses Docker's own features, such as `docker buildx`, is a reason to keep Docker locally; a platform that already builds with Podman or Buildah is a reason to match it.
