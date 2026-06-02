

namespace Application.DTOs
{
    public class CreateUserDto
    {
        public int UserId { get; set; }

        public string Username { get; set; } = null!;

        // قد ترغب في إضافة حقل Password فقط إذا كان المستخدم يريد تغييرها
        // أو اجعلها اختيارية
        public string? Password { get; set; } = null!;

        public byte IsActive { get; set; }

        public int PersonId { get; set; }
    }
}
