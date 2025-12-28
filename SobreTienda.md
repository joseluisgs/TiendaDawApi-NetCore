

## 📚 Proyecto Educativo: Exception vs Result Pattern

Esta API REST completa demuestra **DOS enfoques pedagógicos** diferentes de manejo de errores para que los estudiantes comparen, aprendan y tomen decisiones informadas:

1. **Categorías**: Enfoque Tradicional con Exceptions (familiar para Java/Spring Boot devs)
2. **Productos**: Result Pattern Moderno (functional programming approach)

## 🎯 Objetivo Educativo

Proporcionar una comparación práctica y completa de dos patrones de manejo de errores:

### 🔴 **Enfoque Tradicional - Exceptions** (Categorías)
```csharp
// Service throws exceptions
public async Task<CategoriaDto> FindByIdAsync(long id)
{
    var categoria = await _repository.FindByIdAsync(id);
    
    if (categoria == null)
        throw new NotFoundException($"Categoría con ID {id} no encontrada");
        
    return _mapper.Map<CategoriaDto>(categoria);
}

// Controller catches exceptions
[HttpGet("{id}")]
public async Task<IActionResult> GetById(long id)
{
    try
    {
        var categoria = await _service.FindByIdAsync(id);
        return Ok(categoria);
    }
    catch (NotFoundException ex)
    {
        return NotFound(new { message = ex.Message });
    }
}
```

**Características**:
- ✅ Familiar para desarrolladores Java/Spring Boot
- ✅ Manejo centralizado con GlobalExceptionHandler
- ✅ Simple para casos CRUD básicos
- ❌ Control de flujo oculto
- ❌ Overhead de performance (stack unwinding)
- ❌ Requiere try/catch en controladores

### 🟢 **Enfoque Moderno - Result Pattern** (Productos)
```csharp
// Service returns Result
public async Task<Result<ProductoDto, AppError>> FindByIdAsync(long id)
{
    var producto = await _repository.FindByIdAsync(id);
    
    if (producto == null)
        return Result<ProductoDto, AppError>.Failure(
            AppError.NotFound($"Producto con ID {id} no encontrado")
        );
        
    var dto = _mapper.Map<ProductoDto>(producto);
    return Result<ProductoDto, AppError>.Success(dto);
}

// Controller uses pattern matching
[HttpGet("{id}")]
public async Task<IActionResult> GetById(long id)
{
    var resultado = await _service.FindByIdAsync(id);
    
    return resultado.Match(
        onSuccess: producto => Ok(producto),
        onFailure: error => error.Type switch
        {
            ErrorType.NotFound => NotFound(new { message = error.Message }),
            _ => StatusCode(500, new { message = error.Message })
        }
    );
}
```

**Características**:
- ✅ Type-safe error handling
- ✅ Explícito en las firmas de métodos
- ✅ Sin try/catch en controladores
- ✅ Mejor performance (sin exceptions)
- ✅ Estilo functional programming
- ✅ Más fácil de testear
- ❌ Curva de aprendizaje para devs acostumbrados a exceptions

## 📊 Comparativa Directa

| Aspecto | Exceptions (Categorías) | Result Pattern (Productos) |
|---------|------------------------|---------------------------|
| **Sintaxis** | `throw new NotFoundException()` | `return AppError.NotFound()` |
| **Control Flow** | Hidden (exceptions) | Explicit (return values) |
| **Performance** | Stack unwinding overhead | Better performance |
| **Testabilidad** | Requires exception mocking | Easy to test |
| **Type Safety** | Runtime errors | Compile-time safety |
| **Java Equivalent** | Spring Boot standard | Either<L,R> from Vavr |
| **Familiar to** | Java/Spring Boot devs | Functional programmers |

## 🏗️ Arquitectura

