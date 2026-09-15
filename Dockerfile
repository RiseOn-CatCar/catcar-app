# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files for restore
COPY CatCar.slnx .
COPY Directory.Build.props .
COPY Directory.Packages.props .
COPY .editorconfig .
COPY src/Directory.Build.props src/
COPY src/SharedKernel/CatCar.SharedKernel.csproj src/SharedKernel/
COPY src/Contracts/CatCar.Contracts.csproj src/Contracts/
COPY src/Contexts/ServiceOperations/CatCar.Contexts.ServiceOperations.csproj src/Contexts/ServiceOperations/
COPY src/Contexts/CatalogInventory/CatCar.Contexts.CatalogInventory.csproj src/Contexts/CatalogInventory/
COPY src/Contexts/Communication/CatCar.Contexts.Communication.csproj src/Contexts/Communication/
COPY src/Contexts/IdentityAccess/CatCar.Contexts.IdentityAccess.csproj src/Contexts/IdentityAccess/
COPY src/Api/CatCar.Api.csproj src/Api/
COPY src/Host/CatCar.AppHost/CatCar.AppHost.csproj src/Host/CatCar.AppHost/
COPY src/Host/CatCar.ServiceDefaults/CatCar.ServiceDefaults.csproj src/Host/CatCar.ServiceDefaults/
COPY tests/Directory.Build.props tests/

# Restore
RUN dotnet restore src/Api/CatCar.Api.csproj

# Copy remaining source
COPY src/ src/
COPY tests/ tests/

# Build and publish
RUN dotnet publish src/Api/CatCar.Api.csproj -c Release -o /app

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create non-root user
RUN groupadd --system appgroup && useradd --system --no-create-home --gid appgroup appuser && chown -R appuser:appgroup /app
USER appuser

# Copy published output
COPY --from=build /app .

# Health check
HEALTHCHECK --interval=30s --timeout=3s --retries=3 \
  CMD curl -f http://localhost:8080/health/live || exit 1

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080
ENTRYPOINT ["dotnet", "CatCar.Api.dll"]
