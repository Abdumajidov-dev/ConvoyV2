using Convoy.Service.Interfaces;
using System.Text;
using System.Text.Json;

namespace Convoy.Api.Middleware;

/// <summary>
/// Middleware for automatic request/response encryption
/// Request body ni decrypt qiladi va response body ni encrypt qiladi
/// </summary>
public class EncryptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<EncryptionMiddleware> _logger;

    public EncryptionMiddleware(RequestDelegate next, ILogger<EncryptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IEncryptionService encryptionService)
    {
        if (!encryptionService.IsEnabled)
        {
            // Encryption o'chirilgan bo'lsa, oddiy request/response
            await _next(context);
            return;
        }

        // Faqat POST/PUT/PATCH request larda body ni decrypt qilish
        if (context.Request.Method == "POST" ||
            context.Request.Method == "PUT" ||
            context.Request.Method == "PATCH")
        {
            await DecryptRequestAsync(context, encryptionService);
        }

        // Response ni encrypt qilish uchun response body ni capture qilish
        var originalBodyStream = context.Response.Body;

        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        try
        {
            await _next(context);

            // Response ni encrypt qilish
            await EncryptResponseAsync(context, encryptionService, originalBodyStream);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private async Task DecryptRequestAsync(HttpContext context, IEncryptionService encryptionService)
    {
        context.Request.EnableBuffering();

        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
        var requestBody = await reader.ReadToEndAsync();

        if (string.IsNullOrEmpty(requestBody))
        {
            context.Request.Body.Position = 0;
            return;
        }

        // Check if this looks like encrypted data (Base64) or plain JSON
        var trimmedBody = requestBody.Trim();

        // Agar Content-Type text/plain bo'lsa, bu encrypted data (decrypt kerak)
        var isEncryptedContentType = context.Request.ContentType?.Contains("text/plain") == true;

        // Agar JSON bilan boshlansa VA Content-Type json bo'lsa, skip decryption
        var isPlainJson = (trimmedBody.StartsWith("{") || trimmedBody.StartsWith("[")) &&
                          context.Request.ContentType?.Contains("application/json") == true;

        if (isPlainJson)
        {
            _logger.LogDebug("Request is plain JSON, skipping decryption for {Path}", context.Request.Path);
            context.Request.Body.Position = 0;
            return;
        }

        // Try to decrypt (it should be Base64)
        try
        {
            // Clean up the encrypted string (remove whitespace, quotes, newlines)
            var cleanedEncrypted = trimmedBody
                .Trim('"')
                .Replace("\n", "")
                .Replace("\r", "")
                .Replace(" ", "")
                .Replace("\t", "");

            _logger.LogInformation("🔐 Attempting to decrypt request for {Path}. Length: {Length}",
                context.Request.Path, cleanedEncrypted.Length);

            // Decrypt the request
            var decryptedJson = encryptionService.Decrypt(cleanedEncrypted);

            _logger.LogInformation("✅ Request decrypted successfully for {Path}. Decrypted: {Json}",
                context.Request.Path, decryptedJson);

            // Write decrypted data to request body
            var decryptedBytes = Encoding.UTF8.GetBytes(decryptedJson);
            var bodyStream = new MemoryStream(decryptedBytes);
            bodyStream.Position = 0;  // Stream boshidan o'qish uchun

            context.Request.Body = bodyStream;
            context.Request.ContentLength = decryptedBytes.Length;

            // ✅ MUHIM: Content-Type'ni JSON ga o'zgartirish
            // Headers read-only bo'lishi mumkin, shuning uchun try-catch
            try
            {
                context.Request.ContentType = "application/json";
                _logger.LogInformation("✅ Content-Type changed to application/json for {Path}", context.Request.Path);
            }
            catch (Exception ctEx)
            {
                _logger.LogWarning(ctEx, "⚠️ Failed to set ContentType property, trying Headers");
                // Agar ContentType o'zgartirib bo'lmasa, Headers orqali
                try
                {
                    if (context.Request.Headers.ContainsKey("Content-Type"))
                    {
                        context.Request.Headers.Remove("Content-Type");
                    }
                    context.Request.Headers.Append("Content-Type", "application/json; charset=utf-8");
                    _logger.LogInformation("✅ Content-Type set via Headers for {Path}", context.Request.Path);
                }
                catch (Exception headerEx)
                {
                    _logger.LogError(headerEx, "❌ Failed to set Content-Type via Headers");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to decrypt request for {Path}, passing through as-is", context.Request.Path);

            // If decryption fails, reset to original body and continue
            context.Request.Body.Position = 0;
        }
    }

    private async Task EncryptResponseAsync(
        HttpContext context,
        IEncryptionService encryptionService,
        Stream originalBodyStream)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();

        if (string.IsNullOrEmpty(responseBody))
        {
            await context.Response.Body.CopyToAsync(originalBodyStream);
            return;
        }

        try
        {
            // Response ni to'g'ridan-to'g'ri encrypt qilish (without wrapper)
            var encryptedResponse = encryptionService.Encrypt(responseBody);

            _logger.LogDebug("Response encrypted successfully for {Path}", context.Request.Path);

            context.Response.ContentType = "text/plain";
            var encryptedBytes = Encoding.UTF8.GetBytes(encryptedResponse);
            context.Response.ContentLength = encryptedBytes.Length;

            await originalBodyStream.WriteAsync(encryptedBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt response for {Path}", context.Request.Path);

            // Xatolik bo'lsa, original response ni qaytarish
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            await context.Response.Body.CopyToAsync(originalBodyStream);
        }
    }
}

/// <summary>
/// Extension method for adding encryption middleware
/// </summary>
public static class EncryptionMiddlewareExtensions
{
    public static IApplicationBuilder UseEncryption(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<EncryptionMiddleware>();
    }
}
