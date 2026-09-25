# Fases de Mejora — TiendaDawApi (optimización + calidad)

> **Proyecto:** `TiendaDawApi-NetCore` (API sin CQRS)  
> **Rama:** `feature/polly`  
> **Base:** doc UD02 · §23 *Optimización de Servicios Web*  
> **Regla de oro:** no romper nada de lo existente (E2E Bruno/Newman, tests unitarios, flujos de pedidos).

---

## Decisiones cerradas

| Decisión | Acuerdo |
|----------|---------|
| Efectos secundarios (email, WS, SignalR, cache) | **Fire & forget** con `_ = Task.Run` — **NO** `await Task.WhenAll` (el email no debe bloquear la respuesta HTTP) |
| Calidad del fire & forget | Sí endurecer: `try/catch + LogError` en el interior de cada `Task.Run` (~30 sitios) |
| `AsNoTracking` | Selectivo en **solo lectura**; **no tocar** `FindByIdAsync` (lo comparten Update/Delete/soft-delete) |
| Caché HTTP | **Opción A: OutputCache + ETag + 304** (no ResponseCaching en los mismos endpoints) |
| Polly | Solo **fase educativa** aditiva (email); no hay `HttpClient` saliente real en la API |
| Migraciones | Sí: salir de `EnsureCreated` puro para que **los índices se apliquen en BD existente** |
| Integración Result→HTTP | **Opción C: extensión `ToHttpResult()`** (doc UD02 §7) — 31 `error switch` → 1 extensión; **preservar códigos actuales** |

---

## Fase 0 — Baseline ✅ COMPLETADA (24/09/2026)

| # | Tarea | Verificación |
|---|-------|--------------|
| 0.1 | `dotnet build` + unit tests como referencia | Build 0 errores 0 warnings (`TreatWarningsAsErrors`), **1034 tests unitarios verdes** |
| 0.2 | Fix `NU1902` SharpCompress 0.30.1 (CVE zip-slip, transitivo de MongoDB.Driver 3.6.0) | Paquetes MongoDB actualizados → SharpCompress 0.48.1 |
| 0.3 | Actualización general de dependencias a últimas versiones estables | `dotnet list package --outdated` y `--vulnerable` limpios (ver tabla) |

### Actualización de paquetes (0.3)

**Api:** AutoMapper 16.2.0 · BCrypt 4.2.0 · CSharpFunctionalExtensions 3.7.0 · FluentValidation 11.3.1 · **HotChocolate 16.6.7** · MailKit 4.18.0 · JwtBearer 10.0.12 · EF Core 10.0.12 · MongoDB.Bson 3.12.0 · Npgsql 10.0.3 · Serilog 10.0.0 · StackExchange.Redis 3.3.1 · **Swashbuckle 10.2.3** · System.IdentityModel 8.23.0 · **`Microsoft.AspNetCore.Mvc.Versioning` (deprecated) → `Asp.Versioning.Mvc` 10.2.1 + ApiExplorer**

**Tests:** coverlet 10.0.1 · FluentAssertions 7.2.2 · Mvc.Testing 10.0.12 · Test.Sdk 18.10.1 · Moq 4.21.0 · NUnit 4.6.1 · NUnit3TestAdapter 6.3.0 · Testcontainers 4.15.0 · MongoDB.Driver 3.12.0

**Adaptaciones de código por saltos major:**
- `SwaggerConfig.cs`: Microsoft.OpenApi 2.x (tipos en raíz, `OpenApiSecuritySchemeReference`, `AddSecurityRequirement(Func<OpenApiDocument,…>)`)
- `ApiVersioningConfig.cs`: cadena `.AddApiVersioning(…).AddApiExplorer().AddMvc()` (analizadores AV0013/AV0021)
- Tests: constructores de Testcontainers con imagen (`new MongoDbBuilder("mongo:7.0")`), `v!` en matchers Moq 4.21, `HotChocolateCompositeImplicitUsings=disable` (colisión `Is` con NUnit)

**Excepciones intencionadas (no actualizar):**
- `AutoMapper.Extensions.MS.DI 12.0.0` → 12.0.1 exige `AutoMapper = 12.0.1` exacto (rompería el 16.2.0)
- `FluentAssertions 7.2.2` → v8 cambió a licencia Xceed (solo gratis no-comercial); rama 7 = Apache 2.0

---

## Fase 1 — Rápido y de bajo riesgo ✅ COMPLETADA (24/09/2026)

### 1A · Health Checks (#10)

| # | Tarea | Archivos |
|---|-------|----------|
| 10.1 | `Infrastructures/HealthChecksConfig.cs`: `AddHealthChecks()` con PG (`CanConnectAsync`), Mongo (ping); Redis si prod. **Sin paquetes NuGet** (checks propios) | nuevo |
| 10.2 | `MapHealthChecks("/health", ...)` con JSON (`status`, `checks[]` con `name`, `status`, `duration`) | `HealthChecksConfig.cs` |
| 10.3 | Registrar en `Program.cs` (servicios + endpoint) | `Program.cs` |
| 10.4 | Verificar | `GET /health` → 200; BD caída → 503 |

> El Bruno `[001] Health Check` y el Automation ya esperan `GET /health`.

### 1B · Índices EF en el modelo (#2)

