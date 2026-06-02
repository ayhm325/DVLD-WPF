namespace Application.Common
{
    public class ValidationResult
    {
        public bool IsValid { get; set; }

        public List<string> Errors { get; set; } = new();

        public static ValidationResult Success()
        {
            return new ValidationResult
            {
                IsValid = true,
                Errors = new List<string>()
            };
        }

        public static ValidationResult Failure(List<string> errors)
        {
            return new ValidationResult
            {
                IsValid = false,
                Errors = errors
            };
        }
    }
}