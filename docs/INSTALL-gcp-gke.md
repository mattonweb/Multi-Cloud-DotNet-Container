# Install: Google Kubernetes Engine

Runs the published image, unchanged, on Google Kubernetes Engine (GKE): an Autopilot cluster, the manifests in
`deploy/gke/`, and Google's HTTP load balancer in front ([SDD.md](SDD.md) section 17). Deployed by hand, or by the
GitHub Actions workflow at the end, which runs the same steps.

Wherever a real value goes, these steps show a placeholder: `<your-project-id>` for the project. It does not belong in
this repository.

## Prerequisites

- **Everything in [INSTALL-gcp-cloud-run.md](INSTALL-gcp-cloud-run.md) up to its Run step**: the project, Cloud
  Shell, and the `multi-cloud` image repository in Artifact Registry, holding at least one image. An image tag such as
  `2026.1111.2` is there once **Deploy to Google Cloud Run** has run with it, or once you pushed it by hand.
- **Cloud Shell**, which already has `kubectl` and the sign-in plugin GKE needs.

Every command below runs in Cloud Shell, in the clone of this repository (`cd Multi-Cloud-DotNet-Container`, then
`git pull`). A shell variable lasts only as long as the Cloud Shell session.

**What it costs while it exists**, roughly: the load balancer about $0.60 a day, the pod about $0.15 a day, and
nothing for the cluster itself, whose fee GKE's free tier covers for one Autopilot cluster. Check Google's pricing
pages for current figures, and tear it down when the checks are done.

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

4. **Let the cluster pull from the image repository.** Autopilot's nodes act as the project's default compute service
   account, which may already be able to; this makes sure, and grants nothing else.

   ```bash
   gcloud artifacts repositories add-iam-policy-binding multi-cloud --location=us-central1 --member="serviceAccount:$(gcloud projects describe $(gcloud config get-value project) --format='value(projectNumber)')-compute@developer.gserviceaccount.com" --role=roles/artifactregistry.reader
   ```

## Build

Nothing is built here: GKE runs an image already in Artifact Registry, the same bytes Publish image built. Name it, by
the tag it was published under; `2026.1111.2` below is an example, so use the newest tag you have:

```bash
IMAGE=us-central1-docker.pkg.dev/$(gcloud config get-value project)/multi-cloud/multi-cloud-dotnet-container:2026.1111.2
```

## Run

1. **Apply the manifests.** `envsubst` fills in `${IMAGE}`; the image's address holds the project's ID, so it is
   never written into the file.

   ```bash
   kubectl apply -f deploy/gke/namespace.yaml
   ```

   ```bash
   export IMAGE && envsubst '$IMAGE' < deploy/gke/deployment.yaml | kubectl apply -f -
   ```

   ```bash
   kubectl apply -f deploy/gke/service.yaml -f deploy/gke/ingress.yaml
   ```

2. **Wait for the pod.** Autopilot may first add a node, so the first rollout can take a few minutes. Expect
   `successfully rolled out`.

   ```bash
   kubectl rollout status deployment/multi-cloud -n multi-cloud --timeout=10m
   ```

3. **Wait for the load balancer.** Google creates it from the Ingress; the address appears after a few minutes, and
   requests answer `404` or `502` from Google until its health check finds the pod ready, often a few minutes more.
   This repeats until the address answers `up`:

   ```bash
   until IP=$(kubectl get ingress multi-cloud -n multi-cloud -o jsonpath='{.status.loadBalancer.ingress[0].ip}') && [ -n "$IP" ] && [ "$(curl -s http://$IP/healthz/ping)" = "up" ]; do echo "waiting for the load balancer..."; sleep 15; done; echo "ready at http://$IP"
   ```

The service answers on plain HTTP at that address. To deploy another image later, set `IMAGE` again and repeat the
`envsubst` command; Kubernetes replaces the pod, and the old one writes its `Stopping` heartbeat.

## Verify

Every check uses `$IP`, set by Run step 3.