| # | Tarea | Archivos |
|---|-------|----------|
| 2.1 | `Producto`: `HasIndex(CategoriaId)`, `HasIndex(CreatedAt)`, `HasIndex(IsDeleted)`, compuesto `(CategoriaId, Precio)` | `Data/TiendaDbContext.cs` |
| 2.2 | `User`: `HasIndex(Role)` | `Data/TiendaDbContext.cs` |
| 2.3 | **Solo añadir**, no quitar índices existentes (categorías/users únicos) | — |
| 2.4 | Aplicación en BD viva → **Fase 8 (migraciones)**; en dev con drop+create bastan | — |

### 1C · Endurecer `Task.Run` (FF)

| # | Tarea | Archivos |
|---|-------|----------|
| FF.1 | Inventario de ~30 `_ = Task.Run(...)` | grep |
| FF.2 | Interior con `try { ... } catch (Exception ex) { logger.LogError(ex, "..."); }` — **sin await, sin WhenAll** | `ProductoService` (~13), `PedidosService` (~11), `CategoriaService` (2), `UserService` (3) |
| FF.3 | Verificar | Build; crear producto → HTTP rápido + logs sin excepciones en background |

### ✅ Verificación Fase 1 (24/09/2026)

| # | Resultado |
|---|-----------|
| 1A | `Infrastructures/HealthChecksConfig.cs` nuevo: `AddHealthChecks(environment)` → PG (`CanConnectAsync`), Mongo (ping con tope de 5 s), Redis (vía `IDistributedCache`, solo fuera de dev). `MapHealthEndpoint()` → `GET /health` con JSON `{status, totalDuration, checks[{name, status, duration, description, error}]}` (semáforo `OK`/`DEGRADED`/`ERROR`). `DatabaseConfig` además registra `IMongoClient` en modo EF; `Program.cs` registra servicios + endpoint. **Sin paquetes NuGet nuevos.** |
| 10.4 | **En vivo:** `GET /health` → **200** `{"status":"OK",...}` (cumple el contrato de Bruno `[001]`); con `docker stop tienda-local-mongodb` → **503** `{"status":"ERROR"}` (mongodb en ERROR, postgresql sigue OK); restaurar contenedor → 200. |
| 1B | Índices añadidos en `TiendaDbContext.OnModelCreating`: `productos(CategoriaId)`, `productos(CategoriaId, Precio)`, `productos(CreatedAt)`, `productos(IsDeleted)`, `users(Role)`; los índices únicos existentes intactos (solo añadir). En BD viva se materializarán con la migración de la **Fase 8**; en dev el drop+create ya los crea. |
| 1C | Inventario: **29** `_ = Task.Run` (Pedidos 11 · Producto 13 · User 3 · Categoría 2). 28 ya tenían `try/catch (Exception)` + log interior (caché/WS/SignalR/email/eventos = `LogWarning`; fallo en el flujo de creación de pedido = `LogError`); endurecido el único lambda sin guarda (`UserService`, invalidación de caché) con `try/catch + LogError`. **Sin `await`, sin `WhenAll`.** |
| FF.3 | Build **0/0** · **1034 tests unitarios** verdes (E2E completo → Fase 7). |

---

## Fase 2 — Consultas (`AsNoTracking`) ✅ COMPLETADA (25/09/2026)

| # | Tarea | Detalle |
|---|-------|---------|
| 1.1 | Sí | `FindAllAsync`, `FindAllPagedAsync`, `FindByCategoriaIdAsync`, `GetRecentlyCreatedAsync`, listados de `User` |
| 1.2 | **No tocar** | `FindByIdAsync`, `DeleteAsync` (soft-delete depende de tracking), rutas de `Update` |
| 1.3 | Verificar | GETs OK; **PUT/DELETE** producto/categoría/user OK (E2E) |

### ✅ Verificación Fase 2 (25/09/2026)

| # | Resultado |
|---|-----------|
| 1.1 | `.AsNoTracking()` en **9 sitios de solo lectura**: `CategoriaRepository` (FindAllAsync, items de FindAllPagedAsync) · `UserRepository` (FindAllAsync, items de FindAllPagedAsync, GetActiveUsersAsync) · `ProductoRepository` (FindAllAsync con Include, items de FindAllPagedAsync, FindByCategoriaIdAsync, GetRecentlyCreatedAsync) |
| 1.2 | **No tocados:** `FindByIdAsync` (producto/categoría/user), `FindByUsernameAsync`, `FindByEmailAsync`, `DeleteAsync` y rutas de `Update` (siguen con tracking) |
| 1.3 | Build **0/0** · **1034 unit tests** verdes · **en vivo 23/23 OK** (PG 17 + Mongo): listados GET categorías/productos/by-categoría/users/pedidos → 200 · round-trips POST→PUT→DELETE de producto, categoría y usuario → 201/200/204 · GET tras DELETE → 404 (soft-delete con tracking intacto) |

---

## Fase 3 — Paginación real de pedidos (#4) ✅ COMPLETADA (25/09/2026)

| # | Tarea | Archivos |
|---|-------|----------|
| 4.1 | `FindAllPagedAsync(page, size)` → `(Items, TotalCount)` | `IPedidosRepository.cs` |
| 4.2 | Mongo: `Skip/Limit` + `CountDocuments` | `PedidosNativeRepository.cs` |
| 4.3 | EF: `Skip/Take` + `CountAsync` | `PedidosEfCoreRepository.cs` |
| 4.4 | Servicio delega al repo; **borrar** paginación en memoria (`PedidosService.cs:65-69`) | `PedidosService.cs` |
| 4.5 | Misma firma de servicio → controller intacto | — |
| 4.6 | Verificar | `GET /api/pedidos/paged` devuelve solo `size` |

