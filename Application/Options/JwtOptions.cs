using System.Text;
using Microsoft.Extensions.Options;

namespace Application.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public int ExpirationMinutes { get; init; } = 60;
}

public sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Issuer))
            errors.Add("JWT Issuer is required.");

        if (string.IsNullOrWhiteSpace(options.Audience))
            errors.Add("JWT Audience is required.");

        if (string.IsNullOrWhiteSpace(options.SecretKey))
            errors.Add("JWT SecretKey is required.");
        else if (Encoding.UTF8.GetByteCount(options.SecretKey) < 32)
            errors.Add("JWT SecretKey must be at least 32 bytes long.");

        if (options.ExpirationMinutes <= 0)
            errors.Add("JWT ExpirationMinutes must be greater than zero.");

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}