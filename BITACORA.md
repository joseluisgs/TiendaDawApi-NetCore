# Bitácora — Fases de mejora (baseline → documentación)

> **Fecha de inicio:** 24/09/2026
> **Proyecto origen:** `TiendaDawApi-NetCore` (rama `feature/polly`, API sin CQRS)
> **Destino de replicación:** `TiendaDawApi-Cqrs-MediatR-NetCore`
> **Objetivo:** documentar cada fase ejecutada según los logs de commits, para replicar los cambios en la API CQRS con la mayor precisión posible. **Complementa** a `FASES-MEJORAS.md` (allí están las tareas y verificaciones en detalle; aquí el qué/cómo/dónde de cada commit).

## Índice de fases (commits)

| Fase | Commit | Resumen | Estado |
|------|--------|---------|--------|
| 0 · Baseline | `88f7e4f` | Dependencias, fix NU1902, adaptaciones de código | ✅ |
| PG 17 | `9166c35` | PostgreSQL 15/16 → 17-alpine (compose, Testcontainers, docs) | ✅ |
| 1 · Health + índices + Task.Run | `8d05352` | `GET /health`, índices EF en modelo, fire & forget endurecido | ✅ |
| 2 · AsNoTracking | `a82e7a5` (+`83fb0dd` docs) | 9 listados de solo lectura sin tracking | ✅ |
| 3 · Pedidos paginados | `9b05682` | Paginación real en repo (Mongo Skip/Limit + EF Skip/Take) | ✅ |
| 4 · OutputCache + ETag | `b16c29e` | Caché HTTP 60s con tags + revalidación 304 | ✅ |
| 9 · ToHttpResult | `a7352de` | 31 `error switch` → 1 extensión (Opción C) | ✅ |
| 8 · Migraciones EF | `ee65489` | `InitialCreate` + `AddOptimizationIndexes` + baseline | ✅ |
| 5 · Verificación global | — | Build + unit + integración + E2E + smoke | ⬜ |
| 6 · Polly educativa | — | Retry + CircuitBreaker + Timeout en email | ⬜ |
| 7 · Automation E2E (Node) | — | `test-runner.mjs` de todos los controladores | ⬜ |
| 10 · Documentación didáctica | — | Secciones en `doc/NN-*.md` existentes (última fase) | ⬜ |

---

## Fase 0 — Baseline + actualización de dependencias (`88f7e4f`) ✅

> **Fecha:** 24/09/2026 · **Objetivo:** dejar `restore/build/test` limpios (0 errores, 0 vulnerabilidades) como baseline de las fases de mejora.

### 1. Problema de partida

- `dotnet restore TiendaApi.slnx` fallaba con **`NU1902`** (advertencia como error por `TreatWarningsAsErrors=true`):
  `SharpCompress 0.30.1` — GHSA-6c8g-7p36-r338 (CVE-2026-44788, zip-slip moderado).
- **Causa raíz transitiva:** `MongoDB.EntityFrameworkCore 10.0.0 → MongoDB.Driver 3.6.0 → SharpCompress 0.30.1`.
  El fix no es añadir SharpCompress a mano, sino **subir la cadena MongoDB** a una versión que traiga SharpCompress ≥ 0.48.0 (parcheado).
- `dotnet list package --vulnerable` no funcionaba porque internamente re-ejecutaba restore con warnings-as-errors → por eso se rastreó el origen con `obj/project.assets.json`.

## 2. Diagnóstico (comandos reutilizables)

```bash
dotnet restore TiendaApi.slnx                      # estado base
dotnet list TiendaApi.slnx package --outdated      # versiones nuevas disponibles
dotnet list TiendaApi.slnx package --vulnerable --include-transitive
# rastreo de quién trae un paquete transitivo:
#   buscar en TiendaApi.Api/obj/project.assets.json  →  '"SharpCompress/0.30.1"' y sus padres
# consulta directa a NuGet (dependencias de un paquete):
#   https://api.nuget.org/v3-flatcontainer/{id}/{version}/{id}.nuspec
```

## 3. Cambios en `.csproj` (antes → después)

### TiendaApi.Api/TiendaApi.csproj

