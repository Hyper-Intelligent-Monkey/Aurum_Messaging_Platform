using Api.Hubs;
using Application.Common.Interfaces.Services;
using Application.DTOs.Message;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Api.Controllers;

[Authorize]
[Route("api/messages")]
public class MessagesController : ApiControllerBase
{
    private readonly IMessagingService _messagingService;
    private readonly IHubContext<ChatHub> _hubContext;

    public MessagesController(
        IMessagingService messagingService,
        IHubContext<ChatHub> hubContext)
    {
        _messagingService = messagingService;
        _hubContext = hubContext;
    }

    // sends a text message to a conversation
    [HttpPost]
    public async Task<ActionResult<MessageResponse>> SendMessage(
        [FromBody] SendMessageRequest request, 
        CancellationToken cancellationToken)
    {
        var message = await _messagingService.SendMessage(CurrentUserId, request, cancellationToken);
        // sends the message to the conversation room
        await _hubContext.Clients.Group($"conversation_{message.ConversationId}")
            .SendAsync("ReceiveMessage", message, cancellationToken); // where to listen for new messages
        return Ok(message);
    }

    // get messages sent in a conversation
    [HttpGet("{conversationId:int}")]
    public async Task<ActionResult<IEnumerable<MessageResponse>>> GetMessages(
        int conversationId,
        [FromQuery] int limit = 50,
        [FromQuery] DateTime? before = null,
        CancellationToken cancellationToken = default)
    {
        var messages = await _messagingService.GetMessages(conversationId, CurrentUserId, limit, before, cancellationToken);
        return Ok(messages);
    }

    // edits own message
    [HttpPut]
    public async Task<ActionResult<MessageResponse>> EditMessage(
        [FromBody] EditMessageRequest request, 
        CancellationToken cancellationToken)
    {
        var response = await _messagingService.EditMessage(CurrentUserId, request, cancellationToken);
        await _hubContext.Clients.Group($"conversation_{response.ConversationId}")
            .SendAsync("MessageEdited", response, cancellationToken); // where to listen for message edits
        return Ok(response);
    }

    // deletes own message
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<MessageResponse>> DeleteMessage(
        int id, 
        CancellationToken cancellationToken)
    {
        var response = await _messagingService.DeleteMessage(CurrentUserId, id, cancellationToken);
        await _hubContext.Clients.Group($"conversation_{response.ConversationId}")
            .SendAsync("MessageDeleted", new { messageId = response.Id, conversationId = response.ConversationId }, cancellationToken); // where to listen for message deletes
        return Ok(response);
    }

    // marks messages as seen
    [HttpPost("read/{conversationId:int}")]
    public async Task<IActionResult> MarkAsSeen(
        int conversationId,
        CancellationToken cancellationToken)
    {
        await _messagingService.MarkMessagesAsSeen(CurrentUserId, conversationId, cancellationToken);
        await _hubContext.Clients.Group($"conversation_{conversationId}")
            .SendAsync("MessagesSeen", new { conversationId, seenByUserId = CurrentUserId, seenAt = DateTime.UtcNow }, cancellationToken); // where to listen for message seen status
        return Ok(new { message = "Messages marked as seen." });
    }
}