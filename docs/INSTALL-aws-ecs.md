# Install: AWS ECS on Fargate

Runs the published image, unchanged, on Amazon ECS with AWS Fargate: one task, the task definition in
`deploy/aws-ecs/`, and an Application Load Balancer in front ([SDD.md](SDD.md) section 18). Deployed by hand; a deploy
workflow follows once these steps pass.

Wherever a real value goes, these steps show a placeholder: `<your-account-id>` for the AWS account. It does not
belong in this repository. The commands find it for themselves, so you never type it.

## Prerequisites

- **An AWS account.** These steps were written for one made with AWS's "new AWS experience", which signs you in with
  an AWS Builder ID instead of a root user; turn on multi-factor sign-in in **AWS Settings**. In **Billing and Cost
  Management**, **Budgets**, a monthly cost budget of, say, $10 with an email alert is a cheap guard rail. The steps
  use only services the new experience's Free Tier includes: ECS, ECR, Elastic Load Balancing, VPC, IAM, CloudWatch
  Logs and CloudShell.
- **The region `us-east-2` (US East, Ohio).** The new experience chooses a project's region from its address and does
  not change it; in the United States that is `us-east-2`, the design's choice. An account made the advanced way
  chooses it in the console's region menu at the top right.
- **AWS CloudShell**, the terminal icon in the console's top bar. It has the AWS CLI, `git` and Docker, signed in as
  you. Your home directory is kept between sessions; everything else, shell variables included, starts fresh.
- **An image on `ghcr.io`** that **Publish image** pushed, such as `2026.1111.5`. The package is public, so CloudShell
  copies it without signing in to GitHub.

Every command below runs in CloudShell, in a clone of this repository. The first time:

```bash
git clone https://github.com/mattonweb/Multi-Cloud-DotNet-Container.git && cd Multi-Cloud-DotNet-Container
```

After that, `cd Multi-Cloud-DotNet-Container`, then `git pull`.

**What it costs while it exists**, roughly: the load balancer about $0.55 a day, the task about $0.30 a day, and
three public IPv4 addresses (two for the load balancer, one for the task) about $0.36 a day. The logs and the stored
image cost cents. Check AWS's pricing pages for current figures, and tear it down when the checks are done.

## Every CloudShell session

These name the region, the account's registry, the default VPC and two of its subnets. They only look things up.
Paste as one block at the start of each session:

```bash
export AWS_REGION=us-east-2
ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
REGISTRY=$ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com
VPC_ID=$(aws ec2 describe-vpcs --filters Name=is-default,Values=true --query 'Vpcs[0].VpcId' --output text)
SUBNETS=$(aws ec2 describe-subnets --filters Name=vpc-id,Values=$VPC_ID Name=default-for-az,Values=true --query "join(',', sort_by(Subnets, &AvailabilityZone)[:2].SubnetId)" --output text)
echo "VPC $VPC_ID, subnets $SUBNETS"
```

Expect a `vpc-` identifier and two `subnet-` identifiers. If it prints `None`, the account has no default VPC, which
these steps rely on: stop there. Once the one-time setup below is done, a new session also needs the names of what it
created. Paste as one block:

```bash
ALB_SG=$(aws ec2 describe-security-groups --filters Name=vpc-id,Values=$VPC_ID Name=group-name,Values=multi-cloud-alb --query 'SecurityGroups[0].GroupId' --output text)
TASK_SG=$(aws ec2 describe-security-groups --filters Name=vpc-id,Values=$VPC_ID Name=group-name,Values=multi-cloud-task --query 'SecurityGroups[0].GroupId' --output text)
ALB_ARN=$(aws elbv2 describe-load-balancers --names multi-cloud --query 'LoadBalancers[0].LoadBalancerArn' --output text)
TG_ARN=$(aws elbv2 describe-target-groups --names multi-cloud --query 'TargetGroups[0].TargetGroupArn' --output text)
URL=http://$(aws elbv2 describe-load-balancers --names multi-cloud --query 'LoadBalancers[0].DNSName' --output text)
echo "security groups $ALB_SG $TASK_SG, address $URL"
```

