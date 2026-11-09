namespace Application.Interfaces;

/// <summary>
/// Creates and hashes opaque refresh tokens. Raw tokens are never persisted.
/// </summary>
public interface IRefreshTokenService
{
    /// <summary>Creates a 256-bit cryptographically random, Base64Url-encoded token.</summary>
    string CreateToken();

    /// <summary>Returns the Base64Url SHA-256 hash of a raw token.</summary>
    string HashToken(string rawToken);

    /// <summary>Constant-time comparison of two token hashes.</summary>
    bool HashesMatch(string left, string right);

    /// <summary>Cheap structural validation before any database lookup.</summary>
    bool LooksLikeValidToken(string? rawToken);
}
