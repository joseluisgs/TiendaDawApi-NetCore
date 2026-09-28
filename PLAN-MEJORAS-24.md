# PLAN-MEJORAS-24 — 24 mejoras (código + didácticas) en ambas APIs

Fecha: 27/09/2026 · Repos: `TiendaDawApi-NetCore` (R1) + `TiendaDawApi-Cqrs-MediatR-NetCore` (R2)
Objetivo: aplicar las 24 mejoras **byte-idénticas en ambos repos** y **transparentes para el cliente** (aditivas en su caso), sin romper nada.

## Protocolo inamovible
- Build `0/0` y suites completas (sin omitidos) en **ambos** repos tras cada fase: R1 = 1232 tests, R2 = 1069 tests.
- **BD nueva antes de cada grupo de tests E2E**: `reset-seeds-origen.ps1` (R1) y `reset-seeds.ps1` (R2) — reinician BD + API (puerto 5031). Las dos APIs comparten el 5031: **no pueden correr a la vez**; parar procesos en 5031 antes de builds/tests.
- E2E completo (Automation 63 · Newman 4 tandas · Bruno-Cli 77) en fases que tocan comportamiento (P2, P4, P5, P9); en fases de config/docs: build + unit + scripts.
- **Commits solo con permiso expreso**: al terminar cada fase se pide permiso (commit de código por fase y por repo + push). Los docs (BITACORA/FASES/README) se respaldan en un commit final por repo (necesitan los hashes de código).
- README **sin conteos de tests** (cifras solo en FASES/BITACORA). Convención de encoding: root `*.md` sin BOM; `doc/*.md` **sin BOM en R1, con BOM en R2**; LF en todo.
- Verificación cruzada siempre: `node scripts/check-parity.mjs` (ambos), `node scripts/check-docs.mjs` (solo R1), `scripts/check-style.ps1`, `scripts/check-audit.mjs`, `scripts/check-openapi.mjs` (P6+).

## Mapa de las 24 mejoras → fases

| # | Mejora | Fase |
|---|--------|------|
| 1 | `.editorconfig` + `scripts/check-style.ps1` (`dotnet format --verify-no-changes`) | P1 |
| 2 | `Directory.Build.props` (props comunes centralizadas) | P1 |
| 3 | `Directory.Packages.props` (Central Package Management + unificar versiones drift) | P1 |
| 4 | `global.json` (SDK 10.0.401, rollForward latestFeature) | P1 |
| 5 | Borrado local de `*.sln.DotSettings.user` (ya ignorados/no trackeados) | P1 |
| 6 | Retirar supresiones `NU1605`/`NU1903` del `NoWarn` (hoy no se disparan) | P2 |
| 7 | Eliminar paquete `GraphiQL` (0 usos) + verificar UI GraphQL de HotChocolate | P2 |
| 8 | Arreglar AutoMapper: quitar `AutoMapper.Extensions.MSDI` (legacy 12.0.0 vs core 16.2.0) | P2 |
| 9 | `scripts/check-audit.mjs` (auditoría de vulnerabilidades NuGet) | P2 |
| 10 | `TimeProvider` en servicios/seeds/middleware/interceptors (tests con FakeTimeProvider) | P4 |
| 11 | Compresión de respuestas (Brotli + gzip) | P4 |
| 12 | Headers `RateLimit-*` / `Retry-After` + cuerpo 429 JSON | P5 |
| 13 | Rate limiting **nativo** (`System.Threading.RateLimiting`), fuera `AspNetCoreRateLimit`; mismas 4 reglas | P5 |
| 14 | Split `/health/ready` + `/health/live` (con tags; `/health` intacto) | P4 |
| 15 | Serialización JSON source-generated (`JsonSerializerContext`, fallback reflexión) | P4 |
| 16 | `GET /version` (appsettings `Application:*` + InformationalVersion/commit) | P4 |
| 17 | `scripts/check-openapi.mjs` (diff de `swagger.json` entre ambos repos en marcha) | P6 |
| 18 | Swagger didáctico: `IncludeXmlComments` + filtro de ejemplos XML + `ProducesResponseType` | P3 |
| 19 | Acelerar suites: contenedores PG+Mongo compartidos por assembly + BD por clase | P7 |
| 20 | Tests de forma de errores (400/401/404/409/429, `/health/*`, `/version`) | P7 |
| 21 | `.dockerignore` (raíz y `TiendaApi.Api/`) + `HEALTHCHECK` + `curl` en imagen | P6 |
| 22 | ADRs en `doc/adr/` (plantilla + 10 ADRs, nota de variante CQRS) | P8 |
| 23 | Archivos `.http` en `doc/http/` (cobertura de las 24 rutas de paridad) | P8 |
| 24 | Completar XML docs (CS1591 104/190, CS1570 30/20, CS1587 106/106) y retirar `1591;1570;1587` del `NoWarn` | P3 |

