# ── Stage 1: Build ───────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore first (layer-cache friendly)
COPY ["Insurance Hub.csproj", "./"]
RUN dotnet restore

# Copy everything else and publish
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

# ── Stage 2: Runtime ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Security: run as non-root
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser

# Install wget for health check (smaller than curl)
USER root
RUN apt-get update && apt-get install -y --no-install-recommends wget && rm -rf /var/lib/apt/lists/*

# Copy published output
COPY --from=build /app/publish .

# Fix permissions
RUN chown -R appuser:appgroup /app
USER appuser

# Expose HTTP only — Coolify/reverse proxy handles TLS
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check using wget (available in aspnet base image)
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
  CMD wget -qO- http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Insurance Hub.dll"]
