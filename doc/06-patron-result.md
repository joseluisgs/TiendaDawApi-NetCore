# 06. Patron Result con CSharpFunctionalExtensions

El Patron Result reemplaza excepciones para errores de negocio. Facilita el control de flujo y hace el codigo mas legible.

---

## 1. Por Que Result en Lugar de Excepciones

Las excepciones son para errores excepcionales. Los errores de negocio son esperados y deben manejarse explicitamente.

```mermaid
flowchart TB
    subgraph "Excepciones"
        E1["Lanzar throw"]
        E2["Capturar en catch"]
        E3["Costoso en rendimiento"]
    end
    
    subgraph "Result"
        R1["Return Result"]
        R2["Match on success/failure"]
        R3["Ligero y tipado"]
    end
```

---

## 2. Instalacion

```bash
dotnet add package CSharpFunctionalExtensions
```

---

## 3. DomainError

```csharp
namespace TiendaApi.Apis.Errors;

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden,
    BusinessRule,
    Internal
}

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

    public static DomainError Validation(string message, List<string>? errors = null) =>
        new(message, ErrorType.Validation, errors);

    public static DomainError NotFound(string message) =>
        new(message, ErrorType.NotFound);

    public static DomainError Conflict(string message) =>
        new(message, ErrorType.Conflict);

    public static DomainError Unauthorized(string message) =>
        new(message, ErrorType.Unauthorized);

    public static DomainError BusinessRule(string message) =>
        new(message, ErrorType.BusinessRule);
}
```

---

## 4. Uso en Servicios

```csharp
public class CategoriaService(
    ICategoriaRepository repository,
    ILogger<CategoriaService> logger
) : ICategoriaService {

    public async Task<Result<CategoriaDto, DomainError>> FindByIdAsync(long id)
    {
        logger.LogInformation("Buscando categoria {Id}", id);
        
        var categoria = await repository.FindByIdAsync(id);
        if (categoria == null)
            return Result.Failure<CategoriaDto, DomainError>(
                DomainError.NotFound($"Categoria {id} no encontrada"));
        
        return Result.Success<CategoriaDto, DomainError>(categoria.ToDto());
    }

    public async Task<Result<CategoriaDto, DomainError>> CreateAsync(CategoriaRequestDto dto)
    {
        // Validacion
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return Result.Failure<CategoriaDto, DomainError>(
                DomainError.Validation("Nombre requerido"));

        if (dto.Nombre.Length < 3)
            return Result.Failure<CategoriaDto, DomainError>(
                DomainError.Validation("Nombre debe tener al menos 3 caracteres"));

        // Verificar duplicados
        if (await repository.ExistsByNombreAsync(dto.Nombre))
            return Result.Failure<CategoriaDto, DomainError>(
                DomainError.Conflict($"Ya existe categoria con nombre '{dto.Nombre}'"));

        // Crear
        var categoria = dto.ToEntity();
        var saved = await repository.SaveAsync(categoria);

        return Result.Success<CategoriaDto, DomainError>(saved.ToDto());
    }

    public async Task<UnitResult<DomainError>> DeleteAsync(long id)
    {
        var categoria = await repository.FindByIdAsync(id);
        if (categoria == null)
            return UnitResult.Failure<DomainError>(
                DomainError.NotFound($"Categoria {id} no encontrada"));

        await repository.DeleteAsync(id);
        return UnitResult.Success<DomainError>();
    }
}
```

---

## 5. Uso en Controladores

```csharp
public class CategoriasController(
    ICategoriaService service,
    ILogger<CategoriasController> logger
) : ControllerBase {

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id)
    {
        var resultado = await service.FindByIdAsync(id);
        
        return resultado.Match(
            onSuccess: categoria => Ok(categoria),
            onFailure: error => error.Type switch {
                ErrorType.NotFound => NotFound(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CategoriaRequestDto dto)
    {
        var resultado = await service.CreateAsync(dto);
        
        return resultado.Match(
            onSuccess: categoria => CreatedAtAction(nameof(GetById), new { id = categoria.Id }, categoria),
            onFailure: error => error.Type switch {
                ErrorType.Validation => BadRequest(new { message = error.Message }),
                ErrorType.Conflict => Conflict(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id)
    {
        var resultado = await service.DeleteAsync(id);
        
        if (resultado.IsSuccess)
            return NoContent();
        
        return resultado.Error.Type switch {
            ErrorType.NotFound => NotFound(new { message = resultado.Error.Message }),
            _ => StatusCode(500, new { message = resultado.Error.Message })
        };
    }
}
```

---

## 6. Beneficios

| Aspecto | Excepciones | Result |
|---------|-------------|--------|
| Legibilidad | Baja | Alta |
| Rendimiento | Costoso | Ligero |
| Tipado | Generico | Explicito |
| Control | optional | Obligatorio |
