using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ShareShowcase.Api.Data;
using ShareShowcase.Api.Services;

namespace ShareShowcase.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly ApplicationDbContext _db;
    private readonly JwtTokenService _jwt;
    private readonly JwtOptions _jwtOpt;

    public AuthController(
        UserManager<ApplicationUser> users,
        ApplicationDbContext db,
        JwtTokenService jwt,
        IOptions<JwtOptions> jwtOptions)
    {
        _users = users;
        _db = db;
        _jwt = jwt;
        _jwtOpt = jwtOptions.Value;
    }

    public sealed class RegisterBody
    {
        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required, MinLength(8)]
        public string Password { get; set; } = "";
    }

    public sealed class LoginBody
    {
        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        public string Password { get; set; } = "";
    }

    public sealed record AuthResponse(
        string Token,
        string Email,
        string UserId,
        DateTimeOffset ExpiresAtUtc);

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterBody body, CancellationToken ct)
    {
        var user = new ApplicationUser
        {
            UserName = body.Email,
            Email = body.Email,
        };

        var create = await _users.CreateAsync(user, body.Password);
        if (!create.Succeeded)
            return BadRequest(new { errors = create.Errors.Select(e => e.Description).ToList() });

        var root = new ContentFolder
        {
            Id = Guid.NewGuid(),
            OwnerId = user.Id,
            ParentFolderId = null,
            Name = "Library",
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _db.Folders.Add(root);
        await _db.SaveChangesAsync(ct);

        var token = _jwt.CreateToken(user);
        var exp = DateTimeOffset.UtcNow.AddMinutes(_jwtOpt.ExpiryMinutes);

        return Ok(new AuthResponse(token, user.Email!, user.Id, exp));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginBody body, CancellationToken ct)
    {
        var user = await _users.FindByEmailAsync(body.Email);
        if (user is null)
            return Unauthorized();

        var ok = await _users.CheckPasswordAsync(user, body.Password);
        if (!ok)
            return Unauthorized();

        var token = _jwt.CreateToken(user);
        var exp = DateTimeOffset.UtcNow.AddMinutes(_jwtOpt.ExpiryMinutes);

        return Ok(new AuthResponse(token, user.Email!, user.Id, exp));
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);
        return Ok(new { userId = id, email });
    }
}
