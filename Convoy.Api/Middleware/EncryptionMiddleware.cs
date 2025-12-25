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
    private readonly IConfiguration _configuration;

    public EncryptionMiddleware(
        RequestDelegate next,
        ILogger<EncryptionMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Route encryption'dan excluded ekanligini tekshirish
    /// appsettings.json dan Encryption:ExcludedRoutes'ni o'qiydi
    /// </summary>
    private bool IsExcludedRoute(string path)
    {
        // appsettings.json'dan excluded routes'ni olish
        var excludedRoutes = _configuration.GetSection("Encryption:ExcludedRoutes").Get<string[]>();

        if (excludedRoutes == null || excludedRoutes.Length == 0)
        {
            // Default excluded routes (agar appsettings'da bo'lmasa)
            excludedRoutes = new[]
            {
                "/swagger",
                "/swagger/*",
                "/health",
                "/hubs/*"
            };
        }

        foreach (var pattern in excludedRoutes)
        {
            // Exact match
            if (pattern.Equals(path, StringComparison.OrdinalIgnoreCase))
                return true;

            // Wildcard pattern match
            if (pattern.EndsWith("/*"))
            {
                var prefix = pattern[..^2]; // Remove "/*"
                if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            else if (pattern.Contains("*"))
            {
                // Advanced wildcard matching
                var regex = new System.Text.RegularExpressions.Regex(
                    "^" + System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*") + "$",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );
                if (regex.IsMatch(path))
                    return true;
            }
        }

        return false;
    }

    public async Task InvokeAsync(HttpContext context, IEncryptionService encryptionService)
    {
        if (!encryptionService.IsEnabled)
        {
            // Encryption o'chirilgan bo'lsa, oddiy request/response
            await _next(context);
            return;
        }

        // Ba'zi route'lar encryption'dan excluded
        if (IsExcludedRoute(context.Request.Path))
        {
            _logger.LogDebug("Route {Path} excluded from encryption", context.Request.Path);
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
        // ✅ YANGI: Endpoint'ni decrypt qilish (header'lardan oldin!)
        DecryptEndpoint(context, encryptionService);

        // ✅ YANGI: Header'larni decrypt qilish
        DecryptHeaders(context, encryptionService);

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

    /// <summary>
    /// Shifrlangan endpoint'ni decrypt qilish va path'ni o'zgartirish
    /// </summary>
    private void DecryptEndpoint(HttpContext context, IEncryptionService encryptionService)
    {
        if (!context.Request.Headers.ContainsKey("encrypted-endpoint"))
        {
            // Agar encrypted-endpoint yo'q bo'lsa, oddiy request
            return;
        }

        try
        {
            var encryptedEndpoint = context.Request.Headers["encrypted-endpoint"].ToString();

            if (string.IsNullOrWhiteSpace(encryptedEndpoint))
            {
                _logger.LogWarning("encrypted-endpoint header is empty");
                return;
            }

            _logger.LogInformation("🔐 Attempting to decrypt endpoint. Length: {Length}", encryptedEndpoint.Length);
            _logger.LogInformation("📍 Current request path BEFORE decrypt: {Path}", context.Request.Path);

            // Decrypt qilish
            var decryptedEndpoint = encryptionService.Decrypt(encryptedEndpoint);

            _logger.LogInformation("✅ Endpoint decrypted: '{Endpoint}'", decryptedEndpoint);
            _logger.LogInformation("📏 Decrypted endpoint length: {Length}", decryptedEndpoint.Length);
            _logger.LogInformation("🔍 Decrypted endpoint starts with '/': {StartsWithSlash}", decryptedEndpoint.StartsWith("/"));

            // Path'ni yangilash
            // ASP.NET Core'da Path read-only, shuning uchun PathBase + Path kombinatsiyasidan foydalanamiz
            // Yoki request feature'larini to'g'ridan-to'g'ri o'zgartiramiz

            var pathFeature = context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpRequestFeature>();
            if (pathFeature != null)
            {
                
                // Agar endpoint "/" bilan boshlanmasa, qo'shamiz
                var newPath = decryptedEndpoint.StartsWith("/") ? decryptedEndpoint : $"/api/{decryptedEndpoint}";

                _logger.LogInformation("🔄 Changing path from '{OldPath}' to '{NewPath}'",
                    context.Request.Path, newPath);
                _logger.LogInformation("📍 RawTarget BEFORE: '{RawTarget}'", pathFeature.RawTarget);
                _logger.LogInformation("📍 Path BEFORE: '{Path}'", pathFeature.Path);

                pathFeature.RawTarget = newPath;
                pathFeature.Path = newPath;
                context.Request.Path = newPath;

                _logger.LogInformation("📍 RawTarget AFTER: '{RawTarget}'", pathFeature.RawTarget);
                _logger.LogInformation("📍 Path AFTER: '{Path}'", pathFeature.Path);
                _logger.LogInformation("📍 context.Request.Path AFTER: '{Path}'", context.Request.Path);
                _logger.LogInformation("✅ Path changed successfully to: {Path}", context.Request.Path);
            }
            else
            {
                _logger.LogWarning("⚠️ Could not get IHttpRequestFeature to change path");
            }

            // encrypted-endpoint header'ni o'chirish (endi kerak emas)
            context.Request.Headers.Remove("encrypted-endpoint");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to decrypt endpoint");
        }
    }

    /// <summary>
    /// Shifrlangan header'larni decrypt qilish
    /// </summary>
    private void DecryptHeaders(HttpContext context, IEncryptionService encryptionService)
    {
        // ✅ YANGI FORMAT: encrypted-headers (bitta header'da barcha ma'lumotlar)
        if (context.Request.Headers.ContainsKey("encrypted-headers"))
        {
            DecryptBulkHeaders(context, encryptionService);
            return;
        }

        // ✅ ESKI FORMAT: Alohida header'lar (backward compatibility)
        DecryptIndividualHeaders(context, encryptionService);
    }

    /// <summary>
    /// Bitta encrypted-headers dan barcha header'larni yechish
    /// </summary>
    private void DecryptBulkHeaders(HttpContext context, IEncryptionService encryptionService)
    {
        try
        {
            var encryptedHeaders = context.Request.Headers["encrypted-headers"].ToString();

            if (string.IsNullOrWhiteSpace(encryptedHeaders))
            {
                _logger.LogWarning("encrypted-headers header is empty");
                return;
            }

            _logger.LogInformation("🔐 Attempting to decrypt bulk headers. Length: {Length}", encryptedHeaders.Length);

            // Decrypt qilish
            var decryptedJson = encryptionService.Decrypt(encryptedHeaders);

            _logger.LogInformation("✅ Bulk headers decrypted successfully");

            // JSON parse qilish - JsonElement ishlatamiz chunki ba'zi value'lar object bo'lishi mumkin
            var headersDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(decryptedJson);

            if (headersDict == null || headersDict.Count == 0)
            {
                _logger.LogWarning("Decrypted headers JSON is empty or invalid");
                return;
            }

            _logger.LogInformation("📦 Found {Count} headers in encrypted-headers", headersDict.Count);

            // Har bir header'ni request'ga qo'shish
            foreach (var kvp in headersDict)
            {
                var headerName = kvp.Key;

                // JsonElement'ni string'ga convert qilish
                // Agar object yoki array bo'lsa, JSON string sifatida saqlanadi
                string headerValue;
                if (kvp.Value.ValueKind == JsonValueKind.String)
                {
                    headerValue = kvp.Value.GetString() ?? "";
                }
                else if (kvp.Value.ValueKind == JsonValueKind.Object || kvp.Value.ValueKind == JsonValueKind.Array)
                {
                    // Object yoki array bo'lsa, JSON string qilamiz
                    headerValue = JsonSerializer.Serialize(kvp.Value);
                }
                else
                {
                    // Boshqa type'lar (number, boolean, null)
                    headerValue = kvp.Value.ToString();
                }

                // Agar header allaqachon mavjud bo'lsa, o'chirish
                if (context.Request.Headers.ContainsKey(headerName))
                {
                    context.Request.Headers.Remove(headerName);
                }

                // Yangi header qo'shish
                context.Request.Headers.Append(headerName, headerValue);

                _logger.LogInformation("  ✅ Added header: {Name} = {Value}", headerName,
                    headerValue.Length > 50 ? headerValue.Substring(0, 50) + "..." : headerValue);
            }

            // encrypted-headers'ni o'chirish (kerak emas endi)
            context.Request.Headers.Remove("encrypted-headers");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to decrypt bulk headers");
        }
    }

    /// <summary>
    /// Alohida header'larni decrypt qilish (eski format)
    /// </summary>
    private void DecryptIndividualHeaders(HttpContext context, IEncryptionService encryptionService)
    {
        // Decrypt qilinadigan header nomlari
        var headersToDecrypt = new[] { "device-info", "Authorization", "X-Custom-Data" };

        foreach (var headerName in headersToDecrypt)
        {
            if (!context.Request.Headers.ContainsKey(headerName))
                continue;

            var encryptedValue = context.Request.Headers[headerName].ToString();

            // Bo'sh yoki juda qisqa bo'lsa skip
            if (string.IsNullOrWhiteSpace(encryptedValue) || encryptedValue.Length < 20)
                continue;

            // Authorization header "Bearer " prefixi bilan boshlanishi mumkin
            var valueToDecrypt = encryptedValue;
            var hasBearer = false;

            if (headerName == "Authorization" && encryptedValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                valueToDecrypt = encryptedValue.Substring(7); // "Bearer " ni olib tashlash
                hasBearer = true;
            }

            try
            {
                _logger.LogInformation("🔐 Attempting to decrypt header: {Header}", headerName);

                // Decrypt qilish
                var decryptedValue = encryptionService.Decrypt(valueToDecrypt);

                _logger.LogInformation("✅ Header decrypted: {Header} = {Value}", headerName, decryptedValue);

                // Header'ni yangilash
                context.Request.Headers.Remove(headerName);

                // Authorization uchun Bearer qayta qo'shish
                if (hasBearer)
                {
                    context.Request.Headers.Append(headerName, $"Bearer {decryptedValue}");
                }
                else
                {
                    context.Request.Headers.Append(headerName, decryptedValue);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Failed to decrypt header: {Header}, keeping original value", headerName);
                // Decrypt qilib bo'lmasa, original qiymatni qoldirish
            }
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
