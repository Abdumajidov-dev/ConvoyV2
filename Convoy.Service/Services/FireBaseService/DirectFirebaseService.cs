using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Convoy.Service.Services.FireBaseService;

public class DirectFirebaseService
{
    private readonly FirebaseMessaging _messaging;
    private readonly string _projectId;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public DirectFirebaseService(IConfiguration configuration, IHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;

        // Firebase ni initialize qilish
        InitializeFirebase();

        _messaging = FirebaseMessaging.DefaultInstance;
        _projectId = FirebaseApp.DefaultInstance?.Options?.ProjectId ?? string.Empty;
    }

    private void InitializeFirebase()
    {
        try
        {
            // Agar Firebase allaqachon initialized bo'lsa, qaytish
            if (FirebaseApp.DefaultInstance != null)
            {
                return;
            }

            // firebase.json faylini topish
            string firebaseJsonPath = GetFirebaseJsonPath();

            if (!File.Exists(firebaseJsonPath))
            {
                throw new FileNotFoundException($"Firebase configuration file not found at: {firebaseJsonPath}");
            }

            // GoogleCredential yaratish
            var credential = GoogleCredential.FromFile(firebaseJsonPath);

            // Firebase App yaratish
            FirebaseApp.Create(new AppOptions()
            {
                Credential = credential
            });

            Console.WriteLine("Firebase successfully initialized from file: " + firebaseJsonPath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to initialize Firebase: {ex.Message}", ex);
        }
    }

    private string GetFirebaseJsonPath()
    {
        // 1. Birinchi configuration dan path ni olishga harakat qilish
        var configPath = _configuration["Firebase:ConfigPath"];
        if (!string.IsNullOrEmpty(configPath))
        {
            if (File.Exists(configPath))
                return configPath;
        }

        // 2. Content root dan qidirish
        var contentRoot = _environment.ContentRootPath;

        // firebase.json (root da)
        var rootPath = Path.Combine(contentRoot, "firebase.json");
        if (File.Exists(rootPath))
            return rootPath;

        // Configs papkasida qidirish
        var configFolderPath = Path.Combine(contentRoot, "Configs", "firebase.json");
        if (File.Exists(configFolderPath))
            return configFolderPath;

        // appsettings.json yonidan qidirish
        var appSettingsDirectory = Path.GetDirectoryName(
            Path.Combine(contentRoot, "appsettings.json")
        ) ?? contentRoot;

        var nearAppSettingsPath = Path.Combine(appSettingsDirectory, "firebase.json");
        if (File.Exists(nearAppSettingsPath))
            return nearAppSettingsPath;

        // 3. Default path
        return Path.Combine(contentRoot, "firebase.json");
    }

    /// <summary>
    /// Bitta device ga notification yuborish
    /// </summary>
    public async Task<string> SendNotificationAsync(string deviceToken, string title, string body, Dictionary<string, string>? customData = null)
    {
        ValidateDeviceToken(deviceToken);

        try
        {
            var message = new Message()
            {
                Token = deviceToken,
                Notification = new Notification()
                {
                    Title = title,
                    Body = body
                },
                Data = customData,

                // Android configuration
                Android = new AndroidConfig()
                {
                    TimeToLive = TimeSpan.FromHours(24),
                    Priority = Priority.High,
                    Notification = new AndroidNotification()
                    {
                        DefaultSound = true,
                        ChannelId = "default_channel"
                    }
                },

                // iOS configuration
                Apns = new ApnsConfig()
                {
                    Headers = new Dictionary<string, string>()
                {
                    { "apns-priority", "10" }
                },
                    Aps = new Aps()
                    {
                        Alert = new ApsAlert()
                        {
                            Title = title,
                            Body = body
                        },
                        Badge = 1,
                        Sound = "default"
                    }
                }
            };

            string response = await _messaging.SendAsync(message);
            Console.WriteLine($"Successfully sent message to {deviceToken}: {response}");
            return response;
        }
        catch (FirebaseMessagingException fme)
        {
            HandleFirebaseException(fme);
            throw;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to send notification: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Ko'p device larga notification yuborish
    /// </summary>
    public async Task<BatchResponse> SendNotificationToMultipleDevicesAsync(
        IEnumerable<string> deviceTokens,
        string title,
        string body,
        Dictionary<string, string>? customData = null)
    {
        var tokens = deviceTokens?.ToList();

        if (tokens == null || !tokens.Any())
        {
            throw new ArgumentException("Device tokens list cannot be empty");
        }

        if (tokens.Count > 500)
        {
            throw new ArgumentException("Maximum 500 tokens allowed per batch");
        }

        try
        {
            var message = new MulticastMessage()
            {
                Tokens = tokens,
                Notification = new Notification()
                {
                    Title = title,
                    Body = body
                },
                Data = customData,
                Android = new AndroidConfig()
                {
                    Priority = Priority.High,
                    Notification = new AndroidNotification()
                    {
                        Icon = "ic_notification",
                        Color = "#1976D2",
                        DefaultSound = true
                    }
                },
                Apns = new ApnsConfig()
                {
                    Aps = new Aps()
                    {
                        Alert = new ApsAlert()
                        {
                            Title = title,
                            Body = body
                        },
                        Sound = "default"
                    }
                }
            };

            BatchResponse response = await _messaging.SendMulticastAsync(message);

            // Natijalarni log qilish
            if (response.FailureCount > 0)
            {
                for (int i = 0; i < response.Responses.Count; i++)
                {
                    if (!response.Responses[i].IsSuccess)
                    {
                        Console.WriteLine($"Failed to send to token at index {i}: {response.Responses[i].Exception?.Message}");
                    }
                }
            }

            Console.WriteLine($"Batch send completed. Success: {response.SuccessCount}, Failed: {response.FailureCount}");
            return response;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to send batch notification: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Topic ga notification yuborish
    /// </summary>
    public async Task<string> SendNotificationToTopicAsync(
        string topic,
        string title,
        string body,
        Dictionary<string, string>? customData = null)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            throw new ArgumentException("Topic cannot be empty");
        }

        // Topic format validation
        if (!topic.StartsWith("/topics/"))
        {
            topic = $"/topics/{topic}";
        }

        try
        {
            var message = new Message()
            {
                Topic = topic,
                Notification = new Notification()
                {
                    Title = title,
                    Body = body
                },
                Data = customData,
                Android = new AndroidConfig()
                {
                    Priority = Priority.High
                },
                Apns = new ApnsConfig()
                {
                    Aps = new Aps()
                    {
                        Alert = new ApsAlert()
                        {
                            Title = title,
                            Body = body
                        },
                        Sound = "default"
                    }
                }
            };

            string response = await _messaging.SendAsync(message);
            Console.WriteLine($"Successfully sent message to topic {topic}: {response}");
            return response;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to send topic notification: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Data message yuborish (background da ishlaydi)
    /// </summary>
    public async Task<string> SendDataMessageAsync(string deviceToken, Dictionary<string, string> data)
    {
        ValidateDeviceToken(deviceToken);

        if (data == null || !data.Any())
        {
            throw new ArgumentException("Data cannot be empty for data message");
        }

        try
        {
            var message = new Message()
            {
                Token = deviceToken,
                Data = data,

                // Android - high priority for background processing
                Android = new AndroidConfig()
                {
                    Priority = Priority.High,
                    TimeToLive = TimeSpan.FromHours(24)
                },

                // iOS - silent notification
                Apns = new ApnsConfig()
                {
                    Headers = new Dictionary<string, string>()
                {
                    { "apns-priority", "5" },
                    { "apns-push-type", "background" }
                },
                    Aps = new Aps()
                    {
                        ContentAvailable = true
                    }
                }
            };

            string response = await _messaging.SendAsync(message);
            Console.WriteLine($"Successfully sent data message: {response}");
            return response;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to send data message: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Subscribe devices to topic
    /// </summary>
    public async Task<TopicManagementResponse> SubscribeToTopicAsync(IEnumerable<string> deviceTokens, string topic)
    {
        var tokens = deviceTokens?.ToList();

        if (tokens == null || !tokens.Any())
        {
            throw new ArgumentException("Device tokens cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(topic))
        {
            throw new ArgumentException("Topic cannot be empty");
        }

        try
        {
            var response = await _messaging.SubscribeToTopicAsync(tokens, topic);
            Console.WriteLine($"Subscribe to topic {topic}. Success: {response.SuccessCount}, Failed: {response.FailureCount}");
            return response;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to subscribe to topic: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Unsubscribe devices from topic
    /// </summary>
    public async Task<TopicManagementResponse> UnsubscribeFromTopicAsync(IEnumerable<string> deviceTokens, string topic)
    {
        var tokens = deviceTokens?.ToList();

        if (tokens == null || !tokens.Any())
        {
            throw new ArgumentException("Device tokens cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(topic))
        {
            throw new ArgumentException("Topic cannot be empty");
        }

        try
        {
            var response = await _messaging.UnsubscribeFromTopicAsync(tokens, topic);
            Console.WriteLine($"Unsubscribe from topic {topic}. Success: {response.SuccessCount}, Failed: {response.FailureCount}");
            return response;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to unsubscribe from topic: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Helper method to validate device token
    /// </summary>
    private void ValidateDeviceToken(string deviceToken)
    {
        if (string.IsNullOrWhiteSpace(deviceToken))
        {
            throw new ArgumentException("Device token cannot be empty");
        }

        if (deviceToken.Length < 10)
        {
            throw new ArgumentException("Invalid device token format");
        }
    }

    /// <summary>
    /// Handle Firebase specific exceptions
    /// </summary>
    private void HandleFirebaseException(FirebaseMessagingException exception)
    {
        Console.WriteLine($"Firebase Messaging Error: {exception.MessagingErrorCode}");
        Console.WriteLine($"HTTP Response: {exception.HttpResponse?.StatusCode}");
        Console.WriteLine($"Error Details: {exception.Message}");

        // Token invalid bo'lsa, uni database dan o'chirish kerak bo'lishi mumkin
        if (exception.MessagingErrorCode == MessagingErrorCode.Unregistered ||
            exception.MessagingErrorCode == MessagingErrorCode.InvalidArgument)
        {
            Console.WriteLine("Token is invalid or unregistered. Consider removing it from database.");
        }
    }

    /// <summary>
    /// Get Firebase project ID
    /// </summary>
    public string GetProjectId() => _projectId;

    /// <summary>
    /// Check if Firebase is initialized
    /// </summary>
    public bool IsInitialized => FirebaseApp.DefaultInstance != null;
}
