namespace Application.Contracts.Common;

public enum ServiceErrorType
{
    NotFound,
    Conflict,
    Validation
}

public sealed class ServiceResult<T>
{
    public bool Success { get; private set; }
    public T? Data { get; private set; }
    public string? Error { get; private set; }
    public ServiceErrorType? ErrorType { get; private set; }

    public static ServiceResult<T> Ok(T data)
    {
        return new ServiceResult<T> { Success = true, Data = data };
    }

    public static ServiceResult<T> Fail(ServiceErrorType type, string error)
    {
        return new ServiceResult<T> { Success = false, ErrorType = type, Error = error };
    }
}
