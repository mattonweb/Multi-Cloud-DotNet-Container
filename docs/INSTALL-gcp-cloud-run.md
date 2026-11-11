# Install: Google Cloud Run

Runs the image from [INSTALL-local.md](INSTALL-local.md), unchanged, on Google Cloud Run: built in Cloud Shell,
pushed to Artifact Registry and deployed by hand. First run on 2026-11-11, with the image built from commit `df0dee9`.

Wherever a real value goes, these steps show a placeholder: `<your-project-id>` for the project, `<url>` for the
service's address. Neither belongs in this repository.

## Prerequisites

- **A Google Cloud project with billing.** A free trial works.
- **Cloud Shell**, the terminal button at the top right of the Google Cloud console. It already has `gcloud`, Docker
  and `git`, signed in as you, and it builds `linux/amd64` images natively. Nothing is installed on your machine.
- **The commits you want to run, pushed to `main` on GitHub.** Cloud Shell clones the repository from there.

Every command below runs in Cloud Shell. A shell variable lasts only as long as the Cloud Shell session, so a step that
uses `$IMAGE` or `$URL` runs in the same session as the step that set it.

## One-time setup

1. **Choose the project.**

   ```bash
   gcloud config set project <your-project-id>
   ```

2. **Turn on Cloud Run and Artifact Registry.**

   ```bash
   gcloud services enable run.googleapis.com artifactregistry.googleapis.com
   ```

3. **Create the image repository.** A project has no region; each resource chooses its own. This one's location is
   permanent, and the Cloud Run service goes in the same region, so the image is never pulled across regions.
   `us-central1` is in Cloud Run's lowest price tier; any region works if it is used throughout.

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

2. **Name the image.** The project comes from `gcloud`, and the tag is the commit's short hash, so every image says
   which code it holds.

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

   Cloud Shell's Docker can already push to Artifact Registry. From your own machine, run
   `gcloud auth configure-docker us-central1-docker.pkg.dev` once first.

## Run

```bash
gcloud run deploy multi-cloud --image $IMAGE --region us-central1 --allow-unauthenticated --set-env-vars Heartbeat__WaitInSeconds=60
```

- `--allow-unauthenticated` **makes the service public**: anyone with its address can call every route, the error
  routes and the notes API included. That is harmless for a placeholder whose notes live in memory, and it lets the
  checks below use plain `curl`. Without it, every call needs a Google sign-in token.
- `Heartbeat__WaitInSeconds=60` writes a heartbeat every minute instead of every ten. Every setting in
  [INSTALL-local.md](INSTALL-local.md) is passed the same way.
- `PORT` is not set here: Cloud Run sets it, to 8080, and the application listens on it.

The deploy names the new revision, such as `multi-cloud-00001-gdz`, and prints the service's address. To deploy a new
image later, run the build steps and this command again; each deploy is a new revision.

Put the address in a variable for the checks:

```bash
URL=$(gcloud run services describe multi-cloud --region us-central1 --format='value(status.url)')
```

## Verify

1. **Where it thinks it is running.** Expect `platform.name` "Google Cloud Run", `cloudProvider` `gcp`,
   `cloudPlatform` `gcp_cloud_run`, `service` `multi-cloud` and `revision` the deployed revision. `region` and
   `instance` are `null`: Cloud Run supplies neither in an environment variable. `request.scheme` is `https` and
   `request.remoteAddress` is your public address, which shows that the forwarded headers from Google's front end are
   honored.

   ```bash
   curl -s $URL/
   ```

2. **Liveness.** Expect `200`, `text/plain`, the body `up`, `x-session-id`, `x-platform: Google Cloud Run`, and
   `server: Google Frontend`, which shows the request came through Google's front end.

   ```bash
   curl -i $URL/healthz/ping
   ```

3. **Errors.** Expect `500`, `403` and `404`, each `application/problem+json` with `x-session-id`: the front end
   passes the application's own error bodies through untouched.

   ```bash
   curl -i $URL/healthz/error/500
   ```

   ```bash
   curl -i $URL/healthz/error/403
   ```

   ```bash
   curl -i $URL/nowhere
   ```