```
api/TiendaApi/
├── Common/                      # Result Pattern implementation
│   ├── Result.cs               # Result<T,E> generic type
│   ├── AppError.cs             # Error types for Result
│   └── Maybe.cs                # Optional pattern
├── Models/
│   ├── Entities/               # Database entities
│   │   ├── Categoria.cs        # PostgreSQL entity
│   │   ├── Producto.cs         # PostgreSQL entity
│   │   ├── User.cs             # PostgreSQL entity
│   │   └── Pedido.cs           # MongoDB document
│   └── DTOs/                   # Data Transfer Objects
│       ├── CategoriaDto.cs
│       ├── ProductoDto.cs
│       ├── UserDto.cs
│       └── PedidoDto.cs
├── Repositories/               # Data access layer
│   ├── ICategoriaRepository.cs
│   ├── CategoriaRepository.cs
│   ├── IProductoRepository.cs
│   └── ProductoRepository.cs
├── Services/                   # Business logic
│   ├── CategoriaService.cs     # Exception-based
│   ├── ProductoService.cs      # Result Pattern
│   └── MappingProfile.cs       # AutoMapper config
├── Controllers/                # API endpoints
│   ├── CategoriasController.cs # Exception-based
│   └── ProductosController.cs  # Result Pattern
├── Exceptions/                 # Custom exceptions
│   ├── NotFoundException.cs
│   ├── ValidationException.cs
│   └── BusinessException.cs
├── Middleware/
│   └── GlobalExceptionHandler.cs  # Exception handler
└── Data/
    ├── TiendaDbContext.cs      # EF Core context
    └── MongoDbContext.cs       # MongoDB context
```

## 🚀 Runtime Features (NEW!)

Esta API ahora incluye características **runtime críticas** que demuestran funcionalidad empresarial completa:

### 🔐 **Autenticación JWT**
- **Registro de usuarios**: POST `/v1/auth/signup` - Hash de contraseñas con BCrypt
- **Inicio de sesión**: POST `/v1/auth/signin` - Generación de tokens JWT
- **Protección de endpoints**: Productos POST/PUT/DELETE requieren autenticación
- **Tokens JWT**: Symmetric key signing, configurable expiration

### 🔌 **WebSockets en Tiempo Real**
- **Endpoint WebSocket**: `ws://localhost:5000/ws/v1/productos`
- **Notificaciones automáticas**: Cuando se crea, actualiza o elimina un producto
- **Formato de mensaje**:
  ```json
  {
    "type": "CREATED|UPDATED|DELETED",
    "productoId": 1,
    "productoNombre": "Laptop Dell",
    "timestamp": "2024-10-26T10:00:00Z",
    "data": { ... }
  }
  ```

### ⚡ **Caché Redis (Cache-Aside Pattern)**
- **Productos en caché**: FindAll y FindById usan Redis para mejor rendimiento
- **TTL configurable**: Expiration time configurable en appsettings.json
- **Invalidación automática**: Al crear, actualizar o eliminar productos
- **Conexión**: Redis en localhost:6379 (configurable)

### 📧 **Servicio de Email con Cola en Background**
- **Email asíncrono**: MailKit SMTP con background worker
- **Channel/Queue**: Procesamiento no bloqueante de emails
- **Notificaciones automáticas**: Al crear productos, email al admin
- **Configuración SMTP**: Configurable en appsettings.json

### 🔍 **GraphQL API**
- **Endpoint GraphQL**: POST `/graphql` para queries
- **GraphiQL UI**: `/graphiql` - Interfaz interactiva para explorar el API
- **Schema**: Queries para productos y categorías
- **Ejemplo de query**:
  ```graphql
  {
    productos {
      id
      nombre
      precio
      categoria {
        nombre
      }
    }
  }
  ```

## 🚀 Getting Started

### Prerequisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) o superior
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (para bases de datos)
- Visual Studio 2022, VS Code, o Rider

### 1. Clonar el Repositorio

```bash
git clone https://github.com/joseluisgs/prueba-c-sharp.git
cd prueba-c-sharp/api
```

### 2. Iniciar Bases de Datos

```bash
# Iniciar PostgreSQL, MongoDB y Redis
docker-compose up -d

# Verificar que están corriendo
docker-compose ps
```

### 3. Restaurar Dependencias

```bash
cd TiendaApi
dotnet restore
```

### 4. Ejecutar la API

```bash
dotnet run
```

La API estará disponible en: `http://localhost:5000`

### 5. Explorar con Swagger

Abre tu navegador en: `http://localhost:5000`

Verás la interfaz de Swagger con todos los endpoints documentados.

## 📝 Endpoints Disponibles

### 🔴 Categorías (Exception-based)

```
GET    /api/categorias          - Listar todas las categorías
GET    /api/categorias/{id}     - Obtener categoría por ID
POST   /api/categorias          - Crear nueva categoría
PUT    /api/categorias/{id}     - Actualizar categoría
DELETE /api/categorias/{id}     - Eliminar categoría (soft delete)
```

**Ejemplo Request - Crear Categoría:**
```bash
curl -X POST http://localhost:5000/api/categorias \
  -H "Content-Type: application/json" \
  -d '{"nombre": "Tecnología"}'
```

