using Domain.Enums;

namespace Domain.Entities;

public class User
{
    public int UserId { get; set; }

    public int PersonId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public UserRole Role { get; set; }

    /// <summary>
    /// Rotated whenever a security-sensitive property (password, role, activation)
    /// changes. Access tokens carry the value they were issued with; a mismatch
    /// invalidates the token immediately.
    /// </summary>
    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");

    public virtual Person Person { get; set; } = null!;

    public virtual ICollection<UserSession> Sessions { get; set; } = [];
}