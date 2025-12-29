using AutoMapper;
using Convoy.Data.DbContexts;
using Convoy.Data.IRepositories;
using Convoy.Domain.Entities;
using Convoy.Service.DTOs;
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

    public UserService(
        AppDbConText context,
        IRepository<User> userRepository,
        ILogger<UserService> logger,
        IMapper mapper)
    {
        _context = context;
        _userRepository = userRepository;
        _logger = logger;
        _mapper = mapper;
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

        // Total count
        var totalCount = await usersQuery.CountAsync();

        // Pagination
        var users = await usersQuery
            .OrderByDescending(u => u.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(u => new UserResponseDto
            {
                Id = u.Id,
                Name = u.Name,
                Phone = u.Phone,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            })
            .ToListAsync();

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
            .Where(u => u.IsActive)
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        return _mapper.Map<IEnumerable<UserResponseDto>>(users);
    }

    public async Task<UserResponseDto?> GetByIdAsync(long id)
    {
        var user = await _userRepository.SelectAsync(u => u.Id == id);

        if (user == null)
        {
            return null;
        }

        return _mapper.Map<UserResponseDto>(user);
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
        user.CreatedAt = DateTime.UtcNow;

        await _userRepository.InsertAsync(user);
        await _userRepository.SaveAsync();

        _logger.LogInformation("User created: {Username} (ID: {UserId})", user.Username, user.Id);

        return _mapper.Map<UserResponseDto>(user);
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

        if (updateDto.IsActive.HasValue)
        {
            user.IsActive = updateDto.IsActive.Value;
        }

        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.Update(user, id);
        await _userRepository.SaveAsync();

        _logger.LogInformation("User updated: {Username} (ID: {UserId})", user.Username, user.Id);

        return _mapper.Map<UserResponseDto>(user);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var user = await _userRepository.SelectAsync(u => u.Id == id);

        if (user == null)
        {
            return false;
        }

        // Soft delete
        user.DeletedAt = DateTime.UtcNow;
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.Update(user, id);
        await _userRepository.SaveAsync();

        _logger.LogInformation("User soft deleted: {Username} (ID: {UserId})", user.Username, user.Id);

        return true;
    }

    public async Task<bool> ExistsAsync(long id)
    {
        return await _context.Users.AnyAsync(u => u.Id == id);
    }
}
