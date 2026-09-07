namespace DVLD.Contracts.User;

public sealed record CreateUserRequest(
    int PersonId,
    string UserName,
    string Password,
    bool IsActive);