using Convoy.Service.DTOs;
using Convoy.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Convoy.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UserController> _logger;

    public UserController(IUserService userService, ILogger<UserController> logger)
    {
        _userService = userService;
        _logger = logger;
    }
    ///<summary>
    /// User statusni o'zgartirish (aktiv/aktiv emas)
    ///</summary>
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> ChangeUserStatus(long id, [FromQuery] bool isActive)
    {
        try
        {
            var updateDto = new UpdateUserDto
            {
                IsActive = isActive
            };
            var user = await _userService.UpdateStatusAsync(id, isActive);
            return Ok(new
            {
                status = true,
                message = "User statusi muvaffaqiyatli o'zgartirildi",
                data = user
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                status = false,
                message = ex.Message,
                data = (object?)null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing user status: {UserId}", id);
            return StatusCode(500, new
            {
                status = false,
                message = "User statusini o'zgartirishda xatolik yuz berdi",
                data = (object?)null
            });
        }
    }

    /// <summary>
    /// Barcha user'larni olish (pagination bilan)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> GetAll([FromBody] UserQueryDto query)
    {
        try
        {
            var result = await _userService.GetAllUsersAsync(query);
            return Ok(new
            {
                status = true,
                message = $"{result.TotalCount} ta user topildi",
                data = result.Data,
                total_count = result.TotalCount,
                page = result.Page,
                page_size = result.PageSize,
                total_pages = result.TotalPages,
                has_next_page = result.HasNextPage,
                has_previous_page = result.HasPreviousPage
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users");
            return StatusCode(500, new
            {
                status = false,
                message = "Userlarni olishda xatolik yuz berdi",
                data = (object?)null
            });
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
            return Ok(new
            {
                status = true,
                message = $"{users.Count()} ta aktiv user topildi",
                data = users
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active users");
            return StatusCode(500, new
            {
                status = false,
                message = "Aktiv userlarni olishda xatolik yuz berdi",
                data = (object?)null
            });
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
                return NotFound(new
                {
                    status = false,
                    message = $"ID {id} bo'yicha user topilmadi",
                    data = (object?)null
                });
            }

            return Ok(new
            {
                status = true,
                message = "User topildi",
                data = user
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by ID: {UserId}", id);
            return StatusCode(500, new
            {
                status = false,
                message = "Userni olishda xatolik yuz berdi",
                data = (object?)null
            });
        }
    }

    /// <summary>
    /// Yangi user yaratish
    /// </summary>
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateUserDto createDto)
    {
        try
        {
            // Validation
            if (string.IsNullOrWhiteSpace(createDto.Name))
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Name majburiy",
                    data = (object?)null
                });
            }

            if (string.IsNullOrWhiteSpace(createDto.Username))
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Username majburiy",
                    data = (object?)null
                });
            }

            var user = await _userService.CreateAsync(createDto);

            var response = new
            {
                status = true,
                message = "User muvaffaqiyatli yaratildi",
                data = user
            };

            return CreatedAtAction(nameof(GetById), new { id = user.Id }, response);
        }
        catch (InvalidOperationException ex)
        {
            // Username or phone already exists
            return BadRequest(new
            {
                status = false,
                message = ex.Message,
                data = (object?)null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return StatusCode(500, new
            {
                status = false,
                message = "User yaratishda xatolik yuz berdi",
                data = (object?)null
            });
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
            return Ok(new
            {
                status = true,
                message = "User muvaffaqiyatli yangilandi",
                data = user
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                status = false,
                message = ex.Message,
                data = (object?)null
            });
        }
        catch (InvalidOperationException ex)
        {
            // Username or phone already exists
            return BadRequest(new
            {
                status = false,
                message = ex.Message,
                data = (object?)null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user: {UserId}", id);
            return StatusCode(500, new
            {
                status = false,
                message = "User yangilashda xatolik yuz berdi",
                data = (object?)null
            });
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
                return NotFound(new
                {
                    status = false,
                    message = $"ID {id} bo'yicha user topilmadi",
                    data = (object?)null
                });
            }

            return Ok(new
            {
                status = true,
                message = "User muvaffaqiyatli o'chirildi",
                data = new { user_id = id }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user: {UserId}", id);
            return StatusCode(500, new
            {
                status = false,
                message = "User o'chirishda xatolik yuz berdi",
                data = (object?)null
            });
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
            return Ok(new
            {
                status = true,
                message = exists ? "User mavjud" : "User mavjud emas",
                data = new { exists }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking user existence: {UserId}", id);
            return StatusCode(500, new
            {
                status = false,
                message = "User mavjudligini tekshirishda xatolik yuz berdi",
                data = (object?)null
            });
        }
    }
}
