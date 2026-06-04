



namespace Application.DTOs
{
    public class UserDto
    {
        public int UserId { get; set; }
        public int PersonId { get; set; }
        public string UserName { get; set; } = null!;
        public string Password { get; set; } = null!;
        public bool IsActive { get; set; }
        public string FullName { get; set; } = null!;

    }
}
