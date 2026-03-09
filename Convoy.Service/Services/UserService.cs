using AutoMapper;
using Convoy.Data.DbContexts;
using Convoy.Data.IRepositories;
using Convoy.Domain.Entities;
using Convoy.Service.DTOs;
using Convoy.Service.Extensions;
using Convoy.Service.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Convoy.Service.Services;

public class UserService : IUserService
{
    private readonly AppDbConText _context;
    private readonly IRepository<User> _userRepository;
    private readonly ILogger<UserService> _logger;
    private readonly IMapper _mapper;
    private readonly IPhpApiService _phpApiService;
    private readonly ILocationService _locationService;
    private readonly IRepository<UserStatusReport> _userStatusReportRepository;

    public UserService(
        IRepository<UserStatusReport> userStatusReportRepository,
        AppDbConText context,
        IRepository<User> userRepository,
        ILogger<UserService> logger,
        IMapper mapper,
        IPhpApiService phpApiService,
        ILocationService locationService)
    {
        _context = context;
        _userStatusReportRepository = userStatusReportRepository;
        _userRepository = userRepository;
        _logger = logger;
        _mapper = mapper;
        _phpApiService = phpApiService;
        _locationService = locationService;
    }

    public async Task<PaginatedResponse<UserResponseDto>> GetAllUsersAsync(UserQueryDto query)
    {
        var usersQuery = _context.Users.AsQueryable();

        // Search filter
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var searchLower = query.SearchTerm.ToLower();
            usersQuery = usersQuery.Where(u =>
                u.Name.ToLower().Contains(searchLower) ||
                u.Username.ToLower().Contains(searchLower) ||
                (u.Phone != null && u.Phone.Contains(query.SearchTerm)));
        }

        // IsActive filter (string: "true", "false", null)
        bool? isActiveFilter = null;
        if (!string.IsNullOrWhiteSpace(query.IsActive))
        {
            if (query.IsActive.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                isActiveFilter = true;
                usersQuery = usersQuery.Where(u => u.IsActive == true);
            }
            else if (query.IsActive.Equals("false", StringComparison.OrdinalIgnoreCase))
            {
                isActiveFilter = false;
                usersQuery = usersQuery.Where(u => u.IsActive == false);
            }
            // null bo'lsa barcha userlar (filter qo'llanmaydi)
        }

        // BranchGuid filter
        if (!string.IsNullOrWhiteSpace(query.BranchGuid))
        {
            usersQuery = usersQuery.Where(u => u.BranchGuid == query.BranchGuid);
        }

        // is_stopped filter (string: "true", "false", null)
        bool? isStoppedFilter = null;
        if (!string.IsNullOrWhiteSpace(query.IsStopped))
        {
            if (query.IsStopped.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                isStoppedFilter = true;
            }
            else if (query.IsStopped.Equals("false", StringComparison.OrdinalIgnoreCase))
            {
                isStoppedFilter = false;
            }
            // null bo'lsa barcha userlar (filter qo'llanmaydi)
        }