**Ejemplo Response - Success:**
```json
{
  "id": 4,
  "nombre": "Tecnología",
  "createdAt": "2025-10-24T21:00:00Z",
  "updatedAt": "2025-10-24T21:00:00Z"
}
```

**Ejemplo Response - Error (Not Found):**
```json
{
  "message": "Categoría con ID 999 no encontrada"
}
```

### 🟢 Productos (Result Pattern)

```
GET    /api/productos                  - Listar todos los productos
GET    /api/productos/{id}             - Obtener producto por ID
GET    /api/productos/categoria/{id}   - Productos por categoría
POST   /api/productos                  - Crear nuevo producto
PUT    /api/productos/{id}             - Actualizar producto
DELETE /api/productos/{id}             - Eliminar producto (soft delete)
```

**Ejemplo Request - Crear Producto:**
```bash
curl -X POST http://localhost:5000/api/productos \
  -H "Content-Type: application/json" \
  -d '{
    "nombre": "iPhone 15 Pro",
    "descripcion": "Último modelo de Apple",
    "precio": 1199.99,
    "stock": 25,
    "categoriaId": 1
  }'
```

**Ejemplo Response - Success:**
```json
{
  "id": 4,
  "nombre": "iPhone 15 Pro",
  "descripcion": "Último modelo de Apple",
  "precio": 1199.99,
  "stock": 25,
  "imagen": null,
  "categoriaId": 1,
  "categoriaNombre": "Electrónica",
  "createdAt": "2025-10-24T21:00:00Z",
  "updatedAt": "2025-10-24T21:00:00Z"
}
```

**Ejemplo Response - Error (Validation):**
```json
{
  "message": "El precio debe ser mayor que 0"
}
```

## 🧪 Testing

### Ejecutar Tests

```bash
cd TiendaApi.Tests
dotnet test
```

Los tests incluyen:
- ✅ Unit tests para servicios (con Moq)
- ✅ Integration tests con Testcontainers
- ✅ Tests de comparación Exception vs Result Pattern

## 🗄️ Base de Datos

### PostgreSQL Schema

La base de datos se crea automáticamente al iniciar la API con datos de seed:

**Categorías:**
- Electrónica
- Ropa
- Libros

**Productos:**
- Laptop Dell XPS 15 (Electrónica)
- Camiseta Nike (Ropa)
- Clean Code (Libros)

**Users:**
- admin / Admin123! (ADMIN role)

### MongoDB Collections

**Pedidos:**
- Estructura de documentos flexible
- Embedded PedidoItems
- Estados: PENDIENTE, PROCESANDO, ENVIADO, ENTREGADO, CANCELADO

## 📚 Aprendiendo de los Patrones

### Cuándo Usar Exceptions (Categorías)

✅ **USAR cuando:**
- Migrating from Java/Spring Boot
- Team familiar with exception handling
- Simple CRUD operations
- Standard HTTP errors only
- Quick prototyping

❌ **EVITAR cuando:**
- Complex business logic with multiple failure paths
- Performance is critical
- Want explicit error handling
- Functional programming style preferred

### Cuándo Usar Result Pattern (Productos)

✅ **USAR cuando:**
- Complex business logic
- Multiple failure scenarios
- Want type-safe error handling
- Performance critical code
- Modern functional approach
- Explicit error handling needed

❌ **EVITAR cuando:**
- Team not familiar with functional concepts
- Simple CRUD only
- Quick prototype needed
- Migrating legacy Java code directly

## 🔄 Migración desde Java/Spring Boot

### Exception Handling

**Java (Spring Boot):**
```java
@RestController
@RequestMapping("/api/categorias")
public class CategoriaController {
    
    @GetMapping("/{id}")
    public ResponseEntity<CategoriaDto> getById(@PathVariable Long id) {
        try {
            CategoriaDto categoria = service.findById(id);
            return ResponseEntity.ok(categoria);
        } catch (NotFoundException e) {
            return ResponseEntity.notFound().build();
        }
    }
}

@ControllerAdvice
public class GlobalExceptionHandler {
    @ExceptionHandler(NotFoundException.class)
    public ResponseEntity<ErrorResponse> handleNotFound(NotFoundException ex) {
        return ResponseEntity.status(HttpStatus.NOT_FOUND)
            .body(new ErrorResponse(ex.getMessage()));
    }
}
```

**C# (ASP.NET Core):**
```csharp
[ApiController]
[Route("api/[controller]")]
public class CategoriasController : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id)
    {
        try
        {
            var categoria = await _service.FindByIdAsync(id);
            return Ok(categoria);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}

// GlobalExceptionHandler middleware catches exceptions
```

