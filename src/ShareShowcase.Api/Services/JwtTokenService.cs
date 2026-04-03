using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ShareShowcase.Api.Data;

namespace ShareShowcase.Api.Services;

public sealed class JwtTokenService(IOptions<JwtOptions> options)
{
    private readonly JwtOptions _opt = options.Value;

    public string CreateToken(ApplicationUser user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_opt.ExpiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new(ClaimTypes.NameIdentifier, user.Id),
        };

        var token = new JwtSecurityToken(
            issuer: _opt.Issuer,
            audience: _opt.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string CreateShareViewerToken(Guid shareLinkId, string shareToken)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_opt.ShareViewerExpiryMinutes);
        var audience = string.IsNullOrWhiteSpace(_opt.ShareViewerAudience)
            ? "ShareShowcase.ShareViewer"
            : _opt.ShareViewerAudience;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, shareLinkId.ToString()),
            new("share_token", shareToken),
        };

        var token = new JwtSecurityToken(
            issuer: _opt.Issuer,
            audience: audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public bool TryValidateShareViewerToken(string? jwt, string routeToken, out Guid shareLinkId)
    {
        shareLinkId = default;
        if (string.IsNullOrWhiteSpace(jwt))
            return false;

        var audience = string.IsNullOrWhiteSpace(_opt.ShareViewerAudience)
            ? "ShareShowcase.ShareViewer"
            : _opt.ShareViewerAudience;

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.SigningKey));
            var principal = handler.ValidateToken(
                jwt,
                new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidIssuer = _opt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(2),
                },
                out _);

            // Inbound JWT claim mapping maps "sub" to ClaimTypes.NameIdentifier.
            var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            var st = principal.FindFirstValue("share_token");
            if (sub is null || st != routeToken || !Guid.TryParse(sub, out shareLinkId))
                return false;
            return true;
        }
        catch
        {
            return false;
        }
    }
}
