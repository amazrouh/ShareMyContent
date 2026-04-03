using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShareShowcase.Api.Data;
using ShareShowcase.Api.Services;

namespace ShareShowcase.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/shares")]
public sealed class SharesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;

    public SharesController(ApplicationDbContext db, IPasswordHasher<ApplicationUser> passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public sealed record ShareSummaryDto(
        Guid Id,
        string Token,
        string TargetType,
        Guid? MediaAssetId,
        Guid? FolderId,
        bool HasPassword,
        DateTimeOffset? ExpiresAt,
        DateTimeOffset? RevokedAt,
        DateTimeOffset CreatedAt);

    public sealed class CreateShareBody
    {
        public Guid? MediaAssetId { get; set; }
        public Guid? FolderId { get; set; }

        [MaxLength(200)]
        public string? Password { get; set; }

        public DateTimeOffset? ExpiresAt { get; set; }
    }

    [HttpPost]
    public async Task<ActionResult<ShareSummaryDto>> Create([FromBody] CreateShareBody body, CancellationToken ct)
    {
        var uid = UserId();
        var hasFile = body.MediaAssetId.HasValue;
        var hasFolder = body.FolderId.HasValue;
        if (hasFile == hasFolder)
            return BadRequest(new { error = "Specify exactly one of mediaAssetId or folderId." });

        if (body.ExpiresAt is { } exp && exp <= DateTimeOffset.UtcNow)
            return BadRequest(new { error = "expiresAt must be in the future." });

        var owner = await _db.Users.FirstAsync(u => u.Id == uid, ct);

        if (hasFile)
        {
            var assetId = body.MediaAssetId!.Value;
            var ok = await _db.MediaAssets.AnyAsync(m => m.Id == assetId && m.OwnerId == uid, ct);
            if (!ok)
                return NotFound(new { error = "File not found." });
        }
        else
        {
            var folderId = body.FolderId!.Value;
            var ok = await _db.Folders.AnyAsync(f => f.Id == folderId && f.OwnerId == uid, ct);
            if (!ok)
                return NotFound(new { error = "Folder not found." });
        }

        string? passwordHash = null;
        if (!string.IsNullOrEmpty(body.Password))
            passwordHash = _passwordHasher.HashPassword(owner, body.Password);

        var entity = new ShareLink
        {
            Id = Guid.NewGuid(),
            Token = ShareTokenGenerator.CreateToken(),
            OwnerId = uid,
            MediaAssetId = body.MediaAssetId,
            FolderId = body.FolderId,
            PasswordHash = passwordHash,
            ExpiresAt = body.ExpiresAt,
            RevokedAt = null,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _db.ShareLinks.Add(entity);
        await _db.SaveChangesAsync(ct);

        return Ok(ToDto(entity));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ShareSummaryDto>>> List(CancellationToken ct)
    {
        var uid = UserId();
        var rows = await _db.ShareLinks
            .AsNoTracking()
            .Where(s => s.OwnerId == uid)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);
        return Ok(rows.Select(ToDto).ToList());
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken ct)
    {
        var uid = UserId();
        var share = await _db.ShareLinks.FirstOrDefaultAsync(s => s.Id == id && s.OwnerId == uid, ct);
        if (share is null)
            return NotFound();

        share.RevokedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static ShareSummaryDto ToDto(ShareLink s) =>
        new(
            s.Id,
            s.Token,
            s.MediaAssetId.HasValue ? "file" : "folder",
            s.MediaAssetId,
            s.FolderId,
            s.PasswordHash != null,
            s.ExpiresAt,
            s.RevokedAt,
            s.CreatedAt);

    private string UserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Missing user id");
}
