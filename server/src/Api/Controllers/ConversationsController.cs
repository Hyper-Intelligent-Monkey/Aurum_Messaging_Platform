using Application.Common.Interfaces.Services;
using Application.DTOs.Conversation;
using Application.DTOs.Message;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Authorize]
[Route("api/conversations")]
public class ConversationsController : ApiControllerBase
{
    private readonly IConversationService _conversationService;
    private readonly IMessagingService _messagingService;

    public ConversationsController(IConversationService conversationService, IMessagingService messagingService)
    {
        _conversationService = conversationService;
        _messagingService = messagingService;
    }

    // get contacted users
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ConversationResponse>>> GetUserConversations(
        [FromQuery] int limit = 30,
        [FromQuery] DateTime? before = null,
        CancellationToken cancellationToken = default
        )
    {
        var conversations = await _conversationService.GetUserConversations(CurrentUserId, limit, before, cancellationToken);
        return Ok(conversations);
    }

    // get a conversation by ID
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ConversationResponse>> GetConversationById(
        int id, 
        CancellationToken cancellationToken
    )
    {
        var conversation = await _conversationService.GetConversationById(id, CurrentUserId, cancellationToken);
        return Ok(conversation);
    }

    // search for contacted users
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<ConversationResponse>>> SearchUserConversations(
        [FromQuery] string query,
        [FromQuery] int limit = 10,
        [FromQuery] DateTime? before = null,
        CancellationToken cancellationToken = default
    )
    {
        var conversations = await _conversationService.SearchUserConversations(CurrentUserId, query, limit, before, cancellationToken);
        return Ok(conversations);
    }

    // mute a conversation
    [HttpPost("{id:int}/mute")]
    public async Task<IActionResult> MuteConversation(
        int id,
        [FromQuery] int? minutes,
        CancellationToken cancellationToken)
    {
        await _conversationService.MuteConversation(CurrentUserId, id, minutes, cancellationToken);
        return Ok(new { message = "Conversation muted successfully." });
    }

    // unmute a conversation
    [HttpPost("{id:int}/unmute")]
    public async Task<IActionResult> UnmuteConversation(
        int id,
        CancellationToken cancellationToken)
    {
        await _conversationService.UnmuteConversation(CurrentUserId, id, cancellationToken);
        return Ok(new { message = "Conversation unmuted successfully." });
    }

    // get media files sent in a conversation
    [HttpGet("{id:int}/media")]
    public async Task<ActionResult<IEnumerable<ConversationMediaResponse>>> GetConversationMedia(
        int id,
        [FromQuery] int limit = 25,
        [FromQuery] DateTime? before = null,
        CancellationToken cancellationToken = default
    )
    {
        var media = await _messagingService.GetConversationMedia(CurrentUserId, id, limit, before, cancellationToken);
        return Ok(media);
    }
}