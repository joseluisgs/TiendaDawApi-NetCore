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
| 5 · Verificación global | `131ec3c` | Build 0/0 · 1034 unit · integración 161 · E2E 95/95 · smoke | ✅ |
| 11 · Infra Docker saludable + imágenes | `6f4cfce` | healthchecks, `mongo:7.0` único, composes E2E oficiales, `retryWrites` | ✅ |
| 6 · Polly educativa | `fc0ee21` (+`70eb1d0` docs) | Retry + CircuitBreaker + Timeout en email | ✅ |
| 7 · Automation E2E (Node) | `227cb9d` | `test-runner.mjs` de todos los controladores (55/55) | ✅ |
| 10 · Documentación didáctica | `f6acd04` (+`79e857b` docs) | Secciones en `doc/NN-*.md` existentes | ✅ |
| 12 · README | `afe3862` | README: 5 errores, comandos E2E reales, estructura, estado actual | ✅ |

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
| Serilog.Sinks.Console | 6.0.0 | StackExchange.Redis | 2.8.16 | **3.3.1** | major (compatible con caching 10.0.12: `>= 2.7.27`) |
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

## Fase 7 — Automation E2E en Node (`227cb9d`) ✅

- **Nuevo:** `TiendaApi.Tests.E2E/Automation/test-runner.mjs` (~660 líneas, **Node nativo, sin npm install**), al estilo del runner de referencia UD02.
- **Qué hace una ejecución** (`node TiendaApi.Tests.E2E/Automation/test-runner.mjs` desde la raíz):
  1. Lee `BASE_URL` (por defecto `http://localhost:5031`) y comprueba si la API ya responde en `/health` → `/swagger` → `/api/productos`.
  2. Si no responde: `docker compose -f docker-compose.local.yml up -d` **solo de los servicios parados** (`postgres`, `mongodb`) — nunca `down -v`; si no hay Docker o compose, continúa y avisa.
  3. `dotnet restore` + `dotnet build TiendaApi.slnx` + `dotnet run --project TiendaApi.Api --no-launch-profile` con `ASPNETCORE_ENVIRONMENT=Development` y `ASPNETCORE_URLS=http://localhost:5031` (fondo), esperando `/health` → 200 (máx. 60 s).
  4. Ejecuta la suite HTTP y, al terminar, **mata la API** y hace `stop` **solo** de los servicios que él mismo levantó (si la API/infra ya estaban, no los toca). Fallback: si `dotnet run` no arranca, intenta `docker compose up -d --build`.
- **Suite: 55 tests, 11 bloques** — Health (1) · Auth (5: signup 201/400, signin admin/user 200, 401) · Categorías (9) · Productos (11: incluye filtros `precioMax`, `GET /categoria/{id}`, PATCH) · Pedidos usuario (6) · Pedidos admin (7: 401/403/200, header `Link`, `PUT estado`, DELETE) · Users admin (7) · Users perfil (3) · Storage (404) · GraphQL (4: queries `productos`/`categorias`/`producto(id)` + mutation sin auth → error) · Limpieza (borra el usuario de signup).
- **Rate-limit aware:** la suite entera usa **18 `POST`** (< límite `POST:*` 20/min); auth usa 5 (< 10/min); helper `st()` convierte cualquier 429 en **FAIL explícito** con el aviso de esperar 1 min.
- **Aserciones con semilla:** `admin/admin` · `userdaw/userdaw`; los datos creados llevan `auto_<timestamp>` y se borran al final (idempotente entre ejecuciones).
- **Resultado en vivo (25/09/2026):** **Total 55 · OK 55 · KO 0**. Ajuste tras la 1ª pasada: `producto.id` de GraphQL puede venir como `string` (aceptado) y el mensaje de autorización es *"not authorized"* (substring `authoriz`). Build **0/0** · **1034 unit** verdes.
- **Fuera del runner HTTP (sin npm):** WebSockets/SignalR y subidas de fichero → siguen en Bruno.

### Replicar en CQRS

