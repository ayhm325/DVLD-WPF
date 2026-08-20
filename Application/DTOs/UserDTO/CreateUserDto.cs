namespace Application.DTOs.UserDTO
{
    public class CreateUserDto
    {
        public string UserName { get; set; } = null!;

        public string Password { get; set; } = null!;

        public bool IsActive { get; set; }

        public int PersonId { get; set; }
    }
}