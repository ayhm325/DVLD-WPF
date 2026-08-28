using Application.Common.Results;

namespace Application.Interfaces;

public interface ILicenseIssuanceService
{
    Task<Result<int>> IssueFirstLicenseAsync(
    int localAppId,
    string? notes);
}
