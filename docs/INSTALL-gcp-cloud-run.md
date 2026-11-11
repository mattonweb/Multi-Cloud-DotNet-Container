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

5. **The logs in Cloud Logging.** Give new lines a few seconds to arrive. This reads the application's own lines,
   which carry a `message`; Cloud Run's request log, beside them, takes its severity from the status code.

   ```bash
   gcloud logging read 'resource.type="cloud_run_revision" AND resource.labels.service_name="multi-cloud" AND jsonPayload.message:*' --limit 20 --format='table(timestamp, severity, jsonPayload.message)'
   ```

   Expect every line to carry a severity: `ERROR` for the 500's two lines, `WARNING` for the 403's refusal, `INFO`
   for the heartbeats, the startup lines and the other requests. Each line writes its level as `severity`, which
   Cloud Logging reads, and its message as `message`, which Cloud Logging shows as the entry's summary
   ([SDD.md](SDD.md) section 7, decision D8); confirmed on 2026-11-11 with image `2026.1111.2`. An image built before
   decision D8 writes `@l` instead, and its lines arrive with no severity. A `WARNING` that Kestrel's heartbeat ran
   late is Cloud Run's doing, not the application's: see What is different here.

6. **Heartbeats, and the end of an idle instance.** Expect `Running` lines one minute apart, numbered in sequence
   with no gap, even while no request is in progress. About 15 minutes after the last request, Cloud Run stops the
   idle instance; expect its last line to be `Stopping`, with the next number. On the first run, one instance wrote
   `Running` 1 to 17 and `Stopping` 18.

   ```bash
   gcloud logging read 'resource.type="cloud_run_revision" AND resource.labels.service_name="multi-cloud" AND jsonPayload.HeartbeatKind:*' --limit 30 --format='table(timestamp, labels.instanceId, jsonPayload.HeartbeatKind, jsonPayload.HeartbeatSequence)'
   ```

## Publish and deploy with GitHub Actions

Two workflows automate the steps above, and each runs only when you start it from the repository's **Actions** tab
([SDD.md](SDD.md) section 16). **Publish image** builds and tests the solution, builds the image once, and pushes it to
GitHub Container Registry (`ghcr.io`) under a tag such as `2026.1111.1`. **Deploy to Google Cloud Run** copies that
image, keeping its digest, into the Artifact Registry repository above, and deploys the digest to the `multi-cloud`
service. The service must already exist, from the Run step above; the workflow changes only its image.

### One-time setup

GitHub signs in to Google through Workload Identity Federation: Google trusts GitHub's own short-lived token for this
one repository's `main` branch, so no Google key is created or stored anywhere. The workflow then acts as a service
account that may push images to the one repository, deploy revisions of the one service, and nothing else.

1. **Put the project's ID and number in variables** for the commands below.

   ```bash
   PROJECT_ID=$(gcloud config get-value project)
   ```

   ```bash
   PROJECT_NUMBER=$(gcloud projects describe $PROJECT_ID --format='value(projectNumber)')
   ```

2. **Turn on the services Google's side of the sign-in uses.**

   ```bash
   gcloud services enable iam.googleapis.com iamcredentials.googleapis.com sts.googleapis.com
   ```

3. **Create the service account the workflow acts as.**

   ```bash
   gcloud iam service-accounts create github-deployer --display-name="GitHub Actions deployer"
   ```

4. **Give it its three permissions:** push images to the repository, deploy revisions of the service, and run them
   as the identity the service runs as. Unless the service was given its own, that identity is the project's default
   compute service account, which the third command names.

   ```bash
   gcloud artifacts repositories add-iam-policy-binding multi-cloud --location=us-central1 --member="serviceAccount:github-deployer@$PROJECT_ID.iam.gserviceaccount.com" --role=roles/artifactregistry.writer
   ```

   ```bash
   gcloud run services add-iam-policy-binding multi-cloud --region=us-central1 --member="serviceAccount:github-deployer@$PROJECT_ID.iam.gserviceaccount.com" --role=roles/run.developer
   ```

   ```bash
   gcloud iam service-accounts add-iam-policy-binding $PROJECT_NUMBER-compute@developer.gserviceaccount.com --member="serviceAccount:github-deployer@$PROJECT_ID.iam.gserviceaccount.com" --role=roles/iam.serviceAccountUser
   ```

5. **Create the identity pool and the GitHub provider.** The condition admits only this repository's `main` branch.

   ```bash
   gcloud iam workload-identity-pools create github --location=global --display-name="GitHub Actions"
   ```

   ```bash
   gcloud iam workload-identity-pools providers create-oidc github --location=global --workload-identity-pool=github --display-name="GitHub" --issuer-uri="https://token.actions.githubusercontent.com" --attribute-mapping="google.subject=assertion.sub,attribute.repository=assertion.repository,attribute.ref=assertion.ref" --attribute-condition="assertion.repository=='mattonweb/Multi-Cloud-DotNet-Container' && assertion.ref=='refs/heads/main'"
   ```

