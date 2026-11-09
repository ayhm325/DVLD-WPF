using Application.Common.Results;

namespace Application.Interfaces;

public interface ILicenseRenewalService
{
    Task<Result<int>> RenewLicenseAsync(
    int oldLicenseId,
    string? notes,
    CancellationToken cancellationToken = default);
}
