namespace AxiomRPG.Data;

public class FileSystemDataStore : IDataStore
{
    private readonly string _basePath;

    public FileSystemDataStore(string basePath)
    {
        _basePath = basePath;
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string?> ReadTextAsync(string path)
    {
        var fullPath = GetFullPath(path);
        if (!File.Exists(fullPath)) return null;
        return await File.ReadAllTextAsync(fullPath);
    }

    public async Task WriteTextAsync(string path, string content)
    {
        var fullPath = GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllTextAsync(fullPath, content);
    }

    public Task<bool> ExistsAsync(string path)
    {
        var fullPath = GetFullPath(path);
        return Task.FromResult(File.Exists(fullPath));
    }

    public Task DeleteAsync(string path)
    {
        var fullPath = GetFullPath(path);
        if (File.Exists(fullPath)) File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> ListFilesAsync(string directory, string pattern = "*")
    {
        var fullPath = GetFullPath(directory);
        if (!Directory.Exists(fullPath)) return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
        var files = Directory.GetFiles(fullPath, pattern, SearchOption.TopDirectoryOnly);
        return Task.FromResult<IReadOnlyList<string>>(files.Select(f => Path.GetRelativePath(_basePath, f)).ToList());
    }

    public Task<IReadOnlyList<string>> ListDirectoriesAsync(string directory)
    {
        var fullPath = GetFullPath(directory);
        if (!Directory.Exists(fullPath)) return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
        var dirs = Directory.GetDirectories(fullPath);
        return Task.FromResult<IReadOnlyList<string>>(dirs.Select(d => Path.GetRelativePath(_basePath, d)).ToList());
    }

    private string GetFullPath(string relativePath) => Path.GetFullPath(Path.Combine(_basePath, relativePath));
}