| Paquete | Antes | Después | Motivo |
|---|---|---|---|
| MongoDB.Bson | 3.6.0 | **3.12.0** | SharpCompress transitivo → 0.48.1 (fix NU1902) |
| MongoDB.EntityFrameworkCore | 10.0.0 | **10.0.4** | requiere EF ≥ 10.0.11 y driver ≥ 3.11.2 |
| Microsoft.EntityFrameworkCore (+Design) | 10.0.4 | **10.0.12** | exigido por MongoDB.EF 10.0.4 |
| HotChocolate.* (AspNetCore, Authorization, Data) | 14.3.1 | **16.6.7** | major (GraphQL) |
| Microsoft.AspNetCore.Mvc.Versioning | 5.1.0 | **→ eliminado** | paquete deprecatado |
| Asp.Versioning.Mvc + Asp.Versioning.Mvc.ApiExplorer | — | **10.2.1** | sustituto oficial |
| Swashbuckle.AspNetCore | 7.3.0 | **10.2.3** | Microsoft.OpenApi 2.x |
| Serilog.AspNetCore | 8.0.3 | **10.0.0** | alineado con .NET 10 |
| Serilog.Extensions.Logging | 8.0.0 | **10.0.0** | ídem |
| Serilog.Sinks.Console | 6.0.0 | **6.1.1** | patch |
| StackExchange.Redis | 2.8.16 | **3.3.1** | major (compatible con caching 10.0.12: `>= 2.7.27`) |
| Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.0 | **10.0.12** | patch |
| Microsoft.Extensions.Caching.StackExchangeRedis | 10.0.4 | **10.0.12** | patch |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.1 | **10.0.3** | patch |
| System.IdentityModel.Tokens.Jwt | 8.3.1 | **8.23.0** | minor |
| AutoMapper | 16.1.1 | **16.2.0** | minor |
| AutoMapper.Extensions.Microsoft.DependencyInjection | 12.0.0 | **12.0.0 (NO subir)** | 12.0.1 exige `AutoMapper = 12.0.1` exacto → rompería |
| BCrypt.Net-Next | 4.0.3 | **4.2.0** | minor |
| CSharpFunctionalExtensions | 3.6.0 | **3.7.0** | minor |
| FluentValidation.AspNetCore | 11.3.0 | **11.3.1** | patch |
| MailKit | 4.16.0 | **4.18.0** | minor |
| AspNetCoreRateLimit / GraphiQL | 5.0.0 / 2.0.0 | sin cambio | ya eran latest |

### TiendaApi.Tests/TiendaApi.Tests.csproj

| Paquete | Antes | Después | Motivo |
|---|---|---|---|
| MongoDB.Driver | 3.6.0 | **3.12.0** | fix SharpCompress + alineado con Bson |
| Microsoft.EntityFrameworkCore.InMemory | 10.0.4 | **10.0.12** | alineado con Api |
| Microsoft.EntityFrameworkCore.Relational | — | **10.0.12 (añadido)** | evita `MSB3277` (conflicto Relational 10.0.4 vs 10.0.12) |
| FluentAssertions | 7.0.0 | **7.2.2** | **NO subir a 8.x**: licencia Xceed (gratis solo no-comercial); 7 = Apache 2.0 |
| HotChocolate.AspNetCore | 14.3.1 | **16.6.7** | sincronizado con Api |
| coverlet.collector / .msbuild | 6.0.3 | **10.0.1** | mayor |
| Microsoft.AspNetCore.Mvc.Testing | 10.0.0 | **10.0.12** | patch |
| Microsoft.NET.Test.Sdk | 17.13.0 | **18.10.1** | mayor |
| Moq | 4.20.72 | **4.21.0** | minor (cambia nullabilidad, ver §4) |
| NUnit / NUnit.Analyzers | 4.3.1 / 4.5.0 | **4.6.1 / 4.15.0** | minor |
| NUnit3TestAdapter | 4.6.0 | **6.3.0** | mayor (soporta VSTest + MTP) |
| Testcontainers.MongoDb / .PostgreSql | 4.3.0 | **4.15.0** | minor; además **corrige vulnerabilidad HIGH de SSH.NET** (GHSA-q939-rpr3-3284) |
| CSharpFunctionalExtensions | 3.6.0 | **3.7.0** | sincronizado con Api |

## 4. Adaptaciones de código por saltos de major (obligatorias)