### ✅ Verificación Fase 3 (25/09/2026)

| # | Resultado |
|---|-----------|
| 4.1-4.3 | `IPedidosRepository.FindAllPagedAsync(page, size)` (base 0) añadido a las 2 implementaciones: **Mongo** (`Filter.Empty` + `CountDocumentsAsync` + `SortByDescending(CreatedAt)` + `Skip/Limit`) · **EF** (`OrderByDescending(CreatedAt)` + `CountAsync` + `Skip/Take`). Mismo orden y misma inclusión de registros que `FindAllAsync` (comportamiento idéntico, sin cambios semánticos) |
| 4.4 | `PedidosService.FindAllPagedAsync` delega al repo; eliminada la paginación en memoria (`FindAllAsync` + `Skip/Take` sobre la lista). `FindMyPedidosAsync` ya delegaba en `FindByUserIdPagedAsync` (intacto) |
| 4.5 | Firmas de servicio y controller **sin cambios**; actualizados los 3 mocks de `PedidosServiceTests` (de `FindAllAsync` a `FindAllPagedAsync`) |
| 4.6 | Build **0/0** · **1034 unit** verdes · **integración pedidos 64 OK** (32 omitidos = EF-272 conocido) · **en vivo 14/14**: `GET /api/pedidos/paged?page=1&size=2` → 200 con **2 items** y `totalCount:3` · página 2 → 1 item · defaults OK · header `Link` (rel=next/last) presente · `me/paged` → 200 |

---

## Fase 4 — Caché HTTP · **Opción A** (OutputCache + ETag) ✅ COMPLETADA (25/09/2026)

| # | Tarea | Archivos |
|---|-------|----------|
| 6.1 | `AddOutputCache`: política 60s + tags `productos`/`categorias` | `Infrastructures/OutputCacheConfig.cs` + `Program.cs` |
| 6.2 | `app.UseOutputCache()` antes de `MapControllers` | `Program.cs` |
| 6.3 | `[OutputCache(...)]` **solo** GET anónimos de `ProductosController` y `CategoriasController` | 2 controllers |
| 6.4 | Invalidación por tag tras CUD (`IOutputCacheStore.EvictByTag`) | services/controllers |
| 6.5 | **Excluir** pedidos, users, auth, GraphQL autenticado | — |
| 6.6 | Verificar | 2º GET → **304**; tras POST/PUT → tag invalidado → 200 con cuerpo nuevo |

### ✅ Verificación Fase 4 (25/09/2026)

| # | Resultado |
|---|-----------|
| 6.1-6.2 | `Infrastructures/OutputCacheConfig.cs` (`AddOutputCacheConfig` + `UseOutputCacheConfig`) registrado en `Program.cs` antes de `MapControllers`; **solo** los endpoints declarados se cachean |
| 6.3 | `[OutputCache(Duration = 60, Tags = ...)]` en los **5 GET anónimos**: productos GetAll/GetById/GetByCategoria + categorías GetAll/GetById. **ETag** fijado en la acción (`Response.Headers.ETag`, patrón doc oficial de MS) → el middleware devuelve **304** con `If-None-Match` |
| 6.4 | `ProductoService`/`CategoriaService` inyectan `IOutputCacheStore` y ejecutan `EvictByTagAsync("productos"/"categorias")` en `InvalidarCache*` (try/catch, background) → cubierto CUD **REST + GraphQL** |
| 6.5 | Pedidos/users/auth sin `[OutputCache]` → sin ETag (verificado en vivo) |
| 6.6 | Build **0/0** · **1034 unit** verdes (2 constructores de tests de controller inicializan `HttpContext` con `DefaultHttpContext`, requerido por `Response.Headers.ETag`) · **en vivo 26/26**: HIT demostrado (2º GET con **misma ETag**) · `If-None-Match` → **304** en list, `/1` y categorías · invalidación tag verificada: Create (3→4), Update (nombre nuevo visible), Delete (4→3) en productos **y** categorías · pedidos/users **sin** ETag |

---

## Fase 5 — Verificación global ✅ COMPLETADA (25/09/2026)

| # | Tarea |
|---|-------|
| 5.1 | `dotnet build` (warnings as errors) |
| 5.2 | `dotnet test --filter "FullyQualifiedName~Unit"` |
| 5.3 | Integration (Docker) |
| 5.4 | E2E Bruno/Newman: auth, productos C/R/U/D, pedidos paged, categorías |
| 5.5 | Smoke: `/health`, `/swagger`, GraphQL, 2º GET → 304, logs Task.Run limpios |
| 5.6 | **Automation Node** (Fase 7) en verde |

### ✅ Verificación Fase 5 (25/09/2026)

