using Application.Common.Interfaces.Services;

namespace Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _baseUploadsPath;

    public LocalFileStorageService()
    {
        // Points to an "uploads" folder ir src/Api/uploads
        _baseUploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
    }

    public async Task<string> SaveFileAsync(
        Stream stream, 
        string originalFileName, 
        string subFolder, 
        CancellationToken cancellationToken = default)
    {
        var targetDirectory = Path.Combine(_baseUploadsPath, subFolder);
        if (!Directory.Exists(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        var extension = Path.GetExtension(originalFileName);
        // assigns a unique file name to avoid overwriting existing files
        var uniqueFileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(targetDirectory, uniqueFileName);

        // creates a file stream for writing
        await using var fileStream = new FileStream(
            fullPath, 
            FileMode.Create, 
            FileAccess.Write, 
            FileShare.None, 
            bufferSize: 4096, 
            useAsync: true);

        // copies each byte from the stream to the file
        await stream.CopyToAsync(fileStream, cancellationToken);

        // Returns relative path to the file
        return $"{subFolder}/{uniqueFileName}";
    }


    public Task<(Stream FileStream, string ContentType)> GetFileAsync(
        string relativePath, 
        CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_baseUploadsPath, relativePath);

        if (!File.Exists(fullPath))
        {
            throw new KeyNotFoundException("File not found on disk.");
        }
        // opens the file stream for reading
        var fileStream = new FileStream(
            fullPath, 
            FileMode.Open, 
            FileAccess.Read, 
            FileShare.Read, 
            bufferSize: 4096, 
            useAsync: true);

        var extension = Path.GetExtension(fullPath).ToLowerInvariant();
        // assigns a content type based on the file extension
        var contentType = extension switch
        {
            // Images
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            ".bmp" => "image/bmp",

            // Audio & Voice Notes
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".ogg" or ".opus" => "audio/ogg",
            ".m4a" or ".aac" => "audio/mp4",

            // Videos
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            ".mov" => "video/quicktime",
            ".avi" => "video/x-msvideo",

            // Documents & Archives
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            ".csv" => "text/csv",
            ".json" => "application/json",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".zip" => "application/zip",
            ".rar" => "application/x-rar-compressed",
            ".7z" => "application/x-7z-compressed",

            // Fallback for any other binary file
            _ => "application/octet-stream"
        };
        
        // returns the file stream and content type
        return Task.FromResult<(Stream, string)>((fileStream, contentType));
    }

    // deletes a file from storage
    public Task DeleteFileAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_baseUploadsPath, relativePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }
}