### Result Pattern

**Java (with Vavr):**
```java
public Either<AppError, ProductoDto> findById(Long id) {
    return repository.findById(id)
        .map(mapper::toDto)
        .map(Either::<AppError, ProductoDto>right)
        .orElse(Either.left(AppError.notFound("Producto no encontrado")));
}

@GetMapping("/{id}")
public ResponseEntity<?> getById(@PathVariable Long id) {
    return service.findById(id)
        .fold(
            error -> ResponseEntity.status(error.getHttpStatus()).body(error),
            producto -> ResponseEntity.ok(producto)
        );
}
```

**C# (ASP.NET Core):**
```csharp
public async Task<Result<ProductoDto, AppError>> FindByIdAsync(long id)
{
    var producto = await _repository.FindByIdAsync(id);
    
    if (producto == null)
        return Result<ProductoDto, AppError>.Failure(
            AppError.NotFound("Producto no encontrado"));
        
    return Result<ProductoDto, AppError>.Success(
        _mapper.Map<ProductoDto>(producto));
}

[HttpGet("{id}")]
public async Task<IActionResult> GetById(long id)
{
    var resultado = await _service.FindByIdAsync(id);
    
    return resultado.Match(
        onSuccess: producto => Ok(producto),
        onFailure: error => error.Type switch
        {
            ErrorType.NotFound => NotFound(new { message = error.Message }),
            _ => StatusCode(500, new { message = error.Message })
        }
    );
}
```

## 🛠️ Tecnologías Utilizadas

### Core Framework
- **ASP.NET Core 8.0** - Web API framework (similar to Spring Boot)
- **Entity Framework Core 8.0** - ORM (similar to JPA/Hibernate)
- **AutoMapper 12.0** - Object mapping (similar to ModelMapper)

### Databases
- **PostgreSQL** - Relational database (Categorías, Productos, Users)
- **Npgsql** - PostgreSQL provider for EF Core
- **MongoDB 7** - NoSQL database (Pedidos)
- **MongoDB.Driver 2.22** - MongoDB client (similar to Spring Data MongoDB)

### Libraries
- **CSharpFunctionalExtensions 2.42** - Functional programming utilities
- **FluentValidation 11.3** - Validation library (similar to Hibernate Validator)
- **Swashbuckle 6.5** - OpenAPI/Swagger (similar to SpringDoc)

### Runtime Features (NEW!)
- **BCrypt.Net-Next 4.0.3** - Password hashing (similar to Spring Security BCrypt)
- **MailKit 4.2.0** - SMTP email client (similar to JavaMailSender)
- **Microsoft.AspNetCore.Authentication.JwtBearer 8.0.0** - JWT authentication
- **System.IdentityModel.Tokens.Jwt 8.0.0** - JWT token generation/validation
- **Microsoft.Extensions.Caching.StackExchangeRedis 8.0.0** - Redis distributed cache
- **StackExchange.Redis 2.7.0** - Redis client
- **GraphQL 7.8.0** - GraphQL server implementation
- **GraphiQL 2.0.0** - GraphQL interactive UI

### Testing
- **NUnit 3.14** - Testing framework (similar to JUnit)
- **Moq 4.20** - Mocking library (similar to Mockito)
- **FluentAssertions 6.12** - Fluent assertions
- **Testcontainers.NET 3.6** - Docker containers for testing

## ⚙️ Configuración

### appsettings.json

El archivo `appsettings.json` debe incluir las siguientes configuraciones:

> ⚠️ **IMPORTANTE SEGURIDAD**: Los valores mostrados son para DESARROLLO. En producción, usa variables de entorno o Azure Key Vault / AWS Secrets Manager para credenciales sensibles.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=tienda;Username=admin;Password=admin123",
    "MongoDB": "mongodb://admin:admin123@localhost:27017/tienda?authSource=admin",
    "Redis": "localhost:6379"
  },
  "Jwt": {
    "Key": "TiendaApi-JWT-Signing-Key-Must-Be-At-Least-32-Characters-Long-For-Security",
    "Issuer": "TiendaApi",
    "Audience": "TiendaApi",
    "ExpireMinutes": "60"
  },
  "Smtp": {
    "Host": "smtp.example.com",
    "Port": "587",
    "Username": "noreply@tienda.com",
    "Password": "your-smtp-password",
    "FromEmail": "noreply@tienda.com",
    "FromName": "TiendaApi Notifications",
    "AdminEmail": "admin@tienda.com"
  },
  "Cache": {
    "DefaultExpirationMinutes": "5",
    "ProductoCacheTTLMinutes": "10"
  }
}
```

> 💡 **Generar JWT Key segura**: Usa `openssl rand -base64 32` o generadores de claves criptográficas.

### Variables de Entorno (Producción)

Para producción, **SIEMPRE** usa variables de entorno en lugar de valores hardcoded:

```bash
# Generar JWT key segura (ejemplo con openssl)
export Jwt__Key=$(openssl rand -base64 64)

