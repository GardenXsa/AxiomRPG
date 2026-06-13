namespace AxiomRPG.Data;

public interface IDataStore
{
    Task<string?> ReadTextAsync(string path);
    Task WriteTextAsync(string path, string content);
    Task<bool> ExistsAsync(string path);
    Task DeleteAsync(string path);
    Task<IReadOnlyList<string>> ListFilesAsync(string directory, string pattern = "*");
    Task<IReadOnlyList<string>> ListDirectoriesAsync(string directory);
}
