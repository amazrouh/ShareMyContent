using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ShareShowcase.Api.Data;

public sealed class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<ContentFolder> Folders => Set<ContentFolder>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<ShareLink> ShareLinks => Set<ShareLink>();
    public DbSet<ShareAccessLog> ShareAccessLogs => Set<ShareAccessLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ContentFolder>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.OwnerId, x.ParentFolderId, x.Name }).IsUnique();
            e.HasOne(x => x.Owner)
                .WithMany()
                .HasForeignKey(x => x.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Parent)
                .WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentFolderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<MediaAsset>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Folder)
                .WithMany(x => x.Files)
                .HasForeignKey(x => x.FolderId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Owner)
                .WithMany()
                .HasForeignKey(x => x.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ShareLink>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Token).IsUnique();
            e.HasOne(x => x.Owner)
                .WithMany()
                .HasForeignKey(x => x.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.MediaAsset)
                .WithMany()
                .HasForeignKey(x => x.MediaAssetId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Folder)
                .WithMany()
                .HasForeignKey(x => x.FolderId)
                .OnDelete(DeleteBehavior.Cascade);
            e.ToTable(t => t.HasCheckConstraint(
                "CK_ShareLink_Target",
                "(\"MediaAssetId\" IS NOT NULL AND \"FolderId\" IS NULL) OR (\"MediaAssetId\" IS NULL AND \"FolderId\" IS NOT NULL)"));
        });

        builder.Entity<ShareAccessLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.ShareLink)
                .WithMany(x => x.AccessLogs)
                .HasForeignKey(x => x.ShareLinkId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