1. Copiar el archivo `TiendaApi.Tests.E2E/Automation/test-runner.mjs` **y ajustar**:
   - `CONFIG.project` → el `.csproj` de la API CQRS y `CONFIG.baseUrl` si cambia el puerto.
   - Rutas: en CQRS los endpoints pueden diferir (MediatR/otros controllers) → revisar cada `req()` contra los controllers CQRS; mantener los mismos bloques y códigos esperados (201/400/401/403/404/204).
   - Los `assert` de body (`items`, `totalCount`, `token`, `message`) son el **contrato** que debe preservar la API CQRS.
2. Mantener la misma filosofía de seguridad: solo `stop` de BDs que el runner levante, nunca `down -v`; esperar siempre `/health` (Fase 1) antes de la suite.
3. Ejecutar tras replicar cada fase CQRS: es el "**¿sigue todo en verde?**" de 55 puntos antes de pasar a la siguiente.
4. CI (opcional, pendiente): job de GitHub Actions con Docker services (PG+Mongo), Node y .NET SDK corriendo este mismo script.

---

## Fase 5 — Verificación global (`131ec3c`) ✅

- **Qué se verificó (todo en vivo, 25/09/2026):**
  - Build `dotnet build TiendaApi.slnx -c Debug` → **0 errores / 0 warnings**.
  - Unit `--filter "FullyQualifiedName~Unit"` → **1034/1034** (cobertura Api 62.09% líneas).
  - Integración con Testcontainers → **161 OK · 0 errores · 32 omitidos** (EF-272 conocido).
  - **E2E con Newman** sobre `TiendaApi.Tests.E2E/Postman-Cli` → **95 assertions, 0 fallos**, en **4 tandas** (exit 0 cada una) separadas por **61 s** para respetar `POST:*` 20/min y auth 10/min (la colección hace 29 POST). Newman corrió de temp (`npm i newman --prefix …`, sin instalación global) vía `node …\newman\bin\newman.js`, con `--delay-request 200` y `--export-environment` entre tandas (los tokens/ids viajan en el environment).
  - Smoke: `/health` 200 · Swagger en `/` (200) + `/swagger/v1/swagger.json` 200 · GraphQL 200 · **ETag 2º GET → 304** · logs: **0 excepciones / 0 ERR**, stderr vacío.
  - Automation de la Fase 7 → 55/55.
- **E2E con Bruno** (ampliación, commits `1780ef6` + `8d1d5d9`): `@usebruno/cli` **4.2.0** instalado en temp (`npm i --prefix …`, sin global), ejecución sobre `Bruno-Local` → **64/64 requests · 108/108 tests · 0 fallos** en **corrida única** con `--delay 3200` (≤20 POST por ventana de 60 s). Ojo: **no se puede ejecutar por tandas** — `bru.setVar` no sobrevive entre invocaciones de `bru run`, así que al partir la colección los tokens se pierden y todo cae en 401/405. Fuera del run: `6 - USUARIOS` (carpeta **vacía**) y `12 - WEBSOCKETS` (sin soporte WS en la CLI).
- **BUG HALLADO Y CORREGIDO (`1780ef6`):** el test Bruno `[019] PUT - Actualizar (Admin)` demostró que `CategoriaService.UpdateAsync` **ignoraba `dto.Descripcion`** (solo copiaba `Nombre`) → `200 OK` con la descripción vieja. Fix: `categoria.Descripcion = dto.Descripcion;` + test unit que ahora cubre ambos campos. Verificado: build 0/0, unit 1034, integración 161/0/32.
- **Colecciones Bruno arregladas** (`8d1d5d9`, Local + Cli, detalle en `FASES-MEJORAS.md` → "Arreglos aplicados a las colecciones Bruno"): environment `5000`→`5031`/`user`→`userdaw`/passwords, `graphqlProductoId` declarada (sin ella `[070]`/`[071]` no parseaban), 10 tests con shape/código antiguo (`errors` RFC 9457, `totalCount`, `imagen`, `destinatario`/`items`, id string, substring `authoriz`, `data null`), mismos códigos que Postman (`[028]`→400, `[039]`/`[041]`→404), orden de la carpeta 4 (`[035]` tras `[036]`).
- **Colección Postman arreglada** (8 puntos, detalle en `FASES-MEJORAS.md` → "Arreglos aplicados a la colección"): JSON inválido (3 items), variables de colección pisadas por el environment (unificar en `pm.environment.*`), **auth raíz heredada** (`Bearer {{adminToken}}` → `noauth` en los tests "sin auth"), GraphQL al esquema actual (**HotChocolate 16**: `Long!`, `create/update/deleteProducto`, `Create/UpdateProductoInput`), orden de carpetas (crear antes de consultar; `[043b]` admin crea su pedido; `[051]` borrar cuenta al final), códigos reales (pedido inexistente **404**, `categoriaId` inválida **400**), `pm.response.code`, `--delay-request`.
- **Bruno:** no hay CLI instalado (`bruno`/`newman` no estaban en el equipo) → la vía ejecutable es **Newman** con la colección Postman; las colecciones `.bru` (Bruno-Local/Bruno-Cli) quedan para uso manual/IDE.

