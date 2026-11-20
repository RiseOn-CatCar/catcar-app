# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:11.0.100 AS build
WORKDIR /src

COPY global.json CatCar.slnx Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/ src/
RUN dotnet publish src/Api/CatCar.Api.csproj -c Release -o /app

# Stage 2: Runtime
# Azure Linux variant: the default Ubuntu (resolute) image ships /usr/bin/pebble,
# whose Go stdlib carries HIGH CVE-2026-97054 and fails the release Trivy gate.
FROM mcr.microsoft.com/dotnet/aspnet:11.0.0-azurelinux3.0 AS runtime
WORKDIR /app

# Run as the image's built-in non-root user (app, UID/GID 1654)
USER $APP_UID

# Copy published output
COPY --from=build /app .


ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080
ENTRYPOINT ["dotnet", "CatCar.Api.dll"]
