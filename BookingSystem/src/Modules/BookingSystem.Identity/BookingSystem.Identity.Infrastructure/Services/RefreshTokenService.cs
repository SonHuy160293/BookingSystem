using System.Security.Cryptography;
using System.Text;
using BookingSystem.Identity.Application.Abstractions.Services;
using Microsoft.IdentityModel.Tokens;

namespace BookingSystem.Identity.Infrastructure.Services;

public sealed class RefreshTokenService : IRefreshTokenService
{
    public string Generate() => Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
    public bool TryHash(string? token, out byte[] hash)
    {
        hash = [];
        if (token is null || token.Length != 43 || token.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not '-' and not '_'))
        {
            return false;
        }

        try
        {
            byte[] bytes = Base64UrlEncoder.DecodeBytes(token);
            if (bytes.Length != 32 || !string.Equals(Base64UrlEncoder.Encode(bytes), token, StringComparison.Ordinal))
            {
                return false;
            }

            hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