- **Bruno (ejecutable):** CLI `@usebruno/cli` 4.2.0 en temp + colecciones `.bru` ya alineadas (ver arriba). Newman sigue siendo la vía para la colección Postman.

### Replicar en CQRS

1. Ejecutar **las mismas comprobaciones** contra la API CQRS (build, unit, integración, **Newman**, **Bruno**, smoke, automation) — son la **condición de "sigue en verde"** tras replicar cada fase.
2. **Copiar las colecciones ya arregladas** (`Postman-Cli`, `Bruno-Local`, `Bruno-Cli`): están alineadas con la API actual (rutas, GraphQL, códigos, shapes) y revisar solo lo que cambie en CQRS (si MediatR altera algún código de error o el esquema GraphQL).
3. El runner de la **Fase 7** (`test-runner.mjs`) es el equivalente rápido sin dependencias: mantener ambos.
4. Rate limit: Newman → tandas + espera 61 s; **Bruno → corrida única con `--delay 3200`** (nunca por tandas: se pierden los `bru.setVar`). Mientras `RateLimitConfig.cs` tenga `POST:*` 20/min, recalcular si cambian las reglas (Postman: 29 POST · Bruno: 64 requests).
5. Reproducir en CQRS el **fix de `CategoriaService`** (`Descripcion` en el PUT) o comprobar que CQRS lo hereda desde el inicio.
6. Documentar en esta bitácora los resultados de CQRS con el mismo formato de tabla.

---

## Fase 11 — Infra Docker saludable + unificación de imágenes (`6f4cfce`) ✅

> Ejecutada **antes de la Fase 6** (Polly) por petición explícita, aunque numerada al final (el plan 0-10 ya estaba cerrado). Detalle completo en `FASES-MEJORAS.md` → "Fase 11".

