namespace ShareShowcase.Api.Data;

public sealed class MediaAsset
{
    public Guid Id { get; set; }
    public string OwnerId { get; set; } = null!;
    public Guid FolderId { get; set; }
    public string OriginalFileName { get; set; } = null!;
    public string StoredFileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long SizeBytes { get; set; }
    /// <summary>Relative path under the app storage root (e.g. files/{id}/name.bin).</summary>
    public string StoragePath { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }

    public ContentFolder Folder { get; set; } = null!;
    public ApplicationUser Owner { get; set; } = null!;
}
