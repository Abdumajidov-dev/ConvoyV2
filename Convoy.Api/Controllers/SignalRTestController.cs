using Convoy.Service.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Convoy.Api.Controllers;

/// <summary>
/// SignalR test va health check API'lari
/// </summary>
[ApiController]
[Route("api/signalr_test")]
public class SignalRTestController : ControllerBase
{
    private readonly IHubContext<Hubs.LocationHub> _hubContext;
    private readonly ILogger<SignalRTestController> _logger;

    public SignalRTestController(
        IHubContext<Hubs.LocationHub> hubContext,
        ILogger<SignalRTestController> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// SignalR health check - ishlayotganini tekshirish
    /// </summary>
    /// <returns>SignalR holati</returns>
    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok(new
        {
            status = "healthy",
            signalR = "active",
            hubEndpoint = "/hubs/location",
            timestamp = DateTime.UtcNow,
            message = "SignalR hub is running and ready to accept connections"
        });
    }

    /// <summary>
    /// Test location yuborish - SignalR broadcast'ni tekshirish
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Broadcast holati</returns>
    [HttpPost("broadcast_test/{user_id}")]
    public async Task<IActionResult> BroadcastTestLocation([FromRoute(Name = "user_id")] int userId)
    {
        try
        {
            // Test location data
            var testLocation = new LocationResponseDto
            {
                Id = 999999,
                UserId = userId,
                RecordedAt = DateTime.UtcNow,
                Latitude = 41.2995m + (decimal)(new Random().NextDouble() * 0.01),
                Longitude = 69.2401m + (decimal)(new Random().NextDouble() * 0.01),
                Accuracy = 10.5m,
                Speed = 5.2m,
                Heading = 180.0m,
                Altitude = 450.0m,
                ActivityType = "walking",
                ActivityConfidence = 85,
                IsMoving = true,
                BatteryLevel = 75,
                IsCharging = false,
                DistanceFromPrevious = 50.0m,
                CreatedAt = DateTime.UtcNow
            };

            // Specific user'ga yuborish
            await _hubContext.Clients.Group($"user_{userId}")
                .SendAsync("LocationUpdated", testLocation);

            // Barcha user'larga yuborish
            await _hubContext.Clients.Group("all_users")
                .SendAsync("LocationUpdated", testLocation);

            _logger.LogInformation("Test location broadcast for UserId={UserId}", userId);

            return Ok(new
            {
                success = true,
                message = $"Test location broadcasted to user_{userId} and all_users groups",
                testData = testLocation,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting test location");
            return StatusCode(500, new
            {
                success = false,
                message = "Failed to broadcast test location",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Barcha connected client'larga test xabar yuborish
    /// </summary>
    /// <param name="message">Test xabar</param>
    /// <returns>Yuborilgan xabar</returns>
    [HttpPost("broadcast_all")]
    public async Task<IActionResult> BroadcastToAll([FromBody] string message)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("TestMessage", new
            {
                message = message,
                timestamp = DateTime.UtcNow,
                type = "test"
            });

            _logger.LogInformation("Broadcast test message to all clients: {Message}", message);

            return Ok(new
            {
                success = true,
                message = "Message broadcasted to all connected clients",
                sentMessage = message,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting to all clients");
            return StatusCode(500, new
            {
                success = false,
                message = "Failed to broadcast message",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Specific user group'ga test xabar yuborish
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="message">Test xabar</param>
    /// <returns>Yuborilgan xabar</returns>
    [HttpPost("broadcast_user/{user_id}")]
    public async Task<IActionResult> BroadcastToUser([FromRoute(Name = "user_id")] int userId, [FromBody] string message)
    {
        try
        {
            var groupName = $"user_{userId}";
            await _hubContext.Clients.Group(groupName).SendAsync("TestMessage", new
            {
                message = message,
                userId = userId,
                timestamp = DateTime.UtcNow,
                type = "user_specific_test"
            });

            _logger.LogInformation("Broadcast test message to user {UserId}: {Message}", userId, message);

            return Ok(new
            {
                success = true,
                message = $"Message broadcasted to {groupName} group",
                userId = userId,
                sentMessage = message,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting to user {UserId}", userId);
            return StatusCode(500, new
            {
                success = false,
                message = "Failed to broadcast message",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// SignalR connection info - Flutter developer uchun
    /// </summary>
    /// <returns>SignalR ulanish ma'lumotlari</returns>
    [HttpGet("connection_info")]
    public IActionResult GetConnectionInfo()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        return Ok(new
        {
            signalR = new
            {
                hubUrl = $"{baseUrl}/hubs/location",
                hubName = "LocationHub",
                protocol = "json",
                transport = "WebSockets, ServerSentEvents, LongPolling"
            },
            serverMethods = new[]
            {
                new
                {
                    name = "JoinUserTracking",
                    description = "Bitta user'ni track qilish uchun group'ga qo'shilish",
                    parameters = new[] { "userId (int)" },
                    example = "await hubConnection.invoke('JoinUserTracking', args: [123]);"
                },
                new
                {
                    name = "LeaveUserTracking",
                    description = "User tracking'dan chiqish",
                    parameters = new[] { "userId (int)" },
                    example = "await hubConnection.invoke('LeaveUserTracking', args: [123]);"
                },
                new
                {
                    name = "JoinAllUsersTracking",
                    description = "Barcha user'larni track qilish",
                    parameters = Array.Empty<string>(),
                    example = "await hubConnection.invoke('JoinAllUsersTracking');"
                },
                new
                {
                    name = "LeaveAllUsersTracking",
                    description = "Barcha user'lar tracking'dan chiqish",
                    parameters = Array.Empty<string>(),
                    example = "await hubConnection.invoke('LeaveAllUsersTracking');"
                }
            },
            clientEvents = new[]
            {
                new
                {
                    name = "LocationUpdated",
                    description = "Yangi location yaratilganda server yuboradi",
                    dataType = "LocationResponseDto",
                    example = "hubConnection.on('LocationUpdated', (data) => { print(data); });"
                },
                new
                {
                    name = "TestMessage",
                    description = "Test xabarlar uchun (faqat testing)",
                    dataType = "Object",
                    example = "hubConnection.on('TestMessage', (data) => { print(data); });"
                }
            },
            locationResponseDtoExample = new LocationResponseDto
            {
                Id = 123456,
                UserId = 1,
                RecordedAt = DateTime.UtcNow,
                Latitude = 41.2995m,
                Longitude = 69.2401m,
                Accuracy = 10.5m,
                Speed = 5.2m,
                Heading = 180.0m,
                Altitude = 450.0m,
                ActivityType = "walking",
                ActivityConfidence = 85,
                IsMoving = true,
                BatteryLevel = 75,
                IsCharging = false,
                DistanceFromPrevious = 50.0m,
                CreatedAt = DateTime.UtcNow
            }
        });
    }

    /// <summary>
    /// SignalR test qilish uchun step-by-step guide
    /// </summary>
    /// <returns>Test qilish bo'yicha qo'llanma</returns>
    [HttpGet("test_guide")]
    public IActionResult GetTestGuide()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        return Ok(new
        {
            title = "SignalR Test Guide",
            steps = new[]
            {
                new
                {
                    step = 1,
                    title = "Health Check",
                    method = "GET",
                    endpoint = $"{baseUrl}/api/signalrtest/health",
                    description = "SignalR ishlayotganini tekshirish"
                },
                new
                {
                    step = 2,
                    title = "Connection Info",
                    method = "GET",
                    endpoint = $"{baseUrl}/api/signalrtest/connection-info",
                    description = "SignalR hub ma'lumotlarini olish"
                },
                new
                {
                    step = 3,
                    title = "Flutter'da Hub'ga Ulanish",
                    method = "Flutter Code",
                    endpoint = $"{baseUrl}/hubs/location",
                    description = "signalr_netcore package'dan foydalanib ulanish"
                },
                new
                {
                    step = 4,
                    title = "Group'ga Qo'shilish",
                    method = "Hub Method",
                    endpoint = "JoinUserTracking(1)",
                    description = "User 1'ni track qilish uchun group'ga qo'shilish"
                },
                new
                {
                    step = 5,
                    title = "Test Broadcast",
                    method = "POST",
                    endpoint = $"{baseUrl}/api/signalrtest/broadcast-test/1",
                    description = "Test location yuborish va Flutter'da olish"
                },
                new
                {
                    step = 6,
                    title = "Real Location POST",
                    method = "POST",
                    endpoint = $"{baseUrl}/api/locations",
                    description = "Haqiqiy location yaratish va SignalR broadcast'ni kuzatish"
                }
            },
            flutterExample = @"
// 1. Package qo'shish: signalr_netcore: ^1.3.7

// 2. Ulanish
final hubConnection = HubConnectionBuilder()
    .withUrl('" + baseUrl + @"/hubs/location')
    .build();

// 3. Event tinglash
hubConnection.on('LocationUpdated', (data) {
    print('Location keldi: $data');
});

// 4. Ulanish
await hubConnection.start();

// 5. Group'ga qo'shilish
await hubConnection.invoke('JoinUserTracking', args: [1]);

// 6. Endi test qiling - Swagger'dan broadcast-test/1 ni chaqiring
"
        });
    }
}
