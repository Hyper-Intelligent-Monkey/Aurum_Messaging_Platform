using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("api/media")]
public class MediaController : ApiControllerBase
{
    private readonly IFileStorageService _fileStorageService;
    private readonly IUnitofWork _unitOfWork;

    public MediaController(
        IFileStorageService fileStorageService,
        IUnitofWork unitOfWork)
    {
        _fileStorageService = fileStorageService;
        _unitOfWork = unitOfWork;
    }

    // uploads an avatar image
    [HttpPost("upload/avatar")]
    [Authorize]
    public async Task<IActionResult> UploadAvatar(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file was uploaded." });
        }

        // limits the avatar file size to 5MB
        if (file.Length > 5 * 1024 * 1024)
        {
            return BadRequest(new { message = "Avatar file size exceeds the 5MB limit." });
        }

        // ensures the avatar file is an image
        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Avatar must be an image file." });
        }

        await using var stream = file.OpenReadStream();
        var storedFileName = await _fileStorageService.SaveFileAsync(stream, file.FileName, "avatars", cancellationToken);

        return Ok(new
        {
            storedFileName,
            fileUrl = $"/api/media/download/{storedFileName}"
        });
    }

    // uploads a file to a conversation
    [HttpPost("upload/attachment")]
    [Authorize]
    public async Task<IActionResult> UploadAttachment(
        [FromForm] IFormFile file, 
        [FromForm] int? conversationId, 
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file was uploaded." });
        }

        if (file.Length > 5 * 1024 * 1024)
        {
            return BadRequest(new { message = "File size exceeds the 5MB limit." });
        }

        string subFolder = "attachments";
        if (conversationId.HasValue && conversationId.Value > 0)
        {
            var conversation = await _unitOfWork.Conversations.GetById(conversationId.Value, cancellationToken);
            if (conversation != null && conversation.HasParticipant(CurrentUserId))
            {
                subFolder = $"attachments/conversation_{conversationId.Value}";
            }
        }

        await using var stream = file.OpenReadStream();
        var storedFileName = await _fileStorageService.SaveFileAsync(stream, file.FileName, subFolder, cancellationToken);

        return Ok(new
        {
            storedFileName,
            originalFileName = file.FileName,
            fileSize = file.Length,
            contentType = file.ContentType,
            fileUrl = $"/api/media/download/{storedFileName}"
        });
    }

    // downloads a file from the server
    [HttpGet("download/{**filePath}")]
    [AllowAnonymous]
    public async Task<IActionResult> DownloadFile(string filePath, CancellationToken cancellationToken)
    {
        // downloading conversation files
        if (filePath.StartsWith("attachments/", StringComparison.OrdinalIgnoreCase))
        {
            var message = await _unitOfWork.Messages.GetByStoredFileName(filePath, cancellationToken);
            if (message != null)
            {   
                // checks if the user has permission to access/download the file
                var conversation = await _unitOfWork.Conversations.GetById(message.ConversationId, cancellationToken);
                if (conversation == null || !conversation.HasParticipant(CurrentUserId))
                {
                    throw new UnauthorizedAccessException("You do not have permission to access this attachment.");
                }
            }
        }

        // Enables browser caching specifically for avatars (URLs contain unique GUIDs and are immutable)
        if (filePath.StartsWith("avatars/", StringComparison.OrdinalIgnoreCase))
        {
            Response.Headers.CacheControl = "public, max-age=604800, immutable";
        }

        var (fileStream, contentType) = await _fileStorageService.GetFileAsync(filePath, cancellationToken);
        return File(fileStream, contentType, enableRangeProcessing: true);
    }

    // deletes a file from the server requires authorization
    [HttpDelete("delete/{**filePath}")]
    [Authorize]
    public async Task<IActionResult> DeleteFile(string filePath, CancellationToken cancellationToken)
    {
        await _fileStorageService.DeleteFileAsync(filePath, cancellationToken);
        return Ok(new { message = "File deleted successfully." });
    }
}