### 4.1 Swashbuckle 7 → 10 (`Microsoft.OpenApi` 2.x) — `SwaggerConfig.cs`

- `using Microsoft.OpenApi.Models;` **NO existe ya** → `using Microsoft.OpenApi;`
- `OpenApiInfo/Contact/License/SecurityScheme/SecurityRequirement` ahora están en la raíz `Microsoft.OpenApi`.
- **Desapareció `OpenApiReference`** → usar `OpenApiSecuritySchemeReference`.
- `AddSecurityDefinition(...)` ahora recibe `IOpenApiSecurityScheme` (igual, funciona).
- `AddSecurityRequirement(...)` ahora recibe **`Func<OpenApiDocument, OpenApiSecurityRequirement>`** (factory):

```csharp
options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
{
    { new OpenApiSecuritySchemeReference("Bearer", document), new List<string>() }
});
```

### 4.2 Versionado de API — `Microsoft.AspNetCore.Mvc.Versioning` → `Asp.Versioning.Mvc`

- Paquetes nuevos: **`Asp.Versioning.Mvc` 10.2.1** + **`Asp.Versioning.Mvc.ApiExplorer` 10.2.1**.
- Usings: `Microsoft.AspNetCore.Mvc.Versioning` → `Asp.Versioning` (atributo `[ApiVersion]` → `using Asp.Versioning;` en controladores).
- Los analizadores del paquete **obligan** (errores AV0013/AV0021) a encadenar:

```csharp
services
    .AddApiVersioning(options => { /* igual que antes */ })
    .AddApiExplorer()   // requiere el paquete ApiExplorer (AV0021, dispara con Swagger)
    .AddMvc();          // versiona controllers MVC (AV0013)
return services;
```

- Quitar el `using Microsoft.AspNetCore.Mvc.Versioning;` muerto de `ControllersConfig.cs` (si no, CS0246).

### 4.3 HotChocolate 14 → 16

- El código GraphQL (`AddGraphQLServer`, `MapGraphQL`, `MapGraphQLWebSocket`) **compila igual** (vive en `HotChocolate.AspNetCore.Pipeline`, incluido como dependencia).
- **OJO en proyectos de tests:** HC 16 inyecta `global using HotChocolate.Types.Composite;` (contiene una clase `Is` que **colisiona con `NUnit.Framework.Is`** → CS0104 masivo). Se desactiva con la propiedad oficial en el `.csproj` de tests:

```xml
<HotChocolateCompositeImplicitUsings>disable</HotChocolateCompositeImplicitUsings>
```

### 4.4 Testcontainers 4.3 → 4.15 (solo si hay tests de integración)

- El ctor sin parámetros está obsoleto (CS0618): pasar la imagen al ctor y borrar el `.WithImage(...)`:

```csharp
// antes
new MongoDbBuilder().WithImage("mongo:7.0")...
new PostgreSqlBuilder().WithImage("postgres:17-alpine")...
// después
new MongoDbBuilder("mongo:7.0")...
new PostgreSqlBuilder("postgres:17-alpine")...
```

### 4.5 Moq 4.20 → 4.21 (nullabilidad en matchers de ILogger)

- El overload `It.Is<T>(Expression<Func<object?, Type, bool>>)` ahora recibe `v` anulable → CS8602 en `v.ToString()`.
- Arreglo en los ~18 sitios: `(v, t) => v.ToString()!` → `(v, t) => v!.ToString()!`.
  (Patrón de búsqueda: `\(v, t\) => v\.ToString\(\)`)

## 5. Excepciones intencionadas (NO actualizar)

| Paquete | Por qué no |
|---|---|
| `AutoMapper.Extensions.Microsoft.DependencyInjection 12.0.0` | 12.0.1 fija `AutoMapper = 12.0.1` exacto (NU1608); AutoMapper 16 no trae `AddAutoMapper` propio público |
| `FluentAssertions 7.2.2` | v8+ usa licencia Xceed (gratis SOLO no-comercial); rama 7 = Apache 2.0 para siempre |

## 6. Verificación (estado final esperado)

