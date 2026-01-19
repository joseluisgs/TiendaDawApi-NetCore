# 6. Patrón Result con CSharpFunctionalExtensions

## Índice

[6. Patrón Result con CSharpFunctionalExtensions](#6-patrón-result-con-csharpextensions)
  - [6.1. Por Qué Excepciones No Son Para Errores de Negocio](#61-por-qué-excepciones-no-son-para-errores-de-negocio)
  - [6.2. CSharpFunctionalExtensions: Result<T, Error>](#62-csharpextensions-resultt-error)
  - [6.3. DomainError y ErrorType Enum](#63-domainerror-y-errortype-enum)
  - [6.4. Result.Match() en Servicios](#64-resultmatch-en-servicios)
  - [6.5. UnitResult para Operaciones Sin Retorno](#65-unitresult-para-operaciones-sin-retorno)
  - [6.6. Integración Result + Controladores](#66-integración-result--controladores)
  - [6.7. Ventajas del Patrón Result](#67-ventajas-del-patrón-result)
  - [6.8. Resumen y Buenas Prácticas](#68-resumen-y-buenas-prácticas)

---

## 6.1. Por Qué Excepciones No Son Para Errores de Negocio

Las excepciones están diseñadas para situaciones excepcionales e inesperadas: un archivo que no existe, una conexión a base de datos que falla, un error de programación. Sin embargo, en una API de negocio, muchas situaciones que los clientes consideran "normales" requieren devolver un error: credenciales inválidas, recurso no encontrado, datos duplicados. Usar excepciones para estos casos hace el código más lento, más difícil de seguir, y oculta el flujo de control.

### El problema con las excepciones para errores de negocio

Imagina un método que valida el login de un usuario. Hay múltiples formas en que puede fallar: email vacío, email no válido, contraseña incorrecta, cuenta bloqueada. Si usas excepciones para cada caso, terminas con un try-catch gigante que no dice nada sobre los posibles caminos del código, el rendimiento se ve afectado porque las excepciones tienen overhead, y es fácil olvidar capturar una excepción y que el error llegue al cliente en un formato inesperado.

```csharp
// ❌ INCORRECTO: Excepciones para errores de negocio
public class AuthService
{
    public User Login(string email, string password)
    {
        if (string.IsNullOrEmpty(email))
            throw new ValidationException("Email es obligatorio");
        
        if (!IsValidEmail(email))
            throw new ValidationException("Email inválido");
        
        var user = _repository.FindByEmail(email);
        if (user == null)
            throw new NotFoundException("Usuario no encontrado");
        
        if (!VerifyPassword(password, user.PasswordHash))
            throw new UnauthorizedException("Contraseña incorrecta");
        
        if (user.IsLocked)
            throw new ForbiddenException("Cuenta bloqueada");
        
        return user;
    }
}

// El cliente tiene que capturar múltiples excepciones
try
{
    var user = authService.Login(email, password);
}
catch (ValidationException ex) { /* mostrar error de validación */ }
catch (NotFoundException ex) { /* mostrar usuario no encontrado */ }
catch (UnauthorizedException ex) { /* mostrar contraseña incorrecta */ }
catch (ForbiddenException ex) { /* mostrar cuenta bloqueada */ }
```

### La solución con el patrón Result

Con el patrón Result, cada método devuelve explícitamente si tuvo éxito o falló, junto con el valor o el error. Esto hace el código auto-documentado: puedes ver todos los posibles resultados leyendo la firma del método. No hay excepciones ocultas, el flujo de control es explícito, y el rendimiento es óptimo porque no hay overhead de stack trace.

```csharp
// ✅ CORRECTO: Result Pattern
public class AuthService
{
    public Result<User, DomainError> Login(string email, string password)
    {
        if (string.IsNullOrEmpty(email))
            return Result.Failure<User, DomainError>(
                DomainError.Validation("Email es obligatorio"));
        
        if (!IsValidEmail(email))
            return Result.Failure<User, DomainError>(
                DomainError.Validation("Email inválido"));
        
        var user = _repository.FindByEmail(email);
        if (user == null)
            return Result.Failure<User, DomainError>(
                DomainError.NotFound("Usuario no encontrado"));
        
        if (!VerifyPassword(password, user.PasswordHash))
            return Result.Failure<User, DomainError>(
                DomainError.Unauthorized("Contraseña incorrecta"));
        
        if (user.IsLocked)
            return Result.Failure<User, DomainError>(
                DomainError.Forbidden("Cuenta bloqueada"));
        
        return Result.Success<User, DomainError>(user);
    }
}

// El cliente usa Match para manejar ambos casos
var resultado = authService.Login(email, password);
resultado.Match(
    onSuccess: user => { /* usar el usuario */ },
    onFailure: error => { /* mostrar error */ });
```

### Comparación de rendimiento

Las excepciones son aproximadamente 100 veces más lentas que devolver un Result porque necesitan crear un stack trace, buscar catch blocks, y pueden causar presión en el recolector de basura. Para errores de negocio que son frecuentes y esperados, el overhead de excepciones es inaceptable.

### Flujo completo de una operación con Result Pattern

Este diagrama muestra el flujo completo de una operación de creación de producto, pasando por validaciones, verificación de base de datos, y finalmente devolviendo el resultado al controlador.

```mermaid
sequenceDiagram
    participant Ctrl as Controller
    participant Svc as ProductoService
    participant Repo as IProductoRepository
    participant Cache as ICacheService
    participant DB as PostgreSQL

    Note over Ctrl: POST /api/productos
    Note over Ctrl: {nombre: "", precio: -5}

    Ctrl->>Svc: CreateAsync(dto)
    
    rect rgb(255, 200, 200)
    Note over Svc: Validacion 1: nombre vacio
    Svc-->>Ctrl: Result.Failure(DomainError.Validation)
    Ctrl-->>Client: 400 Bad Request
    end

    Note over Ctrl: POST /api/productos
    Note over Ctrl: {nombre: "Laptop", precio: -5}

    Ctrl->>Svc: CreateAsync(dto)
    
    rect rgb(255, 200, 200)
    Note over Svc: Validacion 2: precio negativo
    Svc-->>Ctrl: Result.Failure(DomainError.Validation)
    Ctrl-->>Client: 400 Bad Request
    end

    Note over Ctrl: POST /api/productos
    Note over Ctrl: {nombre: "Laptop", precio: 999}

    Ctrl->>Svc: CreateAsync(dto)
    
    rect rgb(255, 255, 200)
    Note over Svc: Validacion: OK
    Note over Svc: Verificar categoria existe
    Svc->>Repo: FindByIdAsync(categoriaId)
    Repo->>DB: SELECT * FROM categorias WHERE id = ?
    DB-->>Repo: Categoria encontrada
    Repo-->>Svc: Result.Success(categoria)
    end

    rect rgb(255, 200, 200)
    Note over Svc: Verificar: nombre unico
    Svc->>Repo: ExistsByNombreAsync("Laptop")
    Repo->>DB: SELECT EXISTS(SELECT * FROM ...)
    DB-->>Repo: true (ya existe)
    Repo-->>Svc: true
    Svc-->>Ctrl: Result.Failure(DomainError.Conflict)
    Ctrl-->>Client: 409 Conflict
    end

    Note over Ctrl: POST /api/productos
    Note over Ctrl: {nombre: "Mouse Nuevo", precio: 29.99}

    Ctrl->>Svc: CreateAsync(dto)
    
    rect rgb(200, 255, 200)
    Note over Svc: Validaciones: OK
    Note over Svc: Categoria: OK
    Note over Svc: Nombre unico: OK
    Svc->>Repo: SaveAsync(producto)
    Repo->>DB: INSERT INTO productos ...
    DB-->>Repo: producto con ID
    Repo-->>Svc: Result.Success(producto)
    
    Note over Svc: Invalidar cache
    Svc->>Cache: RemoveAsync("productos:all")
    Cache-->>Svc: OK
    
    Svc-->>Ctrl: Result.Success(productoDto)
    Ctrl-->>Client: 201 Created {productoDto}
    end
```

### Comparación visual

```mermaid
flowchart TB
    subgraph "Excepciones (para errores excepcionales)"
        A1["throw new Exception()"]
        A2["Stack trace completo"]
        A3["Búsqueda de catch blocks"]
        A4["~100x más lento"]
        A5["Para: Bugs, fallos de infraestructura"]
    end
    
    subgraph "Result Pattern (para errores de negocio)"
        B1["return Result.Failure()"]
        B2["Solo el mensaje de error"]
        B3["Match directo"]
        B4["Rendimiento óptimo"]
        B5["Para: Validación, no encontrado, conflictos"]
    end
    
    A1 --> A2 --> A3 --> A4 --> A5
    B1 --> B2 --> B3 --> B4 --> B5
```

---

## 6.2. CSharpFunctionalExtensions: Result<T, Error>

CSharpFunctionalExtensions es una librería que proporciona tipos funcionales como `Result`, `Maybe`, y `Either`. La librería maneja la complejidad de implementar el patrón Result manualmente y proporciona métodos útiles como `Map`, `Bind`, y `Match` para encadenar operaciones de forma funcional.

### Flujo del Patrón Result

El Result puede estar en uno de dos estados: **Success** (la operación fue exitosa y contiene un valor) o **Failure** (la operación falló y contiene un error). El método `Match` permite ejecutar código diferente según el estado, forzando al desarrollador a manejar ambos casos explícitamente.

```mermaid
flowchart TD
    subgraph "Llamada al metodo"
        START("Metodo que devuelve<br/>Result&lt;Usuario, DomainError&gt;")
    end
    
    subgraph "Resultado posible"
        START --> RESULT{"Es Success?"}
    end
    
    subgraph "CASO DE EXITO"
        RESULT -->|IsSuccess| SUCCESS["Result.Success(usuario)"]
        SUCCESS --> MAP["Map(usuario =&gt; usuarioDto)"]
        MAP --> TAP["Tap(loggear)"]
        TAP --> MATCH["Match()"]
        MATCH --> VALOR["Value: UsuarioDto"]
    end
    
    subgraph "CASO DE FALLO"
        RESULT -->|IsFailure| FAILURE["Result.Failure(error)"]
        FAILURE --> MAP_ERR["MapError(error)"]
        MAP_ERR --> MATCH_ERR["Match()"]
        MATCH_ERR --> ERROR["Error: DomainError"]
    end
    
    VALOR -.-> CONTINUE["Continuar con el flujo"]
    ERROR -.-> HANDLER["Manejar error"]
    
    style START fill:#2E7D32,stroke:#1B5E20,color:#ffffff
    style SUCCESS fill:#4CAF50,stroke:#388E3C,color:#ffffff
    style FAILURE fill:#E57373,stroke:#D32F2F,color:#ffffff
    style VALOR fill:#388E3C,stroke:#2E7D32,color:#ffffff
    style ERROR fill:#D32F2F,stroke:#B71C1C,color:#ffffff
    style RESULT fill:#F57C00,color:#ffffff
    style MAP fill:#388E3C,color:#ffffff
    style TAP fill:#388E3C,color:#ffffff
    style MATCH fill:#388E3C,color:#ffffff
    style MAP_ERR fill:#C62828,color:#ffffff
    style MATCH_ERR fill:#C62828,color:#ffffff
    style CONTINUE fill:#1976D2,color:#ffffff
    style HANDLER fill:#1976D2,color:#ffffff
```

### Instalación

```bash
dotnet add TiendaApi.Core package CSharpFunctionalExtensions
```

### Tipos básicos de Result

La librería proporciona varios tipos de Result dependiendo de lo que necesites. `Result<T, TError>` es para operaciones que devuelven un valor o un error. `UnitResult<TError>` es para operaciones que no devuelven valor pero pueden fallar. `Result<T>` es un atajo cuando solo te importa si tuvo éxito y el valor.

```csharp
using CSharpFunctionalExtensions;

// Result<T, TError> - Para operaciones que devuelven un valor
Result<User, string> loginResult = Result.Success<User, string>(user);
Result<User, string> errorResult = Result.Failure<User, string>("Email inválido");

// UnitResult<TError> - Para operaciones sin valor de retorno
UnitResult<string> deleteResult = UnitResult.Success<string>();
UnitResult<string> deleteError = UnitResult.Failure<string>("Usuario no encontrado");

// Result<T> - Simplified cuando solo te importa éxito/fracaso
Result<User> createResult = Result.Success(user);
Result<User> failureResult = Result.Failure("Error al crear");
```

### Métodos comunes de Result

El método `IsSuccess` y `IsFailure` permiten verificar el estado del Result. Las propiedades `Value` y `Error` acceden al valor o error. Los métodos `Map`, `Bind`, y `Tap` permiten transformar y encadenar Results.

```csharp
// Verificar estado
if (resultado.IsSuccess)
{
    var usuario = resultado.Value;
    // usar usuario
}
else
{
    var error = resultado.Error;
    // manejar error
}

// Map: transformar el valor si es éxito
Result<string, string> nombreResult = resultado
    .Map(user => user.Nombre);

// Bind: encadenar operaciones que pueden fallar
Result<User, string> validarResult = resultado
    .Bind(user => ValidarUsuario(user))
    .Bind(user => VerificarSuscripcion(user));

// Tap: ejecutar acción sin transformar
resultado
    .Tap(user => Log.Info($"Login exitoso: {user.Email}"))
    .TapError(error => Log.Warn($"Login fallido: {error}"));
```

### Combinar múltiples Results

A veces necesitas combinar varios Results, por ejemplo cuando múltiples validaciones deben pasar:

```csharp
// Combine: múltiples operations deben ser éxito
Result<(User user, Producto producto), string> combined = 
    Result.Combine(
        userResult,  // Result<User, string>
        productoResult,  // Result<Producto, string>
        (user, producto) => (user, producto)
    );

// Sequence: convertir List<Result<T>> en Result<List<T>>
var results = new List<Result<Producto, string>>
{
    Result.Success(producto1),
    Result.Success(producto2),
    Result.Success(producto3)
};

Result<List<Producto>, string> allProducts = results.Sequence();

// FirstFailureOrSuccess: obtener el primer error o el último éxito
var finalResult = Result.FirstFailureOrSuccess(result1, result2, result3);
```

---

## 6.3. DomainError y ErrorType Enum

En el proyecto TiendaApi, definimos un tipo `DomainError` que encapsula toda la información sobre un error de negocio. Esto incluye un mensaje legible, un tipo de error para clasificarlo, y opcionalmente una lista de errores de validación. Este enfoque permite que los controladores traduzcan fácilmente errores de dominio a códigos HTTP.

### Definición de ErrorType

El enum `ErrorType` clasifica los errores en categorías estándar de HTTP, lo que facilita la traducción a códigos de estado:

```csharp
namespace TiendaApi.Core.Errors;

public enum ErrorType
{
    Validation,      // 400 Bad Request - Datos inválidos
    NotFound,        // 404 Not Found - Recurso no existe
    Unauthorized,    // 401 Unauthorized - No autenticado
    Forbidden,       // 403 Forbidden - Autenticado pero sin permisos
    Conflict,        // 409 Conflict - Conflicto con estado actual
    BusinessRule,    // 422 Unprocessable Entity - Regla de negocio violada
    Internal         // 500 Internal Server Error - Error inesperado
}
```

### Definición de DomainError

La clase `DomainError` encapsula toda la información del error y proporciona factory methods estáticos para crear errores comunes:

```csharp
namespace TiendaApi.Core.Errors;

public class DomainError
{
    public string Message { get; }
    public ErrorType Type { get; }
    public List<string>? ValidationErrors { get; }

    private DomainError(string message, ErrorType type, List<string>? validationErrors = null)
    {
        Message = message;
        Type = type;
        ValidationErrors = validationErrors;
    }

    // Factory methods para errores comunes
    public static DomainError Validation(string message, List<string>? errors = null) =>
        new(message, ErrorType.Validation, errors);

    public static DomainError NotFound(string message) =>
        new(message, ErrorType.NotFound);

    public static DomainError Unauthorized(string message) =>
        new(message, ErrorType.Unauthorized);

    public static DomainError Forbidden(string message) =>
        new(message, ErrorType.Forbidden);

    public static DomainError Conflict(string message) =>
        new(message, ErrorType.Conflict);

    public static DomainError BusinessRule(string message) =>
        new(message, ErrorType.BusinessRule);

    public static DomainError Internal(string message) =>
        new(message, ErrorType.Internal);

    // Método de conveniencia para crear error de validación con lista
    public static DomainError Validation(string message, params string[] errors) =>
        new(message, ErrorType.Validation, errors.ToList());
}
```

### Uso de DomainError en servicios

```csharp
public class ProductoService
{
    public Result<ProductoDto, DomainError> GetById(long id)
    {
        // Error de no encontrado
        if (id <= 0)
            return Result.Failure<ProductoDto, DomainError>(
                DomainError.NotFound($"Producto {id} no encontrado"));
        
        var producto = _repository.FindById(id);
        if (producto == null)
            return Result.Failure<ProductoDto, DomainError>(
                DomainError.NotFound($"Producto {id} no encontrado"));
        
        return Result.Success<ProductoDto, DomainError>(producto.ToDto());
    }

    public Result<ProductoDto, DomainError> Create(ProductoCreateDto dto)
    {
        // Error de validación
        if (string.IsNullOrEmpty(dto.Nombre))
            return Result.Failure<ProductoDto, DomainError>(
                DomainError.Validation("El nombre es obligatorio"));
        
        if (dto.Precio <= 0)
            return Result.Failure<ProductoDto, DomainError>(
                DomainError.Validation("El precio debe ser mayor a 0"));
        
        // Error de conflicto
        if (_repository.ExistsByNombre(dto.Nombre))
            return Result.Failure<ProductoDto, DomainError>(
                DomainError.Conflict($"Ya existe un producto con el nombre '{dto.Nombre}'"));
        
        // Error de regla de negocio
        if (dto.Stock < 0)
            return Result.Failure<ProductoDto, DomainError>(
                DomainError.BusinessRule("El stock no puede ser negativo"));
        
        var producto = new Producto(dto);
        var guardado = _repository.Save(producto);
        
        return Result.Success<ProductoDto, DomainError>(guardado.ToDto());
    }
}
```

---

## 6.4. Result.Match() en Servicios

El método `Match` es la forma principal de trabajar con Results. Permite ejecutar código diferente dependiendo de si el Result fue éxito o fracaso, de forma similar a cómo funcionan las expresiones switch pero para Results. Match fuerza al desarrollador a manejar ambos casos, haciendo el código más seguro.

### Sintaxis básica de Match

El método `Match` toma dos funciones: una para el caso de éxito y otra para el caso de fracaso. Ambas funciones deben devolver el mismo tipo, que es el tipo de retorno del Match.

```csharp
public class ProductoService
{
    public Result<ProductoDto, DomainError> GetById(long id)
    {
        var producto = _repository.FindById(id);
        
        // Usar Match para devolver el resultado
        return producto
            .Map(p => p.ToDto())
            .MapError(error => DomainError.NotFound($"Producto {id} no encontrado"));
    }

    public async Task<IActionResult> CreateAsync(ProductoCreateDto dto)
    {
        var resultado = await _service.CreateAsync(dto);
        
        // Match en el controlador
        return resultado.Match(
            onSuccess: producto => CreatedAtAction(
                nameof(GetById),
                new { id = producto.Id },
                producto),
            onFailure: error => error.Type switch
            {
                ErrorType.Validation => BadRequest(new { error.Message }),
                ErrorType.NotFound => NotFound(new { error.Message }),
                ErrorType.Conflict => Conflict(new { error.Message }),
                ErrorType.BusinessRule => UnprocessableEntity(new { error.Message }),
                _ => StatusCode(500, new { error.Message })
            });
    }
}
```

### Match con encadenamiento

Puedes encadenar operaciones con Map y Bind antes de hacer el Match final:

```csharp
public Result<OrderConfirmationDto, DomainError> ProcessOrder(OrderDto order)
{
    // Encadenar validaciones y transformaciones
    return ValidarOrden(order)
        .Bind(ord => CalcularTotal(ord))
        .Bind(ord => AplicarDescuentos(ord))
        .Bind(ord => ReservarInventario(ord))
        .Map(ord => ord.ToConfirmationDto())
        .Match(
            confirmation => Result.Success<OrderConfirmationDto, DomainError>(confirmation),
            error => Result.Failure<OrderConfirmationDto, DomainError>(error));
}
```

### Match con resultado diferente

A veces quieres que el Match devuelva un tipo diferente al del Result, como un IActionResult en un controlador:

```csharp
[HttpGet("{id:long}")]
public IActionResult GetById(long id)
{
    var resultado = _service.GetById(id);
    
    // Convertir Result a IActionResult
    return resultado.IsSuccess
        ? Ok(resultado.Value)
        : resultado.Error.Type switch
        {
            ErrorType.NotFound => NotFound(new { resultado.Error.Message }),
            ErrorType.Unauthorized => Unauthorized(new { resultado.Error.Message }),
            _ => BadRequest(new { resultado.Error.Message })
        };
}
```

---

## 6.5. UnitResult para Operaciones Sin Retorno

UnitResult es la versión de Result para operaciones que no devuelven un valor significativo, como operaciones de delete o update donde solo te importa si tuvieron éxito. Es análogo a usar `void` pero con soporte para errores.

### Cuándo usar UnitResult

Usa UnitResult cuando el método no necesita devolver ningún dato en caso de éxito, solo confirmar que la operación se completó. Ejemplos típicos son eliminar un recurso, actualizar un recurso (donde la respuesta es 204 No Content), y ejecutar una acción que no tiene valor de retorno.

```csharp
public interface IProductoService
{
    // Para operaciones con valor de retorno
    Result<ProductoDto, DomainError> GetById(long id);
    Result<List<ProductoDto>, DomainError> GetAll();
    Result<ProductoDto, DomainError> Create(ProductoCreateDto dto);
    
    // Para operaciones sin valor de retorno
    UnitResult<DomainError> Delete(long id);
    UnitResult<DomainError> UpdateStock(long id, int cantidad);
}
```

### Implementación con UnitResult

```csharp
public class ProductoService
{
    public UnitResult<DomainError> Delete(long id)
    {
        var producto = _repository.FindById(id);
        
        if (producto == null)
            return UnitResult.Failure<DomainError>(
                DomainError.NotFound($"Producto {id} no encontrado"));
        
        if (producto.TienePedidosPendientes)
            return UnitResult.Failure<DomainError>(
                DomainError.BusinessRule("No se puede eliminar un producto con pedidos pendientes"));
        
        _repository.Delete(producto);
        
        return UnitResult.Success<DomainError>();
    }

    public async Task<IActionResult> DeleteAsync(long id)
    {
        var resultado = await _service.DeleteAsync(id);
        
        if (resultado.IsSuccess)
            return NoContent();
        
        return resultado.Error.Type switch
        {
            ErrorType.NotFound => NotFound(new { resultado.Error.Message }),
            ErrorType.BusinessRule => UnprocessableEntity(new { resultado.Error.Message }),
            _ => StatusCode(500, new { resultado.Error.Message })
        };
    }
}
```

---

## 6.6. Integración Result + Controladores

La integración del patrón Result con controladores es natural una vez que entiendes cómo usar Match. El patrón típico es que los servicios devuelven Result, y los controladores usan Match para convertir esos Results en respuestas HTTP apropiadas.

### Controlador con Result Pattern

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProductosController : ControllerBase
{
    private readonly IProductoService _service;

    public ProductosController(IProductoService service)
    {
        _service = service;
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var resultado = await _service.GetByIdAsync(id);
        
        return resultado.Match(
            producto => Ok(producto),
            error => GetHttpResult(error));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var resultado = await _service.GetPagedAsync(page, pageSize);
        
        return resultado.Match(
            paged => Ok(paged),
            error => GetHttpResult(error));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProductoCreateDto dto)
    {
        var resultado = await _service.CreateAsync(dto);
        
        return resultado.Match(
            producto => CreatedAtAction(
                nameof(GetById),
                new { id = producto.Id },
                producto),
            error => GetHttpResult(error));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var resultado = await _service.DeleteAsync(id);
        
        return resultado.IsSuccess
            ? NoContent()
            : GetHttpResult(resultado.Error);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] ProductoUpdateDto dto)
    {
        var resultado = await _service.UpdateAsync(id, dto);
        
        return resultado.Match(
            producto => Ok(producto),
            error => GetHttpResult(error));
    }

    private IActionResult GetHttpResult(DomainError error)
    {
        return error.Type switch
        {
            ErrorType.Validation => BadRequest(new
            {
                message = error.Message,
                errors = error.ValidationErrors
            }),
            ErrorType.NotFound => NotFound(new { message = error.Message }),
            ErrorType.Unauthorized => Unauthorized(new { message = error.Message }),
            ErrorType.Forbidden => StatusCode(403, new { message = error.Message }),
            ErrorType.Conflict => Conflict(new { message = error.Message }),
            ErrorType.BusinessRule => UnprocessableEntity(new { message = error.Message }),
            _ => StatusCode(500, new { message = "Ha ocurrido un error interno" })
        };
    }
}
```

### Ayudante para errores de validación

Cuando hay múltiples errores de validación, es útil devolverlos en un formato estructurado:

```csharp
private IActionResult GetHttpResult(DomainError error)
{
    var response = new
    {
        message = error.Message,
        code = error.Type.ToString(),
        traceId = HttpContext.TraceIdentifier
    };

    return error.Type switch
    {
        ErrorType.Validation => BadRequest(new
        {
            response.message,
            response.code,
            response.traceId,
            errors = error.ValidationErrors != null
                ? error.ValidationErrors.Select((msg, i) => new { id = i, message = msg })
                : null
        }),
        ErrorType.NotFound => NotFound(response),
        ErrorType.Conflict => Conflict(response),
        ErrorType.BusinessRule => UnprocessableEntity(response),
        ErrorType.Unauthorized => Unauthorized(response),
        ErrorType.Forbidden => StatusCode(403, response),
        _ => StatusCode(500, new
        {
            message = "Ha ocurrido un error interno",
            code = "INTERNAL_ERROR",
            traceId = HttpContext.TraceIdentifier
        })
    };
}
```

---

## 6.7. Ventajas del Patrón Result

El patrón Result proporciona múltiples ventajas sobre el uso de excepciones para errores de negocio, desde rendimiento hasta mantenibilidad del código.

### Legibilidad y explicitud

El código con Result es más fácil de leer porque todos los posibles caminos están explícitos. No hay try-catch ocultos, no hay excepciones que "pueden" saltar. La firma del método dice exactamente qué puede salir mal, y el Match fuerza a manejar todos los casos.

```mermaid
flowchart TB
    subgraph "Con Excepciones"
        A1["Método con throw"]
        A2["¿Dónde está el try-catch?"]
        A3["¿Qué excepciones pueden saltar?"]
        A4["Flow decontrol oculto"]
    end
    
    subgraph "Con Result"
        B1["Método devuelve Result"]
        B2["Match explícito"]
        B3["Todos los casos visibles"]
        B4["Flow de control claro"]
    end
    
    A1 --> A2 --> A3 --> A4
    B1 --> B2 --> B3 --> B4
```

### Testabilidad

Los tests con Result son más directos: solo necesitas verificar IsSuccess/IsFailure y los valores/error correspondientes. No try-catch en tests, no fear de perder excepciones.

```csharp
[Test]
public void GetById_ProductoNoExistente_ReturnsNotFound()
{
    // Arrange
    var service = new ProductoService(_repositoryMock.Object);
    _repositoryMock.Setup(r => r.FindById(999))
        .Returns((Producto)null!);
    
    // Act
    var resultado = service.GetById(999);
    
    // Assert
    resultado.IsSuccess.Should().BeFalse();
    resultado.Error.Type.Should().Be(ErrorType.NotFound);
    resultado.Error.Message.Should().Contain("999");
}

[Test]
public void Create_ProductoValido_ReturnsSuccess()
{
    // Arrange
    var dto = new ProductoCreateDto { Nombre = "Laptop", Precio = 999 };
    _repositoryMock.Setup(r => r.ExistsByNombre("Laptop"))
        .Returns(false);
    _repositoryMock.Setup(r => r.Save(It.IsAny<Producto>()))
        .Returns((Producto p) => p);
    
    // Act
    var resultado = _service.Create(dto);
    
    // Assert
    resultado.IsSuccess.Should().BeTrue();
    resultado.Value.Nombre.Should().Be("Laptop");
}
```

### Rendimiento

El Result no tiene el overhead de las excepciones: no stack trace, no búsqueda de catch blocks, no presión en el recolector de basura. Para errores frecuentes como validación de input, la diferencia de rendimiento es significativa.

### Tabla comparativa

| Aspecto | Excepciones | Result Pattern |
|---------|-------------|----------------|
| Legibilidad | Media (try-catch oculto) | Alta (explícito) |
| Rendimiento | Bajo (overhead ~100x) | Alto (sin overhead) |
| Completitud | Easy olvidar capturar | Match fuerza manejar todos |
| Testabilidad | Requiere Assert.Throws | Tests directos |
| Stack trace | Siempre presente | Opcional |

---

## 6.8. Resumen y Buenas Prácticas

A lo largo de este documento hemos explorado el patrón Result como alternativa a las excepciones para errores de negocio.

### Puntos clave del módulo

Las excepciones son para errores excepcionales e inesperados. Los errores de negocio son frecuentes y esperados, deben usar Result. CSharpFunctionalExtensions proporciona Result<T, Error> y UnitResult<TError>. DomainError encapsula mensaje, tipo y errores de validación. Match permite manejar ambos casos de forma explícita.

### Buenas prácticas

```mermaid
flowchart TB
    subgraph "Cuándo usar Result"
        A1["Errores de validación"]
        A2["Recursos no encontrados"]
        A3["Conflictos de negocio"]
        A4["Reglas de negocio violadas"]
    end
    
    subgraph "Cuándo usar Excepciones"
        B1["Errores de infraestructura"]
        B2["Bugs en el código"]
        B3["Fallos de conexión"]
        B4["Errores inesperados"]
    end
    
    subgraph "Implementación"
        C1["DomainError con ErrorType"]
        C2["Match siempre"]
        C3["Traducir a HTTP en Controller"]
        C4["UnitResult para operaciones void"]
    end
    
    A1 --> A2 --> A3 --> A4
    B1 --> B2 --> B3 --> B4
    C1 --> C2 --> C3 --> C4
```

### Siguientes pasos

Con el patrón Result dominado, el siguiente paso es aprender sobre el Repository Pattern, que abstrae el acceso a datos y trabaja naturalmente con Result.

### Recursos adicionales

- CSharpFunctionalExtensions: https://github.com/vkhorikov/CSharpFunctionalExtensions
- Error handling guidelines: https://docs.microsoft.com/azure/architecture/best-practices/api-design#error-response-problems
