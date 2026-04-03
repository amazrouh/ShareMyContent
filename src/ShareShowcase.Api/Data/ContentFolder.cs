namespace ShareShowcase.Api.Data;

public sealed class ContentFolder
{
    public Guid Id { get; set; }
    public string OwnerId { get; set; } = null!;
    public Guid? ParentFolderId { get; set; }
    public string Name { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }

    public ApplicationUser Owner { get; set; } = null!;
    public ContentFolder? Parent { get; set; }
    public ICollection<ContentFolder> Children { get; set; } = new List<ContentFolder>();
    public ICollection<MediaAsset> Files { get; set; } = new List<MediaAsset>();
}
