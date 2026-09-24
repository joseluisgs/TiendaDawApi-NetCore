# Bitácora — Fase 0: Baseline + actualización de dependencias

> **Fecha:** 24/09/2026
> **Proyecto origen:** `TiendaDawApi-NetCore` (rama `feature/polly`, API sin CQRS)
> **Destino de replicación:** `TiendaDawApi-Cqrs-MediatR-NetCore`
> **Objetivo:** dejar `restore/build/test` limpios (0 errores, 0 vulnerabilidades) como baseline de las fases de mejora.

---

## 1. Problema de partida

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

*Replicado con éxito el: (fecha de replicación en CQRS)*
