using System.Security.Cryptography;
using System.Text;

using Application.Interfaces;

namespace Application.Services;

/// <summary>
/// Opaque refresh tokens: 256-bit random values, persisted only as SHA-256 hashes.
/// </summary>
public sealed class RefreshTokenService : IRefreshTokenService
{
    private const int TokenEntropyBytes = 32;
    private const int EncodedTokenLength = 43; // 32 bytes -> 44 Base64 chars minus padding

    public string CreateToken() =>
        Base64UrlEncode(RandomNumberGenerator.GetBytes(TokenEntropyBytes));

    public string HashToken(string rawToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawToken);

        return Base64UrlEncode(
            SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
    }

    public bool HashesMatch(string left, string right)
    {
        if (string.IsNullOrEmpty(left) || string.IsNullOrEmpty(right))
        {
            return false;
        }

        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);

        return leftBytes.Length == rightBytes.Length &&
               CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    public bool LooksLikeValidToken(string? rawToken) =>
        !string.IsNullOrWhiteSpace(rawToken) &&
        rawToken.Length == EncodedTokenLength &&
        rawToken.All(static character =>
            char.IsAsciiLetterOrDigit(character) ||
            character is '-' or '_');

    private static string Base64UrlEncode(byte[] data) =>
        Convert.ToBase64String(data)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