## One-time setup

1. **Make sure Docker can copy an image by digest.** The copy uses `docker buildx imagetools`, as the workflows do.
   Check what CloudShell has; expect `x86_64`, then a buildx version:

   ```bash
   uname -m
   ```

   ```bash
   docker buildx version
   ```

   If the second answers that `buildx` is not a docker command, add the plugin to your home directory, which
   CloudShell keeps. It is pinned to buildx v0.38.1 and checked against the checksum that release publishes; expect
   `docker-buildx: OK`, then the version.

   ```bash
   mkdir -p ~/.docker/cli-plugins && curl -fsSL -o ~/.docker/cli-plugins/docker-buildx https://github.com/docker/buildx/releases/download/v0.38.1/buildx-v0.38.1.linux-amd64 && echo "e25d5cd675092bb5d1d03632fdc46cdf3fede7b514dc5f023a105e00d3ce5ad4  $HOME/.docker/cli-plugins/docker-buildx" | sha256sum -c && chmod +x ~/.docker/cli-plugins/docker-buildx && docker buildx version
   ```

   If the checksum fails, delete `~/.docker/cli-plugins/docker-buildx` and stop: the download is not the release.

2. **Let ECS manage its own resources.** ECS needs a role of its own in the account, which it usually creates on
   first use; the first cluster in a new account can fail without it. If this answers that the role name has been
   taken, it already exists, which is fine.

   ```bash
   aws iam create-service-linked-role --aws-service-name ecs.amazonaws.com
   ```

3. **Create the image repository in ECR**, AWS's container registry.

   ```bash
   aws ecr create-repository --repository-name multi-cloud-dotnet-container
   ```

4. **Create the log group**, and keep its logs for a week.

   ```bash
   aws logs create-log-group --log-group-name /ecs/multi-cloud
   ```

   ```bash
   aws logs put-retention-policy --log-group-name /ecs/multi-cloud --retention-in-days 7
   ```

5. **Create the role ECS acts as to start the task**: it pulls the image from ECR and writes the logs, and nothing
   else. AWS's own managed policy grants exactly that.

   ```bash
   aws iam create-role --role-name multi-cloud-task-execution --assume-role-policy-document '{"Version":"2012-10-17","Statement":[{"Effect":"Allow","Principal":{"Service":"ecs-tasks.amazonaws.com"},"Action":"sts:AssumeRole"}]}'
   ```

   ```bash
   aws iam attach-role-policy --role-name multi-cloud-task-execution --policy-arn arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy
   ```

6. **Create the two security groups.** The load balancer's admits HTTP from anywhere; the task's admits port 8080 from
   the load balancer only, so the task's public address answers nobody else.

   ```bash
   ALB_SG=$(aws ec2 create-security-group --group-name multi-cloud-alb --description "multi-cloud load balancer, HTTP from anywhere" --vpc-id $VPC_ID --query GroupId --output text) && echo $ALB_SG
   ```

   ```bash
   aws ec2 authorize-security-group-ingress --group-id $ALB_SG --protocol tcp --port 80 --cidr 0.0.0.0/0
   ```

   ```bash
   TASK_SG=$(aws ec2 create-security-group --group-name multi-cloud-task --description "multi-cloud task, port 8080 from the load balancer only" --vpc-id $VPC_ID --query GroupId --output text) && echo $TASK_SG
   ```

   ```bash
   aws ec2 authorize-security-group-ingress --group-id $TASK_SG --protocol tcp --port 8080 --source-group $ALB_SG
   ```

