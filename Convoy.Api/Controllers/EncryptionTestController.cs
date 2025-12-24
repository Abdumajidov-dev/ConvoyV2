using Convoy.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Convoy.Api.Controllers;

[ApiController]
[Route("api/encryption-test")]
public class EncryptionTestController : ControllerBase
{
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<EncryptionTestController> _logger;

    public EncryptionTestController(
        IEncryptionService encryptionService,
        ILogger<EncryptionTestController> logger)
    {
        _encryptionService = encryptionService;
        _logger = logger;
    }

    /// <summary>
    /// Test encryption service directly (returns encrypted string)
    /// </summary>
    [HttpPost("encrypt")]
    public IActionResult Encrypt([FromBody] TestRequest request)
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(request);
            var encrypted = _encryptionService.Encrypt(json);

            _logger.LogInformation("Encrypted test data. Length: {Length}", encrypted.Length);

            return Content(encrypted, "text/plain");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Encryption test failed");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Test decryption service (accepts encrypted string in body)
    /// </summary>
    [HttpPost("decrypt")]
    public IActionResult Decrypt()
    {
        try
        {
            using var reader = new StreamReader(Request.Body);
            var encryptedData = reader.ReadToEndAsync().Result.Trim();

            _logger.LogInformation("Attempting to decrypt. Length: {Length}", encryptedData.Length);

            var decrypted = _encryptionService.Decrypt(encryptedData);

            return Content(decrypted, "application/json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Decryption test failed");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Check encryption status
    /// </summary>
    [HttpGet("status")]
    public IActionResult Status()
    {
        return Ok(new
        {
            encryption_enabled = _encryptionService.IsEnabled,
            message = _encryptionService.IsEnabled
                ? "Encryption is ENABLED"
                : "Encryption is DISABLED"
        });
    }

    /// <summary>
    /// Echo test - returns same data (goes through encryption middleware if enabled)
    /// </summary>
    [HttpPost("echo")]
    public IActionResult Echo([FromBody] TestRequest request)
    {
        return Ok(new
        {
            status = true,
            message = "Echo successful",
            data = request
        });
    }
}

public class TestRequest
{
    public string Message { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
}
