namespace Application.Common.Interfaces.Services;

public interface IFileStorageService
{
    // saves a file to storage
    Task<string> SaveFileAsync(Stream stream, string originalFileName, string subFolder, CancellationToken cancellationToken = default);
    // retrieves a file from storage
    Task<(Stream FileStream, string ContentType)> GetFileAsync(string relativePath, CancellationToken cancellationToken = default);
    // deletes a file from storage
    Task DeleteFileAsync(string relativePath, CancellationToken cancellationToken = default);
}