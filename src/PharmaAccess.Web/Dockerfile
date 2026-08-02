# syntax=docker/dockerfile:1.7
ARG SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:10.0.302
ARG RUNTIME_IMAGE=mcr.microsoft.com/dotnet/aspnet:10.0.10-noble-chiseled
FROM ${SDK_IMAGE} AS build
WORKDIR /source
COPY . .
ARG APP_VERSION=0.0.0-local
ARG COMMIT_SHA=unknown
ARG BUILD_TIMESTAMP=unknown
RUN dotnet restore src/PharmaAccess.Web/PharmaAccess.Web.csproj --locked-mode \
 && dotnet publish src/PharmaAccess.Web/PharmaAccess.Web.csproj -c Release --no-restore -o /out/app /p:UseAppHost=false /p:ReleaseVersion=${APP_VERSION} /p:SourceRevisionId=${COMMIT_SHA} /p:BuildTimestamp=${BUILD_TIMESTAMP} \
 && dotnet publish src/PharmaAccess.ContainerProbe/PharmaAccess.ContainerProbe.csproj -c Release -o /out/probe /p:UseAppHost=false

FROM ${RUNTIME_IMAGE} AS final
WORKDIR /app
COPY --from=build --chown=app:app /out/app/ ./
COPY --from=build --chown=app:app /out/probe/ /probe/
ENV ASPNETCORE_HTTP_PORTS=8080 DOTNET_EnableDiagnostics=0
EXPOSE 8080
USER app
HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 CMD ["dotnet","/probe/PharmaAccess.ContainerProbe.dll","http://127.0.0.1:8080/health/live"]
ENTRYPOINT ["dotnet","PharmaAccess.Web.dll"]
