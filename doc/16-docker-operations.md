# 16. Docker y Docker Operations

Docker permite empquetar la aplicacion en contenedores portablees y reproducibles.

---

## 1. Estructura de Contenedores

```mermaid
flowchart TB
    subgraph "Docker Network"
        API[TiendaApi Apis\nPuerto 5000]
        PG[PostgreSQL\nPuerto 5432]
        MONGO[MongoDB\nPuerto 27017]
        REDIS[Redis\nPuerto 6379]
    end
    
    API --> PG
    API --> MONGO
    API --> REDIS
```

---

## 2. Dockerfile Multi-Stage

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY *.slnx ./
COPY TiendaApi.Apis/*.csproj ./TiendaApi.Apis/
COPY TiendaApi.Tests/*.csproj ./TiendaApi.Tests/

RUN dotnet restore

COPY TiendaApi.Apis/ ./TiendaApi.Apis/
COPY TiendaApi.Tests/ ./TiendaApi.Tests/

RUN dotnet publish TiendaApi.Apis/TiendaApi.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN adduser -u 1000 appuser
USER appuser

COPY --from=build /app/publish .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "TiendaApi.Apis.dll"]
```

---

## 3. docker-compose.yml

```yaml
version: '3.8'

services:
  postgres:
    image: postgres:15-alpine
    container_name: tienda-postgres
    environment:
      POSTGRES_DB: tienda
      POSTGRES_USER: admin
      POSTGRES_PASSWORD: admin123
    ports:
      - "5432:5432"
    volumes:
      - postgres-data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U admin -d tienda"]
      interval: 10s
      timeout: 5s
      retries: 5

  mongodb:
    image: mongo:7
    container_name: tienda-mongodb
    environment:
      MONGO_INITDB_ROOT_USERNAME: admin
      MONGO_INITDB_ROOT_PASSWORD: admin123
      MONGO_INITDB_DATABASE: tienda
    ports:
      - "27017:27017"
    volumes:
      - mongodb-data:/data/db

  redis:
    image: redis:7-alpine
    container_name: tienda-redis
    ports:
      - "6379:6379"
    volumes:
      - redis-data:/data

  api:
    build:
      context: ./TiendaApi.Apis
      dockerfile: Dockerfile
    container_name: tienda-api
    ports:
      - "5000:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=tienda;Username=admin;Password=admin123
      - ConnectionStrings__MongoDB=mongodb://admin:admin123@mongodb:27017/tienda?authSource=admin
      - ConnectionStrings__Redis=redis:6379
    depends_on:
      postgres:
        condition: service_healthy
      mongodb:
        condition: service_healthy
      redis:
        condition: service_healthy

volumes:
  postgres-data:
  mongodb-data:
  redis-data:
```

---

## 4. Comandos Docker

```bash
# Levantar servicios
docker-compose up -d

# Ver logs
docker-compose logs -f api

# Detener servicios
docker-compose down

# Reconstruir imagen
docker-compose build --no-cache

# Ver estado
docker-compose ps
```

---

## 5. Beneficios

- **Portabilidad**: Funciona en cualquier entorno
- **Aislamiento**: Cada servicio independiente
- **Escalabilidad**: Facilidad para escalar
- **Reproducibilidad**: Entornos identicos
