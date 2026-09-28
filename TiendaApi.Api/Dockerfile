# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Build argument
ARG BUILD_CONFIGURATION=Release

# Copy solution file first (works with both .sln and .slnx)
COPY *.slnx ./

# Central Package Management: sin estas, los csproj no tienen Version= (NU1015)
COPY Directory.Packages.props ./
COPY Directory.Build.props ./

COPY TiendaApi.Api/*.csproj ./TiendaApi.Api/
COPY TiendaApi.Tests/*.csproj ./TiendaApi.Tests/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY TiendaApi.Api/ ./TiendaApi.Api/
COPY TiendaApi.Tests/ ./TiendaApi.Tests/

# Build the application
RUN dotnet publish TiendaApi.Api/TiendaApi.csproj -c $BUILD_CONFIGURATION -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# curl es necesario para el HEALTHCHECK (y para el healthcheck de docker-compose.prod)
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Non-root user: la imagen aspnet:10.0 ya incluye el usuario "app" (UID 1000)
USER app

# Copy published application
COPY --from=build /app/publish .

# Expose ports
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check del contenedor (misma ruta que usa docker-compose.prod)
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "TiendaApi.Api.dll"]
