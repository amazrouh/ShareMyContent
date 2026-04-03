namespace ShareShowcase.Api.Services;

/// <summary>Stores blobs under a root directory on disk (dev-friendly; swap for S3 in production).</summary>
public sealed class LocalFileStorage(IWebHostEnvironment env, ILogger<LocalFileStorage> log)
{
    private readonly string _root = Path.Combine(env.ContentRootPath, "App_Data", "blobs");

    public string Root => _root;

    public async Task<string> SaveAsync(Stream content, string relativePath, CancellationToken ct = default)
    {
        var full = Path.Combine(_root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        await using var fs = File.Create(full);
        await content.CopyToAsync(fs, ct);
        log.LogInformation("Saved blob {Path}", relativePath);
        return relativePath;
    }

    public Stream OpenRead(string relativePath)
    {
        var full = Path.Combine(_root, relativePath);
        if (!File.Exists(full))
            throw new FileNotFoundException(relativePath);
        return File.OpenRead(full);
    }

    public void Delete(string relativePath)
    {
        var full = Path.Combine(_root, relativePath);
        if (File.Exists(full))
            File.Delete(full);
    }

    public void EnsureRootExists() => Directory.CreateDirectory(_root);
}
