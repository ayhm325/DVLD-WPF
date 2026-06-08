public interface ICurrentUserService
{
    int UserId { get; set; }
    string Username { get; set; }
    string FullName { get; set; }

    bool IsLoggedIn { get; }

    void Clear();
}