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

    /// <summary>
    /// PHP API worker_id (user_id) bo'yicha user'ni topish
    /// User sync uchun kerak
    /// </summary>
    Task<Domain.Entities.User?> GetByUserIdAsync(int userId);

    /// <summary>
    /// User entity yaratish (AuthService'dan kerak)
    /// </summary>
    Task CreateAsync(Domain.Entities.User user);

    /// <summary>
    /// User entity yangilash (AuthService'dan kerak)
    /// </summary>
    Task UpdateAsync(long id, Domain.Entities.User user);

    /// <summary>
    /// User'ni active/inactive qilish (SignalR connection/disconnection uchun)
    /// user_id (PHP worker_id) bo'yicha topiladi
    /// </summary>
    Task SetUserActiveStatusAsync(int userId, bool isActive);
}
