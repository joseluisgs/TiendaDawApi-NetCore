# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution file first (works with both .sln and .slnx)
COPY *.slnx ./
COPY TiendaApi.Apis/*.csproj ./TiendaApi.Apis/
COPY TiendaApi.Tests/*.csproj ./TiendaApi.Tests/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY TiendaApi.Apis/ ./TiendaApi.Apis/
COPY TiendaApi.Tests/ ./TiendaApi.Tests/

# Build the application
RUN dotnet publish TiendaApi.Apis/TiendaApi.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN adduser -u 1000 appuser
USER appuser

# Copy published application
COPY --from=build /app/publish .

# Expose ports
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Entry point
ENTRYPOINT ["dotnet", "TiendaApi.Apis.dll"]
