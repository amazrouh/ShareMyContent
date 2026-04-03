using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShareShowcase.Api.Data;
using ShareShowcase.Api.Services;

namespace ShareShowcase.Api.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/v1/public/shares")]
public sealed class PublicSharesController : ControllerBase
{
    public const string ShareViewerHeaderName = "X-Share-Viewer";

    private readonly ApplicationDbContext _db;
    private readonly LocalFileStorage _storage;
    private readonly JwtTokenService _jwt;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;

    public PublicSharesController(
        ApplicationDbContext db,
        LocalFileStorage storage,
        JwtTokenService jwt,
        IPasswordHasher<ApplicationUser> passwordHasher)
    {
        _db = db;
        _storage = storage;
        _jwt = jwt;
        _passwordHasher = passwordHasher;
    }

    public sealed record PublicShareItemDto(
        Guid Id,
        string OriginalFileName,
        string ContentType,
        long SizeBytes);

    public sealed class UnlockBody
    {
        [Required]
        public string Password { get; set; } = "";
    }

    [HttpGet("{token}/metadata")]
    public async Task<IActionResult> Metadata(string token, CancellationToken ct)
    {
        var share = await FindActiveShareAsync(token, ct);
        if (share is null)
            return NotFound();

        LogAccess(share.Id, "metadata");
        await _db.SaveChangesAsync(ct);

        var targetType = share.MediaAssetId.HasValue ? "file" : "folder";
        var viewerOk = share.PasswordHash is null
            || (TryGetViewerShareId(token, out var vid) && vid == share.Id);

        if (share.PasswordHash is not null && !viewerOk)
        {
            return Ok(new
            {
                targetType,
                requiresPassword = true,
                items = (IReadOnlyList<PublicShareItemDto>?)null,
            });
        }

        var items = await BuildItemsAsync(share, ct);
        return Ok(new
        {
            targetType,
            requiresPassword = false,
            items,
        });
    }

    [HttpPost("{token}/unlock")]
    public async Task<IActionResult> Unlock(string token, [FromBody] UnlockBody body, CancellationToken ct)
    {
        var share = await _db.ShareLinks.AsNoTracking().FirstOrDefaultAsync(s => s.Token == token, ct);
        if (share is null || share.RevokedAt is not null)
            return NotFound();
        if (share.ExpiresAt is { } exp && exp <= DateTimeOffset.UtcNow)
            return NotFound();

        if (share.PasswordHash is null)
            return BadRequest(new { error = "This share is not password-protected." });

        var owner = await _db.Users.AsNoTracking().FirstAsync(u => u.Id == share.OwnerId, ct);
        if (_passwordHasher.VerifyHashedPassword(owner, share.PasswordHash, body.Password) == PasswordVerificationResult.Failed)
            return Unauthorized(new { error = "Invalid password." });

        var viewerToken = _jwt.CreateShareViewerToken(share.Id, share.Token);
        return Ok(new { viewerToken });
    }

    [HttpGet("{token}/download")]
    public async Task<IActionResult> Download(string token, [FromQuery] Guid? fileId, CancellationToken ct)
    {
        var share = await FindActiveShareAsync(token, ct);
        if (share is null)
            return NotFound();

        var viewerOk = share.PasswordHash is null
            || (TryGetViewerShareId(token, out var vid) && vid == share.Id);
        if (!viewerOk)
            return Unauthorized(new { error = "Password required or invalid viewer token." });

        if (share.MediaAssetId is { } aid)
        {
            var asset = share.MediaAsset
                ?? await _db.MediaAssets.AsNoTracking().FirstAsync(m => m.Id == aid, ct);
            LogAccess(share.Id, "download");
            await _db.SaveChangesAsync(ct);
            return OpenFile(asset);
        }

        if (!fileId.HasValue)
            return BadRequest(new { error = "fileId is required for folder shares." });

        var file = await _db.MediaAssets.AsNoTracking()
            .FirstOrDefaultAsync(
                m => m.Id == fileId && m.FolderId == share.FolderId && m.OwnerId == share.OwnerId,
                ct);
        if (file is null)
            return NotFound();

        LogAccess(share.Id, "download");
        await _db.SaveChangesAsync(ct);
        return OpenFile(file);
    }

    private async Task<ShareLink?> FindActiveShareAsync(string token, CancellationToken ct)
    {
        var share = await _db.ShareLinks
            .AsNoTracking()
            .Include(s => s.MediaAsset)
            .Include(s => s.Folder)
            .FirstOrDefaultAsync(s => s.Token == token, ct);
        if (share is null || share.RevokedAt is not null)
            return null;
        if (share.ExpiresAt is { } exp && exp <= DateTimeOffset.UtcNow)
            return null;
        return share;
    }

    private bool TryGetViewerShareId(string routeToken, out Guid shareLinkId)
    {
        shareLinkId = default;
        if (!Request.Headers.TryGetValue(ShareViewerHeaderName, out var h))
            return false;
        var jwt = h.ToString();
        return _jwt.TryValidateShareViewerToken(jwt, routeToken, out shareLinkId);
    }

    private async Task<IReadOnlyList<PublicShareItemDto>> BuildItemsAsync(ShareLink share, CancellationToken ct)
    {
        if (share.MediaAssetId is { } mid)
        {
            var m = share.MediaAsset
                ?? await _db.MediaAssets.AsNoTracking().FirstAsync(x => x.Id == mid, ct);
            return new[]
            {
                new PublicShareItemDto(m.Id, m.OriginalFileName, m.ContentType, m.SizeBytes),
            };
        }

        var fid = share.FolderId!.Value;
        return await _db.MediaAssets.AsNoTracking()
            .Where(x => x.FolderId == fid && x.OwnerId == share.OwnerId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new PublicShareItemDto(x.Id, x.OriginalFileName, x.ContentType, x.SizeBytes))
            .ToListAsync(ct);
    }

    private void LogAccess(Guid shareLinkId, string accessType)
    {
        var ua = Request.Headers.UserAgent.ToString();
        if (ua.Length > 512)
            ua = ua[..512];

        _db.ShareAccessLogs.Add(new ShareAccessLog
        {
            Id = Guid.NewGuid(),
            ShareLinkId = shareLinkId,
            AccessedAt = DateTimeOffset.UtcNow,
            AccessType = accessType,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = string.IsNullOrEmpty(ua) ? null : ua,
        });
    }

    private IActionResult OpenFile(MediaAsset asset)
    {
        try
        {
            var stream = _storage.OpenRead(asset.StoragePath);
            return File(stream, asset.ContentType, asset.OriginalFileName);
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { error = "Blob missing." });
        }
    }
}
