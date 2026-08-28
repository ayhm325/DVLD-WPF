using Application.Common.Results;

namespace Application.Interfaces;

public interface ILicenseReplacementService
{
    Task<Result<int>> ReplaceLicenseAsync(
    int oldLicenseId,
    string replacementReason,
    int applicationTypeId);
}
