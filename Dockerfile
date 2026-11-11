# The one image for every environment. The code and the image are identical everywhere; everything that differs
# between environments is an environment variable or a platform setting (docs/SDD.md section 10).
#
# Build it for linux/amd64 from the repository root:
#   docker build --platform linux/amd64 --tag multi-cloud-dotnet-container:local .

# Build stage: restore, then publish the web application in Release.
FROM mcr.microsoft.com/dotnet/sdk:10.0.403-noble AS build
WORKDIR /src

# The house build settings and style first, so the analyzers behave here as they do on a developer machine, with
# warnings as errors in both.
COPY src/Directory.Build.props src/.editorconfig ./

# The project files alone, then the restore, so a change to the source does not repeat the restore.
COPY src/MultiCloud.Adapter.Notes/MultiCloud.Adapter.Notes.csproj MultiCloud.Adapter.Notes/
COPY src/MultiCloud.Adapter.Platform/MultiCloud.Adapter.Platform.csproj MultiCloud.Adapter.Platform/
COPY src/MultiCloud.Orchestrator.Heartbeat/MultiCloud.Orchestrator.Heartbeat.csproj MultiCloud.Orchestrator.Heartbeat/
COPY src/MultiCloud.Application.Web/MultiCloud.Application.Web.csproj MultiCloud.Application.Web/
RUN dotnet restore MultiCloud.Application.Web/MultiCloud.Application.Web.csproj

# The source, then the publish. The tests do not run here; they run before the image is built.
COPY src/MultiCloud.Adapter.Notes/ MultiCloud.Adapter.Notes/
COPY src/MultiCloud.Adapter.Platform/ MultiCloud.Adapter.Platform/
COPY src/MultiCloud.Orchestrator.Heartbeat/ MultiCloud.Orchestrator.Heartbeat/
COPY src/MultiCloud.Application.Web/ MultiCloud.Application.Web/
RUN dotnet publish MultiCloud.Application.Web/MultiCloud.Application.Web.csproj \
    --configuration Release --no-restore --output /app

# Run stage: the ASP.NET Core runtime and the published application, nothing else.
FROM mcr.microsoft.com/dotnet/aspnet:10.0.14-noble AS runtime
WORKDIR /app
COPY --from=build /app ./

# Every target platform ends HTTPS at a proxy in front of the container. This makes the host honor X-Forwarded-For
# and X-Forwarded-Proto, from any proxy, without a line of code.
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

# The base image's own non-root user. The base image listens on 8080 (ASPNETCORE_HTTP_PORTS); a platform that sets
# PORT moves it.
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "MultiCloud.Application.Web.dll"]