1. **Where it thinks it is running.** Expect `platform.name` "Kubernetes", `instance` the pod's name, and
   `cloudProvider` and `cloudPlatform` `null`: a cluster announces itself the same way whoever runs it, so the
   application cannot tell GKE from any other Kubernetes ([SDD.md](SDD.md) section 5). `request.scheme` is `http`.
   `request.remoteAddress` should be your public address, here Cloud Shell's. Google's load balancer appends two
   entries to `X-Forwarded-For`, your address and then its own, and the deployment sets
   `ForwardedHeaders__ForwardLimit` to 2 to trust both ([SDD.md](SDD.md) decision D11). If it shows the load
   balancer's own address, the same as `$IP`, the image predates that setting: deploy a newer one.

   ```bash
   curl -s http://$IP/
   ```

2. **Liveness through the load balancer.** Expect `200`, the body `up`, `x-session-id`, and `x-platform: Kubernetes`.
   A `via: 1.1 google` header shows the request came through Google's load balancer.

   ```bash
   curl -i http://$IP/healthz/ping
   ```

3. **Errors.** Expect `500`, `403` and `404`, each `application/problem+json` with `x-session-id`.

   ```bash
   curl -i http://$IP/healthz/error/500
   ```

   ```bash
   curl -i http://$IP/healthz/error/403
   ```

   ```bash
   curl -i http://$IP/nowhere
   ```

4. **Every verb.** Paste as one block. Expect `201 200 200 200 200 204 204 404`.

   ```bash
   J='Content-Type: application/json'
   curl -s -o /dev/null -w 'POST    %{http_code}\n' -X POST http://$IP/api/notes -H "$J" -d '{"text":"Sent through every verb.","done":false}'
   curl -s -o /dev/null -w 'GET     %{http_code}\n' http://$IP/api/notes/4
   curl -s -o /dev/null -w 'HEAD    %{http_code}\n' -I http://$IP/api/notes/4
   curl -s -o /dev/null -w 'PUT     %{http_code}\n' -X PUT http://$IP/api/notes/4 -H "$J" -d '{"text":"Replaced.","done":true}'
   curl -s -o /dev/null -w 'PATCH   %{http_code}\n' -X PATCH http://$IP/api/notes/4 -H "$J" -d '{"done":false}'
   curl -s -o /dev/null -w 'OPTIONS %{http_code}\n' -X OPTIONS http://$IP/api/notes
   curl -s -o /dev/null -w 'DELETE  %{http_code}\n' -X DELETE http://$IP/api/notes/4
   curl -s -o /dev/null -w 'GET     %{http_code}\n' http://$IP/api/notes/4
   ```

5. **The logs in Cloud Logging.** GKE sends the pod's console to Cloud Logging, as Cloud Run does. Expect the same
   severities as on Cloud Run: `ERROR` for the 500's two lines, `WARNING` for the 403's refusal, `INFO` for the rest.
   The load balancer's health checks call `/healthz/ready` every few seconds; expect none of them in the request log.

   ```bash
   gcloud logging read 'resource.type="k8s_container" AND resource.labels.namespace_name="multi-cloud" AND jsonPayload.message:*' --limit 20 --format='table(timestamp, severity, jsonPayload.message)'
   ```

6. **Heartbeats, and a polite stop.** Restart the deployment: Kubernetes starts a new pod, sends the old one a stop
   signal, and waits up to 30 seconds. Expect the old pod's last heartbeat to be `Stopping`, and the new pod's first
   `Started` with a new session.

   ```bash
   kubectl rollout restart deployment/multi-cloud -n multi-cloud && kubectl rollout status deployment/multi-cloud -n multi-cloud
   ```

   ```bash
   gcloud logging read 'resource.type="k8s_container" AND resource.labels.namespace_name="multi-cloud" AND jsonPayload.HeartbeatKind:*' --limit 10 --format='table(timestamp, resource.labels.pod_name, jsonPayload.HeartbeatKind, jsonPayload.HeartbeatSequence)'
   ```

