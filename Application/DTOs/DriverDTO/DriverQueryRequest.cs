using Application.Common.Pagination;

namespace Application.DTOs.DriverDTO;

public class DriverQueryRequest : PaginationRequest
{
    public string? SearchQuery { get; init; }
    public string? SearchBy { get; init; }
    public string? LicenseFilter { get; init; }
}
