# syntax=docker/dockerfile:1.7
# ============================================================================
# NimPulse — self-hosted family health platform on Azure
# ============================================================================
# Multi-stage:
#   1. build   — restore + publish the ASP.NET Core 8 app
#   2. runtime — small aspnet:8.0 image, non-root user, /data volume for SQLite
# ============================================================================

ARG DOTNET_VERSION=8.0

# ---------------------------------------------------------------------------
# Stage 1: build
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src

# Copy csproj files first so restore can cache when only source changes.
COPY NimPulse.sln ./
COPY VERSION ./
COPY src/NimPulse.Api/NimPulse.Api.csproj src/NimPulse.Api/
COPY src/NimPulse.Core/NimPulse.Core.csproj src/NimPulse.Core/
RUN dotnet restore src/NimPulse.Api/NimPulse.Api.csproj

# Now the source tree.
COPY src/ src/
RUN dotnet publish src/NimPulse.Api/NimPulse.Api.csproj \
    -c Release -o /app --no-restore /p:UseAppHost=false

# ---------------------------------------------------------------------------
# Stage 2: runtime
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS runtime

# The aspnet:8.0 image already ships with a non-root "app" user (UID 1654),
# exposed via the $APP_UID build arg. Reuse it instead of adding a new one.
ARG APP_UID=1654

WORKDIR /app
COPY --from=build /app ./
RUN chown -R ${APP_UID}:${APP_UID} /app

# App Service reads WEBSITES_PORT; the ASP.NET Core listener follows ASPNETCORE_URLS.
ENV ASPNETCORE_URLS=http://0.0.0.0:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    ConnectionStrings__Default="Data Source=/data/nimpulse.db;Cache=Shared" \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_gcServer=1

# /data is mounted from Azure Files by the App Service azureStorageAccounts config.
VOLUME ["/data"]

USER ${APP_UID}
EXPOSE 8080

ENTRYPOINT ["dotnet", "NimPulse.Api.dll"]