| # | Resultado |
|---|-----------|
| 5.1 | `dotnet build TiendaApi.slnx -c Debug` → **0 errores / 0 warnings** (`TreatWarningsAsErrors`) |
| 5.2 | Unit: **1034/1034** en verde (0 con error, 0 omitidos) · cobertura Api: 62.09% líneas / 59.85% ramas / 78.16% métodos |
| 5.3 | Integration (Testcontainers, Docker): **161 OK · 0 errores · 32 omitidos** (EF-272 conocido) · 193 totales · 1 m 11 s |
| 5.4 | **E2E con Newman** (colección Postman) → **95 assertions, 0 fallos, 77 requests** en **4 tandas exit 0** (esperas de 61 s por rate limit `POST:*` 20/min · auth 10/min; 29 POST en la colección; `--delay-request 200`) · **E2E con Bruno** (`@usebruno/cli` 4.2.0 instalado en temp, colección `Bruno-Local`) → **64/64 requests · 108/108 tests · 0 fallos** en **corrida única** con `--delay 3200` (los `bru.setVar` no sobreviven entre invocaciones, así que no se puede ejecutar por tandas: perdería los tokens y saltaría 401/405 en cadena) |
| 5.5 | Smoke en vivo: `GET /health` → **200** `{"status":"OK", checks[...]}` · Swagger en `/` → **200** (HTML) + `/swagger/v1/swagger.json` → 200 · `POST /graphql` → **200** con `data.productos` · **ETag → 304**: 1er GET `ETag: "1cce9ade…"` + `If-None-Match` → **304** · **logs limpios**: 330 líneas, **0 excepciones / 0 ERR/FTL**, stderr vacío (fire & forget sin ruido) |
| 5.6 | Automation Node (Fase 7) → **55/55** verde |

### Arreglos aplicados a la colección `Postman-Cli` durante la verificación

La colección estaba **desactualizada/rota** respecto a la API actual; sin estos arreglos no podía ejecutarse (6 iteraciones de depuración):

| # | Problema | Arreglo |
|---|----------|---------|
| A | **JSON inválido** (3 items de categorías sin cerrar su objeto `request`) → `newman` ni siquiera parseaba | Cerrado el balance de llaves de los 3 items |
| B | **Variables de colección pisadas**: newman prioriza `environment` sobre `collectionVariables` → tokens/ids llegaban vacíos (401 en cadena) | `pm.collectionVariables.*` → `pm.environment.*` (20 sitios), un solo scope de variables |
| C | **Auth raíz heredada**: la colección declara `auth: Bearer {{adminToken}}` a nivel raíz → los tests "sin auth" recibían el token real y devolvían 201/200 | `"auth": {"type":"noauth"}` en `[016]`, `[043]`, `[058]`, `[067]` |
| D | **GraphQL obsoleto** (HotChocolate 16): `Int!` → `Long!` (`categoria`/`producto`), `crearProducto`→`createProducto`, `actualizarProducto`→`updateProducto`, `eliminarProducto`→`deleteProducto`, `ProductoInput`→`Create/UpdateProductoInput` | 8 queries/mutations reescritas al esquema actual (introspección `__schema`) |
| E | **Orden de ejecución**: `[035]` leía `pedidoId` antes de crearlo; la carpeta admin reutilizaba el pedido borrado por el usuario; `[053]` leía `testUserId` antes de `[055]`; `[051]` borraba `userdaw` antes de `[059]` | Reordenados: `[035]` tras `[036]`, `[051]` al final de la carpeta 6, **nuevo `[043b]`** (admin crea su propio pedido al entrar en la carpeta 5) |
| F | **Códigos esperados incorrectos**: ids inexistentes de pedido devuelven **404** (no 403) y `categoriaId` inexistente en producto devuelve **400** (validación), no 404 | Ajustados los asserts de `[039]`, `[041]`, `[028]` al comportamiento real de la API (verificado con curl) |
| G | `pm.response.status` es **string** en newman (rompía `[060]`) | `pm.response.code` |
| H | `--delay` no existe en newman 7 | `--delay-request` |

*Re-ejecución final (BD reiniciada con semilla): **4/4 tandas exit 0**, 0 fallos.*

### Arreglos aplicados a las colecciones Bruno (`Bruno-Local` + `Bruno-Cli`)

Ejecución: `bru run <carpetas> --env-file environments/… --delay 3200 -o results.json --format json` (CLI en temp; sin instalar globalmente). Carpetas fuera del run: `6 - USUARIOS` (**vacía**, sin requests) y `12 - WEBSOCKETS` (bru CLI no soporta WS).

