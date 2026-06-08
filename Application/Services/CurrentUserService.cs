





public class CurrentUserService : ICurrentUserService
{
    public int UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public bool IsLoggedIn => UserId > 0;

    public void Clear()
    {
        UserId = 0;
        Username = string.Empty;
        FullName = string.Empty;
    }
}