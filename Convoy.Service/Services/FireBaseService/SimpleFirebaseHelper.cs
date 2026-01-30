using Convoy.Data.IRepositories;
using Convoy.Domain.Entities;
using Convoy.Service.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convoy.Service.Services.FireBaseService;

public class SimpleFirebaseHelper
{
    private readonly DirectFirebaseService _firebaseService;
    private readonly ILogger<SimpleFirebaseHelper> _logger;
    IRepository<DeviceToken> _deviceTokenRepository;

    public SimpleFirebaseHelper(
        DirectFirebaseService firebaseService,
        ILogger<SimpleFirebaseHelper> logger,
        IRepository<DeviceToken> deviceToken)
    {
        _firebaseService = firebaseService;
        _logger = logger;
        _deviceTokenRepository = deviceToken;
    }

    public async Task<bool> SendNotificationToSupport(NotifationCreateDto notifationCreateDto)
    {
        try
        {
            var token = await _deviceTokenRepository.SelectAll()
                            .Where(dt => dt.UserId == notifationCreateDto.UserId && dt.IsActive)
                            .FirstOrDefaultAsync();

            if (token == null)
            {
                _logger.LogWarning($"FCM token topilmadi: WorkerId {notifationCreateDto.UserId}");
                return false;
            }

            // Custom data qo'shish
            var customData = new Dictionary<string, string>
            {
                ["UserId"] = notifationCreateDto.UserId.ToString(),
                ["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                ["type"] = "support_notification"
            };

            // Notification yuborish
            await _firebaseService.SendNotificationAsync(token.Token, notifationCreateDto.Title, notifationCreateDto.Body);

            _logger.LogInformation($"Notification yuborildi: UserId {notifationCreateDto.UserId}");
            return true;
        }
        catch (FirebaseAdmin.Messaging.FirebaseMessagingException fme)
        {
            // Token invalid yoki unregistered bo'lsa, database'dan o'chirish
            if (fme.MessagingErrorCode == FirebaseAdmin.Messaging.MessagingErrorCode.Unregistered ||
                fme.MessagingErrorCode == FirebaseAdmin.Messaging.MessagingErrorCode.InvalidArgument)
            {
                var token = await _deviceTokenRepository.SelectAll()
                    .FirstOrDefaultAsync(dt => dt.UserId == notifationCreateDto.UserId && dt.IsActive);

                if (token != null)
                {
                    token.IsActive = false;
                    await _deviceTokenRepository.Update(token, token.Id);
                    _logger.LogWarning($"Yaroqsiz token deactivate qilindi: UserId {notifationCreateDto.UserId}, Token: {token.Token.Substring(0, 10)}...");
                }
            }

            _logger.LogError(fme, $"Firebase xatolik: WorkerId {notifationCreateDto.UserId}, ErrorCode: {fme.MessagingErrorCode}");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Notification yuborishda xatolik: WorkerId {notifationCreateDto.UserId}");
            return false;
        }
    }
}