| # | Problema | Arreglo |
|---|----------|---------|
| I | **Environment desactualizado**: `baseUrl:5000` (pisa la `5031` de `collection.bru`), `userUsername:user` (real: `userdaw`), passwords vacíos | Corregido en `Bruno-Local/environments/*.json`; `Bruno-Cli/local.bru` (Docker, `host.docker.internal`) se conserva y se pasa por `--env-var` si se ejecuta desde el host |
| J | **`graphqlProductoId` sin declarar** en `vars:pre-request` → quedaba `{{…}}` literal y `body:graphql:vars` de `[070]`/`[071]` no parseaba (`Expected property name…`) | Declarada con valor inicial `1` en `collection.bru` (ambas colecciones) |
| K | **Tests con shape/código antiguo**: `[003]` esperaba `message` (real: `errors` RFC 9457), `[011]`/`[034]` `totalItems` (real: **`totalCount`**), `[031]` `imagenUrl` (real: **`imagen`**), `[044]` `cliente`/`lineasPedido` (real: **`destinatario`**/**`items`**), `[063]`/`[065]`/`[066]` id `number` (real: **string**), `[067]`/`[068]` mensajes `Unauthorized`/`forbidden` (real: *"The current user is not authorized…"* → substring `authoriz`), `[069]` esperaba array `errors` (real: `data.createProducto: null` sin `errors`) | 10 tests ajustados al comportamiento verificado en vivo |
| L | **Códigos reales** (idéntico a Postman): `[028]` 404→**400**, `[039]`/`[041]` 403→**404** | Asserts corregidos |
| M | **Orden**: `[035]` (lee `pedidoId`) se ejecutaba antes de `[036]` (lo crea) | `seq` reordenados en la carpeta 4 |
| N | **Ejecución por tandas rompe los tokens** (`bru.setVar` vive solo en la sesión del proceso) | Corrida única de las 10 carpetas con `--delay 3200` (≤20 POST por ventana de 60 s) |

### 🐛 Hallazgo en la API (corregido en esta fase)

El test **`[019] PUT - Actualizar (Admin)`** de Bruno descubrió un bug real: `CategoriaService.UpdateAsync` solo copiaba `Nombre` y **ignoraba `dto.Descripcion`** → `200 OK` con la descripción antigua. Corregido (`categoria.Descripcion = dto.Descripcion;`) + test unit ampliado; verificado con build 0/0, unit 1034 y integration 161/0/32. Commits `1780ef6` (fix) y `8d1d5d9` (colecciones Bruno).

---

## Fase 6 — Polly educativa

| # | Tarea | Detalle |
|---|-------|---------|
| 6.1 | Paquetes | `Polly` + `Microsoft.Extensions.Http.Polly` |
| 6.2 | `Infrastructures/PollyConfig.cs` | Retry(3, backoff 2^n) + CircuitBreaker(3, 30s) + Timeout(10s) con `Wrap` + logs Serilog |
| 6.3 | Envolver email | `ExecuteAsync` en `MailKitEmailService.SendEmailAsync` (reintentos **en background**) |
| 6.4 | Fallback | Agota reintentos → `Log.Warning`; el request HTTP **no falla** |
| 6.5 | Doc/comentario | Comparar con `MaxRetries` a mano (`PedidosService.cs:40`) |
| 6.6 | Test | Mock falla 2× y al 3º OK → assert intentos |
| 6.7 | *(opt.)* | Endpoint demo `GET /api/demo/polly` |
| 6.8 | Verificar | Build + test; email caído no rompe POST de pedido/producto |

**No aplica:** Retry sobre EF/BD (ya hay `EnableRetryOnFailure`), HttpClient real (no hay llamadas salientes).

---

## Fase 7 — Automation E2E en Node (todos los controladores) ✅ COMPLETADA (25/09/2026)

> Estilo UD02 `ejemplos/*/automation/test-runner.mjs` (Node nativo, sin npm install).  
> **Directorio:** `TiendaApi.Tests.E2E/Automation/`

| # | Tarea | Detalle |
|---|-------|---------|
| 7.1 | ✅ Crear `TiendaApi.Tests.E2E/Automation/test-runner.mjs` | Runner completo: docker compose (postgres+mongodb) → `dotnet restore/build/run` → suite HTTP → limpieza |
| 7.2 | Cobertura de **todos** los controllers | Ver tabla siguiente |
| 7.3 | Rate limit awareness | Helper `st()`: falla claro si 429 (100/15s · auth 10/min · POST 20/min) |
| 7.4 | Credenciales seed | `admin/admin`, `userdaw/userdaw` |
| 7.5 | Ejecución | `node TiendaApi.Tests.E2E/Automation/test-runner.mjs` desde la raíz del repo |
| 7.6 | CI (opcional) | Job GitHub Actions con Docker services + Node + .NET SDK |

### Cobertura por controlador

| Controller | Métodos cubiertos |
|------------|-------------------|
| **Health** | `GET /health` |
| **Auth** | `POST signup` (201 / 400), `POST signin` admin y user (200 / 401) |
| **Categorías** | `GET` paged, `GET/{id}`, 404, `POST` 401/403/201, `PUT`, `DELETE`, DELETE 404 |
| **Productos** | `GET` paged + filtros, `GET/{id}`, `GET/categoria/{id}`, 404, `POST` 401/201/400, `PUT`, `PATCH`, `DELETE` |
| **Pedidos (usuario)** | `GET me`, `GET me/paged`, `POST me` 201, `POST` sin auth 401, `GET/PUT me/{id}` |
| **Pedidos (admin)** | `GET` 401/403/200, `GET paged`, `GET/{id}`, `PUT estado`, `DELETE` |
| **Users (admin)** | `GET` 401/403/200, `GET/{id}`, 404, `POST`, `PUT`, `DELETE` |
| **Users (perfil)** | `GET/PUT me/profile`, 401 sin token |
| **Storage** | `GET /storage/...` 404 |
| **GraphQL** | queries `productos`, `categorias`, `producto(id)`; mutation sin auth → error |

*WebSockets/SignalR fuera del runner HTTP puro (sin dependencias npm); se quedan en Bruno.*

### Notas de diseño del runner

- **No** usa `docker compose down -v` al final: solo `stop` de servicios BD para no romper el entorno de desarrollo.
- Fallback: si `dotnet run` no responde, intenta `docker compose up -d --build`.
- Compatible con Fase 1: espera `/health` primero; si aún no existe, acepta `/swagger` o `/api/productos`.

### ✅ Verificación Fase 7 (25/09/2026)

| # | Resultado |
|---|-----------|
| 7.1-7.2 | `TiendaApi.Tests.E2E/Automation/test-runner.mjs` creado (**~660 líneas**, Node nativo, sin npm): helpers (`run`/`spawnApi`/`waitForApi`/`req`/`st`/asserts) + suite de **55 tests** sobre **los 11 bloques de la tabla de cobertura** (Health · Auth · Categorías · Productos · Pedidos user · Pedidos admin · Users admin · Users perfil · Storage · GraphQL · Limpieza) |
| 7.3 | Helper `st()` marca **FAIL explícito en 429**; la suite se diseñó con **18 POSTs** (< límite 20/min) y las URLs con `page` respetan los límites |
| 7.4 | Usa seed `admin/admin` · `userdaw/userdaw`; crea/borra usuarios con nombres únicos (`auto_<timestamp>`) para no chocar con el seed ni entre ejecuciones |
| 7.5 | `node TiendaApi.Tests.E2E/Automation/test-runner.mjs` desde la raíz: detecta infra corriendo (**no la toca**), `restore → build → dotnet run --no-launch-profile` (`ASPNETCORE_ENVIRONMENT=Development`, `ASPNETCORE_URLS=http://localhost:5031`) → espera `/health` → suite → **kill API** + `stop` solo de los servicios de BD que **él** levantó (nunca `down -v`) |
| 7.6 | CI pendiente (opcional, fuera del alcance de esta fase) |
| Resultado | **Total: 55 · OK: 55 · KO: 0** en vivo (2 aserciones relajadas en la 1ª pasada: `producto.id` puede venir como string en GraphQL y el mensaje de auth es *"not authorized"*). Build **0/0** · **1034 unit** verdes |

---

## Fase 8 — Migraciones EF Core (índices y esquema en BD existente) ✅ COMPLETADA (25/09/2026)

> **Problema:** hoy `EnsureCreated` **no** altera BDs ya creadas → los índices de la Fase 1B **no se aplican** en producción ni en volúmenes Docker persistentes.  
> **Objetivo:** introducir **EF Core Migrations** sin romper el flujo de desarrollo.

| # | Tarea | Detalle |
|---|-------|---------|
| 8.1 | Design-time | Asegurar `Microsoft.EntityFrameworkCore.Design` (ya en csproj) + `IDesignTimeDbContextFactory<TiendaDbContext>` si hace falta para `dotnet ef` |
| 8.2 | Migración inicial | `dotnet ef migrations add InitialCreate` → script del esquema actual (tablas + índices únicos existentes) |
| 8.3 | Migración de índices | Tras Fase 1B: `dotnet ef migrations add AddOptimizationIndexes` → `CREATE INDEX` para `CategoriaId`, `CreatedAt`, `IsDeleted`, `(CategoriaId, Precio)`, `Role` |
| 8.4 | Arranque por entorno | **Producción:** `context.Database.Migrate()` en lugar de `EnsureCreated()` (`DatabaseInitializationExtensions.cs:45`)<br>**Desarrollo:** mantener `EnsureDeleted + EnsureCreated` **o** `Migrate()` tras drop (decidir; drop+create ya funciona) |
| 8.5 | BD existente viva | `Migrate()` aplica solo lo pendiente → índices **sin** perder datos |
| 8.6 | Docker | Volumen `postgres-data` persistente: con migraciones, el siguiente arranque crea índices; sin ellas, haría falta `Reset-Database.ps1` |
| 8.7 | Scripts | Mantener `Reset-Database.ps1` para reset local completo |
| 8.8 | Verificar | En BD ya creada sin índices → arranque → `\di` en PG muestra los índices nuevos; E2E en verde |

**Orden recomendado:** Fase 1B (definir índices en modelo) → **Fase 8** (migración que los materializa) → Fase 7 Automation valida todo.

### ✅ Verificación Fase 8 (25/09/2026)

| # | Resultado |
|---|-----------|
| 8.1 | `Microsoft.EntityFrameworkCore.Design` 10.0.12 ya presente + **nuevo** `Data/TiendaDbContextFactory.cs` (`IDesignTimeDbContextFactory`): lee la connection de `appsettings.json` y **evita ejecutar `Program.cs` en design-time** — sin ella, cada `dotnet ef` ejecutaría `EnsureDeleted + EnsureCreated` en desarrollo. `dotnet-ef` global actualizado 10.0.7 → 10.0.12 |
| 8.2-8.3 | **Dos migraciones** en `TiendaApi.Api/Migrations/`: `InitialCreate` (tablas `categorias`/`users`/`productos` + FK + los **3 índices únicos preexistentes**: `Nombre`, `Email`, `Username`) y `AddOptimizationIndexes` (**los 5 índices de Fase 1B**: `productos(CategoriaId)`, `(CategoriaId, Precio)`, `CreatedAt`, `IsDeleted`, `users(Role)`, con su `Down` → `DropIndex`). Generadas editando a mano el `InitialCreate` + `ModelSnapshot` (quitando los5 `HasIndex`) para que la segunda migración materialice solo los índices nuevos · `has-pending-model-changes` → **"No changes"** |
| 8.4 | **Decisión:** Producción → `Migrate()` vía `ApplyPendingMigrationsAsync` (con **baseline**, ver 8.5); **Desarrollo → se mantiene `EnsureDeleted + EnsureCreated`** (drop+create ya funciona y es más rápido; anotado como decisión explícita) |
| 8.5 | **Baseline implementado:** si `InitialCreate` está pendiente **y** la tabla `categorias` ya existe (BD creada con `EnsureCreated` y sin `__EFMigrationsHistory`), se crea la tabla de historial y se marca `InitialCreate` como aplicada **sin ejecutarla** → `Migrate()` solo corre las migraciones futuras. Verificado con datos: usuarios/categorías/productos **intactos** tras migrar |
| 8.6-8.7 | Arranque prod sobre BD con volumen persistente aplica lo pendiente (mismo camino verificado en vivo) · `Reset-Database.ps1` **sin cambios** |
| 8.8 | **En vivo:** BD local → `DROP INDEX` de los5 (simula BD pre-Fase 1B) → arranque con `ASPNETCORE_ENVIRONMENT=Production` (**`--no-launch-profile`**: `Properties/launchSettings.json` fuerza `Development` y anula la env var) → logs: *"'…_InitialCreate' marcada como aplicada (baseline, sin ejecutar)"* + *"Migraciones aplicadas — pendientes antes: [InitialCreate, AddOptimizationIndexes]"* → psql `\di`: **los8 índices** (5 recreados + 3 únicos) · `SELECT` de `__EFMigrationsHistory`: **2 filas** · **datos preservados** (2 users / 3 categorías / 3 productos). Build **0/0** · **1034 unit** verdes |

---

## Fase 9 — Integración Result→HTTP · **Opción C** (`ToHttpResult`) ✅ COMPLETADA (25/09/2026)

> **Fuente:** doc UD02 §7 *Excepciones y patrón Result* (`UD02/07-excepciones-patron-result.md`) + ejemplo `UD02/ejemplos/07-ProductosResult/Extensions/DomainErrorExtensions.cs`.  
> **Viabilidad:** ✅ **ALTA** — todos los prerrequisitos ya existen en la API: `DomainError` tipado (`NotFoundError`, `ValidationError`, `BusinessRuleError`, `ConflictError`, `UnauthorizedError`, `ForbiddenError`, `InternalError`), fábricas de error por dominio (`ProductoError`, `CategoriaError`, `UsuarioError`, `AuthError`, `PedidoError`, `StorageError`), `Result<T, DomainError>` + `Match` en los controladores y CSharpFunctionalExtensions 3.7.0.  
> **Situación actual:** **31 `error switch` inline** repetidos en 5 controladores (Users 9 · Pedidos 8 · Productos 7 · Categorías 5 · Auth 2).  
> **Por qué Opción C y no B** (§7.8.3): con 5+ controladores, la extensión única es la recomendada por el doc.

| # | Tarea | Detalle |
|---|-------|---------|
| 9.1 | Nuevo `Extensions/DomainErrorExtensions.cs` | `public static IActionResult ToHttpResult(this DomainError error)` con switch tipado. **Mapeo = códigos actuales** para no romper E2E: `NotFoundError`→404 · `ValidationError`→400 (+ `ValidationErrors`) · `ConflictError`→409 · `BusinessRuleError`→400 (según XML docs del proyecto) · `UnauthorizedError`→401 · `ForbiddenError`→403 · `InternalError`/default→500 (mensaje `error.Message`, igual que hoy) |
| 9.2 | Sustituir los 31 switches | `onFailure: error => error switch {...}` → `onFailure: error => error.ToHttpResult()` en los 5 controladores; conservar `Match`/`IsSuccess` en flujos simples (ej. DELETE) |
| 9.3 | Auditar mapeos divergentes | Switches actuales que no siguen la matriz (p. ej. `BusinessRuleError` hoy cae a 500 en algunos endpoints → con `ToHttpResult` pasaría a 400): anotar como mejora, decisión explícita |
| 9.4 | Tests | Actualizar aserciones de controlador afectadas (tipos `StatusCodeResult`/objetos) |
| 9.5 | Verificar | Build 0/0 · unit · Bruno/Newman (400/401/403/404/409) — ideal **tras la Fase 7**, que cubre todos los códigos |

**Riesgo:** 🟡 bajo — solo capa de presentación; obligatorio preservar códigos y shape `{message, ...}` de cada respuesta.

### ✅ Verificación Fase 9 (25/09/2026)

| # | Resultado |
|---|-----------|
| 9.1 | Nuevo `TiendaApi.Api/Extensions/DomainErrorExtensions.cs`: `ToHttpResult(this DomainError)` con la matriz del plan (404/400+errors/409/400/401/403/500). Tipos `*ObjectResult` **idénticos** a los que producían los switches → aserciones de tipo intactas |
| 9.2 | Los **31 `error switch` eliminados** (grep en `Controllers/` → 0): `error => error.ToHttpResult()` en `Match` y `return resultado.Error.ToHttpResult()` en los flujos `IsSuccess`/DELETE (Auth 2 · Users 9 · Pedidos 8 · Productos 7 · Categorías 5). `using TiendaApi.Api.Extensions` en los 5 controladores |
| 9.3 | **Divergencias auditadas (decisiones explícitas):** ① `BusinessRuleError` **500 → 400** donde no había rama (matriz del plan + XML docs "HTTP 400/422") — verificado en: *Delete categoría con productos* y *Update conflicto de stock* (este test ya se llamaba `...RetornaBadRequest` pero asertaba 500: asentaba el bug). ② `ValidationError` unificado a `{message, errors}`: 9 endpoints ya lo incluían, 8 mandaban solo `{message}` → ahora todos con `errors` (códigos sin cambio). ③ Tipos sin rama en algún switch (p. ej. `ForbiddenError` en Users Create) pasan de 500 a su código real (403/401) — hoy inalcanzables por `Authorize` previo |
| 9.4 | 2 tests actualizados: `Delete_CategoriaConProductos_RetornaBadRequest` (renombrado, ahora `BadRequestObjectResult`) · `Update_ConflictoDeStock_RetornaBadRequest` (aserción coherente con su nombre) |
| 9.5 | Build **0/0** · **1034 unit** verdes · **en vivo 15/15**: **401** users sin token · **403** rol USER en `/api/users` · **404** producto/categoría/pedido inexistentes + `DELETE` (con `{message}`) · **400** signup inválido · **409** signup duplicado (con `{message}`) · regresiones 200 en users/productos/categorías/pedidos |

---

## Fase 10 — Documentación didáctica (insertar en los docs existentes)

> **Regla:** **NO** crear documentos nuevos. Insertar secciones explicativas en el `doc/NN-*.md` **oportuno** para cada tema, con el estilo del resto del documento (código real del proyecto) y **actualizando su Índice**. Cubre **todas** las fases del plan, estén completas (✅) o previstas.

| # | Tema | Fase(s) | Documento → sección |
|---|------|---------|---------------------|
| 10.1 | Result → HTTP con `ToHttpResult()` (Opción C) | 9 ✅ | `doc/11-patron-result.md` → 11.6 Integración Result + Controladores |
| 10.2 | Caché de salida: `OutputCache` + invalidación por tags | 4 ✅ | `doc/10-redis-caching.md` → nueva sección (antes del resumen) |
| 10.3 | `ETag` + revalidación `304` (patrón real del proyecto) | 4 ✅ | `doc/06-rest-best-practices.md` → 6.7 ETag para Cacheo |
| 10.4 | Paginación real en BD (`Skip/Limit`, no en memoria) | 3 ✅ | `doc/06-rest-best-practices.md` → 6.3 Paginación |
| 10.5 | Migraciones EF Core: factory design-time, `InitialCreate`/`AddOptimizationIndexes`, baseline en BD existente, dev vs prod | 8 ✅ | `doc/08-ef-core-postgresql.md` → 8.5 Migraciones |
| 10.6 | `AsNoTracking` en consultas de solo lectura · índices de optimización | 2 ✅ · 1 ✅ | `doc/27-optimizacion.md` → 27.5 EF Core · 27.3 Índices |
| 10.7 | Fire & forget endurecido (`Task.Run` + try/catch) | FF ✅ | `doc/22-background-jobs.md` |
| 10.8 | Polly educativa (Retry + CircuitBreaker + Timeout en email) | 6 prevista | `doc/13-pedidos-transacciones.md` → 13.3 · `doc/21-email-services.md` |
| 10.9 | Automation E2E en Node (runner de todas las fases) | 7 prevista | `doc/24-testing.md` → tras 24.14 |
| 10.10 | Verificar | — | Build 0/0 · 1034 unit · Índices (TOC) de cada doc actualizados |

---

## Fuera de alcance (confirmado)

| Tema | Motivo |
|------|--------|
| `await Task.WhenAll` en email/efectos | Bloquearía la respuesta HTTP — **rechazado** |
| Proyecciones `Select`→DTO en SQL | Riesgo alto GraphQL/SignalR/E2E — aparte |
| Rate limiting **nativo** `AddRateLimiter` | Ya está AspNetCoreRateLimit activo; no duplicar |
| OpenTelemetry / MiniProfiler | No pedido en esta iteración |
| Tocar transacciones `Serializable` de pedidos | Riesgo alto (flujo central `POST /api/pedidos/me`) |
| Polly sobre BD/HttpClient real | Sin caso de uso real en la API |

---

## Orden de ejecución

```
0.1 → 10 → 2 → FF → 1 (AsNoTracking) → 4 (pedidos paged)
   → 6-OutputCache → 9 (ToHttpResult)
   → 8 (migraciones/índices)
   → 7 (Automation) → 5.x (verificación global) → 6-Polly
   → 10 (documentación didáctica de todas las fases)
```

> **Nota:** Fase 8 antes que 7 para que el Automation valide una BD con índices reales.  
> **Fase 9** va tras OutputCache y antes de 7: la Automation valida los códigos HTTP de la refactorización.  
> Las “6” son distintas: **Fase 4 = OutputCache (#6 del análisis)**; **Fase 6 = Polly**.

---

## Resumen de impacto

| Fase | Impacto | Dificultad | Riesgo |
|------|---------|------------|--------|
| 10 Health | 🟡 Ops | 🟢 Muy baja | 🟢 |
| 2 Índices (modelo) | 🟢 Alto | 🟢 Muy baja | 🟢 |
| FF Task.Run | 🟡 Calidad | 🟢 Baja | 🟢 |
| 1 AsNoTracking | 🟢 Alto | 🟢 Baja | 🟡 |
| 4 Pedidos paged | 🟢 Alto | 🟡 Media | 🟡 |
| 4 OutputCache | 🟡 Medio | 🟢 Baja | 🟡 |
| 8 Migraciones | 🟢 Alto (prod) | 🟡 Media | 🟡 |
| 7 Automation | 🟢 Muy alto (QA) | 🟡 Media | 🟢 |
| 9 ToHttpResult | 🟢 Mantenibilidad | 🟢 Baja | 🟡 |
| 6 Polly | 🟡 Educativo | 🟢 Baja | 🟢 |

---

## Cómo ejecutar la Automation (Fase 7)

```bash
# Desde la raíz del repo (requiere Docker + .NET 10 + Node 18+)
node TiendaApi.Tests.E2E/Automation/test-runner.mjs

# Contra una API ya levantada
BASE_URL=http://localhost:5000 node TiendaApi.Tests.E2E/Automation/test-runner.mjs
```

Salir con código `0` si todo OK, `1` si algún test falla (listo para CI).
