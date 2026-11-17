# Install: Google Cloud Run

Deploys the container image described in [INSTALL-local-docker.md](INSTALL-local-docker.md), unchanged, to Google Cloud Run. The image is built in Cloud Shell, pushed to Artifact Registry, and deployed manually.

## Before you begin

These instructions use placeholders wherever a real value is required:

- `<your-project-id>` — Your Google Cloud project ID.
- `<url>` — The deployed service's URL.

Replace each placeholder with the actual value when following the instructions. Do not commit real project identifiers or service URLs to this repository.

## Prerequisites

- **A Google Cloud project with billing enabled.** A free trial is sufficient.
- **Google Cloud Shell.** Open it using the terminal icon in the Google Cloud console. It includes `gcloud`, Docker, and Git, authenticated as your Google account. It also builds `linux/amd64` images natively, so nothing needs to be installed on your local machine.
- **The source commits pushed to GitHub.** Cloud Shell clones the repository from GitHub, so the commit you intend to deploy must be available there.

**All commands in this guide run in Cloud Shell.**

Shell variables are scoped to the current Cloud Shell session. If a step uses `$IMAGE` or `$URL`, run it in the same session where that variable was defined.

## One-time setup

1. **Choose the project.**

   ```bash
   gcloud config set project <your-project-id>
   ```

2. **Turn on Cloud Run and Artifact Registry.**

   ```bash
   gcloud services enable run.googleapis.com artifactregistry.googleapis.com
   ```

3. **Create the image repository.** A project has no region; each resource chooses its own. This one's location is  permanent, and the Cloud Run service goes in the same region, so the image is never pulled across regions.  `us-central1` is in Cloud Run's lowest price tier; any region works if it is used throughout.

   ```bash
   gcloud artifacts repositories create multi-cloud --repository-format=docker --location=us-central1 --description="Multi-Cloud DotNet Container images"
   ```

## Build

1. **Get the code.** The first time, clone it. After that, run `git pull` inside the clone instead.

   ```bash
   git clone https://github.com/mattonweb/Multi-Cloud-DotNet-Container.git
   ```

   ```bash
   cd Multi-Cloud-DotNet-Container
   ```

2. **Name the image.** The project comes from `gcloud`, and the tag is the commit's short hash, so every image says which code it holds.

   ```bash
   IMAGE=us-central1-docker.pkg.dev/$(gcloud config get-value project)/multi-cloud/multi-cloud-dotnet-container:$(git rev-parse --short HEAD)
   ```

3. **Build it and push it.** The build takes about a minute.

   ```bash
   docker build --platform linux/amd64 --tag $IMAGE .
   ```

   ```bash
   docker push $IMAGE
   ```

   Cloud Shell's Docker can already push to Artifact Registry. From your own machine, run `gcloud auth configure-docker us-central1-docker.pkg.dev` once first.

## Run

```bash
gcloud run deploy multi-cloud --image $IMAGE --region us-central1 --allow-unauthenticated --set-env-vars Heartbeat__WaitInSeconds=60
```

- `--allow-unauthenticated` **makes the service public**: anyone with its address can call every route, the error routes and the notes API included. That is harmless for a placeholder whose notes live in memory, and it lets you call the service with plain `curl`. Without it, every call needs a Google sign-in token.
- `Heartbeat__WaitInSeconds=60` writes a heartbeat every minute instead of every ten. Every setting in [INSTALL-local-docker.md](INSTALL-local-docker.md) is passed the same way.
- `PORT` is not set here: Cloud Run sets it, to 8080, and the application listens on it.

The deploy names the new revision, such as `multi-cloud-00001-gdz`, and prints the service's address. To deploy a new image later, run the build steps and this command again; each deploy is a new revision.

Put the service's address in a variable, to call it with `curl`:

```bash
URL=$(gcloud run services describe multi-cloud --region us-central1 --format='value(status.url)')
```


## Publish and deploy with GitHub Actions

Two GitHub Actions workflows automate the build and deployment process. Both are started manually from the repository's **Actions** tab ([SDD.md](SDD.md), section 16).

1. **Publish image** — Builds and tests the solution, builds the container image once, and publishes it to GitHub Container Registry (`ghcr.io`) with a versioned tag, such as `2026.1111.1`.
2. **Deploy to Google Cloud Run** — Copies the published image to the Artifact Registry repository configured above and deploys it to the existing `multi-cloud` service. The image is referenced by its digest, preserving the exact image built and published by the first workflow.

The Cloud Run service must already exist, as created in the manual deployment steps above. The deployment workflow updates only the service's container image; it does not create or reconfigure the service.

> **Read the workflow files for the details.** Each one is a short YAML file with comments that explain every step:
>
> - [publish-image.yml](https://github.com/mattonweb/Multi-Cloud-DotNet-Container/blob/main/.github/workflows/publish-image.yml)
> - [deploy-gcp-cloud-run.yml](https://github.com/mattonweb/Multi-Cloud-DotNet-Container/blob/main/.github/workflows/deploy-gcp-cloud-run.yml)
>
> All of the workflows are in [.github/workflows](https://github.com/mattonweb/Multi-Cloud-DotNet-Container/tree/main/.github/workflows).
>
> The deploy workflow signs in to Google without a stored key, using Workload Identity Federation. Before it can run, your Google Cloud project needs a service account named `github-deployer`, and your GitHub repository needs two secrets: `GCP_PROJECT_ID` and `GCP_PROJECT_NUMBER`. This guide does not cover that setup.


---

## What is different here

- **The platform** is row 1 of the table in [SDD.md](SDD.md) section 5: Cloud Run sets `K_SERVICE` and `K_REVISION`, confirmed on the first run. The log lines carry `cloud.provider` `gcp` and `cloud.platform` `gcp_cloud_run`, and no `cloud.region`.
- **HTTPS ends at Google's front end.** The caller speaks HTTP/2 over HTTPS to `Google Frontend`; the container gets plain HTTP on port 8080, with `X-Forwarded-Proto` and `X-Forwarded-For`, which the image honors. The front end also adds an `x-cloud-trace-context` header to each response.
- **The port** comes from `PORT`, which Cloud Run sets.
- **The logs** go to Cloud Logging, where each JSON line becomes the entry's `jsonPayload`, its `severity` the entry's severity and its `message` the entry's summary (decision D8). Cloud Run adds its own request log beside them.
- **Instances come and go.** Cloud Run may run several at once, each with its own notes and session. It stops an instance about 15 minutes after its last request, with a polite stop signal, so the instance writes its `Stopping` heartbeat; the next request starts a new session. By default an instance has CPU only while a request is in progress, and the heartbeat kept time through the idle minutes all the same.
- **Kestrel may warn once after a start.** Kestrel, the web server inside the application, keeps a one-second timer it calls its heartbeat; it is not the application's heartbeat. While Cloud Run withholds CPU between startup and the first request, that timer runs late, and Kestrel logs a `WARNING` that it "has been running for" a few seconds, "longer than 00:00:01", and mentions thread pool starvation. It is harmless. Cloud Run's startup CPU boost, or CPU always allocated, would remove it, at a cost.
- **The service is public**, through `--allow-unauthenticated`.