4. **Every verb.** Paste as one block. Expect `201 200 200 200 200 204 204 404`.

   ```bash
   J='Content-Type: application/json'
   curl -s -o /dev/null -w 'POST    %{http_code}\n' -X POST $URL/api/notes -H "$J" -d '{"text":"Sent through every verb.","done":false}'
   curl -s -o /dev/null -w 'GET     %{http_code}\n' $URL/api/notes/4
   curl -s -o /dev/null -w 'HEAD    %{http_code}\n' -I $URL/api/notes/4
   curl -s -o /dev/null -w 'PUT     %{http_code}\n' -X PUT $URL/api/notes/4 -H "$J" -d '{"text":"Replaced.","done":true}'
   curl -s -o /dev/null -w 'PATCH   %{http_code}\n' -X PATCH $URL/api/notes/4 -H "$J" -d '{"done":false}'
   curl -s -o /dev/null -w 'OPTIONS %{http_code}\n' -X OPTIONS $URL/api/notes
   curl -s -o /dev/null -w 'DELETE  %{http_code}\n' -X DELETE $URL/api/notes/4
   curl -s -o /dev/null -w 'GET     %{http_code}\n' $URL/api/notes/4
   ```

   A `404` part-way through means Cloud Run started a second instance and the note lives in the other one; each
   instance has its own notes, and the `x-session-id` header shows which answered.

5. **The logs in Cloud Logging.** Give new lines a few seconds to arrive. The application's own lines carry a
   `SourceContext`; the lines without one are Cloud Run's request log, whose severity comes from the status code.

   ```bash
   gcloud logging read 'resource.type="cloud_run_revision" AND resource.labels.service_name="multi-cloud"' --limit 25 --format='table(timestamp, severity, jsonPayload.SourceContext, jsonPayload.HeartbeatKind, jsonPayload.HeartbeatSequence, jsonPayload.StatusCode)'
   ```

   **Known on 2026-11-11:** the application's lines arrive, but with no severity, its errors and warnings included;
   Cloud Logging reads a line's level from a `severity` field, and the application writes it as `@l`. This is open
   decision D8 in [SDD.md](SDD.md) section 7.

6. **Heartbeats.** Expect `Running` lines one minute apart, numbered in sequence, even while no request is in
   progress. On the first run they kept time over two idle minutes.

   ```bash
   gcloud logging read 'resource.type="cloud_run_revision" AND resource.labels.service_name="multi-cloud" AND jsonPayload.HeartbeatKind:*' --limit 30 --format='table(timestamp, labels.instanceId, jsonPayload.HeartbeatKind, jsonPayload.HeartbeatSequence)'
   ```

## Tear down

Delete the service, then the repository and every image in it. Nothing else was created.

```bash
gcloud run services delete multi-cloud --region us-central1
```

```bash
gcloud artifacts repositories delete multi-cloud --location us-central1
```

## What is different here

- **The platform** is row 1 of the table in [SDD.md](SDD.md) section 5: Cloud Run sets `K_SERVICE` and `K_REVISION`,
  confirmed on the first run. The log lines carry `cloud.provider` `gcp` and `cloud.platform` `gcp_cloud_run`, and no
  `cloud.region`.
- **HTTPS ends at Google's front end.** The caller speaks HTTP/2 over HTTPS to `Google Frontend`; the container gets
  plain HTTP on port 8080, with `X-Forwarded-Proto` and `X-Forwarded-For`, which the image honors. The front end also
  adds an `x-cloud-trace-context` header to each response.
- **The port** comes from `PORT`, which Cloud Run sets.
- **The logs** go to Cloud Logging, where each JSON line becomes the entry's `jsonPayload`. They carry no severity yet
  (decision D8).
- **Instances come and go.** Cloud Run may run several at once, each with its own notes and session, and stops an idle
  one. By default an instance has CPU only while a request is in progress; whether the heartbeat keeps time over a long
  idle stretch, and whether a stopped instance writes its `Stopping` line, is still being observed.
- **The service is public**, through `--allow-unauthenticated`.
