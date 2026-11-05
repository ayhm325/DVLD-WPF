using Domain.Enums;

namespace Application.DTOs.UserDTO
{
    public class UpdateUserDto
    {
        public string UserName { get; set; } = null!;

        //public string? Password { get; set; }

        public bool IsActive { get; set; }

        public int PersonId { get; set; }

        /// <summary>
        /// Optional new role. Null keeps the user's current role unchanged.
        /// </summary>
        public UserRole? Role { get; set; }
    }
}
