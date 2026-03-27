namespace Application.Contracts.Common;

public sealed class PagedData<T>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public T[] Items { get; set; } = Array.Empty<T>();
}