## Datos verificados (27/09/2026)
- `TiendaApi.csproj` NoWarn actual: `NU1605;NU1903;1591;1570;1587` (Tests: `NU1903`). **NU1605/NU1903 no se disparan** (auditoría limpia); sí faltan docs: CS1591=104 (R1)/190 (R2), CS1570=30/20, CS1587=106/106.
- No existen: `.editorconfig`, `Directory.Build.props`, `Directory.Packages.props`, `global.json`, `.dockerignore`, `AddJsonOptions`, `IncludeXmlComments`, compresión, `TimeProvider`, `MapGet` alguno, ejemplos Swagger, `doc/adr/`, `*.http`.
- `dotnet format whitespace`: 139 avisos en 28 ficheros (R1) → fix mecánico en P1; `style` con defaults ya en verde.
- Rate limiting actual: `RateLimitConfig.UseRateLimiting()` → `UseIpRateLimiting()` de `AspNetCoreRateLimit 5.0.0`; reglas `* 100/15s`, `auth 10/1m`, `POST 20/min`, `POST /graphql 200/min`. E2E **no aserta 429** (solo guard `st()!==429`).
- Tests: NUnit **sin fixtures compartidos** — 10 (R1)/11 (R2) clases de integración arrancan sus propios contenedores (20-22 arranques/suite) → palanca de P7.
- Docker: Dockerfile ya copia csproj→restore; falta `HEALTHCHECK`; compose local sin servicio api (por diseño); compose prod usa `curl` en healthcheck → `curl` debe estar en la imagen.
- Docs: `check-docs.mjs` solo en R1; `check-parity.mjs` no camina `doc/`; filas "Mantenimiento" de BITACORA/FASES se apilan al final.
- Drift de versiones a unificar en P1 (CPM): `CSharpFunctionalExtensions` 3.3.0→3.7.0 (cliente), `NUnit` 4.3.1→4.6.1, `Microsoft.NET.Test.Sdk` 17.12.0→18.10.1, `NUnit3TestAdapter` 4.6.0→6.3.0, `Moq` 4.20.72→4.21.0, `FluentAssertions` 7.0.0→7.2.2 (cliente).

## Fases (orden de ejecución)

