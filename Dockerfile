# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY src/Iscuissatis.Domain/Iscuissatis.Domain.csproj src/Iscuissatis.Domain/
COPY src/Iscuissatis.Application/Iscuissatis.Application.csproj src/Iscuissatis.Application/
COPY src/Iscuissatis.Infrastructure/Iscuissatis.Infrastructure.csproj src/Iscuissatis.Infrastructure/
COPY src/Iscuissatis.Web/Iscuissatis.Web.csproj src/Iscuissatis.Web/
RUN dotnet restore src/Iscuissatis.Web/Iscuissatis.Web.csproj

COPY src/ src/
RUN dotnet publish src/Iscuissatis.Web/Iscuissatis.Web.csproj \
    --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_NOLOGO=true \
    DOTNET_CLI_TELEMETRY_OPTOUT=true

RUN useradd --create-home --uid 10001 appuser
USER appuser

COPY --from=build --chown=appuser:appuser /app/publish ./

EXPOSE 8080
ENTRYPOINT ["dotnet", "Iscuissatis.Web.dll"]
