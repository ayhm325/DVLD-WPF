



namespace Application.DTOs
{
    public class UserDto
    {
        public int UserId { get; set; }
        public int PersonId { get; set; }
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
        public byte IsActive { get; set; }       

    }
}
