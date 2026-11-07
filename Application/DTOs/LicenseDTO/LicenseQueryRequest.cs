using Application.Common.Pagination;

namespace Application.DTOs.LicenseDTO;

public class LicenseQueryRequest : PaginationRequest
{
    public string? SearchQuery { get; init; }
    public string? SearchBy { get; init; }
    public string? StatusFilter { get; init; }
    public bool? IsExpired { get; init; }
}
