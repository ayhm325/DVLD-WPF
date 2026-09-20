namespace Application.Common.Pagination;

public sealed class PaginationRequest
{
    private const int DefaultPageNumber = 1;
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 100;

    private int _pageNumber = DefaultPageNumber;
    private int _pageSize = DefaultPageSize;

    public int PageNumber
    {
        get => _pageNumber;
        init => _pageNumber = value < 1
            ? DefaultPageNumber
            : value;
    }

    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value switch
        {
            <= 0 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => value
        };
    }

    public int Skip =>
        (PageNumber - 1) * PageSize;
}