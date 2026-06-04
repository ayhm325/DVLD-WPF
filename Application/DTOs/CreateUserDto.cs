

namespace Application.DTOs
{
    public class CreateUserDto
    {
        public int UserId { get; set; }

        public string UserName { get; set; } = null!;
       
        public string? Password { get; set; } = null!;

        public bool IsActive { get; set; }

        public int PersonId { get; set; }
    }
}
