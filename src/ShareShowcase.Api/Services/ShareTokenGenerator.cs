using System.Security.Cryptography;

namespace ShareShowcase.Api.Services;

internal static class ShareTokenGenerator
{
    /// <summary>32 random bytes, URL-safe base64 (no padding).</summary>
    public static string CreateToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