7. **Create the load balancer**, in the two subnets, and its target group: the task's port 8080, checked on
   `/healthz/ready` every 10 seconds, healthy after two passes, and drained for 30 seconds instead of AWS's 300 before
   ECS stops a task. Then the listener, which forwards port 80 to the target group.

   ```bash
   ALB_ARN=$(aws elbv2 create-load-balancer --name multi-cloud --type application --scheme internet-facing --subnets ${SUBNETS//,/ } --security-groups $ALB_SG --query 'LoadBalancers[0].LoadBalancerArn' --output text) && echo $ALB_ARN
   ```

   ```bash
   TG_ARN=$(aws elbv2 create-target-group --name multi-cloud --protocol HTTP --port 8080 --vpc-id $VPC_ID --target-type ip --health-check-path /healthz/ready --health-check-interval-seconds 10 --healthy-threshold-count 2 --query 'TargetGroups[0].TargetGroupArn' --output text) && echo $TG_ARN
   ```

   ```bash
   aws elbv2 modify-target-group-attributes --target-group-arn $TG_ARN --attributes Key=deregistration_delay.timeout_seconds,Value=30
   ```

   ```bash
   aws elbv2 create-listener --load-balancer-arn $ALB_ARN --protocol HTTP --port 80 --default-actions Type=forward,TargetGroupArn=$TG_ARN
   ```

8. **Create the cluster.** On Fargate it is only a name to run tasks under; there are no servers in it.

   ```bash
   aws ecs create-cluster --cluster-name multi-cloud
   ```

## Build

Nothing is built here: ECS runs a copy of the image **Publish image** pushed to `ghcr.io`, the same bytes. Name it by
its tag; `2026.1111.5` below is an example, so use the newest tag you have:

```bash
TAG=2026.1111.5
```

1. **Sign Docker in to ECR.** The sign-in lasts 12 hours.

   ```bash
   aws ecr get-login-password | docker login --username AWS --password-stdin $REGISTRY
   ```

2. **Copy the image**, registry to registry. The manifest is copied as it is, so the digest does not change.

   ```bash
   docker buildx imagetools create --prefer-index=false --tag $REGISTRY/multi-cloud-dotnet-container:$TAG ghcr.io/mattonweb/multi-cloud-dotnet-container:$TAG
   ```

3. **Name the image by its digest.** Expect the digest the publish run reported for that tag: for `2026.1111.5`,
   `sha256:1632bb0b…`.

   ```bash
   DIGEST=$(aws ecr describe-images --repository-name multi-cloud-dotnet-container --image-ids imageTag=$TAG --query 'imageDetails[0].imageDigest' --output text) && echo $DIGEST
   ```

   ```bash
   IMAGE=$REGISTRY/multi-cloud-dotnet-container@$DIGEST
   ```

## Run

1. **Register the task definition.** `envsubst` fills in `${IMAGE}` and `${EXECUTION_ROLE_ARN}`; both hold the
   account's ID, so neither is written into the file. The first command adds `envsubst` and `jq` if CloudShell lacks
   them. Expect `multi-cloud` and the revision, `1` the first time.

   ```bash
   command -v envsubst > /dev/null || sudo dnf install -y gettext; command -v jq > /dev/null || sudo dnf install -y jq
   ```

   ```bash
   export IMAGE EXECUTION_ROLE_ARN=arn:aws:iam::$ACCOUNT_ID:role/multi-cloud-task-execution && envsubst '$IMAGE $EXECUTION_ROLE_ARN' < deploy/aws-ecs/task-definition.yaml > ~/task-definition.yaml && aws ecs register-task-definition --cli-input-yaml file://$HOME/task-definition.yaml --query 'taskDefinition.[family, revision]' --output text
   ```

2. **Create the service**: one task, in the two subnets with a public address, behind the load balancer. A new task
   starts before an old one stops, and a deployment whose task never becomes healthy rolls back. Expect
   `multi-cloud ACTIVE`.

   ```bash
   aws ecs create-service --cluster multi-cloud --service-name multi-cloud --task-definition multi-cloud --desired-count 1 --launch-type FARGATE --network-configuration "awsvpcConfiguration={subnets=[$SUBNETS],securityGroups=[$TASK_SG],assignPublicIp=ENABLED}" --load-balancers targetGroupArn=$TG_ARN,containerName=web,containerPort=8080 --health-check-grace-period-seconds 30 --deployment-configuration "maximumPercent=200,minimumHealthyPercent=100,deploymentCircuitBreaker={enable=true,rollback=true}" --query 'service.[serviceName, status]' --output text
   ```

