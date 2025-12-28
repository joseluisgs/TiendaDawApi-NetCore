namespace TiendaApi.Common;

/// <summary>
/// Maybe (Optional) pattern for values that may not exist
/// Alternative to null references
/// 
/// Java equivalent: Optional<T>
/// Benefits: Explicit handling of absence, no null reference exceptions
/// </summary>
public class Maybe<T>
{
    private readonly T? _value;
    
    public bool HasValue { get; }
    public bool HasNoValue => !HasValue;
    
    public T Value => HasValue 
        ? _value! 
        : throw new InvalidOperationException("Maybe has no value");

    private Maybe(T? value)
    {
        _value = value;
        HasValue = value != null;
    }

    public static Maybe<T> Some(T value) => new(value);
    public static Maybe<T> None() => new(default);

    /// <summary>
    /// Creates Maybe from a nullable value
    /// Java equivalent: Optional.ofNullable()
    /// </summary>
    public static Maybe<T> From(T? value) => 
        value != null ? Some(value) : None();

    /// <summary>
    /// Maps the value if present
    /// Java equivalent: Optional.map()
    /// </summary>
    public Maybe<TResult> Map<TResult>(Func<T, TResult> mapper) =>
        HasValue ? Maybe<TResult>.Some(mapper(Value)) : Maybe<TResult>.None();

    /// <summary>
    /// Returns value or default
    /// Java equivalent: Optional.orElse()
    /// </summary>
    public T Or(T defaultValue) => HasValue ? Value : defaultValue;

    /// <summary>
    /// Returns value or gets default from function
    /// Java equivalent: Optional.orElseGet()
    /// </summary>
    public T OrElse(Func<T> defaultProvider) => HasValue ? Value : defaultProvider();

    /// <summary>
    /// Executes action if value is present
    /// Java equivalent: Optional.ifPresent()
    /// </summary>
    public void IfPresent(Action<T> action)
    {
        if (HasValue)
            action(Value);
    }

    /// <summary>
    /// Converts to Result
    /// </summary>
    public Result<T, TError> ToResult<TError>(TError error) =>
        HasValue 
            ? Result<T, TError>.Success(Value) 
            : Result<T, TError>.Failure(error);
}