- **Imagen única:** `mongo:7` → `mongo:7.0` en `docker-compose.local.yml` y `docker-compose.prod.yml`; literales de test centralizados en la nueva constante `TestContainerImages.cs` (10 ficheros, 18 llamadas a `MongoDbBuilder`/`PostgreSqlBuilder`); tag legacy `mongo:7` eliminada de Docker. Resultado: **una sola tag por base de datos** en `docker images` (`mongo:7.0` + `postgres:17-alpine`), sin duplicados.
- **Compose local:** `start_period: 30s` en healthchecks de postgres y mongo; `depends_on: condition: service_healthy` en adminer y mongo-express (antes esperaban solo a que el contenedor arrancara, no a que estuviera sano).
- **Compose prod:** **fix de indentación YAML** — 3 líneas del `environment` de `api` tenían 7 espacios en vez de 6 (`MongoDbSettings__DatabaseName`, `MongoDbSettings__PedidosCollection`, `Pedidos__RepositoryType`); `start_period: 30s` en los healthchecks (postgres/mongo/redis). Requiere un `.env` local (está en `.gitignore`): para validar, copiar temporalmente desde `.env.prod.example` y borrar.
- **Compose E2E `Bruno-Cli`:** reescrito con la **imagen oficial `usebruno/cli`** (entrypoint `bru`, workdir `/bruno`; verificada en el registry con `docker manifest inspect`) — antes usaba `node:20-alpine` + `@usebruno/cli@1.4.0` antiguo y un comando inválido. Ahora: `run . --env-file environments/local.bru --delay 3200`, reporters nativos (json/junit/html → `/reports`), `extra_hosts: host.docker.internal:host-gateway`, `restart: on-failure:3`, colección montada `:ro`.
- **Compose E2E `Postman-Cli`:** la colección y el environment estaban montados en `/etc/newman/collections` mientras el `working_dir`/`run` los buscaba en la raíz (**el compose no funcionaba**); además Newman no lee la env `BASE_URL` → añadido `--env-var baseUrl=…`, `--delay-request 3200` (rate limit `POST:*` 20/min) y `restart: on-failure:3`.
- **Mongo, reconexión a nivel de driver:** `&retryWrites=true&retryReads=true` en las **12 connection strings** `mongodb://` de 6 ficheros (`appsettings.json`/`Development`/`Production`, `.env.development`, `.env.example`, `docker-compose.prod.yml`).
- **EF Core `EnableRetryOnFailure`: NO se activa (decisión, no olvido):** `PedidosService.cs:458` usa `BeginTransactionAsync` (transacción explícita) y con *retrying strategy* EF lanza `InvalidOperationException`. El patrón oficial (`CreateExecutionStrategy().ExecuteAsync(...)`) tocaría el flujo central de `POST /api/pedidos/me` → riesgo alto fuera de alcance.
- **`.gitignore`:** carpetas `TiendaApi.Tests.E2E/**/reports/` (informes generados por los compose).
- **Verificado:** `docker compose config` **4/4 OK** · `docker compose -f docker-compose.local.yml up -d` → postgres y mongo **healthy** con la nueva tag · `docker images` sin duplicados · build **0/0** · unit **1034/1034** · integración **161/0/32** (con `TestContainerImages` en vivo).

### Replicar en CQRS

1. Copiar los 4 compose (`docker-compose.local.yml`, `docker-compose.prod.yml`, `Bruno-Cli/`, `Postman-Cli/`) y ajustar solo las connection strings propias de CQRS.
2. Copiar `TiendaApi.Tests/Integration/TestContainers/TestContainerImages.cs` y sustituir los literales de imagen de `MongoDbBuilder`/`PostgreSqlBuilder` en los tests de integración.
3. Añadir `retryWrites=true&retryReads=true` a las connection strings Mongo de CQRS (appsettings + `.env*` + prod).
4. **No** activar `EnableRetryOnFailure` mientras haya transacciones explícitas (mismo motivo: `BeginTransactionAsync`).
5. Validar: `docker compose config` ×4 → `up -d` → health → `docker images` sin `mongo:7` suelto.

---

## Fase 6 — Polly educativa (`fc0ee21`) ✅

- **Paquete:** `Polly` **8.8.0** (v8, `ResiliencePipeline`). `Microsoft.Extensions.Http.Polly` **NO**: solo aporta policies para `HttpClient` y la API no tiene llamadas salientes reales (coherente con "Fuera de alcance").
- **`Infrastructures/PollyConfig.cs`:** pipeline del email = **Retry(3, backoff exponencial 2^n → 1s/2s/4s) → CircuitBreaker(ratio 100%, mínimo 3 fallos, abierto 30s) → Timeout(10s por intento)** con logs Serilog en cada transición (`OnRetry`, `OnOpened`, `OnClosed`, `OnHalfOpened`; en Polly 8.8 esos son los nombres reales, no `OnBreak`/`OnReset`). Builders públicos (`EmailRetryOptions`, `EmailCircuitBreakerOptions`, `BuildEmailPipeline`) para poder testear con `delay: TimeSpan.Zero`.
- **`MailKitEmailService`:** el bloque SMTP (`Connect → Auth → Send → Disconnect`) va dentro del callback de `ExecuteAsync` — un `SmtpClient` por intento —. El pipeline se registra como singleton en `EmailConfig.AddEmail` (rama prod/MailKit) y entra por el constructor **opcional** → los 11 call sites de los tests existentes no tuvieron que cambiar.
- **Fallback (6.4):** circuito abierto → `LogWarning` + `BrokenCircuitException` relanzada; timeout → `LogWarning`; el caller es `EmailBackgroundService` (try/catch) → **el request HTTP nunca falla por email**.
- **Comparación con el reintento a mano (6.5):** `PedidosService.cs:40` (`MaxRetries = 3` + bucle `for` + `Task.Delay`) frente al pipeline declarativo, testeable por partes y con backoff/cortacircuitos/timeout.
- **Tests (+5 → unit 1039):** `Unit/Infrastructures/PollyConfigTests.cs` — falla 2× y al 3º OK (3 intentos) · siempre falla → 4 intentos (1 original + 3 reintentos) y propaga · CB: 3 fallos abren y la 4ª llamada no ejecuta el callback (`BrokenCircuitException`) · pipeline completo: el retry no insiste con el circuito abierto.
- **6.7 (endpoint demo `GET /api/demo/polly`):** no realizado (era opcional) — no existe controlador demo y añadiría superficie HTTP solo con fines didácticos; los 5 tests cubren el comportamiento.
- **Verificado (6.8):** build **0/0** · unit **1039/1039** · integración **161/0/32** · runner **55/55** · smoke: health OK, Swagger 200, ETag→304 · logs **0 excepciones / 0 ERR**.

