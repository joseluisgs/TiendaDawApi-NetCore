# 🐳 Docker en TiendaDawApi

> Contenedores para **desarrollo local** (`docker-compose.local.yml`) y para
> **producción** (`docker-compose.prod.yml`). Todo lo de esta página está tomado
> de los ficheros reales del repositorio.

---

## 1. El Dockerfile (multi-stage)

Fichero: `TiendaApi.Api/Dockerfile` (idéntico en los dos proyectos de la tienda).

| Etapa | Imagen | Qué hace |
|---|---|---|
| **build** | `mcr.microsoft.com/dotnet/sdk:10.0` | Copia `.slnx` + `Directory.Packages.props` + `.csproj` → `dotnet restore` (la capa de dependencias se cachea: solo se repite si cambian los csproj) → `dotnet publish` en `Release` a `/app/publish` |
| **runtime** | `mcr.microsoft.com/dotnet/aspnet:10.0` | Copia **solo** el publish, expone el puerto y arranca la API |

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build     # compilación
...
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime # ejecución (sin SDK)
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "TiendaApi.Api.dll"]
```

🎓 **Por qué dos etapas**: la imagen final no lleva el SDK ni el código fuente —
solo el runtime y el publish (~imagen pequeña y sin herramientas de compilación).
La API escucha en el **8080 interno** (estándar de las imágenes `aspnet`).

## 2. `.dockerignore`

Hay dos: en la raíz (638 bytes) y en `TiendaApi.Api/` (346 bytes). Su trabajo:
mantener `bin/`, `obj/`, docs y tests fuera del contexto de build → el `COPY` del
Dockerfile no arrastra artefactos locales y las capas se mantienen cacheadas.

## 3. Desarrollo local — `docker-compose.local.yml`

Contenedores de **solo infraestructura** (la API se ejecuta fuera con `dotnet run`):

| Servicio | Contenedor | Puerto (host:contenedor) | Healthcheck |
|---|---|---|---|
| `postgres` | tienda-local-postgres | `${POSTGRES_PORT:-5432}:5432` | ✅ |
| `mongodb` | tienda-local-mongodb | `${MONGO_PORT:-27017}:27017` | ✅ |
| `adminer` | tienda-local-adminer | `${ADMINER_PORT:-8080}:8080` | — |
| `mongo-express` | tienda-local-mongo-express | `${MONGO_EXPRESS_PORT:-8081}:8081` | — |
| `cassandra` | tienda-local-cassandra | `${CASSANDRA_PORT:-9042}:9042` | ✅ (la imagen JVM tarda ~30-60 s) |

```bash
docker compose -f docker-compose.local.yml up -d   # levantar infra
dotnet run --project TiendaApi.Api                 # la API en http://localhost:5031
```

🎓 Las variables `*_PORT` permiten cambiar puertos sin tocar el compose
(`${POSTGRES_PORT:-5432}` = "usa POSTGRES_PORT o5432 por defecto").

## 4. Producción — `docker-compose.prod.yml`

Todos los servicios **más la API contenedora**:

| Servicio | Contenedor | Puerto | Healthcheck |
|---|---|---|---|
| `postgres` | tienda-postgres | `${POSTGRES_PORT:-5432}:5432` | ✅ |
| `mongodb` | tienda-mongodb | `${MONGO_PORT:-27017}:27017` | ✅ |
| `redis` | tienda-redis | `${REDIS_PORT:-6379}:6379` | ✅ |
| `cassandra` | tienda-cassandra | `${CASSANDRA_PORT:-9042}:9042` | ✅ |
| `api` | tienda-api (build del Dockerfile) | `${API_PORT:-5000}:8080` | ✅ |

```bash
# Crear .env con tus contraseñas y configuración de producción antes
docker compose -f docker-compose.prod.yml up -d --build   # levantar todo (con build)
docker compose -f docker-compose.prod.yml logs -f api     # logs de la API
docker compose -f docker-compose.prod.yml down            # parar
docker compose -f docker-compose.prod.yml down -v         # parar y borrar volúmenes (⚠ datos)
```

🎓 `up -d --build` reconstruye la imagen de la API si cambió el código; el resto
de servicios usan imágenes oficiales con **volúmenes nombrados**
(`postgres-data`, `mongodb-data`, `cassandra-data`, `redis-data`,
`tienda-images`) que sobreviven al `down`.

## 5. Composes de E2E (CLI en contenedor)

| Fichero | Para qué |
|---|---|
| `TiendaApi.Tests.E2E/Bruno-Cli/docker-compose.yml` | Ejecuta la colección **Bruno** desde su CLI contenedor contra la API levantada |
| `TiendaApi.Tests.E2E/Postman-Cli/docker-compose.yml` | Ídem con **Newman/Postman** |

🎓 Van en contenedor para que la ejecución del E2E no dependa de Node/Bruno
instalados en la máquina: solo hace falta Docker.

## 6. Reglas rápidas

- **Desarrollo**: compose `local` (infra) + `dotnet run` (API en5031) — recarga rápida.
- **Producción**: compose `prod` (todo, API incluida en el8080 interno → `API_PORT`).
- **Tests con BD reales**: la suite de integración usa Testcontainers (Docker
  arrancado); los composes E2E van aparte.
- Nunca subas `.env` al repositorio: el compose de prod lo lee como variables.