```bash
dotnet restore TiendaApi.slnx                 # OK, sin NU1902
dotnet build TiendaApi.slnx -c Debug          # 0 errores / 0 advertencias
dotnet test TiendaApi.slnx -c Debug --no-build --filter "FullyQualifiedName~Unit"
                                              # 1034 tests, 0 fallos (09/2026)
dotnet list TiendaApi.slnx package --vulnerable --include-transitive   # 0
dotnet list TiendaApi.slnx package --outdated  # solo las 2 excepciones de §5
```

## 7. Checklist de replicación en `TiendaDawApi-Cqrs-MediatR-NetCore`

Diferencias conocidas respecto al proyecto origen:

1. **`NoWarn` enmascara vulnerabilidades:** el `.csproj` de Api tiene `NU1902` y Tests `NU1902;NU1903` en `NoWarn`
   → **quitar `NU1902`** (es el que esconde SharpCompress) y valorar quitar `NU1903` (esconde alerts HIGH como la de SSH.NET).
2. **Extra: `MediatR 12.4.1`** → comprobar `dotnet list package --outdated` aparte (el ecosistema MediatR ha cambiado de licencia/owner; revisar antes de subir major).
3. **Extra: proyectos `ClientBlazor`** (Cliente/Tests/E2E) en la solución → decidir si entran en esta ronda; `dotnet list --outdated` por proyecto.
4. Mismos cambios que §3 (tablas) en `TiendaApi.Api` y `TiendaApi.Tests`, **más** el `Microsoft.EntityFrameworkCore.Relational 10.0.12` en Tests si aparece `MSB3277`.
5. Mismas adaptaciones de código que §4 (`SwaggerConfig`, `ApiVersioningConfig`, `AuthController` using, `ControllersConfig` using muerto, Testcontainers, Moq, `HotChocolateCompositeImplicitUsings`).
6. Verificar igual que §6 (ajustar el número de tests al del repo CQRS).

---

## PostgreSQL 17 (`9166c35`) ✅

- **Qué:** unificar PostgreSQL en **17-alpine** en todo el proyecto (antes `15-alpine` en compose/docs y `16-alpine` en Testcontainers).
- **Dónde:** `docker-compose.local.yml` · `docker-compose.prod.yml` · **10 ficheros** de `TiendaApi.Tests/Integration/TestContainers/**` (`new PostgreSqlBuilder("postgres:17-alpine")`) · `doc/24-testing.md` · `doc/01-configuracion-proyectos-dotnet.md` · snippet de este archivo.
- **Ojo:** el datadir de PG15 no es compatible con PG17 → en local hizo falta `docker compose down -v` (en dev no hay pérdida: `InitializeDatabaseAsync` hace `EnsureDeleted + seed`).
- **Replicar en CQRS:** buscar/replace `postgres:15-alpine`/`16-alpine` → `17-alpine` en ambos compose y en los builders de Testcontainers; `grep -r "postgres:1[56]"` debe quedar a 0.

---

## Fase 1 — Health checks + índices EF + Task.Run (`8d05352`) ✅

### 1A · `GET /health` (sin paquetes NuGet nuevos)

- **Nuevo:** `TiendaApi.Api/Infrastructures/HealthChecksConfig.cs`
  - `AddHealthChecks(environment)` extiende el nativo con 3 checks propios:
    - `PostgresHealthCheck` → `Database.CanConnectAsync` con tope de **5 s** (CancellationTokenSource ligado al token del middleware).
    - `MongoHealthCheck` → `RunCommand {ping:1}` con tope de **5 s** (el server selection de Mongo es 30 s por defecto → hay que acotarlo).
    - `RedisHealthCheck` → **solo fuera de desarrollo** (en dev no se registra `IDistributedCache`); sonda vía `IDistributedCache.SetString` para reusar la misma conexión que la API.
  - `MapHealthEndpoint()` → `MapHealthChecks("/health")` con `ResponseWriter` JSON propio: `{ status, totalDuration, checks[{name, status, duration, description, error}] }` (semáforo OK/DEGRADED/ERROR → 200/503).
- **Registrado en** `Program.cs` (servicios tras `AddBackgroundJobs`, endpoint tras `MapGraphQLEndpoints`).
- `DatabaseConfig.cs`: en modo EF se registra además singleton `IMongoClient` (el modo native ya lo tenía) para que el check funcione en ambos modos (perezoso, no conecta hasta el ping).
- **Contrato:** Bruno `[001] Health Check` espera 200 y cuerpo con `"OK"`.