3. **Wait for the task.** This waits until the deployment is steady, up to 10 minutes. Expect
   `PRIMARY COMPLETED 1`.

   ```bash
   aws ecs wait services-stable --cluster multi-cloud --services multi-cloud && aws ecs describe-services --cluster multi-cloud --services multi-cloud --query 'services[0].deployments[0].[status, rolloutState, runningCount]' --output text
   ```

   If the wait gives up, the service's recent events say why:

   ```bash
   aws ecs describe-services --cluster multi-cloud --services multi-cloud --query 'services[0].events[:5].message' --output text
   ```

4. **Wait for the load balancer.** Its address is a name under `elb.amazonaws.com`, which can take a few minutes to
   resolve the first time. This repeats until the address answers `up`:

   ```bash
   URL=http://$(aws elbv2 describe-load-balancers --names multi-cloud --query 'LoadBalancers[0].DNSName' --output text) && until [ "$(curl -s $URL/healthz/ping)" = "up" ]; do echo "waiting for the load balancer..."; sleep 10; done; echo "ready at $URL"
   ```

The service answers on plain HTTP at that address. To deploy another image later, run the Build steps with its tag,
register the task definition again (Run step 1, which adds a revision), then point the service at the newest
revision; ECS starts the new task, and the old one writes its `Stopping` heartbeat:

```bash
aws ecs update-service --cluster multi-cloud --service multi-cloud --task-definition multi-cloud --query 'service.taskDefinition' --output text && aws ecs wait services-stable --cluster multi-cloud --services multi-cloud
```

## Verify

Every check uses `$URL`, set by Run step 4.

1. **Where it thinks it is running.** Expect `platform.name` "AWS ECS", `cloudProvider` `aws`, `cloudPlatform`
   `aws_ecs`, `region` `us-east-2`, and `service`, `revision` and `instance` `null` ([SDD.md](SDD.md) section 5, row
   2). `request.scheme` is `http`. `request.remoteAddress` should be CloudShell's own public address, which the
   second command prints: the load balancer appends one entry to `X-Forwarded-For`, so the default limit of 1 trusts
   exactly that ([SDD.md](SDD.md) decision D11). If it is a private `172.31.` address, that is the load balancer's own
   address inside the VPC, and a finding.

   ```bash
   curl -s $URL/ | jq '{platform, request}'
   ```

   ```bash
   curl -s https://checkip.amazonaws.com
   ```

2. **Liveness through the load balancer.** Expect `200`, the body `up`, `x-session-id`, and `x-platform: AWS ECS`.
   Google's load balancer added `Via: 1.1 google`; note what AWS's adds, if anything.

   ```bash
   curl -i $URL/healthz/ping
   ```

3. **Errors.** Expect `500`, `403` and `404`, each `application/problem+json` with `x-session-id`.

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

5. **The logs in CloudWatch.** ECS sends the task's console to the log group, one JSON line per event, as the
   application wrote it. CloudWatch has no severity of its own, but it can search the JSON: the second command
   expects the 500's two `ERROR` lines and the 403's `WARNING`. The load balancer checks `/healthz/ready` every 10
   seconds; the third expects `0`, none of those checks in the request log.

   ```bash
   aws logs tail /ecs/multi-cloud --since 15m --format short
   ```

   ```bash
   aws logs filter-log-events --log-group-name /ecs/multi-cloud --filter-pattern '{ ($.severity = "ERROR") || ($.severity = "WARNING") }' --query 'events[].message' --output text
   ```

   ```bash
   aws logs filter-log-events --log-group-name /ecs/multi-cloud --filter-pattern '"/healthz/ready"' --query 'length(events)'
   ```