## Deploy with GitHub Actions

**Deploy to Google Kubernetes Engine** automates the Run steps above, and runs only when you start it from the
repository's **Actions** tab ([SDD.md](SDD.md) section 16). It copies a published image, keeping its digest, into
Artifact Registry, applies the manifests in `deploy/gke/` with that digest, waits for the rollout, and checks through
the load balancer that a new pod answers. The cluster must already exist, from the One-time setup above; the workflow
creates the namespace, deployment, service and Ingress if they are missing.

### One-time setup

The workflow signs in exactly as **Deploy to Google Cloud Run** does, so first finish the one-time setup under
"Publish and deploy with GitHub Actions" in [INSTALL-gcp-cloud-run.md](INSTALL-gcp-cloud-run.md): the
`github-deployer` service account, the identity pool and the two repository secrets. Then give the service account
one more role, which lets it change Kubernetes objects in the project's clusters, here only `multi-cloud`:

```bash
PROJECT_ID=$(gcloud config get-value project)
```

```bash
gcloud projects add-iam-policy-binding $PROJECT_ID --member="serviceAccount:github-deployer@$PROJECT_ID.iam.gserviceaccount.com" --role=roles/container.developer --condition=None
```

### Run it

1. **Publish**, if the image you want is not on `ghcr.io` yet: **Publish image**, then **Run workflow** on `main`.
2. **Deploy.** Choose **Deploy to Google Kubernetes Engine**, then **Run workflow**. Leave the tag blank to deploy the
   newest image Publish image pushed, or enter a tag to deploy that one. The workflow checks that every new pod runs
   the copied digest, then waits for a new pod to answer through the load balancer: `up`, the platform "Kubernetes",
   and the caller's address, the runner's, rather than the load balancer's. With the cluster and the Ingress already
   in place, a run takes about a minute; the first run after the Ingress is created waits several minutes more for
   the load balancer. The summary names the tag, the digest, the deployment's revision and the pod.
3. **Verify** as in the Verify section above, after setting `IP` in Cloud Shell with Run step 3.

## Tear down

Delete the namespace first: it removes the Ingress, and with it the load balancer, before the cluster goes. Then the
cluster. The image repository stays for Cloud Run.

```bash
kubectl delete namespace multi-cloud
```

```bash
gcloud container clusters delete multi-cloud --region=us-central1
```

If you set up the workflow and keep the service account for Cloud Run, take back its GKE role:

```bash
gcloud projects remove-iam-policy-binding <your-project-id> --member="serviceAccount:github-deployer@<your-project-id>.iam.gserviceaccount.com" --role=roles/container.developer --condition=None
```

## What is different here

- **The platform** is row 3 of the table in [SDD.md](SDD.md) section 5: "Kubernetes", with the pod's name as the
  instance, and no cloud provider or platform, because nothing in a pod's environment says the cluster is Google's.
- **HTTPS is not set up.** Google's load balancer answers plain HTTP on a public address and forwards it to the pod;
  HTTPS would need a domain and a certificate.
- **Two proxies' entries in `X-Forwarded-For`.** Google's load balancer appends the caller's address and then its
  own public address, where Cloud Run's front end appends only the caller's. The first deploy, with the default of
  trusting one entry, reported the load balancer as the caller; the deployment now sets
  `ForwardedHeaders__ForwardLimit` to 2 ([SDD.md](SDD.md) decision D11), and the workflow's first run, with image
  `2026.1111.5`, reported the caller. The scheme is `http`, as the caller used.
- **The port** is the image's own 8080; nothing sets `PORT`.
- **Kubernetes probes the application itself**: startup and liveness on `/healthz/ping`, readiness on
  `/healthz/ready`, which the load balancer's health check follows. None of them appears in the request log.
- **A stopped pod gets 30 seconds**, longer than the application's 8-second shutdown, so it writes its `Stopping`
  heartbeat.
- **The pod runs as the image's non-root user**, with no added privileges.