### Replicar en CQRS

1. Copiar `Infrastructures/PollyConfig.cs`, `PollyConfigTests.cs`, el `MailKitEmailService` y el registro del singleton en `EmailConfig`.
2. Añadir `<PackageReference Include="Polly" Version="8.8.0" />` al csproj de la API CQRS.
3. Regla: el pipeline de email **nunca** se invoca dentro del request HTTP — que el envío siga vía background service (o añadirlo si CQRS no lo tiene).
4. Verificar: build 0/0 · unit (+5) · runner E2E 55/55.

---

## Fase 10 — Documentación didáctica (`f6acd04`) ✅

- **Regla cumplida:** **0 documentos nuevos**; 9 ficheros `doc/NN-*.md` modificados (**730 líneas añadidas**) con código real del proyecto y TOC actualizado en todos.
- **10.1** → `doc/11` 11.6: subsección *Opción C aplicada: `ToHttpResult()`* (el `DomainErrorExtensions` real, 31 call sites en 5 controladores, tabla de enfoques).
- **10.2** → `doc/10`: nueva **10.10 "Caché HTTP con OutputCache y ETag"** (Resumen → 10.11): `OutputCacheConfig`, `[OutputCache(60, tags)]`, `EvictByTagAsync` de `ProductoService`/`CategoriaService`, flujo mermaid 200→304 y tabla frente a cache-aside Redis.
- **10.3/10.4** → `doc/06`: 6.7 subsección *Patrón real (Fase 4)*; 6.3 subsección *Paginación real (Fase 3)* con `Skip/Take` (EF) y `Skip/Limit` (Mongo) reales.
- **10.5** → `doc/08` 8.5: subsecciones *factory design-time* (`TiendaDbContextFactory`), *migraciones reales* (`InitialCreate` + `AddOptimizationIndexes`), *baseline en BD existente* (el `ApplyPendingMigrationsAsync` real) y *dev vs prod*.
- **10.6** → `doc/27`: 27.3 subsección *Índices reales (Fase 1)* (los 9 `HasIndex` del `TiendaDbContext`); 27.5 subsección *AsNoTracking selectivo (Fase 2)* (9 sitios + los que NO se tocan y por qué).
- **10.7** → `doc/22`: nueva **22.12 "Fire & Forget Endurecido"** (Resumen → 22.13): patrón, inventario real (29 `Task.Run`), 3 reglas y tabla de cuándo NO usarlo.
- **10.8** → `doc/13` 13.3 subsección *Polly: dónde está y dónde NO* (loop a mano de `PedidosService`, por qué no `EnableRetryOnFailure`, tabla por I/O) + `doc/21`: nueva **21.10 "Resiliencia con Polly (Fase 6)"** (Resumen → 21.11).
- **10.9** → `doc/24`: nueva **24.15 "Automation E2E con Node"** (tras 24.14): modos auto/externo, tabla de los 55 checks, pirámide de testing (mermaid).
- **10.10 Verificación:** TOC↔headings coherentes en los **9** docs (script) · `git status` → **0 ficheros nuevos** · build **0/0** · unit **1039/1039**.

