using Convoy.Service.DTOs;
using Convoy.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Convoy.Api.Controllers;

[ApiController]
[Route("api/branches")]
public class BranchController : ControllerBase
{
    private readonly IPhpApiService _phpApiService;
    private readonly ILogger<BranchController> _logger;

    public BranchController(IPhpApiService phpApiService, ILogger<BranchController> logger)
    {
        _phpApiService = phpApiService;
        _logger = logger;
    }

    /// <summary>
    /// PHP API dan filiallar ro'yxatini olish (search bilan)
    /// </summary>
    /// <remarks>
    /// Bu endpoint PHP API'ning branch-list endpoint'iga murojaat qiladi
    /// va filiallar ro'yxatini qaytaradi. Search parametri orqali filtrlash mumkin.
    ///
    /// **Authentication:** Talab qilinmaydi (public endpoint)
    ///
    /// **PHP API Endpoint:** POST /branch-list
    ///
    /// **Request Body:**
    /// ```json
    /// {
    ///   "search": "Наманган"
    /// }
    /// ```
    ///
    /// Bo'sh body yuborilsa, barcha filiallar qaytariladi.
    /// </remarks>
    /// <param name="request">Search request (optional search term)</param>
    /// <returns>Filiallar ro'yxati</returns>
    /// <response code="200">Filiallar muvaffaqiyatli olindi</response>
    /// <response code="500">Server xatosi</response>
    [HttpPost("branch-list")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBranchList([FromBody] BranchSearchRequest? request)
    {
        try
        {
            var searchTerm = request?.Search?.Trim();
            _logger.LogInformation("Branch list requested with search term: {SearchTerm}", searchTerm ?? "null");

            var branches = await _phpApiService.GetBranchesAsync(searchTerm);

            var message = branches.Any()
                ? (string.IsNullOrWhiteSpace(searchTerm)
                    ? $"{branches.Count} ta filial topildi"
                    : $"'{searchTerm}' bo'yicha {branches.Count} ta filial topildi")
                : (string.IsNullOrWhiteSpace(searchTerm)
                    ? "Filiallar topilmadi"
                    : $"'{searchTerm}' bo'yicha filial topilmadi");

            var response = new
            {
                status = true,
                message = message,
                data = branches
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting branch list");
            return StatusCode(500, new
            {
                status = false,
                message = "Filiallar ro'yxatini olishda xatolik yuz berdi",
                data = new List<object>()
            });
        }
    }
}
