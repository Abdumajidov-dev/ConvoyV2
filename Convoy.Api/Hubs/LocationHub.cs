using Convoy.Service.Interfaces;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace Convoy.Api.Hubs;

/// <summary>
/// Real-time GPS location tracking hub
/// Connection'da user'ni active qiladi, disconnection'da inactive qiladi
/// </summary>
public class LocationHub : Hub
{
    private readonly ILogger<LocationHub> _logger;
    private readonly IUserService _userService;

    // ConnectionId -> UserId mapping (in-memory store)
    private static readonly ConcurrentDictionary<string, int> _connectionUserMap = new();

    public LocationHub(ILogger<LocationHub> logger, IUserService userService)
    {
        _logger = logger;
        _userService = userService;
    }

    /// <summary>
    /// Client ulanganida chaqiriladi
    /// IMPORTANT: Flutter client connect bo'lgandan keyin RegisterUser(userId) ni chaqirishi kerak!
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var connectionId = Context.ConnectionId;
        _logger.LogInformation("✅ SignalR client connected: {ConnectionId}", connectionId);
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Client uzilganida chaqiriladi - user'ni inactive qiladi
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;
        _logger.LogInformation("❌ SignalR client disconnected: {ConnectionId}", connectionId);

        // ConnectionId bo'yicha userId topish
        if (_connectionUserMap.TryRemove(connectionId, out var userId))
        {
            try
            {
                // User'ni inactive qilish
                await _userService.SetUserActiveStatusAsync(userId, false);
                _logger.LogWarning("🔴 User marked as INACTIVE: user_id={UserId}, connection={ConnectionId}",
                    userId, connectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking user {UserId} as inactive", userId);
            }
        }
        else
        {
            _logger.LogWarning("Connection {ConnectionId} disconnected but no userId mapping found (user didn't call RegisterUser)",
                connectionId);
        }

        if (exception != null)
        {
            _logger.LogError(exception, "Client {ConnectionId} disconnected with error", connectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Flutter client ulanganidan keyin bu metodini chaqirishi SHART!
    /// User'ni active qiladi va connection mapping'ni saqlaydi
    /// </summary>
    public async Task RegisterUser(int userId)
    {
        var connectionId = Context.ConnectionId;

        try
        {
            // User'ni active qilish
            await _userService.SetUserActiveStatusAsync(userId, true);

            // Connection -> UserId mapping saqlash
            _connectionUserMap[connectionId] = userId;

            _logger.LogInformation("🟢 User marked as ACTIVE: user_id={UserId}, connection={ConnectionId}",
                userId, connectionId);

            // Client'ga tasdiqlash yuborish
            await Clients.Caller.SendAsync("UserRegistered", new {
                user_id = userId,
                is_active = true,
                message = "User successfully registered and marked as active"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user {UserId}", userId);
            await Clients.Caller.SendAsync("UserRegistrationFailed", new {
                user_id = userId,
                error = ex.Message
            });
        }
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

    /// <summary>
    /// User o'zini inactive qilganda SignalR connection'ni yopish
    /// Flutter client bu metodini chaqirishi kerak status false qilgandan keyin
    /// </summary>
    public async Task UnregisterUser(int userId)
    {
        var connectionId = Context.ConnectionId;

        try
        {
            // User'ni inactive qilish
            await _userService.SetUserActiveStatusAsync(userId, false);

            // Connection -> UserId mapping o'chirish
            _connectionUserMap.TryRemove(connectionId, out _);

            _logger.LogInformation("🔴 User marked as INACTIVE (manual): user_id={UserId}, connection={ConnectionId}",
                userId, connectionId);

            // Client'ga tasdiqlash yuborish
            await Clients.Caller.SendAsync("UserUnregistered", new {
                user_id = userId,
                is_active = false,
                message = "User successfully unregistered and marked as inactive"
            });

            // Connection'ni yopish
            Context.Abort();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unregistering user {UserId}", userId);
            await Clients.Caller.SendAsync("UserUnregistrationFailed", new {
                user_id = userId,
                error = ex.Message
            });
        }
    }
}