# Otras configuraciones sensibles
export ConnectionStrings__DefaultConnection="Host=prod-db;Database=tienda;Username=app_user;Password=$(cat /run/secrets/db_password)"
export Smtp__Password=$(cat /run/secrets/smtp_password)

# O usando Azure/AWS secrets managers
export Jwt__Key="@Microsoft.KeyVault(SecretUri=https://...)"
```

## 🧪 Testing de Nuevas Características

### 1. Autenticación JWT

```bash
# Registrar usuario
curl -X POST http://localhost:5000/v1/auth/signup \
  -H "Content-Type: application/json" \
  -d '{
    "username": "testuser",
    "email": "test@example.com",
    "password": "Password123!"
  }'

# Respuesta: { "token": "eyJhbGc...", "user": {...} }

# Login
curl -X POST http://localhost:5000/v1/auth/signin \
  -H "Content-Type: application/json" \
  -d '{
    "username": "testuser",
    "password": "Password123!"
  }'

# Usar token en requests protegidos
curl -X POST http://localhost:5000/api/productos \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "nombre": "Nuevo Producto",
    "precio": 99.99,
    "stock": 10,
    "categoriaId": 1
  }'
```

### 2. WebSocket

Conectar con cliente WebSocket:

```javascript
const ws = new WebSocket('ws://localhost:5000/ws/v1/productos');

ws.onmessage = (event) => {
  const notification = JSON.parse(event.data);
  console.log('Notification:', notification);
  // { type: "CREATED", productoId: 1, productoNombre: "...", ... }
};

ws.onerror = (error) => console.error('WebSocket Error:', error);
```

O usando herramientas como [websocat](https://github.com/vi/websocat):

```bash
websocat ws://localhost:5000/ws/v1/productos
```

### 3. GraphQL

```bash
# Query productos con GraphQL
curl -X POST http://localhost:5000/graphql \
  -H "Content-Type: application/json" \
  -d '{
    "query": "{ productos { id nombre precio categoria { nombre } } }"
  }'

# O visitar GraphiQL UI en el navegador
# http://localhost:5000/graphiql
```

### 4. Redis Cache

El cache se maneja automáticamente. Para verificar:

```bash
# Conectar a Redis CLI (si tienes docker)
docker exec -it tienda-redis redis-cli

# Ver todas las keys
KEYS TiendaApi:*

# Ver valor de una key
GET TiendaApi:productos:1

# Ver TTL de una key
TTL TiendaApi:productos:all
```

### 5. Email Service

Los emails se envían automáticamente cuando se crea un producto (si SMTP está configurado).
Para testear sin servidor SMTP real, revisa los logs:

```bash
dotnet run

# Verás en los logs:
# [Information] Email queued for background processing to: admin@tienda.com
# [Warning] SMTP not configured, skipping email send (si no hay config)
```

## 📖 Recursos Adicionales

### Documentación
- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core)
- [Entity Framework Core](https://docs.microsoft.com/ef/core)
- [Railway Oriented Programming](https://fsharpforfunandprofit.com/rop/)
- [Result Pattern in C#](https://enterprisecraftsmanship.com/posts/functional-c-primitive-obsession/)

### Videos
- [Exception vs Result Pattern](https://www.youtube.com/watch?v=a1ye9eGTB98)
- [Functional Error Handling](https://www.youtube.com/watch?v=E8I19uA-wGY)

## 👨‍💻 Autor

**José Luis González Sánchez**

- Twitter: [@JoseLuisGS_](https://twitter.com/JoseLuisGS_)
- GitHub: [@joseluisgs](https://github.com/joseluisgs)
- Web: [joseluisgs.dev](https://joseluisgs.dev)

## 📄 Licencia

Este proyecto está bajo la licencia Creative Commons. Ver [LICENSE](../LICENSE) para más detalles.

## 🙏 Agradecimientos

Este proyecto está basado en los ejemplos educativos del repositorio original y adaptado específicamente para demostrar patrones modernos de manejo de errores en C#/.NET.

---


