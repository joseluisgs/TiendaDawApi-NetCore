namespace TiendaApi.Common;

/// <summary>
/// Result Pattern implementation for functional error handling
/// Alternative to throwing exceptions - returns success/failure explicitly
/// 
/// Java equivalent: Either<L,R> from Vavr or Result types
/// Benefits: Type-safe, explicit error handling, no hidden control flow
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
    /// Pattern matching - executes the appropriate function based on success/failure
    /// Similar to Java's pattern matching or Scala's match expressions
    /// </summary>
    public TResult Match<TResult>(
        Func<TValue, TResult> onSuccess,
        Func<TError, TResult> onFailure) =>
        IsSuccess ? onSuccess(Value) : onFailure(Error);

    /// <summary>
    /// Maps the value if successful (functor pattern)
    /// Java equivalent: Optional.map() or Stream.map()
    /// </summary>
    public Result<TNewValue, TError> Map<TNewValue>(Func<TValue, TNewValue> mapper) =>
        IsSuccess 
            ? Result<TNewValue, TError>.Success(mapper(Value))
            : Result<TNewValue, TError>.Failure(Error);

    /// <summary>
    /// Chains operations that return Results (monad pattern)
    /// Java equivalent: Optional.flatMap() or CompletableFuture.thenCompose()
    /// </summary>
    public Result<TNewValue, TError> Bind<TNewValue>(Func<TValue, Result<TNewValue, TError>> binder) =>
        IsSuccess ? binder(Value) : Result<TNewValue, TError>.Failure(Error);
}

/// <summary>
/// Result type for operations that don't return a value (void operations)
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