        // is_stopped filter - GetFilteredUserIdsAsync'dan foydalanish
        if (isStoppedFilter.HasValue)
        {
            // Parse date (default: bugungi kun)
            DateTime checkDate = DateTime.UtcNow.Date; // Default: bugun
            if (!string.IsNullOrWhiteSpace(query.Date))
            {
                try
                {
                    checkDate = query.Date.ParseToApplicationTime();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse date: {Date}, using today", query.Date);
                }
            }

            // Time range filtering
            DateTime? startDateTime = null;
            DateTime? endDateTime = null;

            if (!string.IsNullOrWhiteSpace(query.StartHour))
            {
                try
                {
                    var timeParts = query.StartHour.Split(':');
                    if (timeParts.Length == 2)
                    {
                        var hour = int.Parse(timeParts[0]);
                        var minute = int.Parse(timeParts[1]);
                        startDateTime = new DateTime(checkDate.Year, checkDate.Month, checkDate.Day, hour, minute, 0, DateTimeKind.Utc);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse start_hour: {StartHour}", query.StartHour);
                }
            }

            if (!string.IsNullOrWhiteSpace(query.EndHour))
            {
                try
                {
                    var timeParts = query.EndHour.Split(':');
                    if (timeParts.Length == 2)
                    {
                        var hour = int.Parse(timeParts[0]);
                        var minute = int.Parse(timeParts[1]);
                        endDateTime = new DateTime(checkDate.Year, checkDate.Month, checkDate.Day, hour, minute, 0, DateTimeKind.Utc);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse end_hour: {EndHour}", query.EndHour);
                }
            }

            // GetFilteredUserIdsAsync orqali to'xtab turgan/harakat qilayotgan userlarni olish
            var filteredUserIds = await GetFilteredUserIdsAsync(
                isActiveFilter,
                isStoppedFilter,
                query.MinStoppedMinutes ?? 60, // Default: 1 soat
                checkDate,
                startDateTime,
                endDateTime,
                query.BranchGuid);

            if (filteredUserIds.Any())
            {
                // Faqat filtered userlarni qoldirish
                var filteredUserIdsSet = filteredUserIds.ToHashSet();
                usersQuery = usersQuery.Where(u => u.UserId.HasValue && filteredUserIdsSet.Contains((int)u.UserId.Value));
            }
            else
            {
                // Agar hech kim yo'q bo'lsa - bo'sh natija
                usersQuery = usersQuery.Where(u => false); // Empty result
            }
        }

        // Total count (har doim to'liq)
        var totalCount = await usersQuery.CountAsync();

        // Pagination faqat Page > 0 va PageSize > 0 bo‘lsa
        if (query.Page > 0 && query.PageSize > 0)
        {
            usersQuery = usersQuery
                .OrderByDescending(u => u.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize);
        }
        else
        {
            // Pagination yo‘q → faqat sort
            usersQuery = usersQuery
                .OrderByDescending(u => u.CreatedAt);
        }

        var users = await usersQuery
            .Select(u => new UserResponseDto
            {
                Id = u.Id,
                UserId = (int)u.UserId,
                Name = u.Name,
                Phone = u.Phone,
                BranchGuid = u.BranchGuid,
                IsActive = u.IsActive,
                Image = u.Image,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            })
            .ToListAsync();

        // Branch ma'lumotlarini olish (agar branch_guid'lar mavjud bo'lsa)
        var branchGuids = users
            .Where(u => !string.IsNullOrWhiteSpace(u.BranchGuid))
            .Select(u => u.BranchGuid!)
            .Distinct()
            .ToList();

        if (branchGuids.Any())
        {
            try
            {
                // PHP API dan barcha branch'larni olish
                var branches = await _phpApiService.GetBranchesAsync();

                // Branch'larni GUID bo'yicha dictionary'ga joylash
                var branchDict = branches
                    .Where(b => !string.IsNullOrWhiteSpace(b.Code))  // Code = GUID
                    .ToDictionary(b => b.Code!, b => b);

                // Har bir user'ga branch ma'lumotini qo'shish
                foreach (var user in users)
                {
                    if (!string.IsNullOrWhiteSpace(user.BranchGuid) &&
                        branchDict.TryGetValue(user.BranchGuid, out var branch))
                    {
                        user.Branch = branch;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Branch ma'lumotlarini olishda xatolik. User'lar branch'siz qaytariladi.");
                // Branch olishda xatolik bo'lsa ham, user'larni qaytaramiz (Branch = null)
            }
        }

        // Barcha userlarning oxirgi location'larini olish
        try
        {
            var locationsResult = await _locationService.GetAllUsersLatestLocationsAsync();
            if (locationsResult.Success && locationsResult.Data != null)
            {
                // Location'larni user_id bo'yicha dictionary'ga joylash
                var locationDict = locationsResult.Data.ToDictionary(l => l.UserId, l => l);

                // Har bir user'ga latest location'ni qo'shish
                foreach (var user in users)
                {
                    if (locationDict.TryGetValue((int)user.Id, out var location))
                    {
                        user.LatestLocation = location;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Latest location'larni olishda xatolik. User'lar location'siz qaytariladi.");
            // Location olishda xatolik bo'lsa ham, user'larni qaytaramiz (LatestLocation = null)
        }

        return new PaginatedResponse<UserResponseDto>
        {
            Data = users,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<IEnumerable<UserResponseDto>> GetAllActiveUsersAsync()
    {
        var users = await _context.Users
            //.Where(u => u.IsActive)
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        var userDtos = _mapper.Map<List<UserResponseDto>>(users);

        // Barcha userlarning oxirgi location'larini olish
        try
        {
            var locationsResult = await _locationService.GetAllUsersLatestLocationsAsync();
            if (locationsResult.Success && locationsResult.Data != null)
            {
                // Location'larni user_id bo'yicha dictionary'ga joylash
                var locationDict = locationsResult.Data.ToDictionary(l => l.UserId, l => l);

                // Har bir user'ga latest location'ni qo'shish
                foreach (var user in userDtos)
                {
                    if (locationDict.TryGetValue((int)user.Id, out var location))
                    {
                        user.LatestLocation = location;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Latest location'larni olishda xatolik. User'lar location'siz qaytariladi.");
            // Location olishda xatolik bo'lsa ham, user'larni qaytaramiz (LatestLocation = null)
        }

        return userDtos;
    }

    public async Task<UserResponseDto?> GetByIdAsync(long id)
    {
        var user = await _userRepository.SelectAsync(u => u.Id == id);

        if (user == null)
        {
            return null;
        }

        var userDto = _mapper.Map<UserResponseDto>(user);

        // User'ning oxirgi location'ini olish
        try
        {
            var locationsResult = await _locationService.GetLastLocationsAsync((int)id, 1);
            if (locationsResult.Success && locationsResult.Data != null && locationsResult.Data.Any())
            {
                userDto.LatestLocation = locationsResult.Data.First();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Latest location'ni olishda xatolik (UserId={UserId}). User location'siz qaytariladi.", id);
            // Location olishda xatolik bo'lsa ham, user'ni qaytaramiz (LatestLocation = null)
        }

        return userDto;
    }

    public async Task<UserResponseDto> CreateAsync(CreateUserDto createDto)
    {
        // Check if username already exists
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == createDto.Username);

        if (existingUser != null)
        {
            throw new InvalidOperationException($"Username '{createDto.Username}' already exists");
        }

        // Check if phone already exists (if provided)
        if (!string.IsNullOrEmpty(createDto.Phone))
        {
            var phoneExists = await _context.Users
                .AnyAsync(u => u.Phone == createDto.Phone);

            if (phoneExists)
            {
                throw new InvalidOperationException($"Phone number '{createDto.Phone}' already exists");
            }
        }

        var user = _mapper.Map<User>(createDto);
        user.CreatedAt = DateTimeExtensions.NowInApplicationTime();

        await _userRepository.InsertAsync(user);
        await _userRepository.SaveAsync();

        _logger.LogInformation("User created: {Username} (ID: {UserId})", user.Username, user.Id);

        var userDto = _mapper.Map<UserResponseDto>(user);
        // Yangi user'da hali location yo'q, shuning uchun LatestLocation = null bo'ladi

        return userDto;
    }

    public async Task<UserResponseDto> UpdateAsync(long id, UpdateUserDto updateDto)
    {
        var user = await _userRepository.SelectAsync(u => u.Id == id);

        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {id} not found");
        }

        // Check username uniqueness if being updated
        if (!string.IsNullOrEmpty(updateDto.Username) && updateDto.Username != user.Username)
        {
            var usernameExists = await _context.Users
                .AnyAsync(u => u.Username == updateDto.Username && u.Id != id);

            if (usernameExists)
            {
                throw new InvalidOperationException($"Username '{updateDto.Username}' already exists");
            }

            user.Username = updateDto.Username;
        }

        // Check phone uniqueness if being updated
        if (updateDto.Phone != null && updateDto.Phone != user.Phone)
        {
            if (!string.IsNullOrEmpty(updateDto.Phone))
            {
                var phoneExists = await _context.Users
                    .AnyAsync(u => u.Phone == updateDto.Phone && u.Id != id);

                if (phoneExists)
                {
                    throw new InvalidOperationException($"Phone number '{updateDto.Phone}' already exists");
                }
            }

            user.Phone = updateDto.Phone;
        }

        // Update other fields
        if (!string.IsNullOrEmpty(updateDto.Name))
        {
            user.Name = updateDto.Name;
        }

        if (updateDto.Image != null)
        {
            user.Image = updateDto.Image;
        }

        if (updateDto.IsActive.HasValue)
        {
            user.IsActive = updateDto.IsActive.Value;
        }

        user.UpdatedAt = DateTimeExtensions.NowInApplicationTime();

        await _userRepository.Update(user, id);
        await _userRepository.SaveAsync();

        _logger.LogInformation("User updated: {Username} (ID: {UserId})", user.Username, user.Id);

        var userDto = _mapper.Map<UserResponseDto>(user);

        // User'ning oxirgi location'ini olish
        try
        {
            var locationsResult = await _locationService.GetLastLocationsAsync((int)id, 1);
            if (locationsResult.Success && locationsResult.Data != null && locationsResult.Data.Any())
            {
                userDto.LatestLocation = locationsResult.Data.First();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Latest location'ni olishda xatolik (UserId={UserId}). User location'siz qaytariladi.", id);
            // Location olishda xatolik bo'lsa ham, user'ni qaytaramiz (LatestLocation = null)
        }

        return userDto;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var user = await _userRepository.SelectAsync(u => u.Id == id);

        if (user == null)
        {
            return false;
        }

        // Soft delete
        user.DeletedAt = DateTimeExtensions.NowInApplicationTime();
        user.IsActive = false;
        user.UpdatedAt = DateTimeExtensions.NowInApplicationTime();

        await _userRepository.Update(user, id);
        await _userRepository.SaveAsync();

        _logger.LogInformation("User soft deleted: {Username} (ID: {UserId})", user.Username, user.Id);

        return true;
    }

    public async Task<bool> ExistsAsync(long id)
    {
        return await _context.Users.AnyAsync(u => u.Id == id);
    }

    /// <summary>
    /// Branch GUID bo'yicha userlarning ID'larini olish
    /// </summary>
    public async Task<List<int>> GetUserIdsByBranchGuidAsync(string branchGuid)
    {
        var userIds = await _context.Users
            .Where(u => u.BranchGuid == branchGuid && u.IsActive)
            .Select(u => (int)u.Id)
            .ToListAsync();

        _logger.LogInformation("Found {Count} users for BranchGuid={BranchGuid}", userIds.Count, branchGuid);

        return userIds;
    }
    public async Task<bool> UpdateStatusAsync(long userId, bool isActive)
    {
        try
        {
            var result = await _userRepository.SelectAsync(u => u.UserId == (long)userId);
            if (result is null)
                throw new CustomException(404, "User not found");

            // User'ning IsActive statusni yangilash
            result.IsActive = isActive;
            await _userRepository.Update(result, result.Id);
            await _userRepository.SaveAsync();

            // ✅ HISTORY: User o'zi status o'zgartirdi - yangi qator qo'shish
            await AddUserStatusReportHistoryAsync(userId, isActive);

            return true;
        }
        catch(Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// User is_active o'zgarganda user_status_reports table'ga yangi qator qo'shish (history)
    /// </summary>
    private async Task AddUserStatusReportHistoryAsync(long userId, bool isActive)
    {
        try
        {
            var statusChangeType = isActive ? "came_to_work" : "left_work";

            var report = new UserStatusReport
            {
                UserId = userId,
                LastLocationTime = DateTime.UtcNow,
                LastNotifiedAt = null,
                OfflineDurationMinutes = 0,
                IsNotified = false,
                NotificationCount = 0,
                IsActive = isActive,
                StatusChangeType = statusChangeType,
                Note = null,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserStatusReports.Add(report);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "📝 User status history added: UserId={UserId}, IsActive={IsActive}, Type={Type}",
                userId, isActive, statusChangeType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding user status report history for UserId={UserId}", userId);
        }
    }

    /// <summary>
    /// PHP API worker_id (user_id) bo'yicha user'ni topish
    /// </summary>
    public async Task<User?> GetByUserIdAsync(int userId)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == userId);
    }

    /// <summary>
    /// PHP API worker_id (user_id) bo'yicha user DTO'sini olish
    /// Multiple users locations uchun kerak
    /// </summary>
    public async Task<UserResponseDto?> GetByUserIdDtoAsync(int userId)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            return null;
        }

        return _mapper.Map<UserResponseDto>(user);
    }

    /// <summary>
    /// Filter bo'yicha userlarni olish (is_active, is_stopped, date, time range)
    /// Multiple users location query uchun
    /// UserStoppedReport'larni ham hisobga oladi
    /// </summary>
    public async Task<List<int>> GetFilteredUserIdsAsync(
        bool? isActive,
        bool? isStopped,
        int minStoppedMinutes,
        DateTime checkDate,
        DateTime? startTime,
        DateTime? endTime,
        string? branchGuid)
    {
        var query = _context.Users.AsQueryable();

        // 1. is_active filter
        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        // 2. branch_guid filter
        if (!string.IsNullOrWhiteSpace(branchGuid))
        {
            query = query.Where(u => u.BranchGuid == branchGuid);
        }

        var users = await query.ToListAsync();
        var userIds = users.Select(u => (int)u.UserId!).ToList();

        // 3. is_stopped filter (to'xtab turgan userlar)
        if (isStopped.HasValue && userIds.Any())
        {
            // User status reports'dan so'nggi location vaqtini olish
            var longUserIds = userIds.Select(id => (long)id).ToList();
            var userStatusReports = await _context.UserStatusReports
                .Where(usr => longUserIds.Contains(usr.UserId))
                .ToListAsync();

            // Aktiv stopped report'larga ega userlarni olish
            var usersWithStoppedReports = await _context.UserStoppedReports
                .Where(r => userIds.Contains(r.UserId) && !r.IsResolved)
                .Select(r => r.UserId)
                .Distinct()
                .ToListAsync();

            var stoppedUserIds = new List<int>();

            foreach (var userId in userIds)
            {
                // Birinchi stopped report'ni tekshirish
                var hasActiveStoppedReport = usersWithStoppedReports.Contains(userId);

                var statusReport = userStatusReports.FirstOrDefault(usr => usr.UserId == (long)userId);

                if (statusReport != null && statusReport.LastLocationTime.HasValue)
                {
                    var lastLocationTime = statusReport.LastLocationTime.Value;

                    // Time range filter (agar start_hour va end_hour berilgan bo'lsa)
                    if (startTime.HasValue || endTime.HasValue)
                    {
                        // Faqat berilgan soat oralig'idagi locationlarni tekshirish
                        if (startTime.HasValue && lastLocationTime < startTime.Value)
                        {
                            continue; // Bu user soat oralig'ida emas
                        }

                        if (endTime.HasValue && lastLocationTime > endTime.Value)
                        {
                            continue; // Bu user soat oralig'ida emas
                        }
                    }

                    var offlineDuration = (DateTime.UtcNow - lastLocationTime).TotalMinutes;

                    if (isStopped.Value)
                    {
                        // is_stopped = true: faqat to'xtab turganlar
                        // 1. Offline >= minStoppedMinutes
                        // 2. Yoki aktiv stopped report bor
                        if (offlineDuration >= minStoppedMinutes || hasActiveStoppedReport)
                        {
                            stoppedUserIds.Add(userId);
                        }
                    }
                    else
                    {
                        // is_stopped = false: faqat harakat qilayotganlar
                        // 1. Offline < minStoppedMinutes
                        // 2. Va aktiv stopped report yo'q
                        if (offlineDuration < minStoppedMinutes && !hasActiveStoppedReport)
                        {
                            stoppedUserIds.Add(userId);
                        }
                    }
                }
                else
                {
                    // Agar status report yo'q bo'lsa
                    if (isStopped.Value)
                    {
                        // Stopped deb hisoblash (hech qachon location yubormagan yoki stopped report bor)
                        stoppedUserIds.Add(userId);
                    }
                    else if (!hasActiveStoppedReport)
                    {
                        // Harakat qilayotgan deb hisoblash (agar stopped report yo'q bo'lsa)
                        stoppedUserIds.Add(userId);
                    }
                }
            }

            userIds = stoppedUserIds;
        }

        _logger.LogInformation(
            "Filtered users: is_active={IsActive}, is_stopped={IsStopped}, min_stopped={MinStopped}min, " +
            "date={Date}, start_time={StartTime}, end_time={EndTime}, branch={Branch}, result_count={Count}",
            isActive, isStopped, minStoppedMinutes, checkDate, startTime, endTime, branchGuid, userIds.Count);

        return userIds;
    }

    /// <summary>
    /// User entity yaratish (AuthService'dan)
    /// </summary>
    public async Task CreateAsync(User user)
    {
        await _userRepository.InsertAsync(user);
        await _userRepository.SaveAsync();

        _logger.LogInformation("User created with user_id={UserId}, Name={Name}",
            user.UserId, user.Name);
    }

    /// <summary>
    /// User entity yangilash (AuthService'dan)
    /// </summary>
    public async Task UpdateAsync(long id, User user)
    {
        // Find existing user
        var existingUser = await _context.Users.FindAsync(id);
        if (existingUser == null)
        {
            throw new InvalidOperationException($"User with id {id} not found");
        }

        // Update fields
        existingUser.Name = user.Name;
        existingUser.Phone = user.Phone;
        existingUser.WorkerGuid = user.WorkerGuid;
        existingUser.BranchGuid = user.BranchGuid;
        existingUser.PositionId = user.PositionId;
        existingUser.Image = user.Image;
        existingUser.IsActive = user.IsActive;

        await _context.SaveChangesAsync();

        _logger.LogInformation("User updated with user_id={UserId}, Name={Name}",
            user.UserId, user.Name);
    }

    /// <summary>
    /// User'ni active/inactive qilish (SignalR connection/disconnection uchun)
    /// user_id (PHP worker_id) bo'yicha topiladi
    /// </summary>
    public async Task SetUserActiveStatusAsync(int userId, bool isActive)
    {
        try
        {
            // user_id (PHP worker_id) bo'yicha user topish
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                _logger.LogWarning("User not found with user_id={UserId} for active status update", userId);
                return;
            }

            // Status o'zgarmagan bo'lsa, skip qilish
            if (user.IsActive == isActive)
            {
                _logger.LogDebug("User user_id={UserId} already has IsActive={IsActive}, skipping update",
                    userId, isActive);
                return;
            }

            // Status yangilash
            user.IsActive = isActive;
            user.UpdatedAt = DateTimeExtensions.NowInApplicationTime();

            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ User active status updated: user_id={UserId}, Name={Name}, IsActive={IsActive}",
                userId, user.Name, isActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating active status for user_id={UserId}", userId);
            throw;
        }
    }

    public Task<PaginatedResponse<UserResponseDto>> ActiveOrNoActiveUsers()
    {

        throw new NotImplementedException();
    }

    /// <summary>
    /// User statistikasi: jami, active va inactive userlar soni
    /// </summary>
    public async Task<UserStatisticsDto> GetUserStatisticsAsync()
    {
        try
        {
            // Barcha userlar sonini olish
            var totalUsers = await _context.Users.CountAsync();

            // Active userlar sonini olish
            var activeUsers = await _context.Users
                .Where(u => u.IsActive == true)
                .CountAsync();

            // Inactive userlar sonini olish
            var inactiveUsers = await _context.Users
                .Where(u => u.IsActive == false)
                .CountAsync();

            var statistics = new UserStatisticsDto
            {
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                InactiveUsers = inactiveUsers
            };

            _logger.LogInformation(
                "📊 User statistikasi: Jami={Total}, Active={Active}, Inactive={Inactive}",
                totalUsers, activeUsers, inactiveUsers);

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user statistics");
            throw;
        }
    }
}
