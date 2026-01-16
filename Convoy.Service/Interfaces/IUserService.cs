using Convoy.Service.DTOs;

namespace Convoy.Service.Interfaces;

public interface IUserService
{
    Task<PaginatedResponse<UserResponseDto>> GetAllUsersAsync(UserQueryDto query);
    Task<IEnumerable<UserResponseDto>> GetAllActiveUsersAsync();
    Task<UserResponseDto?> GetByIdAsync(long id);
    Task<UserResponseDto> CreateAsync(CreateUserDto createDto);
    Task<UserResponseDto> UpdateAsync(long id, UpdateUserDto updateDto);
    Task<bool> DeleteAsync(long id);
    Task<bool> ExistsAsync(long id);
    Task<bool> UpdateStatusAsync(long id, bool isActive);

    /// <summary>
    /// Branch GUID bo'yicha userlarning ID'larini olish
    /// </summary>
    Task<List<int>> GetUserIdsByBranchGuidAsync(string branchGuid);
}
