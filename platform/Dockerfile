# The Aqlaan platform: one ASP.NET Core app for every game. Built by ~/Desktop/projects/servers/tools/release.sh
# for linux/amd64 with --build-arg VERSION=X.Y.Z. The build context is the repo root (deploy.conf BUILD_CONTEXT=..),
# filtered by Dockerfile.dockerignore next to this file, so the Unity projects never enter it.

# The SDK runs on the build machine and cross-compiles for the target: no emulation on an Apple silicon Mac.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
ARG VERSION=0.0.0
WORKDIR /src
# Restore first, so a code change does not download the packages again.
COPY platform/src/Platform.Domain/Platform.Domain.csproj platform/src/Platform.Domain/
COPY platform/src/Platform.Api/Platform.Api.csproj platform/src/Platform.Api/
RUN dotnet restore platform/src/Platform.Api/Platform.Api.csproj -a $TARGETARCH
COPY platform/src/ platform/src/
RUN dotnet publish platform/src/Platform.Api/Platform.Api.csproj -c Release -a $TARGETARCH --no-restore \
      -p:Version=$VERSION -p:IncludeSourceRevisionInInformationalVersion=false -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
ARG VERSION=0.0.0
WORKDIR /app
COPY --from=build /out ./
# What the platform reads from games/ at runtime: each game.json, the balance.json it names (balanceConfig) and
# the legal pages. Dockerfile.dockerignore lets only those files in.
COPY games/ ./games/
# APP_VERSION is what GET /healthz reports. Plain HTTP on 8080: TLS ends at Cloudflare and Traefik.
ENV APP_VERSION=$VERSION \
    ASPNETCORE_HTTP_PORTS=8080 \
    Games__Path=/app/games
EXPOSE 8080
# The image's non-root user "app" (uid 1654). The files above stay owned by root, so the app cannot change them.
USER $APP_UID
ENTRYPOINT ["dotnet", "Platform.Api.dll"]
