using Convoy.Service.DTOs;
using Convoy.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Convoy.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UserController> _logger;

    public UserController(IUserService userService, ILogger<UserController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Barcha user'larni olish (pagination bilan)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] UserQueryDto query)
    {
        try
        {
            var result = await _userService.GetAllUsersAsync(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Aktiv user'larni olish
    /// </summary>
    [HttpGet("active")]
    public async Task<IActionResult> GetAllActive()
    {
        try
        {
            var users = await _userService.GetAllActiveUsersAsync();
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active users");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// User'ni ID bo'yicha olish
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id)
    {
        try
        {
            var user = await _userService.GetByIdAsync(id);

            if (user == null)
            {
                return NotFound(new { error = $"User with ID {id} not found" });
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by ID: {UserId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Yangi user yaratish
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserDto createDto)
    {
        try
        {
            // Validation
            if (string.IsNullOrWhiteSpace(createDto.Name))
            {
                return BadRequest(new { error = "Name is required" });
            }

            if (string.IsNullOrWhiteSpace(createDto.Username))
            {
                return BadRequest(new { error = "Username is required" });
            }

            var user = await _userService.CreateAsync(createDto);
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
        }
        catch (InvalidOperationException ex)
        {
            // Username or phone already exists
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// User'ni yangilash
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateUserDto updateDto)
    {
        try
        {
            var user = await _userService.UpdateAsync(id, updateDto);
            return Ok(user);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            // Username or phone already exists
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user: {UserId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// User'ni o'chirish (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id)
    {
        try
        {
            var result = await _userService.DeleteAsync(id);

            if (!result)
            {
                return NotFound(new { error = $"User with ID {id} not found" });
            }

            return Ok(new { message = "User deleted successfully", userId = id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user: {UserId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// User mavjudligini tekshirish
    /// </summary>
    [HttpGet("{id}/exists")]
    public async Task<IActionResult> Exists(long id)
    {
        try
        {
            var exists = await _userService.ExistsAsync(id);
            return Ok(new { exists });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking user existence: {UserId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
