using Application.Common.Pagination;

namespace Application.DTOs.PersonDTO;

public class PersonQueryRequest : PaginationRequest
{
    public string? SearchQuery { get; init; }
    public string? SearchBy { get; init; }
    public string? GenderFilter { get; init; }
}
