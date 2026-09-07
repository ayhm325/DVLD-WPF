namespace DVLD.Contracts.User;

public sealed record UserResponse(
    int UserId,
    int PersonId,
    string UserName,
    bool IsActive,
    string? PersonFullName);