using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Security.Claims;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private static readonly ConcurrentDictionary<int, HashSet<string>> _userConnections = new();

    // Holds pending disconnect timers to prevent offline flickering on page refresh
    private static readonly ConcurrentDictionary<int, CancellationTokenSource> _disconnectTokens = new();

    private static readonly object _lock = new();

    private readonly IUnitofWork _unitOfWork;
    private readonly IUserService _userService;
    private readonly IServiceScopeFactory _scopeFactory;

    public ChatHub(
        IUnitofWork unitOfWork,
        IUserService userService,
        IServiceScopeFactory scopeFactory)
    {
        _unitOfWork = unitOfWork;
        _userService = userService;
        _scopeFactory = scopeFactory;
    }

    // Extracts authenticated user ID from JWT claims
    private int CurrentUserId
    {
        get
        {
            var claimValue = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claimValue, out var userId) ? userId : 0;
        }
    }

    // Runs when a client establishes a WebSocket connection
    public override async Task OnConnectedAsync()
    {
        var userId = CurrentUserId;
        if (userId > 0)
        {
            // Join personal room for user-targeted push events
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

            // Cancel pending disconnect timer if user reconnected before timeout
            bool wasPendingDisconnect = false;
            if (_disconnectTokens.TryRemove(userId, out var pendingCts))
            {
                pendingCts.Cancel();
                wasPendingDisconnect = true;
            }

            // adds the connection from an open tab to the user's list of connections
            bool isFirstConnection;
            lock (_lock)
            {
                var connections = _userConnections.GetOrAdd(userId, _ => new HashSet<string>());
                connections.Add(Context.ConnectionId);
                isFirstConnection = connections.Count == 1;
            }

            // Broadcast online status to others only if this is a fresh session
            if (isFirstConnection && !wasPendingDisconnect)
            {
                var user = await _userService.SetUserOnline(userId);
                if (user != null)
                {
                    await Clients.Others.SendAsync("UserPresenceChanged", new
                    {
                        userId = user.Id,
                        isOnline = user.IsOnline,
                        lastSeen = user.LastSeen
                    });
                }
            }
        }

        await base.OnConnectedAsync();
    }

    // Runs when a client connection drops or tab closes
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = CurrentUserId;
        if (userId > 0)
        {
            // instance of the connection leaves the user's personal notification room
            // meaning it does not receive push events from other users or updates
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");

            // Untrack connections from the user
            bool isLastConnection = false;
            lock (_lock)
            {
                if (_userConnections.TryGetValue(userId, out var connections))
                {
                    connections.Remove(Context.ConnectionId);
                    if (connections.Count == 0)
                    {
                        _userConnections.TryRemove(userId, out _);
                        isLastConnection = true;
                    }
                }
            }

            // 5-second grace period before marking offline
            if (isLastConnection)
            {
                var cts = new CancellationTokenSource();
                _disconnectTokens[userId] = cts;

                _ = Task.Run(async () =>
                {
                    try
                    {
                        // Wait 5 seconds; aborts if client reconnects within this window
                        await Task.Delay(TimeSpan.FromSeconds(5), cts.Token);

                        // the code below doesn't run if the client reconnects within 5 seconds
                        _disconnectTokens.TryRemove(userId, out _);

                        // Create isolated DI scope for background database operations
                        using var scope = _scopeFactory.CreateScope();
                        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<ChatHub>>();

                        // Mark offline and broadcast last seen timestamp to all clients
                        var user = await userService.SetUserOffline(userId);
                        if (user != null)
                        {
                            await hubContext.Clients.All.SendAsync("UserPresenceChanged", new
                            {
                                userId = user.Id,
                                isOnline = user.IsOnline,
                                lastSeen = user.LastSeen
                            });
                        } 
                    } 
                    catch (OperationCanceledException) 
                    {
                        // User reconnected before timer elapsed; suppress offline broadcast
                    } 
                    finally
                    {
                        cts.Dispose();
                    }
                });
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    // Subscribes connection to a conversation group after verifying the user as an actual participant
    public async Task JoinConversation(int conversationId)
    {
        var userId = CurrentUserId;
        var conversation = await _unitOfWork.Conversations.GetById(conversationId);
        if (conversation != null && conversation.HasParticipant(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
        }
    }

    // Unsubscribes connection from a conversation group
    public async Task LeaveConversation(int conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
    }

    // Broadcasts typing status to all participants currently in the conversation room
    public async Task SendTypingIndicator(int conversationId, bool isTyping)
    {
        var userId = CurrentUserId;
        var conversation = await _unitOfWork.Conversations.GetById(conversationId);
        if (conversation != null && conversation.HasParticipant(userId))
        {
            await Clients.Group($"conversation_{conversationId}").SendAsync("UserTyping", new
            {
                conversationId,
                userId,
                isTyping
            });
        }
    }
}