6. **Heartbeats, and a polite stop.** Force a new deployment: ECS starts a new task, waits for it to be healthy,
   drains the old one from the load balancer for 30 seconds, sends it a stop signal, and waits up to 30 seconds. It
   takes a minute or two. Expect the old task's last heartbeat to be `Stopping`, and the new task's first `Started`
   with a new session. The first column is the task's ID.

   ```bash
   aws ecs update-service --cluster multi-cloud --service multi-cloud --force-new-deployment --query 'service.serviceName' --output text && aws ecs wait services-stable --cluster multi-cloud --services multi-cloud
   ```

   ```bash
   aws logs filter-log-events --log-group-name /ecs/multi-cloud --filter-pattern '"HeartbeatKind"' --output json | jq -r '.events[-10:][] | (.message | fromjson) as $m | [(.logStreamName | split("/") | last), $m.HeartbeatKind, $m.HeartbeatSequence] | @tsv'
   ```

## Tear down

Delete the service first, then the cluster, then the load balancer, which bills by the hour, and what it used. Run
the session blocks first if CloudShell has restarted.

```bash
aws ecs delete-service --cluster multi-cloud --service multi-cloud --force --query 'service.status' --output text
```

```bash
aws ecs wait services-inactive --cluster multi-cloud --services multi-cloud && aws ecs delete-cluster --cluster multi-cloud --query 'cluster.status' --output text
```

```bash
aws elbv2 delete-load-balancer --load-balancer-arn $ALB_ARN && aws elbv2 wait load-balancers-deleted --load-balancer-arns $ALB_ARN
```

```bash
aws elbv2 delete-target-group --target-group-arn $TG_ARN
```

The task's security group can stay in use for a minute or two after the task stops, while AWS releases its network
interface; if the first command answers `DependencyViolation`, wait and repeat it.

```bash
aws ec2 delete-security-group --group-id $TASK_SG
```

```bash
aws ec2 delete-security-group --group-id $ALB_SG
```

Then the repository and every image in it, the logs, and the role:

```bash
aws ecr delete-repository --repository-name multi-cloud-dotnet-container --force
```

```bash
aws logs delete-log-group --log-group-name /ecs/multi-cloud
```

```bash
aws iam detach-role-policy --role-name multi-cloud-task-execution --policy-arn arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy && aws iam delete-role --role-name multi-cloud-task-execution
```

The registered task definitions and ECS's own role cost nothing and can stay.

## What is different here

- **The platform** is row 2 of the table in [SDD.md](SDD.md) section 5: ECS sets `ECS_CONTAINER_METADATA_URI_V4` and
  `AWS_REGION`, so the application reports "AWS ECS", `aws`, `aws_ecs` and the region, with no service, revision or
  instance. The first run confirms which variables Fargate really sets.
- **HTTPS is not set up.** The load balancer answers plain HTTP on its public name and forwards it to the task;
  HTTPS would need a domain and a certificate.
- **One forwarded entry.** The load balancer appends the caller's address to `X-Forwarded-For`, one entry, as Cloud
  Run's front end does, so the default `ForwardedHeaders__ForwardLimit` of 1 applies and nothing sets it.
- **The port** is the image's own 8080; nothing sets `PORT`.
- **The load balancer's health check is the only probe.** The image has no `curl` for a container health check, so
  the load balancer checks `/healthz/ready`, and ECS replaces a task it finds unhealthy. None of the checks appears in
  the request log.
- **A stopped task is drained, then gets 30 seconds.** The load balancer stops sending it requests for 30 seconds,
  then ECS sends the stop signal and waits 30 seconds, longer than the application's 8-second shutdown, so it writes
  its `Stopping` heartbeat.
- **Logs are JSON in CloudWatch.** Each line arrives as the application wrote it; CloudWatch searches its fields, such
  as `severity`, but has no severity of its own.
- **The task runs as the image's non-root user**, with every Linux capability dropped.