### 1B · Índices EF en el modelo (`Data/TiendaDbContext.cs`)

- `Producto`: `HasIndex(CategoriaId)`, `HasIndex(CategoriaId, Precio)` (compuesto), `HasIndex(CreatedAt)`, `HasIndex(IsDeleted)`.
- `User`: `HasIndex(Role)`.
- Solo añadir: intactos los únicos (`categorias.Nombre`, `users.Username`, `users.Email`), query filters y soft-delete.
- ⚠️ `EnsureCreated` **NO** aplica índices a BDs ya creadas → materializados en BD viva por la **Fase 8**.

### 1C · Endurecer fire & forget

- Inventario: **29** `_ = Task.Run` (PedidosService 11 · ProductoService 13 · UserService 3 · CategoriaService 2).
- Auditoría: 28 ya tenían `try/catch (Exception) + logger` interior (caché/WS/SignalR/email/eventos = `LogWarning`; creación de pedido = `LogError`); se conservan.
- Endurecido el único lambda sin guarda: `UserService.DeleteAsync` (~línea 288), invalidación de caché → `try/catch + LogError`. **Sin `await`, sin `WhenAll`.**

### Verificación Fase 1

Build 0/0 · 1034 unit verdes · en vivo: `GET /health` → 200 `OK`; `docker stop tienda-local-mongodb` → 503 (mongodb ERROR, postgresql OK) → restaurar → 200.

### Replicar en CQRS

Copiar `HealthChecksConfig.cs` (ojo con el namespace `Infrastructures`), registrar igual en `Program.cs`, índices en el `OnModelCreating` del DbContext CQRS, y aplicar el mismo inventario de `Task.Run` a los services CQRS (MediatR puede tener otros efectos secundarios: revisar handlers).

---

## Fase 2 — `AsNoTracking` selectivo (`a82e7a5`) ✅

- **9 sitios de solo lectura** (`.AsNoTracking()` justo antes del materializado):
  - `CategoriaRepository`: `FindAllAsync` · items de `FindAllPagedAsync`.
  - `UserRepository`: `FindAllAsync` · items de `FindAllPagedAsync` · `GetActiveUsersAsync`.
  - `ProductoRepository`: `FindAllAsync` (con `Include`) · items de `FindAllPagedAsync` · `FindByCategoriaIdAsync` · `GetRecentlyCreatedAsync`.
- **NO tocados** (comparten tracking con Update/Delete/soft-delete): `FindByIdAsync`, `FindByUsernameAsync`, `FindByEmailAsync`, `DeleteAsync`, rutas de `Update`.
- **Verificación:** build 0/0 · 1034 unit · **en vivo 23/23** (listados 200, round-trips POST→PUT→DELETE 201/200/204, GET tras DELETE → 404).

### Replicar en CQRS

Mismo criterio en los repositorios/queries EF del CQRS: `AsNoTracking` **solo** en listados y proyecciones de lectura; nunca en `FindById` compartido por escrituras. En CQRS cuidado con los *projections/read models* de MediatR: ahí `AsNoTracking` siempre.

---

## Fase 3 — Paginación real de pedidos (`9b05682`) ✅

- **Interfaz:** `IPedidosRepository` + `FindAllPagedAsync(page, size) → (Items, TotalCount)` (página base 0).
- **Implementaciones:**
  - `PedidosNativeRepository` (Mongo): `CountDocumentsAsync` + `SortByDescending(CreatedAt)` + `Skip/Limit`.
  - `PedidosEfCoreRepository`: `CountAsync` + `OrderByDescending(CreatedAt)` + `Skip/Take`.
- **Servicio:** `PedidosService.FindAllPagedAsync` delega al repo; **eliminada la paginación en memoria** (antes: `FindAllAsync` + `Skip/Take` sobre la lista completa).
- Firmas de servicio y controller **sin cambios** → solo se actualizaron los 3 mocks en `PedidosServiceTests`.
- **Verificación:** build 0/0 · 1034 unit · integración pedidos 64 OK (32 omitidos = `[Ignore]` EF-272 preexistente) · en vivo 14/14 (`?page=1&size=2` → 2 items / `totalCount:3`, header `Link`).

### Replicar en CQRS

