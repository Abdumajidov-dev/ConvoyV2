using Microsoft.AspNetCore.SignalR;

namespace Convoy.Api.Hubs;

/// <summary>
/// Real-time GPS location tracking hub
/// </summary>
public class LocationHub : Hub
{
    private readonly ILogger<LocationHub> _logger;

    public LocationHub(ILogger<LocationHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Client connection
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var connectionId = Context.ConnectionId;
        _logger.LogInformation("Client connected: {ConnectionId}", connectionId);
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Client disconnection
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;
        _logger.LogInformation("Client disconnected: {ConnectionId}", connectionId);

        if (exception != null)
        {
            _logger.LogError(exception, "Client {ConnectionId} disconnected with error", connectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Bir user'ni track qilish uchun group'ga qo'shish
    /// </summary>
    public async Task JoinUserTracking(int userId)
    {
        var groupName = $"user_{userId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} joined tracking for user {UserId}",
            Context.ConnectionId, userId);
    }

    /// <summary>
    /// User tracking'dan chiqish
    /// </summary>
    public async Task LeaveUserTracking(int userId)
    {
        var groupName = $"user_{userId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} left tracking for user {UserId}",
            Context.ConnectionId, userId);
    }

    /// <summary>
    /// Barcha aktiv user'larni track qilish
    /// </summary>
    public async Task JoinAllUsersTracking()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "all_users");
        _logger.LogInformation("Client {ConnectionId} joined tracking for all users",
            Context.ConnectionId);
    }

    /// <summary>
    /// Barcha user'lar tracking'dan chiqish
    /// </summary>
    public async Task LeaveAllUsersTracking()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "all_users");
        _logger.LogInformation("Client {ConnectionId} left tracking for all users",
            Context.ConnectionId);
    }
}
