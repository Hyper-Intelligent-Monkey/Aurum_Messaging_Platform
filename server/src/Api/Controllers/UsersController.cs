using Api.Hubs;
using Application.Common.Interfaces.Services;
using Application.DTOs.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Api.Controllers;

[Authorize]
[Route("api/users")]
public class UsersController : ApiControllerBase
{
    private readonly IUserService _userService;
    private readonly IHubContext<ChatHub> _hubContext;

    public UsersController(
        IUserService userService,
        IHubContext<ChatHub> hubContext)
    {
        _userService = userService;
        _hubContext = hubContext;
    }
    // get own info
    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> GetMe(CancellationToken cancellationToken)
    {
        var user = await _userService.GetById(CurrentUserId, cancellationToken);
        return Ok(user);
    }

    // update own avatar
    [HttpPut("avatar")]
    public async Task<ActionResult<UserResponse>> UpdateAvatar(
        [FromBody] UpdateAvatarRequest request, 
        CancellationToken cancellationToken)
    {
        var user = await _userService.UpdateAvatar(CurrentUserId, request, cancellationToken);
        await _hubContext.Clients.All.SendAsync("UserAvatarChanged", new
        {
            userId = user.Id,
            avatar = user.Avatar
        }, cancellationToken);
        return Ok(user);
    }

    // set own password
    [HttpPost("set-password")]
    public async Task<IActionResult> SetPassword(
        [FromBody] SetPasswordRequest request, 
        CancellationToken cancellationToken)
    {
        await _userService.SetPassword(CurrentUserId, request, cancellationToken);
        return NoContent();
    }

    // change own password
    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request, 
        CancellationToken cancellationToken)
    {
        await _userService.ChangePassword(CurrentUserId, request, cancellationToken);
        return NoContent();
    }

    // search for uncontacted users
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<UserResponse>>> SearchUsers(
        [FromQuery] string query,
        [FromQuery] int limit = 10,
        [FromQuery] int offset = 0,
        CancellationToken cancellationToken = default)
    {
        var users = await _userService.SearchUsers(CurrentUserId, query, limit, offset, cancellationToken);
        return Ok(users);
    }

    // blocks a user
    [HttpPost("block/{userId:int}")]
    public async Task<IActionResult> BlockUser(int userId, CancellationToken cancellationToken)
    {
        await _userService.BlockUser(CurrentUserId, userId, cancellationToken);
        return Ok(new { message = "User blocked successfully." });
    }

    // unblocks a user
    [HttpDelete("unblock/{userId:int}")]
    public async Task<IActionResult> UnblockUser(int userId, CancellationToken cancellationToken)
    {
        await _userService.UnblockUser(CurrentUserId, userId, cancellationToken);
        return Ok(new { message = "User unblocked successfully." });
    }

    // gets blocked users
    [HttpGet("blocked")]
    public async Task<ActionResult<IEnumerable<UserResponse>>> GetBlockedUsers(CancellationToken cancellationToken)
    {
        var users = await _userService.GetBlockedUsers(CurrentUserId, cancellationToken);
        return Ok(users);
    }

    // gets block status between current user and target user
    [HttpGet("block-status/{userId:int}")]
    public async Task<ActionResult<BlockStatusResponse>> GetBlockStatus(int userId, CancellationToken cancellationToken)
    {
        var status = await _userService.GetBlockStatus(CurrentUserId, userId, cancellationToken);
        return Ok(status);
    }
}