En el handler/mediator de pedidos del CQRS, misma regla: la paginación va en la consulta (Mongo/EF), nunca en memoria. Si CQRS tiene `GetAllPedidos` con paginación en memoria, aplicar el mismo patrón `FindAllPagedAsync`.

---

## Fase 4 — OutputCache + ETag/304 (`b16c29e`) ✅

- **Nuevo:** `TiendaApi.Api/Infrastructures/OutputCacheConfig.cs` → `AddOutputCacheConfig()` (política 60 s, tags `productos`/`categorias`) + `UseOutputCacheConfig()`; registrado en `Program.cs` **antes de** `MapControllers`.
- **`[OutputCache(Duration = 60, Tags = ...)]` solo en los 5 GET anónimos:** productos GetAll/GetById/GetByCategoria + categorías GetAll/GetById.
- **ETag server-side** en la acción (`Response.Headers.ETag = $"\"{Guid.NewGuid():n}\""` en `onSuccess`) → el middleware devuelve **304** con `If-None-Match`. (Pedidos/users/auth: sin `[OutputCache]` → sin ETag.)
- **Invalidación por tag:** `ProductoService`/`CategoriaService` inyectan `IOutputCacheStore` y ejecutan `EvictByTagAsync("productos"/"categorias")` en `InvalidarCache*` (try/catch, background) → cubre CUD de **REST + GraphQL**.
- **Tests:** 2 constructores de tests de controller (`ProductosControllerTests`, `CategoriasControllerTests`) inicializan `ControllerContext` con `DefaultHttpContext` (si no, NRE en `Response.Headers`).
- **Verificación:** build 0/0 · 1034 unit · **en vivo 26/26**: HIT demostrado (2º GET con misma ETag), `If-None-Match` → 304 (list, `/1`, categorías), invalidación de tag tras Create/Update/Delete en productos y categorías, pedidos/users sin ETag.

### Replicar en CQRS

Mismo `OutputCacheConfig` + atributos en los GET anónimos; en CQRS la invalidación por tag debe ir por los **eventos/handlers** de MediatR tras CUD (o en el mismo servicio), y no olvidar `AddOutputCache`+`UseOutputCache` en su `Program.cs`. Ojo: si el CQRS ya usara `ResponseCaching`, decisión cerrada = **Opción A** (OutputCache, no ResponseCaching en los mismos endpoints).

---

## Fase 9 — `ToHttpResult()` centraliza los 31 `error switch` (`a7352de`) ✅

- **Nuevo:** `TiendaApi.Api/Extensions/DomainErrorExtensions.cs` → `public static IActionResult ToHttpResult(this DomainError error)` con switch tipado.
- **Matriz (preserva códigos actuales):** `NotFoundError`→404 · `ValidationError`→400 con `{message, errors}` · `ConflictError`→409 · `BusinessRuleError`→400 · `UnauthorizedError`→401 · `ForbiddenError`→403 · default/`InternalError`→500.
- **Sustituidos los 31 switches** en los 5 controladores (Users 9 · Pedidos 8 · Productos 7 · Categorías 5 · Auth 2): `onFailure: error => error.ToHttpResult()` en `Match` y `return resultado.Error.ToHttpResult()` en flujos `IsSuccess`/DELETE. Grep `error switch` en `Controllers/` → 0. `using TiendaApi.Api.Extensions` en los 5.
- **Divergencias auditadas (decisiones):** ① `BusinessRuleError` **500 → 400** donde no había rama (2 tests actualizados: `Delete_CategoriaConProductos_RetornaBadRequest`, `Update_ConflictoDeStock_RetornaBadRequest` — este asertaba 500 con nombre "BadRequest"). ② `ValidationError` unificado a `{message, errors}` (9 endpoints ya, 8 solo `{message}`) sin cambiar códigos.
- **Neto:** -120 líneas (84 insertadas / 204 borradas).
- **Verificación:** build 0/0 · 1034 unit · **en vivo 15/15**: 401 sin token · 403 rol USER en `/api/users` · 404×4 con `{message}` · 400 signup inválido · 409 signup duplicado · regresiones 200.

### Replicar en CQRS

Los 5 controladores CQRS (o los handlers que devuelvan `Result`) pueden usar la misma extensión: copiar `DomainErrorExtensions.cs` y sustituir sus `error switch`/`Match` por `error.ToHttpResult()`. Verificar primero con tests qué código devolvían para preservarlos (la divergencia BusinessRule 500→400 es intencionada).

