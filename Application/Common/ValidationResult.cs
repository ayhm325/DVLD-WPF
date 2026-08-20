namespace Application.Common;

public sealed class ValidationResult
{
    public bool IsValid { get; }

    public IReadOnlyList<string> Errors { get; }

    private ValidationResult(
        bool isValid,
        IReadOnlyList<string> errors)
    {
        IsValid = isValid;
        Errors = errors;
    }

    public static ValidationResult Success()
    {
        return new ValidationResult(
            true,
            Array.Empty<string>());
    }

    public static ValidationResult Failure(
        IEnumerable<string> errors)
    {
        var errorList = errors
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .ToList();

        return new ValidationResult(
            false,
            errorList);
    }
}