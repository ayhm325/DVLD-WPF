
namespace Domain.Entities;

public class User
{
    public int UserId { get; set; }

    public int PersonId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    // =========================================================
    // NAVIGATION
    // =========================================================

    public virtual Person Person { get; set; } = null!;
}
