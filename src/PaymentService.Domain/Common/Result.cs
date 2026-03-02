using System.Text.Json.Serialization;

namespace PaymentService.Domain.Common;

public class Result
{
    public bool IsSuccess { get; init; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; init; }

    public Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("Success result cannot have an error");

        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("Failure result must have an error");

        IsSuccess = isSuccess;
        Error = error;
    }

    [JsonConstructor]
    public Result() { }

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);

    public static Result<T> Success<T>(T value) => new(value, true, Error.None);

    public static Result<T> Failure<T>(Error error) => new(default!, false, error);
}

public class Result<T> : Result
{
    [JsonConstructor]
    public Result(T value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        Value = value;
    }

    public Result() { }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T Value => (IsSuccess
        ? field
        : default)!;
    
    public static implicit operator Result<T>(T value) => value is not null 
        ? Success(value) 
        : Failure<T>(Error.NullValue);

    public static Result<T> ValidationFailure(Error error) => new(default!, false, error);
}
