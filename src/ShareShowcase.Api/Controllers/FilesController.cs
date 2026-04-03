using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShareShowcase.Api.Data;
using ShareShowcase.Api.Services;

namespace ShareShowcase.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1")]
public sealed class FilesController : ControllerBase
{
    private const long MaxUploadBytes = 100L * 1024 * 1024; // 100 MB (tune per product)

    private readonly ApplicationDbContext _db;
    private readonly LocalFileStorage _storage;

    public FilesController(ApplicationDbContext db, LocalFileStorage storage)
    {
        _db = db;
        _storage = storage;
    }

    public sealed record MediaFileDto(
        Guid Id,
        Guid FolderId,
        string OriginalFileName,
        string ContentType,
        long SizeBytes,
        DateTimeOffset CreatedAt);

    [HttpGet("folders/{folderId:guid}/files")]
    public async Task<ActionResult<IReadOnlyList<MediaFileDto>>> ListFiles(Guid folderId, CancellationToken ct)
    {
        var uid = UserId();
        var folderOk = await _db.Folders.AnyAsync(f => f.Id == folderId && f.OwnerId == uid, ct);
        if (!folderOk)
            return NotFound();

        var rows = await _db.MediaAssets
            .AsNoTracking()
            .Where(m => m.FolderId == folderId && m.OwnerId == uid)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new MediaFileDto(m.Id, m.FolderId, m.OriginalFileName, m.ContentType, m.SizeBytes, m.CreatedAt))
            .ToListAsync(ct);
        return Ok(rows);
    }

    [HttpPost("folders/{folderId:guid}/files")]
    [RequestSizeLimit(MaxUploadBytes)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<MediaFileDto>> Upload(Guid folderId, [FromForm] IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0)
            return BadRequest(new { error = "Empty file." });
        if (file.Length > MaxUploadBytes)
            return BadRequest(new { error = "File too large." });

        var uid = UserId();
        var folder = await _db.Folders.FirstOrDefaultAsync(f => f.Id == folderId && f.OwnerId == uid, ct);
        if (folder is null)
            return NotFound();

        var id = Guid.NewGuid();
        var safeName = Path.GetFileName(file.FileName);
        var ext = Path.GetExtension(safeName);
        var stored = $"{id:N}{ext}";
        var relative = Path.Combine("files", id.ToString("N"), stored).Replace('\\', '/');

        await using (var stream = file.OpenReadStream())
            await _storage.SaveAsync(stream, relative, ct);

        var entity = new MediaAsset
        {
            Id = id,
            OwnerId = uid,
            FolderId = folderId,
            OriginalFileName = safeName,
            StoredFileName = stored,
            ContentType = string.IsNullOrEmpty(file.ContentType) ? "application/octet-stream" : file.ContentType,
            SizeBytes = file.Length,
            StoragePath = relative,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _db.MediaAssets.Add(entity);
        await _db.SaveChangesAsync(ct);

        return Ok(new MediaFileDto(entity.Id, entity.FolderId, entity.OriginalFileName, entity.ContentType, entity.SizeBytes, entity.CreatedAt));
    }

    [HttpGet("files/{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var uid = UserId();
        var asset = await _db.MediaAssets.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id && m.OwnerId == uid, ct);
        if (asset is null)
            return NotFound();

        Stream stream;
        try
        {
            stream = _storage.OpenRead(asset.StoragePath);
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { error = "Blob missing." });
        }

        return File(stream, asset.ContentType, asset.OriginalFileName);
    }

    private string UserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Missing user id");
}
