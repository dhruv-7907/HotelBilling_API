namespace HotelBilling.Application.Common.Models;
public class ApiResponse<T>
{
    public bool   Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public T?     Data    { get; init; }
    public IEnumerable<string> Errors { get; init; } = [];

    public static ApiResponse<T> Ok(T data, string message = "Success")
        => new() { Success=true, Message=message, Data=data };

    public static ApiResponse<T> Fail(string message, IEnumerable<string>? errors = null)
        => new() { Success=false, Message=message, Errors=errors ?? [] };
}

public class PagedResult<T>
{
    public IEnumerable<T> Items  { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page       { get; init; }
    public int PageSize   { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
