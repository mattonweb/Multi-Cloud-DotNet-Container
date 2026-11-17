# Install: Google Kubernetes Engine

Deploys the published container image, unchanged, to Google Kubernetes Engine (GKE). The deployment uses an Autopilot cluster, the Kubernetes manifests in `deploy/gke/`, and a Google Cloud HTTP load balancer ([SDD.md](SDD.md), section 17).

The deployment can be performed manually or through GitHub Actions. Both methods use the same Kubernetes manifests and deployment steps.

## Before you begin

These instructions use placeholders wherever a real value is required:

- `<your-project-id>` — Your Google Cloud project ID.

Replace each placeholder with the actual value when following the instructions. Do not commit real project identifiers to this repository.

## Prerequisites

- **The Cloud Run guide, done first.** Follow [INSTALL-gcp-cloud-run.md](INSTALL-gcp-cloud-run.md) through its Build section. That chooses your project and creates the `multi-cloud` image repository in Artifact Registry, with one image in it. This guide deploys an image from that repository and does not build one.
- **A Google Cloud project with billing enabled.** A free trial is sufficient.
- **Google Cloud Shell.** Open it using the terminal icon in the Google Cloud console. It includes `gcloud`, Docker, and Git, authenticated as your Google account. It includes `kubectl` and the GKE authentication plugin required to connect to your cluster.

**All commands in this guide run in Cloud Shell**, from a clone of this repository. Open the repository directory with `cd Multi-Cloud-DotNet-Container` and run `git pull` to get the latest changes.

Shell variables are scoped to the current Cloud Shell session. If a step uses a variable defined earlier, run it in the same session.

**Cost.** While it runs, this setup costs about $0.75 a day: about $0.60 for the load balancer and about $0.15 for the pod. Google's free tier covers the cluster fee for one Autopilot cluster. These are rough figures from November 2026, so check Google's pricing pages for current prices.

## One-time setup

1. **Turn on GKE.**

   ```bash
   gcloud services enable container.googleapis.com
   ```

2. **Create the Autopilot cluster.** Google runs, sizes and patches the nodes. It takes five to ten minutes.

   ```bash
   gcloud container clusters create-auto multi-cloud --region=us-central1
   ```

3. **Point `kubectl` at it.**

   ```bash
   gcloud container clusters get-credentials multi-cloud --region=us-central1
   ```

4. **Let the cluster pull from the image repository.** Autopilot's nodes act as the project's default compute service account, which may already be able to; this makes sure, and grants nothing else.

   ```bash
   gcloud artifacts repositories add-iam-policy-binding multi-cloud --location=us-central1 --member="serviceAccount:$(gcloud projects describe $(gcloud config get-value project) --format='value(projectNumber)')-compute@developer.gserviceaccount.com" --role=roles/artifactregistry.reader
   ```

## Build

Nothing is built here. GKE runs an image that is already in Artifact Registry. Name that image by its tag. The Cloud Run guide's Build section pushes an image tagged with the commit's short hash, and the **Deploy to Google Cloud Run** workflow copies one tagged like `2026.1111.2`. The tag below is an example, so replace it with a tag that is in your repository:

```bash
IMAGE=us-central1-docker.pkg.dev/$(gcloud config get-value project)/multi-cloud/multi-cloud-dotnet-container:2026.1111.2
```

## Run

1. **Apply the manifests.** `envsubst` fills in `${IMAGE}`; the image's address holds the project's ID, so it is  never written into the file.

   ```bash
   kubectl apply -f deploy/gke/namespace.yaml
   ```

   ```bash
   export IMAGE && envsubst '$IMAGE' < deploy/gke/deployment.yaml | kubectl apply -f -
   ```

   ```bash
   kubectl apply -f deploy/gke/service.yaml -f deploy/gke/ingress.yaml
   ```

2. **Wait for the pod.** Autopilot may first add a node, so the first rollout can take a few minutes. Expect  `successfully rolled out`.

   ```bash
   kubectl rollout status deployment/multi-cloud -n multi-cloud --timeout=10m
   ```

3. **Wait for the load balancer.** Google creates it from the Ingress; the address appears after a few minutes, and  requests answer `404` or `502` from Google until its health check finds the pod ready, often a few minutes more.  This repeats until the address answers `up`:

   ```bash
   until IP=$(kubectl get ingress multi-cloud -n multi-cloud -o jsonpath='{.status.loadBalancer.ingress[0].ip}') && [ -n "$IP" ] && [ "$(curl -s http://$IP/healthz/ping)" = "up" ]; do echo "waiting for the load balancer..."; sleep 15; done; echo "ready at http://$IP"
   ```

The service answers on plain HTTP at that address. To deploy another image later, set `IMAGE` again and repeat the `envsubst` command; Kubernetes replaces the pod, and the old one writes its `Stopping` heartbeat.


## Deploy with GitHub Actions

Two GitHub Actions workflows automate the build and deployment process. Both are started manually from the repository's **Actions** tab ([SDD.md](SDD.md), section 16).

1. **Publish image** — Builds and tests the solution, builds the container image once, and publishes it to GitHub Container Registry (`ghcr.io`) with a versioned tag, such as `2026.1111.1`.
2. **Deploy to Google Kubernetes Engine** — Copies the published image into Artifact Registry, keeping its digest. It then applies the manifests in `deploy/gke/` with that digest, waits for the rollout, and checks through the load balancer that a new pod answers.

The cluster must already exist, as created in the one-time setup above. The workflow creates the namespace, deployment, service and Ingress if they are missing.

> **Read the workflow files for the details.** Each one is a short YAML file with comments that explain every step:
>
> - [publish-image.yml](https://github.com/mattonweb/Multi-Cloud-DotNet-Container/blob/main/.github/workflows/publish-image.yml)
> - [deploy-gcp-gke.yml](https://github.com/mattonweb/Multi-Cloud-DotNet-Container/blob/main/.github/workflows/deploy-gcp-gke.yml)
>
> All of the workflows are in [.github/workflows](https://github.com/mattonweb/Multi-Cloud-DotNet-Container/tree/main/.github/workflows).
>
> The deploy workflow signs in to Google the same way the Cloud Run workflow does. It needs the same service account, `github-deployer`, and the same two repository secrets, `GCP_PROJECT_ID` and `GCP_PROJECT_NUMBER`. The service account also needs the role `roles/container.developer` on the project. This guide does not cover that setup.


---

## What is different here

- **The platform** is row 3 of the table in [SDD.md](SDD.md) section 5: "Kubernetes", with the pod's name as the instance, and no cloud provider or platform, because nothing in a pod's environment says the cluster is Google's.
- **HTTPS is not set up.** Google's load balancer answers plain HTTP on a public address and forwards it to the pod; HTTPS would need a domain and a certificate.
- **Two proxies' entries in `X-Forwarded-For`.** Google's load balancer appends the caller's address and then its own public address, where Cloud Run's front end appends only the caller's. The first deploy, with the default of trusting one entry, reported the load balancer as the caller; the deployment now sets `ForwardedHeaders__ForwardLimit` to 2 ([SDD.md](SDD.md) decision D11), and the workflow's first run, with image `2026.1111.5`, reported the caller. The scheme is `http`, as the caller used.
- **The port** is the image's own 8080; nothing sets `PORT`.
- **Kubernetes probes the application itself**: startup and liveness on `/healthz/ping`, readiness on `/healthz/ready`, which the load balancer's health check follows. None of them appears in the request log.
- **A stopped pod gets 30 seconds**, longer than the application's 8-second shutdown, so it writes its `Stopping` heartbeat.
- **The pod runs as the image's non-root user**, with no added privileges.
