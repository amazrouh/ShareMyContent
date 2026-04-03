namespace ShareShowcase.Api.Data;

public sealed class ShareLink
{
    public Guid Id { get; set; }

    /// <summary>Opaque high-entropy token used in public URLs (e.g. /s/{token}).</summary>
    public string Token { get; set; } = null!;

    public string OwnerId { get; set; } = null!;

    /// <summary>Set when sharing a single file; mutually exclusive with <see cref="FolderId"/>.</summary>
    public Guid? MediaAssetId { get; set; }

    /// <summary>Set when sharing a folder; mutually exclusive with <see cref="MediaAssetId"/>.</summary>
    public Guid? FolderId { get; set; }

    /// <summary>Null = public share; otherwise password-protected.</summary>
    public string? PasswordHash { get; set; }

    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public ApplicationUser Owner { get; set; } = null!;
    public MediaAsset? MediaAsset { get; set; }
    public ContentFolder? Folder { get; set; }
    public ICollection<ShareAccessLog> AccessLogs { get; set; } = new List<ShareAccessLog>();
}
