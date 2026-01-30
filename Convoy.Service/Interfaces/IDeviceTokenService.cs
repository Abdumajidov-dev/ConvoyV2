using Convoy.Service.DTOs;

namespace Convoy.Service.Interfaces;

public interface IDeviceTokenService
{
    Task<bool> SaveOrUpdateDeviceTokenAsync(int supportId, DeviceInfoDto deviceInfo);
    Task<List<string>> GetActiveTokensBySupportIdAsync(int supportId);
    Task<bool> DeactivateTokenAsync(string token);
    Task<bool> DeactivateAllTokensBySupportIdAsync(int supportId);
    Task<DeviceTokenResultDto?> GetTokenByDeviceIdAsync(string deviceId);
    Task<List<DeviceTokenResultDto>> GetTokensBySupportIdAsync(int supportId);
}