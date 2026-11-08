# Multi-stage build: the SDK image compiles, the much smaller ASP.NET runtime image runs.
# Build from the repository root:  docker build -t conversation-service .

# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# global.json is not copied on purpose: the image tag (10.0) already fixes the SDK,
# and the image may ship a different 10.0 feature band than the one pinned for local development.
COPY Directory.Build.props ./
COPY protos/ protos/

# Copy only the project files first and restore. Docker caches this layer,
# so packages are downloaded again only when a .csproj changes, not on every code change.
COPY src/Pigeon.Conversations.Domain/Pigeon.Conversations.Domain.csproj src/Pigeon.Conversations.Domain/
COPY src/Pigeon.Conversations.Api/Pigeon.Conversations.Api.csproj src/Pigeon.Conversations.Api/
RUN dotnet restore src/Pigeon.Conversations.Api/Pigeon.Conversations.Api.csproj

COPY src/ src/
RUN dotnet publish src/Pigeon.Conversations.Api/Pigeon.Conversations.Api.csproj \
    --configuration Release --no-restore --output /app

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# Run as the non-root user that the .NET images provide (APP_UID).
USER $APP_UID

# 8080: REST over HTTP/1.1 (gateway, probes). 8081: gRPC over HTTP/2 (other services). See appsettings.json.
EXPOSE 8080 8081
ENTRYPOINT ["dotnet", "Pigeon.Conversations.Api.dll"]
