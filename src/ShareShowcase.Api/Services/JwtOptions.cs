namespace ShareShowcase.Api.Services;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";
    public string SigningKey { get; set; } = "";
    public int ExpiryMinutes { get; set; } = 60;

    /// <summary>Audience claim for short-lived tokens used to view password-protected shares.</summary>
    public string ShareViewerAudience { get; set; } = "ShareShowcase.ShareViewer";

    public int ShareViewerExpiryMinutes { get; set; } = 480;
}
