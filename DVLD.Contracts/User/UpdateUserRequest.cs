namespace DVLD.Contracts.User;

public sealed record UpdateUserRequest(
    int PersonId,
    string UserName,
    bool IsActive);