- [x] **P1 — Fundamentos de repo (1,2,3,4,5)**: `.editorconfig`, `check-style.ps1`, fix `dotnet format whitespace`, `Directory.Build.props` (+ limpiar props de los 5 csproj), `Directory.Packages.props` (+ quitar `Version=` de PackageReference, unificar drift de cliente), `global.json`, borrar locales `*.DotSettings.user`. Verificación: format 0 avisos · build `0/0` ×2 · build `ClientBlazor.slnx` ×2 · `ClientBlazor.Tests` ×2 · suites ×2 · check-parity. Sin E2E.
- [x] **P2 — Dependencias (6,7,8,9)**: quitar ext. AutoMapper (+verificar DI), quitar GraphiQL (+verificar UI HC), retirar `NU1605`/`NU1903` del NoWarn, `check-audit.mjs`. Verificación: build ×2 · suites ×2 · **E2E full ×2 (BD nueva)**.
- [x] **P3 — XML docs + Swagger (24,18)**: arreglar CS1570/CS1587, documentar CS1591 (excluir `Migrations/*` con supresión puntual si hace falta), retirar `1591;1570;1587`; Swagger `IncludeXmlComments` + filtro de ejemplos XML + `ProducesResponseType`. Verificación: build `0/0` ×2 · suites ×2 · smoke swagger dev ×2.
- [x] **P4 — Runtime aditivo (10,11,14,15,16)**: compresión; `/health/ready|live` (tags, `/health` intacto); `GET /version`; `TimeProvider` (ctor opcional `TimeProvider? = null` → `TimeProvider.System`, registro DI, tests FakeTimeProvider); `AppJsonContext` source-gen. Verificación: build ×2 · suites ×2 · **E2E full ×2 (BD nueva)**.
- [x] **P5 — Rate limiting nativo + headers (13,12)**: fuera `AspNetCoreRateLimit`; middleware propio sobre `System.Threading.RateLimiting` con réplica exacta de las 4 reglas (IP + X-Forwarded-For, todas las reglas aplican); headers `RateLimit-Limit/Remaining/Reset` + `Retry-After`; 429 JSON; tests unit 429; actualizar menciones en docs. Verificación: build ×2 · suites ×2 · **E2E full ×2 (BD nueva, crítico)**.
- [x] **P6 — Contrato + Docker (17,21)**: `check-openapi.mjs` (R1:5041, R2:5042, deep-diff); `.dockerignore` ×2 rutas; Dockerfile `curl`+`HEALTHCHECK`; `docker build` smoke ×2. Verificación: build ×2 · suites ×2 · check-openapi verde.
- [x] **P7 — Tests (19,20)**: fixtures de contenedor compartidos por assembly con BD por clase (10/11 clases), tests de forma de errores (sin tocar colecciones E2E), medición antes/después. Verificación: suites ×2 **dos pasadas** · recuentos documentados en FASES/BITACORA.
- [x] **P8 — Didáctico (22,23)**: `doc/adr/` (plantilla + 10 ADRs; BOM R2), `doc/http/` (00-base + 6 por área), filas en tablas README. Verificación: check-docs · check-parity · build ×2.
- [ ] **P9 — Final**: **completar mejora 15** (conectar `AppJsonContext` en los 7 puntos de serialización — MVC, HTTP JSON, GlobalExceptionHandler, RateLimit, WS ×2, caché — con `JsonTypeInfoResolver.Combine` + fallback reflexión, + sección en `doc/`; decisión 28/09) · builds ×2 · suites ×2 · **E2E full ×2 (BD nueva)** · check-docs · check-parity · check-openapi · check-style · check-audit · docs finales (BITACORA filas con hashes antes de `## Fases pendientes`, FASES notas al final, README) · permiso · commits docs · push · sync `0 0`.

## Riesgos y fallbacks
| Riesgo | Mitigación |
|---|---|
| P5 rompe timing E2E | Réplica exacta de límites; E2E full inmediato; si falla → revert del commit de fase |
| CPM rompe clientes Blazor | `ManagePackageVersionsCentrally=false` puntual en esos csproj |
| AutoMapper 16 sin paquete ext. no compila | Config manual `AddAutoMapper(cfg…)` (2 líneas) |
| TimeProvider rompe tests por firmas de ctor | Parámetro opcional con default `TimeProvider.System` |
| P7 introduce flakiness | Dos pasadas de suite; si inestable → fallback a solo fixture de contenedor sin tocar seed por clase |
| XML docs con `TreatWarningsAsErrors` | Corrección incremental + supresión puntual SOLO en `Migrations/*` |
| Proceso en 5031 bloquea builds | Matar listeners de 5031 al inicio de cada verificación |

## Fuera de alcance (ya descartados)
NetArchTest/architecture tests · middleware CorrelationId · k6 · operationIds de Swagger · GitHub Actions CI (el `ci.yml` existente no se modifica). Hallazgos laterales no tocados: `doc/25-docker.md` vacío (R1), `doc/27-docker-ci-cd.md` casi vacío (R2).