### Replicar en CQRS

1. **Copiar los 9 doc/ tal cual** si CQRS mantiene la misma arquitectura; si MediatR cambia algún código/contrato, ajustar solo los snippets (los docs citan ficheros reales: revisar rutas de CQRS).
2. Verificar TOC tras cualquier renumeración (las secciones nuevas renumeraron el Resumen en 10, 21 y 22).

---

## Fase 12 — README al día con las fases 0-11 (`afe3862`) ✅

- **Alcance:** solo `README.md` (+116/−84), **sin código**. Cierra la deuda del README frente a las 12 fases anteriores.
- **5 errores corregidos:**
  - `dotnet run --project TiendaApi.Apis` → **`TiendaApi.Api`** (el proyecto citado no existe).
  - Acceso dev `localhost:5000` → **`localhost:5031`** (launchSettings real; 5000 es prod, `API_PORT`); añadido `GET /health`.
  - Tecnologías: `PostgreSQL 15` → **17** (`postgres:17-alpine`).
  - Rutas E2E `TiendaApi.ApiTests/{Postman,Bruno}` (carpeta **inexistente**) → **`TiendaApi.Tests.E2E/{Postman-Cli,Bruno-Cli,Bruno-Local,Automation}`**.
  - `bru run … --env <fichero>` → **`--env-file`** + `--delay 3200` (rate limit `POST:*` 20/min); `docker-compose` (v1) → `docker compose`; prod: `cp .env.prod.example .env`.
- **Actualizado al estado real:** pirámide con cifras (**1039 unit · 161 integración · 95 Newman · 108 Bruno · 55 runner**); +5 características (health, OutputCache+ETag/304, Polly, paginación real, `ToHttpResult`); **Polly 8** en tecnologías; nueva subsección **Automation (Node)** (auto/externo); árbol de proyecto real (retirado `docker-compose.yml` de raíz que **no existía**; subárbol E2E con `Automation/`, `Postman-Cli/`, `Bruno-Local/`, `Bruno-Cli/`; `FASES-MEJORAS.md`/`BITACORA.md`/`.env*`; `Services/Users` + `Cache/` + `Extensions/`; tabla con `StorageController` y fila **Extensions**); endpoints de ejemplo (`imagen` GraphQL y `WS ws://…`) → `5031`.
- **Hallazgos (verificados en vivo):**
  1. **`bru run .` en `Bruno-Local` falla**: arrastra `12 - WEBSOCKETS` (la CLI no soporta WS y `basews` no está en el env). README documenta la **lista explícita de 10 carpetas**, validada con `@usebruno/cli` 4.2.0 → **64 requests** (mismo conteo que la Fase 5). `6 - USUARIOS` está vacía (PASS con 0).
  2. **`--env-file` acepta `.json`** según el `--help` de la CLI 4.2.0 → el environment JSON de `Bruno-Local` es válido.
  3. Los composes E2E **no levantan la API** (apuntan a `host.docker.internal:5031`) → ahora es requisito explícito en la sección.
- **Verificación:** TOC **66/66** anclas resueltas (script, algoritmo GitHub) · **18 paths** citados existen en disco · **0 restos** de referencias viejas · sin cambios de código (build/tests intactos).

### Replicar en CQRS

1. Revisar el README del repo CQRS con la misma lista: nombre de proyecto (`TiendaApi.Api` o el real), puerto dev, rutas E2E y comandos de compose.
2. Las cifras de la pirámide de tests **son las de este repo**; en CQRS habrá que poner las suyas (unit/integración/E2E propios).
3. Si CQRS usa las mismas colecciones (`Postman-Cli`, `Bruno-*`), hereda el comando de las 10 carpetas del Bruno Local.

---

## Fases pendientes

**Ninguna — las 13 fases del plan (0-12) están completadas y documentadas.**

---

*Replicado con éxito el: (fecha de replicación en CQRS)*
