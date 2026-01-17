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

        // IsActive filter
        if (query.IsActive.HasValue)
        {
            usersQuery = usersQuery.Where(u => u.IsActive == query.IsActive.Value);
        }

        // BranchGuid filter
        if (!string.IsNullOrWhiteSpace(query.BranchGuid))
        {
            usersQuery = usersQuery.Where(u => u.BranchGuid == query.BranchGuid);
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
                Name = u.Name,
                Phone = u.Phone,
                BranchGuid = u.BranchGuid,
                IsActive = u.IsActive,
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
    public async Task<bool> UpdateStatusAsync(long userId,bool isActive)
    {
        try
        {
            var result = await _userRepository.SelectAsync(u => u.Id == userId);
            if (result is null)
                throw new CustomException(404, "User not found");
            var newReport = new UserStatusReport{
                UserId = userId,
                Status = isActive,
                CreatedAt = DateTimeExtensions.NowInApplicationTime()
            };
            var resultReport = await _userStatusReportRepository.InsertAsync(newReport);
            if(resultReport is null)
                throw new CustomException(500, "Could not create status report");
            result.IsActive = isActive;
            await _userStatusReportRepository.SaveAsync();
            return true;

        }
        catch(Exception ex)
        {
            return false;
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
    /// User entity yaratish (AuthService'dan)
    /// </summary>
    public async Task CreateAsync(User user)
    {
        await _userRepository.CreateAsync(user);
        await _userRepository.SaveAsync();

        _logger.LogInformation("User created with user_id={UserId}, Name={Name}",
            user.UserId, user.Name);
    }

    /// <summary>
    /// User entity yangilash (AuthService'dan)
    /// </summary>
    public async Task UpdateAsync(long id, User user)
    {
        var existingUser = await _userRepository.GetByIdAsync(id);
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

        await _userRepository.SaveAsync();

        _logger.LogInformation("User updated with user_id={UserId}, Name={Name}",
            user.UserId, user.Name);
    }
}