6. **Let this repository act as the service account.**

   ```bash
   gcloud iam service-accounts add-iam-policy-binding github-deployer@$PROJECT_ID.iam.gserviceaccount.com --role=roles/iam.workloadIdentityUser --member="principalSet://iam.googleapis.com/projects/$PROJECT_NUMBER/locations/global/workloadIdentityPools/github/attribute.repository/mattonweb/Multi-Cloud-DotNet-Container"
   ```

7. **Give the workflow the project's ID and number, as secrets.** In the repository on GitHub, open **Settings**,
   **Secrets and variables**, **Actions**, and add two repository secrets: `GCP_PROJECT_ID` and
   `GCP_PROJECT_NUMBER`, with the values this prints. They are secrets rather than variables so that GitHub masks them
   in the run logs, which a public repository shows to everyone.

   ```bash
   echo "GCP_PROJECT_ID=$PROJECT_ID GCP_PROJECT_NUMBER=$PROJECT_NUMBER"
   ```

### Run them

1. **Publish.** In the **Actions** tab, choose **Publish image**, then **Run workflow** on `main`. It runs the build
   and the unit tests first, then publishes. The run's summary names the tag and the digest, for example
   `Deploy it by running a deploy workflow with tag 2026.1111.1`. The first run creates the package
   `multi-cloud-dotnet-container` under your GitHub account, linked to this repository.
2. **Deploy.** Choose **Deploy to Google Cloud Run**, then **Run workflow**. Leave the tag blank to deploy the newest
   image Publish image pushed, or enter a tag to deploy that one, for instance to go back to an earlier image. The tag
   is the image's tag on `ghcr.io`, shown on the package's page; it is not a git tag. The workflow copies the image,
   checks the copy has the same digest, deploys that digest, and checks that the new revision answers `up` and reports
   "Google Cloud Run" and its own revision name. The summary names the tag, the digest and the revision.
3. **Verify** as in the Verify section above. The image is the same bytes the publish run built: the digest in the
   deploy's summary is the publish's digest.

If the deploy's copy step is refused reading from `ghcr.io`, open the package's settings on GitHub, and under
**Manage Actions access** give this repository read access.

If Publish image's push is refused with `denied`, look under **Packages** on your GitHub profile. A package named
`multi-cloud-dotnet-container` that is not linked to this repository, perhaps left by an earlier attempt, blocks the
run's token: in its settings, under **Manage Actions access**, give this repository write access, or delete the package
and run the workflow again.

## Tear down

Delete the service, then the repository and every image in it.

```bash
gcloud run services delete multi-cloud --region us-central1
```

```bash
gcloud artifacts repositories delete multi-cloud --location us-central1
```

If you set up GitHub Actions, also delete the identity pool, which Google keeps recoverable for 30 days, and the
service account; then delete the two secrets in the repository's settings, and the package on GitHub if you no
longer want the published images.

```bash
gcloud iam workload-identity-pools delete github --location=global
```

```bash
gcloud iam service-accounts delete github-deployer@<your-project-id>.iam.gserviceaccount.com
```

## What is different here

- **The platform** is row 1 of the table in [SDD.md](SDD.md) section 5: Cloud Run sets `K_SERVICE` and `K_REVISION`,
  confirmed on the first run. The log lines carry `cloud.provider` `gcp` and `cloud.platform` `gcp_cloud_run`, and no
  `cloud.region`.
- **HTTPS ends at Google's front end.** The caller speaks HTTP/2 over HTTPS to `Google Frontend`; the container gets
  plain HTTP on port 8080, with `X-Forwarded-Proto` and `X-Forwarded-For`, which the image honors. The front end also
  adds an `x-cloud-trace-context` header to each response.
- **The port** comes from `PORT`, which Cloud Run sets.
- **The logs** go to Cloud Logging, where each JSON line becomes the entry's `jsonPayload`, its `severity` the entry's
  severity and its `message` the entry's summary (decision D8). Cloud Run adds its own request log beside them.
- **Instances come and go.** Cloud Run may run several at once, each with its own notes and session. It stops an
  instance about 15 minutes after its last request, with a polite stop signal, so the instance writes its `Stopping`
  heartbeat; the next request starts a new session. By default an instance has CPU only while a request is in
  progress, and the heartbeat kept time through the idle minutes all the same.
- **Kestrel may warn once after a start.** Kestrel, the web server inside the application, keeps a one-second timer
  it calls its heartbeat; it is not the application's heartbeat. While Cloud Run withholds CPU between startup and the
  first request, that timer runs late, and Kestrel logs a `WARNING` that it "has been running for" a few seconds,
  "longer than 00:00:01", and mentions thread pool starvation. It is harmless. Cloud Run's startup CPU boost, or CPU
  always allocated, would remove it, at a cost.
- **The service is public**, through `--allow-unauthenticated`.
