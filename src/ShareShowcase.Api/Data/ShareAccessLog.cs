namespace ShareShowcase.Api.Data;

public sealed class ShareAccessLog
{
    public Guid Id { get; set; }
    public Guid ShareLinkId { get; set; }
    public DateTimeOffset AccessedAt { get; set; }

    /// <summary>metadata | download</summary>
    public string AccessType { get; set; } = null!;

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public ShareLink ShareLink { get; set; } = null!;
}
