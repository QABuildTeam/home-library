namespace HomeLibrary.Application.Common;

/// <summary>
/// One page of a list together with paging information.
/// </summary>
/// <typeparam name="T">Item type.</typeparam>
/// <param name="Items">Items of the current page.</param>
/// <param name="TotalCount">Total number of items on all pages.</param>
/// <param name="Page">Current page number, starting from 1.</param>
/// <param name="PageSize">Maximum number of items on a page.</param>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => PageSize > 0 ? (TotalCount + PageSize - 1) / PageSize : 0;

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage => Page < TotalPages;
}
