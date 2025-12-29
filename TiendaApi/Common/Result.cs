namespace TiendaApi.Common;

/// <summary>
/// Patrón Result para manejo funcional de errores (Railway Oriented Programming)
/// Alternativa a lanzar excepciones - retorna éxito/fallo de forma explícita
/// 
/// Equivalente en Java: Either<L,R> de Vavr o tipos Result
/// Beneficios: Type-safe, manejo explícito de errores, sin flujo de control oculto
/// 
/// NOTA PEDAGÓGICA:
/// Este patrón es fundamental en programación funcional. Imagina dos vías de tren:
/// - Vía del Éxito: Todo va bien, el tren avanza
/// - Vía del Fracaso: Algo salió mal, el tren se desvía
/// Una vez en la vía del fracaso, todas las operaciones subsiguientes se omiten
/// hasta que alguien "maneje" el error (con Match o similar)
/// </summary>
public class Result<TValue, TError>
{
    private readonly TValue? _value;
    private readonly TError? _error;
    
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    
    public TValue Value => IsSuccess 
        ? _value! 
        : throw new InvalidOperationException("Cannot access Value of a failed result");
    
    public TError Error => IsFailure 
        ? _error! 
        : throw new InvalidOperationException("Cannot access Error of a successful result");

    private Result(TValue value)
    {
        _value = value;
        _error = default;
        IsSuccess = true;
    }

    private Result(TError error)
    {
        _error = error;
        _value = default;
        IsSuccess = false;
    }

    public static Result<TValue, TError> Success(TValue value) => new(value);
    public static Result<TValue, TError> Failure(TError error) => new(error);

    /// <summary>
    /// Pattern matching - ejecuta la función apropiada según éxito/fallo
    /// Similar al pattern matching de Java o match expressions de Scala
    /// </summary>
    public TResult Match<TResult>(
        Func<TValue, TResult> onSuccess,
        Func<TError, TResult> onFailure) =>
        IsSuccess ? onSuccess(Value) : onFailure(Error);

    /// <summary>
    /// Transforma el valor si es éxito (patrón functor)
    /// Equivalente en Java: Optional.map() o Stream.map()
    /// 
    /// Ejemplo: Result<int, Error>.Map(x => x * 2)
    /// </summary>
    public Result<TNewValue, TError> Map<TNewValue>(Func<TValue, TNewValue> mapper) =>
        IsSuccess 
            ? Result<TNewValue, TError>.Success(mapper(Value))
            : Result<TNewValue, TError>.Failure(Error);

    /// <summary>
    /// Versión asíncrona de Map
    /// Útil cuando la transformación requiere operaciones async
    /// </summary>
    public async Task<Result<TNewValue, TError>> MapAsync<TNewValue>(Func<TValue, Task<TNewValue>> mapper) =>
        IsSuccess 
            ? Result<TNewValue, TError>.Success(await mapper(Value))
            : Result<TNewValue, TError>.Failure(Error);

    /// <summary>
    /// Encadena operaciones que retornan Results (patrón monad)
    /// Equivalente en Java: Optional.flatMap() o CompletableFuture.thenCompose()
    /// 
    /// Ejemplo (Railway):
    /// ValidarUsuario(dto)
    ///     .Bind(usuario => GuardarEnBD(usuario))
    ///     .Bind(usuario => EnviarEmail(usuario))
    /// 
    /// Si ValidarUsuario falla, GuardarEnBD y EnviarEmail NO se ejecutan
    /// </summary>
    public Result<TNewValue, TError> Bind<TNewValue>(Func<TValue, Result<TNewValue, TError>> binder) =>
        IsSuccess ? binder(Value) : Result<TNewValue, TError>.Failure(Error);

    /// <summary>
    /// Versión asíncrona de Bind
    /// Esencial para encadenar operaciones async en Railway Pattern
    /// </summary>
    public async Task<Result<TNewValue, TError>> BindAsync<TNewValue>(
        Func<TValue, Task<Result<TNewValue, TError>>> binder) =>
        IsSuccess ? await binder(Value) : Result<TNewValue, TError>.Failure(Error);

    /// <summary>
    /// Ejecuta un efecto lateral sin modificar el resultado (side effect)
    /// Útil para logging, métricas, notificaciones, etc.
    /// 
    /// Ejemplo:
    /// CrearUsuario(dto)
    ///     .Tap(usuario => _logger.LogInformation("Usuario creado: {Id}", usuario.Id))
    ///     .Tap(usuario => _metrics.IncrementCounter("usuarios.creados"))
    /// </summary>
    public Result<TValue, TError> Tap(Action<TValue> action)
    {
        if (IsSuccess)
        {
            action(Value);
        }
        return this;
    }

    /// <summary>
    /// Versión asíncrona de Tap
    /// </summary>
    public async Task<Result<TValue, TError>> TapAsync(Func<TValue, Task> action)
    {
        if (IsSuccess)
        {
            await action(Value);
        }
        return this;
    }
}

/// <summary>
/// Result type para operaciones que no retornan un valor (operaciones void)
/// </summary>
public class Result<TError>
{
    private readonly TError? _error;
    
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    
    public TError Error => IsFailure 
        ? _error! 
        : throw new InvalidOperationException("Cannot access Error of a successful result");

    private Result(bool isSuccess, TError? error = default)
    {
        IsSuccess = isSuccess;
        _error = error;
    }

    public static Result<TError> Success() => new(true);
    public static Result<TError> Failure(TError error) => new(false, error);

    public TResult Match<TResult>(
        Func<TResult> onSuccess,
        Func<TError, TResult> onFailure) =>
        IsSuccess ? onSuccess() : onFailure(Error);
}
