using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShareShowcase.Api.Data;

namespace ShareShowcase.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public sealed class FoldersController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public FoldersController(ApplicationDbContext db) => _db = db;

    public sealed record FolderDto(Guid Id, Guid? ParentFolderId, string Name, DateTimeOffset CreatedAt);

    public sealed class CreateBody
    {
        [Required, MaxLength(255)]
        public string Name { get; set; } = "";

        public Guid? ParentFolderId { get; set; }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FolderDto>>> List(CancellationToken ct)
    {
        var uid = UserId();
        var rows = await _db.Folders
            .AsNoTracking()
            .Where(f => f.OwnerId == uid)
            .OrderBy(f => f.Name)
            .Select(f => new FolderDto(f.Id, f.ParentFolderId, f.Name, f.CreatedAt))
            .ToListAsync(ct);
        return Ok(rows);
    }

    [HttpPost]
    public async Task<ActionResult<FolderDto>> Create([FromBody] CreateBody body, CancellationToken ct)
    {
        var uid = UserId();
        if (body.ParentFolderId is null)
            return BadRequest(new { error = "Create subfolders under your Library (set parentFolderId)." });

        var pid = body.ParentFolderId.Value;
        var parentOk = await _db.Folders.AnyAsync(
            f => f.Id == pid && f.OwnerId == uid, ct);
        if (!parentOk)
            return BadRequest(new { error = "Parent folder not found." });

        var folder = new ContentFolder
        {
            Id = Guid.NewGuid(),
            OwnerId = uid,
            ParentFolderId = pid,
            Name = body.Name.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _db.Folders.Add(folder);
        await _db.SaveChangesAsync(ct);
        return Ok(new FolderDto(folder.Id, folder.ParentFolderId, folder.Name, folder.CreatedAt));
    }

    private string UserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Missing user id");
}
