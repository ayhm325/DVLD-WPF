using Application.Common.Pagination;

namespace Application.DTOs.LocalDrivingLicenseApplicationDTO;

public class LocalDrivingLicenseApplicationQueryRequest : PaginationRequest
{
    public string? SearchQuery { get; init; }
    public string? SearchBy { get; init; }
    public string? StatusFilter { get; init; }
    public int? LicenseClassId { get; init; }
}
