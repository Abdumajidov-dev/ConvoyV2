using AutoMapper;
using Convoy.Data.IRepositories;
using Convoy.Domain.Entities;
using Convoy.Service.DTOs;
using Convoy.Service.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convoy.Service.Services.DeviceTokens
{
    public class DeviceTokenService : IDeviceTokenService
    {
        private readonly IRepository<DeviceToken> _deviceTokenRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<DeviceTokenService> _logger;

        public DeviceTokenService(
            IRepository<DeviceToken> deviceTokenRepository,
            IMapper mapper,
            ILogger<DeviceTokenService> logger)
        {
            _deviceTokenRepository = deviceTokenRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<bool> SaveOrUpdateDeviceTokenAsync(int supportId, DeviceInfoDto deviceInfo)
        {
            try
            {
                var existingToken = await _deviceTokenRepository
                            .SelectAsync(dt => dt.DeviceId == deviceInfo.DeviceId && dt.UserId == supportId);

                if (existingToken != null)
                {
                    if (existingToken.Token == deviceInfo.DeviceToken)
                        return true;

                    // mavjud tokenni yangilash
                    existingToken.Token = deviceInfo.DeviceToken;
                    existingToken.DeviceSystem = deviceInfo.DeviceSystem ?? "sys";
                    existingToken.Model = deviceInfo.Model ?? "android";
                    existingToken.IsPhysicalDevice = true;
                   // existingToken.UpdatedAt = TimeHelper.GetCurrentServerTime().ToString();
                    existingToken.IsActive = true;

                    await _deviceTokenRepository.Update(existingToken, existingToken.Id);
                    await _deviceTokenRepository.SaveAsync();

                    _logger.LogInformation("Device token yangilandi. SupportId: {SupportId}, DeviceId: {DeviceId}",
                        supportId, deviceInfo.DeviceId);
                }
                else
                {
                    // yangi token yaratish
                    var newToken = new DeviceToken
                    {
                        UserId = supportId,
                        Token = deviceInfo.DeviceToken,
                        DeviceSystem = deviceInfo.DeviceSystem ?? "android",
                        Model = deviceInfo.Model ?? "android",
                        DeviceId = deviceInfo.DeviceId ?? "android",
                        IsPhysicalDevice = true,
                        IsActive = true,
                        //CreatedAt = TimeHelper.GetCurrentServerTime().ToString(),
                        //UpdatedAt = TimeHelper.GetCurrentServerTime().ToString(),
                    };

                    await _deviceTokenRepository.InsertAsync(newToken);
                    await _deviceTokenRepository.SaveAsync();

                    _logger.LogInformation("Yangi device token saqlandi. SupportId: {SupportId}, DeviceId: {DeviceId}",
                        supportId, deviceInfo.DeviceId);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Device token saqlashda xatolik. SupportId: {SupportId}", supportId);
                return false;
            }
        }

        public async Task<List<string>> GetActiveTokensBySupportIdAsync(int supportId)
        {
            try
            {
                return await _deviceTokenRepository
                    .SelectAll(dt => dt.UserId == supportId && dt.IsActive == true)
                    .Select(dt => dt.Token)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Active tokenlarni olishda xatolik. SupportId: {SupportId}", supportId);
                return new List<string>();
            }
        }

        public async Task<bool> DeactivateTokenAsync(string token)
        {
            try
            {
                var deviceToken = await _deviceTokenRepository.SelectAsync(dt => dt.Token == token);
                if (deviceToken != null)
                {
                    deviceToken.IsActive = false;
                   // deviceToken.UpdatedAt = TimeHelper.GetCurrentServerTime().ToString();

                    await _deviceTokenRepository.Update(deviceToken, deviceToken.Id);
                    await _deviceTokenRepository.SaveAsync();

                    _logger.LogInformation("Device token deaktivlashtirildi. Token: {Token}",
                        token.Substring(0, Math.Min(20, token.Length)) + "...");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Device token deaktivlashtirishda xatolik");
                return false;
            }
        }

        public async Task<bool> DeactivateAllTokensBySupportIdAsync(int supportId)
        {
            try
            {
                var tokens = await _deviceTokenRepository
                    .SelectAll(dt => dt.UserId == supportId && dt.IsActive == true)
                    .ToListAsync();

                foreach (var token in tokens)
                {
                    token.IsActive = false;
                   // token.UpdatedAt = TimeHelper.GetCurrentServerTime().ToString();
                }

                await _deviceTokenRepository.SaveAsync();

                _logger.LogInformation("Barcha device tokenlar deaktivlashtirildi. SupportId: {SupportId}, Count: {Count}",
                    supportId, tokens.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Barcha tokenlarni deaktivlashtirishda xatolik. SupportId: {SupportId}", supportId);
                return false;
            }
        }

        public async Task<DeviceTokenResultDto?> GetTokenByDeviceIdAsync(string deviceId)
        {
            try
            {
                var token = await _deviceTokenRepository
                    .SelectAsync(dt => dt.DeviceId == deviceId && dt.IsActive == true);

                return token != null ? _mapper.Map<DeviceTokenResultDto>(token) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Device ID bo'yicha token olishda xatolik. DeviceId: {DeviceId}", deviceId);
                return null;
            }
        }

        public async Task<List<DeviceTokenResultDto>> GetTokensBySupportIdAsync(int supportId)
        {
            try
            {
                var tokens = await _deviceTokenRepository
                    .SelectAll(dt => dt.UserId == supportId)
                    .ToListAsync();

                return _mapper.Map<List<DeviceTokenResultDto>>(tokens);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Support ID bo'yicha tokenlarni olishda xatolik. SupportId: {SupportId}", supportId);
                return new List<DeviceTokenResultDto>();
            }
        }
    }

}