---

## Fase 8 — Migraciones EF Core con baseline (`ee65489`) ✅

- **Problema:** `EnsureCreated` no altera BDs creadas → los índices de la 1B no llegaban a producción/volumen persistente.
- **Nuevo:** `TiendaApi.Api/Data/TiendaDbContextFactory.cs` → `IDesignTimeDbContextFactory<TiendaDbContext>` que lee `appsettings.json` desde `AppContext.BaseDirectory`; **evita ejecutar `Program.cs` en design-time** (si no, `dotnet ef` ejecutaría `EnsureDeleted + EnsureCreated` en dev). `dotnet-ef` global 10.0.7 → 10.0.12.
- **Migraciones** en `TiendaApi.Api/Migrations/`:
  - `20260925065103_InitialCreate` — tablas + FK + los 3 índices únicos preexistentes.
  - `20260925065258_AddOptimizationIndexes` — los 5 índices de Fase 1B (con `Down` → `DropIndex`).
  - Generadas editando a mano `InitialCreate` + `ModelSnapshot` + Designer (quitando los 5 `HasIndex`) para que la segunda migración solo materialice los nuevos; `dotnet ef migrations has-pending-model-changes` → "No changes".
- **Arranque por entorno** (`DatabaseInitializationExtensions.cs`):
  - **Producción:** `ApplyPendingMigrationsAsync` (Migrate) con **baseline**: si `InitialCreate` está pendiente **y** la tabla `categorias` ya existe (BD creada con `EnsureCreated`, sin `__EFMigrationsHistory`) → crea la tabla de historial y marca `InitialCreate` como aplicada **sin ejecutarla** (helpers: `CategoriasTableExistsAsync` con ADO `information_schema.tables`; `ExecuteSqlAsync(FormattableString)` — `ExecuteSqlRawAsync` con interpolación da **EF1002**).
  - **Desarrollo:** se mantiene `EnsureDeleted + EnsureCreated` (decisión explícita; más rápido).
- **Ojo con `launchSettings.json`:** fuerza `ASPNETCORE_ENVIRONMENT=Development` → para probar prod: `dotnet run --no-launch-profile` + env var `ConnectionStrings__DefaultConnection`.
- **Verificación (en vivo):** DROP de los 5 índices (psql por **stdin**: los nombres EF van entre comillas, case-sensitive) → arranque Production → logs de baseline + "pendientes antes: [InitialCreate, AddOptimizationIndexes]" → psql `\di`: 8 índices · `__EFMigrationsHistory`: 2 filas · **datos intactos** (2 users / 3 categorías / 3 productos). Build 0/0 · 1034 unit.

### Replicar en CQRS

1. `dotnet ef migrations add InitialCreate` en el DbContext CQRS (o copiar la carpeta `Migrations/` si el modelo es idéntico — **no lo es en CQRS**; mejor generarla).
2. Copiar `TiendaDbContextFactory.cs` y `ApplyPendingMigrationsAsync` + baseline (mismo problema con BDs creadas con `EnsureCreated`).
3. En dev, decisión: mantener drop+create (como aquí) o migrar siempre.
4. `Reset-Database.ps1` queda como reset manual (sin cambios).

---

## Fases pendientes (se documentarán aquí tras su commit)

| Fase | Qué se documentará |
|------|--------------------|
| **5 · Verificación global** | Resultado de build + unit + integración + E2E Bruno/Newman + smoke (`/health`, `/swagger`, GraphQL, 304, logs Task.Run) |
| **6 · Polly educativa** | Paquetes, `Infrastructures/PollyConfig.cs` (Retry/CircuitBreaker/Timeout), envoltura de `MailKitEmailService`, comparación con `MaxRetries` a mano |
| **7 · Automation E2E (Node)** | `TiendaApi.Tests.E2E/Automation/test-runner.mjs`: diseño, cobertura por controlador, rate-limit awareness, ejecución en CI |
| **10 · Documentación didáctica** | Secciones insertadas en cada `doc/NN-*.md` existente (nada nuevo creado) |

---

*Replicado con éxito el: (fecha de replicación en CQRS)*
