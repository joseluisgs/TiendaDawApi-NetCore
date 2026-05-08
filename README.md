# TiendaDawApi 🛒

![banner](./banner.png)

[![.NET](https://img.shields.io/badge/.NET-10-blue)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-10-blue)](https://dotnet.microsoft.com/en-us/apps/aspnet)
[![C#](https://img.shields.io/badge/C%23-14-blue)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![EF Core](https://img.shields.io/badge/EF%20Core-10-blue)](https://docs.microsoft.com/en-us/ef/core/)


**Ejemplo educativo de uso de APIS para el desarrollo de servicios backend en .NET 10 ASP.NET Core y C# 14.**

Una API de comercio electrónico con arquitectura profesional, múltiples bases de datos, cacheo con Redis, GraphQL, WebSockets para notificaciones y versionado de API.

## 🎯 Descripción

TiendaDawApi es una serie de servicios backend desarrollados con .NET 10 ASP.NET Core y C# 14 que implementa una API RESTful completa para una tienda en línea, además usa GraphQL y WebSockets. El proyecto está diseñado con una arquitectura en capas utilizando múltiples bases de datos (PostgreSQL, MongoDB y Redis) para diferentes propósitos educativos para la formación de Desarrollo Web en Entornos Servidor (DAW).

- 🏪 **Gestión de Productos y Categorías**: CRUD completo con validaciones
- 🛒 **Sistema de Pedidos**: Documentos embebidos con MongoDB
- 👥 **Gestión de Usuarios**: Autenticación JWT con roles (ADMIN, USER)
- 💾 **Multi-Base de Datos**: PostgreSQL (relacional), MongoDB (documentos), Redis (caché)
- 🔐 **Seguridad**: JWT, validaciones FluentValidation, manejo global de excepciones
- 📡 **APIs Avanzadas**: GraphQL con HotChocolate, WebSockets y SignalR para notificaciones en tiempo real
- ⏰ **Background Jobs**: Tareas programadas con BackgroundService para reportes y sincronización
- 📊 **Versionado de API**: Control de versiones por URL.
- 🧪 **Testing**: Tests con NUnit, Moq, Tescontainers y Newman.

## 📑 Tabla de Contenidos

- [TiendaDawApi 🛒](#tiendadawapi-)
  - [🎯 Descripción](#-descripción)
  - [📑 Tabla de Contenidos](#-tabla-de-contenidos)
  - [✨ Características](#-características)
  - [🚀 Tecnologías](#-tecnologías)
  - [🏃‍♂️ Inicio Rápido](#️-inicio-rápido)
    - [Desarrollo Local](#desarrollo-local)
    - [Desarrollo en Producción con Docker](#desarrollo-en-producción-con-docker)
  - [🧪 Estrategia de Testing](#-estrategia-de-testing)
    - [Ejecución de Tests](#ejecución-de-tests)
      - [Comandos específicos por tipo de test](#comandos-específicos-por-tipo-de-test)
      - [Con coverage](#con-coverage)
      - [Configuración de tests](#configuración-de-tests)
    - [Tests E2E con Newman (Postman) y Bruno](#tests-e2e-con-newman-postman-y-bruno)
      - [Postman (Newman)](#postman-newman)
      - [Bruno (CLI)](#bruno-cli)
  - [📚 Documentación](#-documentación)
    - [Fundamentos y Configuración](#fundamentos-y-configuración)
    - [API y Controllers](#api-y-controllers)
    - [Persistencia de Datos](#persistencia-de-datos)
    - [Lógica de Negocio](#lógica-de-negocio)
    - [Seguridad](#seguridad)
    - [APIs Avanzadas](#apis-avanzadas)
    - [Servicios Externos](#servicios-externos)
    - [Tareas en Segundo Plano](#tareas-en-segundo-plano)
    - [Documentación](#documentación)
    - [Servicios Externos](#servicios-externos-1)
    - [Testing y Calidad](#testing-y-calidad)
    - [DevOps y Producción](#devops-y-producción)
    - [Arquitectura](#arquitectura)
  - [⚒️ Diagrama de Clases del Dominio](#️-diagrama-de-clases-del-dominio)
  - [🗄️ Entidades por Base de Datos](#️-entidades-por-base-de-datos)
    - [🐘 PostgreSQL (Datos Maestros)](#-postgresql-datos-maestros)
    - [🍃 MongoDB (Pedidos - Documentos Embebidos)](#-mongodb-pedidos---documentos-embebidos)
  - [📂 Estructura del Proyecto](#-estructura-del-proyecto)
    - [Descripción de Carpetas Principales](#descripción-de-carpetas-principales)
  - [🏗️ Arquitectura Híbrida Onion-Like](#️-arquitectura-híbrida-onion-like)
    - [Principios Fundamentales](#principios-fundamentales)
    - [Capas de la Arquitectura](#capas-de-la-arquitectura)
    - [Estructura de Dependencias](#estructura-de-dependencias)
      - [Ventajas de Esta Arquitectura](#ventajas-de-esta-arquitectura)
    - [🛤️ Railway Oriented Programming (ROP)](#️-railway-oriented-programming-rop)
      - [Conceptos Fundamentales](#conceptos-fundamentales)
      - [Anatomía del Patrón (Two-Track Model)](#anatomía-del-patrón-two-track-model)
      - [Beneficios de ROP](#beneficios-de-rop)
      - [Ejemplos de Uso](#ejemplos-de-uso)
      - [Comparación: ROP vs Try-Catch](#comparación-rop-vs-try-catch)
  - [🗄️ Estrategia Multi-Base de Datos](#️-estrategia-multi-base-de-datos)
  - [🔐 Seguridad](#-seguridad)
  - [📡 Endpoints](#-endpoints)
    - [Auth (versionado)](#auth-versionado)
    - [Categorías](#categorías)
    - [Productos](#productos)
    - [Pedidos (Administrador)](#pedidos-administrador)
    - [Pedidos (Usuario - Mis Pedidos)](#pedidos-usuario---mis-pedidos)
    - [Usuarios (Administrador)](#usuarios-administrador)
    - [Usuarios (Perfil Propio)](#usuarios-perfil-propio)
    - [Storage (Archivos)](#storage-archivos)
    - [WebSockets (Tiempo Real)](#websockets-tiempo-real)
    - [SignalR (Realtime)](#signalr-realtime)
    - [GraphQL](#graphql)
  - [👥 Usuarios Demo](#-usuarios-demo)
  - [📝 Licencia](#-licencia)
  - [👨‍💻 Autor](#-autor)
    - [Contacto](#contacto)
  - [Licencia de uso](#licencia-de-uso)


## ✨ Características

- 🏪 **CRUD Completo**: Productos, Categorías, Pedidos y Usuarios
- 🔐 **Autenticación JWT**: Token-based con roles y claims
- 🔒 **HTTPS + HSTS**: Redirección HTTP→HTTPS, HSTS 365 días, Security Headers
- 📧 **Notificaciones por Email**: Envío asíncrono con MailKit
- 📊 **Cacheo con Redis**: Patrón Cache-Aside para mejorar rendimiento
- 📡 **GraphQL**: Consultas flexibles con HotChocolate
- 🔌 **WebSockets/SignalR**: Notificaciones en tiempo real personalizadas por roles
- ⏰ **Background Jobs**: Tareas programadas con BackgroundService (reportes semanales de productos)
- 🗄️ **Multi-Database**: PostgreSQL + MongoDB + Redis
- 📈 **Versionado de API**: Control de versiones por URL
- ✅ **Validaciones**: FluentValidation declarativo
- 🛡️ **Exception Handling**: Middleware global de errores
- 🧪 **Testing**: Unit tests con NUnit y Moq
- 📊 **Code Coverage**: Métricas con Coverlet
- 🐳 **Docker**: Contenedores para desarrollo y producción

## 🚀 Tecnologías

- **.NET 10 con C# 14** - Plataforma principal
- **ASP.NET Core Web API** - Framework REST
- **EF Core 10** - ORM con PostgreSQL y MongoDB
- **PostgreSQL 15** - Base de datos relacional
- **MongoDB 7.0** - Base de datos de documentos
- **Redis** - Cache distribuido
- **JWT** - Autenticación basada en tokens
- **FluentValidation** - Validaciones declarativas
- **AutoMapper** - Mapeo de objetos
- **Websockets/SignalR** - WebSockets en tiempo real puros y usando SignalR
- **HotChocolate** - GraphQL server
- **NUnit + Moq** - Testing unitario
- **CSharpFunctionalExtensions** - Railway Oriented Programming
- **Testcontainers** - Tests con bases de datos reales
- **Swashbuckle/Swagger** - Documentación automática de API
- **Coverlet** - Métricas de coverage
- **Docker** - Containerización
- **Newman/Bruno** - Pruebas de API
- **BackgroundService** - Tareas programadas y jobs en segundo plano
- **Security Headers** - X-Content-Type-Options, X-Frame-Options, X-XSS-Protection, Referrer-Policy

## 🏃‍♂️ Inicio Rápido

### Desarrollo Local

```bash
# Clonar repositorio
git clone https://github.com/joseluisgs/TiendaDawApi-NetCore.git
cd TiendaDawApi-NetCore

# Restaurar dependencias
dotnet restore

# Iniciar servicios (PostgreSQL y MongoDB, la cache con Redis es opcional, usa en memoria si no está)
docker-compose -f docker-compose.local.yml up -d

# Ejecutar aplicación en modo desarrollo
dotnet run --project TiendaApi.Apis

# O con Hot Reload
dotnet watch run --project TiendaApi.Apis

# Acceso a la API (Desarrollo - HTTP)
open http://localhost:5000

# Acceso a Swagger UI (Desarrollo - HTTP)
open http://localhost:5000/swagger

> **Nota:** En producción, la API usa HTTPS obligatorio con HSTS.
```

### Desarrollo en Producción con Docker

Para desplegar en producción, usa `docker-compose.prod.yml` que incluye todos los servicios con configuración optimizada:

```bash
# Crear archivo .env con tus variables de producción
cp .env.example .env
# Edita .env con tus contraseñas y configuración segura

# Construir y ejecutar todos los servicios
docker-compose -f docker-compose.prod.yml up -d --build

# Ver logs de la API
docker-compose -f docker-compose.prod.yml logs -f api

# Ver logs de todos los servicios
docker-compose -f docker-compose.prod.yml logs -f

# Detener servicios
docker-compose -f docker-compose.prod.yml down

# Detener y eliminar volúmenes
docker-compose -f docker-compose.prod.yml down -v
```

**Servicios incluidos:**
- **PostgreSQL** (puerto 5432): Base de datos relacional
- **MongoDB** (puerto 27017): Base de datos de documentos
- **Redis** (puerto 6379): Cache distribuido
- **API** (puerto 5000): Tu aplicación containerizada

**Variables de entorno requeridas en `.env`:**
```env
POSTGRES_USER=admin
POSTGRES_PASSWORD=tu_contraseña_segura
POSTGRES_DB=tienda
MONGO_ROOT_USER=admin
MONGO_ROOT_PASSWORD=tu_contraseña_segura
MONGO_DB=tienda
JWT_KEY=TuClaveJWTMuyLargaYSegura123456789
JWT_ISSUER=TiendaApi
JWT_AUDIENCE=TiendaApi
SMTP_USERNAME=tu@email.com
SMTP_PASSWORD=tu_contraseña_app
API_PORT=5000
```



## 🧪 Estrategia de Testing

TiendaDawApi implementa una pirámide de pruebas profesional:

- **Unit Tests**: Validación de servicios, repositorios y lógica de negocio 
- **Integration Tests**: Tests con bases de datos reales usando Testcontainers
- **Coverage**: Indicadores de cobertura con Coverlet
- **Newman o Bruno**: Pruebas de API automatizadas

### Ejecución de Tests

```bash
# 🚀 Ejecutar TODOS los tests (requiere Docker ejecutándose)
dotnet test
```

#### Comandos específicos por tipo de test

| Escenario                                     | Comando                                                 | Docker |
| --------------------------------------------- | ------------------------------------------------------- | ------ |
| **Solo unitarios** (rápido, sin dependencias) | `dotnet test --filter "FullyQualifiedName~Unit"`        | ❌      |
| **Solo integración** (requiere servicios)     | `dotnet test --filter "FullyQualifiedName~Integration"` | ✅      |
| **Todos sin Docker**                          | `SKIP_INTEGRATION_TESTS=true dotnet test`               | ❌      |
| **Todos con Docker** (completo)               | `dotnet test`                                           | ✅      |

#### Con coverage

```bash
# Ejecutar todos los tests con coverage
dotnet test --collect:"XPlat Code Coverage"

# Ver reporte de coverage
open coverage/index.html
```

#### Configuración de tests

- **Unit Tests**: Ejecutan en paralelo (`ParallelScope.Children`) para máximo rendimiento
- **Integration Tests**: No paralelos (`NonParallelizable`) para evitar conflictos de recursos
- **CI**: 
  - Job `test`: Unit tests siempre (parallel)
  - Job `test-integration`: Solo bajo demanda con `workflow_dispatch` en main

### Tests E2E con Newman (Postman) y Bruno

Pruebas end-to-end de la API usando Newman y Bruno CLI:

#### Postman (Newman)

```bash
# Opción 1: Con Docker (recomendado)
cd TiendaApi.ApiTests/Postman
docker-compose up --build

# Ver informes generados
open reports/report.html

# Opción 2: Con Newman local
npm install -g newman
newman run TiendaApi.ApiTests/Postman/TiendaApi.NetCore.postman_collection.json \
  -e TiendaApi.ApiTests/Postman/TiendaApi.NetCore.postman_environment.json \
  -r html,json,junit --reporter-html-export report.html \
  --reporter-json-export report.json \
  --reporter-junit-export junit-report.xml
```

**Colección disponible en:** `TiendaApi.ApiTests/Postman/TiendaApi.NetCore.postman_collection.json`

**Informes generados:**
- `report.html` - Informe visual
- `report.json` - Datos estructurados
- `junit-report.xml` - Para CI/CD

#### Bruno (CLI)

```bash
# Opción 1: Con Docker (recomendado)
cd TiendaApi.ApiTests/Bruno
docker-compose up --build

# Ver informes generados
open reports/report.html

# Opción 2: Con Bruno CLI local
npm install -g @usebruno/cli
bru run TiendaApi.ApiTests/Bruno \
  --env TiendaApi.ApiTests/Bruno/environments/local.bru \
  --output reports/report.json \
  --format json
```

**Tests disponibles en:** `TiendaApi.ApiTests/Bruno/`

**Informes generados:**
- `report.html` - Informe visual
- `report.json` - Datos estructurados
- `junit-report.xml` - Para CI/CD

## 📚 Documentación

Para una comprensión profunda de la arquitectura y las tecnologías utilizadas, consulta los documentos en la carpeta [`doc/`](doc/):

### Fundamentos y Configuración
| #   | Documento                                                          | Descripción                    |
| --- | ------------------------------------------------------------------ | ------------------------------ |
| 01  | [Configuración proyectos .NET](doc/01-configuracion-proyectos.md)  | IDEs, estructura, herramientas |
| 02  | [Arquitectura Pipeline HTTP](doc/02-arquitectura-pipeline-http.md) | Middlewares, Request/Response  |
| 03  | [Inyección Dependencias](doc/03-inyeccion-dependencias.md)         | DI Containers, Scopes          |

### API y Controllers
| #   | Documento                                             | Descripción                     |
| --- | ----------------------------------------------------- | ------------------------------- |
| 04  | [Controladores REST](doc/04-controladores-rest.md)    | Routing, Model Binding, Actions |
| 05  | [Validación en Cascada](doc/05-validacion-cascada.md) | Data Annotations, validaciones  |
| 18  | [REST Best Practices](doc/18-rest-best-practices.md)  | Convenciones REST               |

### Persistencia de Datos
| #   | Documento                                          | Descripción          |
| --- | -------------------------------------------------- | -------------------- |
| 07  | [Repository Pattern](doc/07-repository-pattern.md) | Abstracción de datos |
| 09  | [EF Core PostgreSQL](doc/09-ef-core-postgresql.md) | ORM relacional       |
| 10  | [MongoDB](doc/10-mongodb.md)                       | Base de documentos   |
| 11  | [Redis Caching](doc/11-redis-caching.md)           | Cache-Aside pattern  |

### Lógica de Negocio
| #   | Documento                                                  | Descripción                       |
| --- | ---------------------------------------------------------- | --------------------------------- |
| 06  | [Patrón Result](doc/06-patron-result.md)                   | Railway Oriented Programming      |
| 08  | [Servicios de Negocio](doc/08-servicios-negocio.md)        | Capa de servicios                 |
| 15  | [Pedidos y Transacciones](doc/15-pedidos-transacciones.md) | Transacciones optimista/pesimista |
| 22  | [Mapeadores](doc/22-mapeadores.md)                         | AutoMapper vs extensiones         |

### Seguridad
| #   | Documento                                          | Descripción              |
| --- | -------------------------------------------------- | ------------------------ |
| 12  | [JWT Authentication](doc/12-jwt-authentication.md) | Tokens, Claims           |
| 13  | [Autorización Roles](doc/13-autorizacion-roles.md) | Policies, Roles          |
| 27  | [Seguridad HTTP](doc/27-seguridad-http.md)         | HSTS, HTTPS, Headers     |

### APIs Avanzadas
| #   | Documento                          | Descripción  |
| --- | ---------------------------------- | ------------ |
| 14  | [WebSockets](doc/14-websockets.md) | Tiempo real  |
| 20  | [GraphQL](doc/20-graphql.md)       | HotChocolate |

### Servicios Externos
| #   | Documento                                  | Descripción          |
| --- | ------------------------------------------ | -------------------- |
| 16  | [File Storage](doc/16-file-storage.md)     | Almacenamiento local |
| 17  | [Email Services](doc/17-email-services.md) | MailKit              |

### Tareas en Segundo Plano
| #   | Documento                                    | Descripción        |
| --- | -------------------------------------------- | ------------------ |
| 25  | [Background Jobs](doc/25-background-jobs.md) | Tareas programadas |

### Documentación
| #   | Documento                                        | Descripción         |
| --- | ------------------------------------------------ | ------------------- |
| 19  | [Documentación API](doc/19-documentacion-api.md) | Swagger, Versionado |

### Servicios Externos
| #   | Documento                                  | Descripción          |
| --- | ------------------------------------------ | -------------------- |
| 16  | [File Storage](doc/16-file-storage.md)     | Almacenamiento local |
| 17  | [Email Services](doc/17-email-services.md) | MailKit              |

### Testing y Calidad
| #   | Documento                    | Descripción            |
| --- | ---------------------------- | ---------------------- |
| 21  | [Testing](doc/21-testing.md) | Unit, Integración, E2E |

### DevOps y Producción
| #   | Documento                                    | Descripción              |
| --- | -------------------------------------------- | ------------------------ |
| 23  | [Docker](doc/23-docker-ci-cd.md)       | Contenedores, pipelines  |
| 24  | [Logging](doc/24-logging.md)                 | Serilog, trazabilidad    |
| 25  | [Background Jobs](doc/25-background-jobs.md) | Tareas programadas       |
| 26  | [Optimización](doc/26-optimizacion.md)       | Rendimiento              |
| 27  | [Seguridad HTTP](doc/27-seguridad-http.md)  | HSTS, HTTPS, Headers    |
| 28  | [CI/CD con GitHub Actions](doc/28-ci-cd.md) | Pipelines, automatización |

### Arquitectura
| #   | Documento                                                 | Descripción                       |
| --- | --------------------------------------------------------- | --------------------------------- |
| 29  | [Clean Architecture](doc/29-clean-architecture.md)        | Capas, estructura                 |
| 30  | [Organización Program.cs](doc/30-organizacion-program.md) | Extension Methods, modularización |

## ⚒️ Diagrama de Clases del Dominio

```mermaid
classDiagram
  direction TB

  %% ENUMS
  class UserRole {
    <<enumeration>>
    +ADMIN = "ADMIN"
    +USER = "USER"
  }

  class PedidoEstado {
    <<enumeration>>
    +PENDIENTE = "PENDIENTE"
    +PROCESANDO = "PROCESANDO"
    +ENVIADO = "ENVIADO"
    +ENTREGADO = "ENTREGADO"
    +CANCELADO = "CANCELADO"
  }

  %% CLASES PRINCIPALES - PostgreSQL
  class User {
    +long Id
    +string Username
    +string Email
    +string PasswordHash
    +string? Avatar
    +string Role
    +bool IsDeleted
    +DateTime CreatedAt
    +DateTime UpdatedAt
    +IsLocalAvatar()
    +HasDefaultAvatar()
    +GetAvatarUrl()
  }

  class Categoria {
    +long Id
    +string Nombre
    +bool IsDeleted
    +DateTime CreatedAt
    +DateTime UpdatedAt
    +ICollection~Producto~ Productos
  }

  class Producto {
    +long Id
    +string Nombre
    +string Descripcion
    +decimal Precio
    +int Stock
    +string? Imagen
    +long CategoriaId
    +bool IsDeleted
    +DateTime CreatedAt
    +DateTime UpdatedAt
    +byte[] RowVersion
    +Categoria Categoria
    +IsLocalImage()
    +HasDefaultImage()
    +GetImagenUrl()
  }

  %% CLASES EMBEBIDAS - MongoDB
  class Pedido {
    +ObjectId Id
    +long UserId
    +Destinatario Destinatario
    +List~PedidoItem~ Items
    +decimal Total
    +PedidoEstado Estado
    +string DireccionEnvio
    +bool IsDeleted
    +DateTime CreatedAt
    +DateTime UpdatedAt
  }

  class PedidoItem {
    +long ProductoId
    +string NombreProducto
    +int Cantidad
    +decimal Precio
    +decimal Subtotal
  }

  class Destinatario {
    +string NombreCompleto
    +string Email
    +string Telefono
    +Direccion Direccion
  }

  class Direccion {
    +string Calle
    +string Numero
    +string Ciudad
    +string Provincia
    +string Pais
    +string CodigoPostal
  }

  %% RELACIONES - Enums y Composiciones
  User ..> UserRole : "usa rol"
  Pedido ..> PedidoEstado : "usa estado"
  User "1" --> "*" Pedido : "referencia"
  Categoria "1" --> "*" Producto : "tiene"
  Producto "*" --> "1" Categoria : "pertenece"
  Pedido "1" *-- "1" Destinatario : "compone"
  Pedido "1" *-- "*" PedidoItem : "compone items"
  Destinatario "1" *-- "1" Direccion : "compone"
  PedidoItem "*" --> "1" Producto : "referencia"
```

## 🗄️ Entidades por Base de Datos

### 🐘 PostgreSQL (Datos Maestros)
```mermaid
erDiagram
    USER ||--o{ PEDIDO : "referencia"
    CATEGORIA ||--o{ PRODUCTO : "tiene"
    
    USER {
        bigint Id PK
        string Username
        string Email
        string PasswordHash
        string Avatar
        string Role
        bool IsDeleted
    }
    
    CATEGORIA {
        bigint Id PK
        string Nombre
        bool IsDeleted
    }
    
    PRODUCTO {
        bigint Id PK
        string Nombre
        string Descripcion
        decimal Precio
        int Stock
        bigint CategoriaId FK
        bool IsDeleted
    }
```

### 🍃 MongoDB (Pedidos - Documentos Embebidos)
```mermaid
erDiagram
    USER ||--o{ PEDIDO : "referencia"
    PEDIDO }o--o{ PEDIDO_ITEM : "items"
    PEDIDO }o--|| DESTINATARIO : "destinatario"
    DESTINATARIO }o--|| DIRECCION : "direccion"
    PEDIDO_ITEM }o--|| PRODUCTO : "referencia"
    
    PEDIDO {
        objectId Id PK
        long UserId FK
        object Destinatario
        array Items
        decimal Total
        string Estado
    }
    
    PEDIDO_ITEM {
        long ProductoId FK
        string NombreProducto
        int Cantidad
        decimal Precio
        decimal Subtotal
    }
    
    DESTINATARIO {
        string NombreCompleto
        string Email
        string Telefono
        object Direccion
    }
    
    DIRECCION {
        string Calle
        string Ciudad
        string Provincia
        string Pais
        string CodigoPostal
    }
```

**Resumen:**
| Base de Datos    | Entidades                                   | Tipo                 |
| ---------------- | ------------------------------------------- | -------------------- |
| **🐘 PostgreSQL** | User, Categoria, Producto                   | Relacional (FK)      |
| **🍃 MongoDB**    | Pedido, PedidoItem, Destinatario, Direccion | Documentos embebidos |



## 📂 Estructura del Proyecto

```
TiendaDawApi-NetCore/
├── TiendaApi.slnx                    # Solución global de .NET (formato moderno)
├── docker-compose.yml                # Orquestación por defecto
├── docker-compose.local.yml          # Desarrollo local (PostgreSQL, MongoDB)
├── docker-compose.prod.yml           # Producción (con API containerizada)
├── .env.example                      # Variables de entorno de ejemplo
│
├── TiendaApi.Api/                    # Proyecto Principal (ASP.NET Core 10)
│   ├── Program.cs                    # Configuración de Pipeline, DI y Middlewares
│   ├── Controllers/                  # Controladores REST (Auth, Categorias, Productos, Pedidos, Users)
│   ├── Services/                     # Lógica de negocio (Auth, Categorias, Productos, Users)
│   │   ├── Background/               # Background Jobs y tareas programadas
│   │   ├── Categorias/               # Servicios de categorías
│   │   ├── Email/                    # Servicio de email (MailKit)
│   │   ├── Pedidos/                  # Servicios de pedidos
│   │   ├── Productos/                # Servicios de productos
│   │   ├── Storage/                  # Servicios de almacenamiento
│   │   └── Usuarios/                 # Servicios de usuarios
│   ├── Repositories/                 # Acceso a datos (Categoria, Producto, User, Pedidos)
│   ├── Models/                       # Modelos de dominio (User, Producto, Categoria, Pedido)
│   ├── Dtos/                         # Data Transfer Objects (Request/Response)
│   ├── Data/                         # DbContext y configuración de bases de datos
│   ├── Mappers/                      # Mapeadores (Modelos <-> DTO)
│   ├── Validators/                   # Validadores FluentValidation
│   ├── Middleware/                   # Manejo global de excepciones
│   ├── GraphQL/                      # Schema y tipos HotChocolate
│   ├── Realtime/                     # WebSockets nativo y SignalR Hubs
│   ├── Helpers/                      # Utilidades y extensiones
│   ├── Errors/                       # Errores personalizados de dominio
│   ├── Exceptions/                   # Excepciones personalizadas
│   ├── Infrastructures/              # Extension Methods (DI, Pipeline, Bases de datos, Cache, SignalR, WebSockets, etc.)
│   ├── Properties/                   # Configuración de lanzamiento
│   ├── wwwroot/                      # Archivos estáticos (uploads, imágenes)
│   ├── appsettings.json              # Configuración general
│   ├── appsettings.Development.json  # Desarrollo (conexiones locales)
│   ├── appsettings.Production.json   # Producción (variables sensibles)
│   └── Dockerfile                    # Multi-stage build para producción
│
├── TiendaApi.Tests/                  # Pruebas Unitarias y de Integración
│   ├── Unit/                         # Tests unitarios (Services, Controllers, Repositories)
│   ├── Integration/                  # Tests de integración con bases de datos reales
│   └── coverage/                     # Reporte de cobertura de código
│
├── TiendaApi.Tests.E2E/              # Tests E2E (Postman + Bruno)
│   ├── Postman/                      # Colección Postman + Newman
│   │   ├── TiendaApi.NetCore.postman_collection.json
│   │   ├── TiendaApi.NetCore.postman_environment.json
│   │   ├── test-image.png
│   │   ├── docker-compose.yml
│   │   └── reports/
│   │
│   ├── Bruno/                        # Tests Bruno CLI
│   │   ├── 00-Setup/ a 13-Teardown/  # Tests organizados por carpeta
│   │   ├── environments/local.bru    # Variables de entorno
│   │   ├── assets/test-image.png
│   │   ├── docker-compose.yml
│   │   └── reports/
│
├── TiendaApi.Clients/                # Clientes frontend de ejemplo
│   ├── signalr-client-js/            # Cliente SignalR en JavaScript
│   ├── websocket-client-js/          # Cliente WebSocket en JavaScript
│   └── graphql-client-js/            # Cliente GraphQL en JavaScript
│
├── doc/                              # Documentación técnica
└── README.md                         # Este archivo
```

### Descripción de Carpetas Principales

| Carpeta               | Propósito                  | Contenido                                                                                     |
| --------------------- | -------------------------- | --------------------------------------------------------------------------------------------- |
| **Controllers**       | Entry points HTTP          | AuthController, CategoriasController, ProductosController, PedidosController, UsersController |
| **Services**          | Lógica de negocio          | AuthService, CategoriaService, ProductoService, UserService                                   |
| **Background**        | Tareas programadas         | BackgroundJobService, ProductoReportTask para reportes                                        |
| **Repositories**      | Abstracción de datos       | CategoriaRepository, ProductoRepository, UserRepository, PedidosRepository                    |
| **Models**            | Modelos de dominio         | User, Producto, Categoria, Pedido, Direccion, Destinatario                                    |
| **Dtos**              | Transferencia de datos     | Request/Response para API                                                                     |
| **Mappers**           | Modelos <-> DTO            | AutoMapper y Funciones de Extensión                                                           |
| **Validators**        | Validación de entrada      | FluentValidation rules                                                                        |
| **Middleware**        | Manejo de errores          | GlobalExceptionHandler                                                                        |
| **GraphQL**           | Queries, Mutations, Subs   | Schema HotChocolate                                                                           |
| **Realtime**          | Tiempo real (WS + SignalR) | WebSocket Handlers y SignalR Hubs para notificaciones por usuario/rol                         |
| **Infrastructures**   | Configuración modular      | Extension Methods para DI, Pipeline, SignalR, WebSockets, Middlewares                         |
| **TiendaApi.Clients** | Clientes frontend          | signalr-client-js, websocket-client-js, graphql-client-js                                     |

## 🏗️ Arquitectura Híbrida Onion-Like

El proyecto implementa una **arquitectura híbrida inspirada en Onion Architecture**, combinando los mejores principios de arquitectura limpia con las ventajas de ASP.NET Core. Esta hibridación sitúa los **modelos de dominio en el centro** (core), permitiendo un diseño desacoplado, testeable y mantenible.

### Principios Fundamentales

| Principio                           | Implementación                                                        |
| ----------------------------------- | --------------------------------------------------------------------- |
| **Core en el centro**               | Modelos (User, Producto, Categoria, Pedido) sin dependencias externas |
| **Inversión de dependencias**       | Interfaces en core, implementaciones en infraestructura               |
| **Separación de responsabilidades** | Controllers → Services → Repositories → Data                          |
| **Cross-cutting concerns**          | AutoMapper, FluentValidation, Result Pattern como utilidades          |
| **Multi-Database**                  | PostgreSQL (datos maestros), MongoDB (documentos), Redis (cache)      |

### Capas de la Arquitectura

```mermaid
graph TB
    subgraph "🌍 External Layer - Entry Points"
        REST[REST API<br/>HTTP Controllers]
        GQL[GraphQL<br/>HotChocolate]
        WS[WebSocket<br/>SignalR]
        SMTP[SMTP<br/>MailKit]
        FS[File System<br/>wwwroot/uploads]
        BG[Background Jobs<br/>BackgroundService<br/>Reportes]
    end

    subgraph "🎯 Application Layer - API"
        subgraph "Controllers & Filters"
            CTRL[Controllers<br/>Auth, Categorias,<br/>Productos, Pedidos, Users]
            FILT[Filters<br/>Auth, Validation,<br/>Exception]
        end
        subgraph "Middleware Pipeline"
            MID[Middleware<br/>Pipeline]
            AUTH[JWT Auth<br/>Bearer Token]
            EXC[Exception<br/>Handler]
            CORS[CORS]
            LOG[Logging]
        end
    end

    subgraph "🔷 Core Layer - Domain & Application"
        subgraph "Application Services"
            SVC[Services<br/>Auth, Categoria,<br/>Producto, User]
        end
        subgraph "Cross-Cutting Concerns"
            ROP[Result~T,E&gt;<br/>Railway Oriented<br/>Programming]
            VAL[FluentValidation<br/>Declarative Rules]
            MAP[AutoMapper<br/>Entity ↔ DTO]
            ERR[Domain Errors<br/>Custom Errors]
        end
        subgraph "Domain Entities"
            DOM[Entities<br/>User, Producto,<br/>Categoria, Pedido]
            DTO[DTOs<br/>Request/Response]
            ENU[Enums<br/>UserRole, PedidoEstado]
        end
    end

    subgraph "🔴 Infrastructure Layer - Data & External"
        subgraph "Data Access"
            REPO[Repositories<br/>Abstraction Layer]
            EF[EF Core<br/>PostgreSQL Provider]
            MONGO[MongoDB<br/>Driver]
            REDIS[Redis<br/>Cache Client]
        end
        subgraph "Security"
            JWT[JWT Tokens<br/>Generation & Validation]
            BCRYPT[BCrypt<br/>Password Hashing]
            CLAIMS[Claims & Roles<br/>Authorization]
        end
        subgraph "External Services"
            SMTP_EXT[SMTP Service<br/>MailKit]
            FS_EXT[File Storage<br/>Static Files]
        end
    end

    subgraph "💾 Data Stores"
        PG[(🐘 PostgreSQL<br/>Users, Categorias,<br/>Productos)]
        MONGO_DB[(🍃 MongoDB<br/>Pedidos, Items<br/>Embebidos)]
        REDIS_DB[(🔴 Redis<br/>Cache, Sessions)]
    end

    %% Flujo de datos
    REST --> CTRL
    GQL --> CTRL
    WS --> CTRL
    
    CTRL --> FILT
    FILT --> MID
    MID --> AUTH
    MID --> EXC
    MID --> CORS
    MID --> LOG
    
    CTRL --> SVC
    SVC --> ROP
    SVC --> VAL
    SVC --> MAP
    
    SVC --> DTO
    SVC --> DOM
    
    DTO --> MAP
    MAP --> DOM
    
    SVC --> REPO
    REPO --> EF
    REPO --> MONGO
    REPO --> REDIS
    
    EF --> PG
    MONGO --> MONGO_DB
    REDIS --> REDIS_DB
    
    CTRL --> JWT
    CTRL --> BCRYPT
    CTRL --> CLAIMS
    
    SVC --> SMTP_EXT
    SVC --> FS_EXT

    %% Estilos
    style REST fill:#3498db,color:#fff
    style GQL fill:#e74c3c,color:#fff
    style WS fill:#9b59b6,color:#fff
    style BG fill:#8e44ad,color:#fff
    style CTRL fill:#2980b9,color:#fff
    style SVC fill:#27ae60,color:#fff
    style DOM fill:#f39c12,color:#000
    style REPO fill:#16a085,color:#fff
    style PG fill:#3366cc,color:#fff
    style MONGO_DB fill:#47a248,color:#fff
    style REDIS_DB fill:#dc382d,color:#fff
    style ROP fill:#e91e63,color:#fff
    style VAL fill:#00bcd4,color:#fff
    style MAP fill:#ff9800,color:#000
```

### Estructura de Dependencias

```mermaid
graph TB
    subgraph "🟣 External Layer" 
        REST["🌐 REST API<br/>HTTP"]
        GQL["📡 GraphQL<br/>HotChocolate"]
        WS["🔌 WebSocket<br/>SignalR"]
        SMTP["📧 SMTP<br/>MailKit"]
        FS["📁 File System<br/>wwwroot/uploads"]
        BG["⏰ Background Jobs<br/>BackgroundService"]
    end

    subgraph "🔵 Application Layer"
        CTRL["🎯 Controllers<br/>Auth, Categorias, Productos<br/>Pedidos, Users, GraphQL"]
        FILT["🔍 Filters & Middleware<br/>Auth, Validation, Exception<br/>JWT, CORS, RateLimit"]
    end

    subgraph "🟠 Core Layer"
        SVC["🏢 Services<br/>Auth, Categoria, Producto<br/>User, Pedidos, Background"]
        CCC["🛠️ Cross-Cutting<br/>AutoMapper, FluentValidation<br/>Result~T,E&gt;, Errors"]
    end

    subgraph "🟡 Abstractions"
        REPO["💾 Repositories<br/>IUser, IProducto, ICategoria<br/>IPedidos, IBackgroundTask"]
    end

    subgraph "🔴 Infrastructure"
        DA["💾 Data Access<br/>Repositories, EF Core<br/>MongoDB, Redis"]
        DS["🗄️ Data Stores<br/>PostgreSQL, MongoDB<br/>Redis Cache"]
        SEC["🔐 Security<br/>JWT, BCrypt, Claims<br/>Roles, Policies"]
        EXT["📧 External Services<br/>SMTP, File System<br/>HTTP Clients, Background Jobs"]
    end

    REST & GQL & WS & SMTP & FS & BG === CTRL
    CTRL === FILT
    FILT === SVC
    SVC === CCC
    CCC === REPO
    REPO === DA
    DA === DS & SEC & EXT
```

#### Ventajas de Esta Arquitectura

| Ventaja            | Descripción                                |
| ------------------ | ------------------------------------------ |
| **Testabilidad**   | Core sin dependencias → fácil mocking      |
| **Mantenibilidad** | Cambios en infraestructura no afectan core |
| **Flexibilidad**   | Multi-database strategy implementado       |
| **Escalabilidad**  | Separación clara de responsabilidades      |
| **Seguridad**      | JWT, BCrypt, Claims bien encapsulados      |

### 🛤️ Railway Oriented Programming (ROP)

El proyecto implementa **Railway Oriented Programming (ROP)** usando la librería `CSharpFunctionalExtensions`, un patrón funcional que permite encadenar operaciones de forma segura, manejando errores sin necesidad de exceptions ni try-catch.

#### Conceptos Fundamentales

| Concepto    | Descripción                                         | Ejemplo                              |
| ----------- | --------------------------------------------------- | ------------------------------------ |
| **Result**  | Wrapper que encapsula éxito o fallo                 | `Result<T, TError>`                  |
| **Success** | Camino happy path con valor                         | `Result.Success(value)`              |
| **Failure** | Camino de error con mensaje                         | `Result.Failure(error)`              |
| **Bind**    | Encadena operaciones que retornan Result            | `result.Bind()`                      |
| **Map**     | Transforma el valor en éxito                        | `result.Map(value => newValue)`      |
| **Match**   | Maneja ambos casos (éxito y fallo)                  | `result.Match(onSuccess, onFailure)` |
| **Ensure**  | Valida condiciones, retorna Failure si no se cumple | `result.Ensure(condition, error)`    |

#### Anatomía del Patrón (Two-Track Model)

```mermaid
flowchart TB
    subgraph ROP["RAILWAY ORIENTED PROGRAMMING"]
        INP["INPUT<br/>(Happy Path)"]
        R1["RAIL 1<br/>(SUCCESS)"]
        OUT_H["OUTPUT<br/>(Happy)"]
        R2["RAIL 2<br/>(FAILURE)"]
        OUT_E["OUTPUT<br/>(Error)"]
        
        INP --> R1
        R1 --> OUT_H
        R1 -.->|"SWITCH (error)"| R2
        R2 --> OUT_E
    end

    style INP fill:#27ae60,color:#fff
    style R1 fill:#27ae60,color:#fff
    style OUT_H fill:#27ae60,color:#fff
    style R2 fill:#e74c3c,color:#fff
    style OUT_E fill:#e74c3c,color:#fff
```

#### Beneficios de ROP

| Beneficio          | Descripción                                  |
| ------------------ | -------------------------------------------- |
| **Sin Exceptions** | Los errores son valores, no excepciones      |
| **Composabilidad** | Encadenar operaciones de forma segura        |
| **Tipado Seguro**  | El tipo de error está en la firma del método |
| **Legibilidad**    | Flujo lineal en lugar de if-else anidados    |
| **Testeabilidad**  | Facilidad para testear casos de error        |

#### Ejemplos de Uso
**Encadenamiento con Bind:**
```csharp
public async Task<Result<PedidoDto, DomainError>> CreatePedidoAsync(PedidoRequestDto dto)
{
    return await Validate(dto)
        .Bind(ValidateStockAsync)      // Si falla, retorna inmediatamente
        .Bind(CreatePedidoAsync)       // Continúa si todo ok
        .Map(p => _mapper.Map<PedidoDto>(p));  // Transforma resultado
}
```

**Manejo de Errores Personalizados:**
```csharp
public static class DomainErrors
{
    public static readonly DomainError NotFound = 
        new("ENTITY_NOT_FOUND", "Entidad no encontrada");
    
    public static readonly DomainError InvalidState = 
        new("INVALID_STATE", "Estado inválido para la operación");
    
    public static readonly DomainError ConcurrencyConflict = 
        new("CONCURRENCY_CONFLICT", "Conflicto de concurrencia");
}

// Uso
return Result.Failure<ProductoDto, DomainError>(DomainErrors.NotFound);
```

**Patrón en Controllers:**
```csharp
[HttpPost]
public async Task<IActionResult> Create([FromBody] ProductoRequestDto dto)
{
    var result = await _productoService.CreateAsync(dto);
    
    return result.Match(
        onSuccess: producto => CreatedAtAction(
            nameof(GetById), 
            new { id = producto.Id }, 
            producto),
        onFailure: error => BadRequest(new { error.Code, error.Message })
    );
}
```

#### Comparación: ROP vs Try-Catch

| Aspecto         | Try-Catch tradicional     | ROP                     |
| --------------- | ------------------------- | ----------------------- |
| **Errores**     | Exceptions                | Valores                 |
| **Flujo**       | Saltos inesperados        | Lineal                  |
| **Tipado**      | Exception genérica        | Error tipado específico |
| **Composición** | Difícil                   | natural con Bind/Map    |
| **Performance** | Costoso (stack unwinding) | Barato (simple wrapper) |

## 🗄️ Estrategia Multi-Base de Datos


| Base de Datos    | Uso                                  | Entidades                                   | Tecnologías                                         |
| ---------------- | ------------------------------------ | ------------------------------------------- | --------------------------------------------------- |
| **🐘 PostgreSQL** | Datos maestros relacionales          | User, Categoria, Producto                   | EF Core SQL (System.ComponentModel.DataAnnotations) |
| **🍃 MongoDB**    | Documentos transaccionales embebidos | Pedido, PedidoItem, Destinatario, Direccion | EF Core MongoDB (Con documentos anidados)           |
| **🔴 Redis**      | Cache distribuido                    | Sessions, consultas frecuentes              | StackExchange.Redis (Cache-Aside)                   |

**Patrón de datos:**
- PostgreSQL: Entidades normalizadas con Foreign Keys
- MongoDB: Documentos embebidos para mantener historial de precios (los items del pedido no cambian si el producto cambia)

## 🔐 Seguridad

- ✅ **JWT Authentication**: Tokens Bearer con expiración configurable
- ✅ **Role-Based Authorization**: ADMIN y USER roles con políticas granulares
- ✅ **FluentValidation**: Validaciones declarativas en DTOs
- ✅ **Global Exception Handler**: Respuestas de error consistentes y centralizadas
- ✅ **Password Hashing**: BCrypt con salt aleatorio y work factor configurable
- ✅ **Soft Delete**: Eliminación lógica en entidades (IsDeleted)
- ✅ **JWT Claims**: Información de usuario (sub, email, role, jti) en tokens
- ✅ **CORS Policy**: Configuración de orígenes permitidos
- ✅ **WebSocket Security**: WS nativo con autenticación via query string (`?token=JWT`)
- ✅ **SignalR Security**: Hubs con `[Authorize]`, Context.User disponible, grupos dinámicos (user-{id}, admins)
- ✅ **Concurrency Control**: RowVersion para control de concurrencia optimista
- ✅ **EF Core Parameterization**: Protección contra SQL Injection via LINQ
- ✅ **HTTPS + HSTS**: Redirección HTTP→HTTPS y HSTS con max-age 365 días
- ✅ **Security Headers**: X-Content-Type-Options, X-Frame-Options, X-XSS-Protection, Referrer-Policy, Permissions-Policy

> **Ver más:** [Seguridad HTTP](doc/27-seguridad-http.md)

## 📡 Endpoints

### Auth (versionado)

| Endpoint              | Método | Auth | Descripción                  |
| --------------------- | ------ | ---- | ---------------------------- |
| `/api/v1/auth/signup` | POST   | No   | Registrar nuevo usuario      |
| `/api/v1/auth/signin` | POST   | No   | Iniciar sesión y obtener JWT |

### Categorías

| Endpoint               | Método | Auth  | Descripción                       |
| ---------------------- | ------ | ----- | --------------------------------- |
| `/api/categorias`      | GET    | No    | Obtener todas (paginado, filtros) |
| `/api/categorias/{id}` | GET    | No    | Obtener por ID                    |
| `/api/categorias`      | POST   | ADMIN | Crear categoría                   |
| `/api/categorias/{id}` | PUT    | ADMIN | Actualizar categoría              |
| `/api/categorias/{id}` | DELETE | ADMIN | Eliminar categoría                |

### Productos

| Endpoint                                 | Método | Auth  | Descripción                       |
| ---------------------------------------- | ------ | ----- | --------------------------------- |
| `/api/productos`                         | GET    | No    | Obtener todos (paginado, filtros) |
| `/api/productos/{id}`                    | GET    | No    | Obtener por ID                    |
| `/api/productos/categoria/{categoriaId}` | GET    | No    | Productos por categoría           |
| `/api/productos`                         | POST   | ADMIN | Crear producto                    |
| `/api/productos/{id}`                    | PUT    | ADMIN | Actualizar producto               |
| `/api/productos/{id}`                    | DELETE | ADMIN | Eliminar producto                 |
| `/api/productos/{id}/imagen`             | PATCH  | ADMIN | Actualizar imagen                 |
| `/api/productos/{id}`                    | PATCH  | ADMIN | Actualizar parcialmente           |

### Pedidos (Administrador)

| Endpoint                   | Método | Auth  | Descripción              |
| -------------------------- | ------ | ----- | ------------------------ |
| `/api/pedidos`             | GET    | ADMIN | Obtener todos            |
| `/api/pedidos/paged`       | GET    | ADMIN | Obtener todos (paginado) |
| `/api/pedidos/{id}`        | GET    | ADMIN | Obtener por ID           |
| `/api/pedidos`             | PUT    | ADMIN | Actualizar pedido        |
| `/api/pedidos/{id}`        | DELETE | ADMIN | Eliminar pedido          |
| `/api/pedidos/{id}/estado` | PUT    | ADMIN | Actualizar estado        |

### Pedidos (Usuario - Mis Pedidos)

| Endpoint                | Método | Auth | Descripción                  |
| ----------------------- | ------ | ---- | ---------------------------- |
| `/api/pedidos/me`       | GET    | USER | Mis pedidos                  |
| `/api/pedidos/me/paged` | GET    | USER | Mis pedidos (paginado)       |
| `/api/pedidos/me/{id}`  | GET    | USER | Obtener pedido propio por ID |
| `/api/pedidos/me`       | POST   | USER | Crear nuevo pedido           |
| `/api/pedidos/me/{id}`  | PUT    | USER | Actualizar pedido propio     |
| `/api/pedidos/me/{id}`  | DELETE | USER | Cancelar pedido propio       |

### Usuarios (Administrador)

| Endpoint                 | Método | Auth  | Descripción                       |
| ------------------------ | ------ | ----- | --------------------------------- |
| `/api/users`             | GET    | ADMIN | Obtener todos (paginado, filtros) |
| `/api/users/{id}`        | GET    | ADMIN | Obtener por ID                    |
| `/api/users`             | POST   | ADMIN | Crear usuario                     |
| `/api/users/{id}`        | PUT    | ADMIN | Actualizar usuario                |
| `/api/users/{id}`        | DELETE | ADMIN | Eliminar usuario                  |
| `/api/users/{id}/avatar` | PATCH  | USER* | Actualizar avatar                 |

### Usuarios (Perfil Propio)

| Endpoint                | Método | Auth | Descripción          |
| ----------------------- | ------ | ---- | -------------------- |
| `/api/users/me/profile` | GET    | USER | Obtener mi perfil    |
| `/api/users/me/profile` | PUT    | USER | Actualizar mi perfil |
| `/api/users/me/profile` | DELETE | USER | Eliminar mi cuenta   |

### Storage (Archivos)

| Endpoint            | Método | Auth | Descripción                |
| ------------------- | ------ | ---- | -------------------------- |
| `/storage/{**path}` | GET    | No   | Obtener archivo (imágenes) |

### WebSockets (Tiempo Real)

| Endpoint                         | Auth | Descripción                             | Eventos                                              |
| -------------------------------- | ---- | --------------------------------------- | ---------------------------------------------------- |
| `ws://host/ws/productos`         | No   | Notificaciones de productos (broadcast) | PRODUCTO_CREATED, PRODUCTO_UPDATED, PRODUCTO_DELETED |
| `ws://host/ws/pedidos?token=JWT` | JWT  | Notificaciones de pedidos (por rol)     | PEDIDO_CREATED, PEDIDO_ESTADO_UPDATED                |

**WebSocket Productos:** Broadcast a todos los clientes conectados (sin autenticación).

**WebSocket Pedidos:**
- USER: Solo recibe notificaciones de SUS pedidos
- ADMIN: Recibe notificaciones de TODOS los pedidos
- Requiere JWT token como query string: `?token=JWT_TOKEN`

### SignalR (Realtime)

| Endpoint          | Auth | Descripción                             | Eventos                                                |
| ----------------- | ---- | --------------------------------------- | ------------------------------------------------------ |
| `/hubs/productos` | No   | Notificaciones de productos (broadcast) | ProductoCreado, ProductoActualizado, ProductoEliminado |
| `/hubs/pedidos`   | JWT  | Notificaciones de pedidos (por rol)     | PedidoCreado, PedidoActualizado, PedidoEliminado       |

**SignalR Productos:** Broadcast a todos los clientes conectados (sin autenticación).

**SignalR Pedidos:**
- USER: Solo recibe notificaciones de SUS pedidos (grupo `user-{id}`)
- ADMIN: Recibe notificaciones de TODOS los pedidos (grupo `admins`)
- Requiere JWT token via `accessTokenFactory()` en cliente

**Cliente JavaScript:**
```javascript
const connection = new HubConnectionBuilder()
    .withUrl("/hubs/pedidos", {
        accessTokenFactory: () => jwtToken
    })
    .build();

connection.on("PedidoCreado", (pedido) => {
    console.log("Nuevo pedido:", pedido);
});

await connection.start();
```

### GraphQL

| Endpoint    | Método | Descripción        |
| ----------- | ------ | ------------------ |
| `/graphql`  | POST   | Endpoint GraphQL   |
| `/graphiql` | GET    | Playground GraphQL |

**Queries disponibles:**

```graphql
# Productos
productos                    # Todos los productos
producto(id: Long!)          # Producto por ID
productos(first: Int, after: String) # Productos paginados

# Categorías
categorias                   # Todas las categorías
categoria(id: Long!)         # Categoría por ID
categorias(first: Int, after: String) # Categorías paginadas
```

**Ejemplo de Query: Obtener producto con solo nombre de categoría**

```graphql
query ObtenerProductoConCategoria($id: Long!) {
  producto(id: $id) {
    id
    nombre
    descripcion
    precio
    stock
    imagen
    categoria {
      nombre
    }
  }
}
```

**Variables:**

```json
{
  "id": 1
}
```

**Respuesta:**

```json
{
  "data": {
    "producto": {
      "id": 1,
      "nombre": "Laptop Dell XPS 15",
      "descripcion": "Portátil de alta gama con procesador Intel Core i7",
      "precio": 1299.99,
      "stock": 10,
      "imagen": "https://localhost:5000/storage/productos/laptop-dell-xps-15.jpg",
      "categoria": {
        "nombre": "Electrónica"
      }
    }
  }
}
```

> **Nota:** GraphQL soporta tanto **Queries**, **Mutations** y **Subscriptions** para notificaciones en tiempo real sobre Productos.

**Mutations disponibles (requieren token JWT con rol ADMIN):**

```graphql
# Productos (requiere ADMIN)

```

**Authorization:** Todas las mutations requieren header:
```
Authorization: Bearer <token_admin>
```

**Mutations de Productos (requieren ADMIN):**

```graphql
# Productos (requiere ADMIN)
createProducto(input: CreateProductoInput!): Producto
updateProducto(id: Long!, input: UpdateProductoInput!): Producto
deleteProducto(id: Long!): Boolean
```

**Input Types:**

```graphql
input CreateProductoInput {
  nombre: String!
  descripcion: String
  precio: Float!
  stock: Int!
  imagen: String
  categoriaId: Long!
}

input UpdateProductoInput {
  nombre: String
  descripcion: String
  precio: Float
  stock: Int
  imagen: String
  categoriaId: Long
}
```

**Ejemplo: Crear producto**

```graphql
mutation CrearProducto($input: CreateProductoInput!) {
  createProducto(input: $input) {
    id
    nombre
    precio
    stock
    categoria {
      nombre
    }
  }
}
```

**Headers:**

```json
{
  "Authorization": "Bearer eyJhbGciOiJIUzI1NiIs...",
  "Content-Type": "application/json"
}
```

**Variables:**

```json
{
  "input": {
    "nombre": "Laptop Dell XPS 15",
    "descripcion": "Portátil de alta gama",
    "precio": 1299.99,
    "stock": 10,
    "categoriaId": 1
  }
}
```

**Ejemplo de Respuesta:**

```json
{
  "data": {
    "createProducto": {
      "id": 1,
      "nombre": "Laptop Dell XPS 15",
      "precio": 1299.99,
      "stock": 10,
      "categoria": {
        "nombre": "Electrónica"
      }
    }
  }
}
```

**Ejemplo de Errores:**

```json
{
  "errors": [
    {
      "message": "El precio no puede ser negativo",
      "extensions": {
        "code": "CONFLICT"
      }
    }
  ]
}
```

**Subscriptions (GraphQL en tiempo real):**

```graphql
# Productos
subscription {
  onProductoCreado { productoId nombre precio stock }
  onProductoActualizado { productoId nombre precio stock }
  onProductoEliminado { productoId }
  onStockBajo { productoId nombre stockActual umbralStock }
}
```

**Conexión Subscripción:**
```
WS ws://localhost:5000/graphql

# Enviar:
{"type": "subscribe", "payload": {"query": "subscription { onProductoCreado { productoId nombre } }"}}
```

**Ejemplo de respuesta en tiempo real:**

```json
{
  "data": {
    "onProductoCreado": {
      "productoId": 10,
      "nombre": "Nuevo Producto",
      "precio": 99.99,
      "stock": 5,
      "createdAt": "2026-01-17T10:30:00Z"
    }
  }
}
```

---

## 👥 Usuarios Demo

| Usuario | Email              | Password | Rol   |
| ------- | ------------------ | -------- | ----- |
| admin   | admin@tienda.com   | admin    | ADMIN |
| userdaw | userdaw@tienda.com | userdaw  | USER  |

**Nota:** Las contraseñas son las mismas que los usernames para facilitar las pruebas.

## 📝 Licencia

Este proyecto es un ejemplo educativo con fines didácticos.

## 👨‍💻 Autor

Codificado con :sparkling_heart: por [José Luis González Sánchez](https://joseluisgs.dev)

[![GitHub](https://img.shields.io/github/followers/joseluisgs?style=social)](https://github.com/joseluisgs)
[![GitHub](https://img.shields.io/github/stars/joseluisgs?style=social)](https://github.com/joseluisgs)

### Contacto

<p>
    <a href="https://joseluisgs.dev/" target="_blank">
        <img loading="lazy" src="https://raw.githubusercontent.com/joseluisgs/joseluisgs/master/images/social-icons/favicon.png" height="32">
    </a>&nbsp;
    <a href="https://github.com/joseluisgs" target="_blank">
        <img loading="lazy" src="https://raw.githubusercontent.com/joseluisgs/joseluisgs/master/images/social-icons/github.svg" height="32">
    </a>&nbsp;
    <a href="https://www.linkedin.com/in/JoseLuisGSDev" target="_blank">
        <img loading="lazy" src="https://raw.githubusercontent.com/joseluisgs/joseluisgs/master/images/social-icons/linkedin.png" height="32">
    </a>&nbsp;
    <a href="https://www.youtube.com/@joseluisgs" target="_blank">
        <img loading="lazy" src="https://raw.githubusercontent.com/joseluisgs/joseluisgs/master/images/social-icons/youtube.png" height="32">
    </a>&nbsp;
    <a href="https://x.com/JoseLuisGSDev" target="_blank">
        <img loading="lazy" src="https://raw.githubusercontent.com/joseluisgs/joseluisgs/master/images/social-icons/twitter.png" height="32">
    </a>&nbsp;
    <a href="https://www.instagram.com/joseluisgs.dev/" target="_blank">
        <img loading="lazy" src="https://raw.githubusercontent.com/joseluisgs/joseluisgs/master/images/social-icons/instagram.png" height="32">
    </a>
</p>

## Licencia de uso

Este repositorio está licenciado bajo **Creative Commons**. Por favor si compartes, usas o modificas este proyecto cita a su autor.

<a rel="license" href="https://creativecommons.org/licenses/by-nc-sa/4.0/deed.es"><img alt="Licencia de Creative Commons" style="border-width:0" src="https://i.creativecommons.org/l/by-nc-sa/4.0/88x31.png" /></a>
