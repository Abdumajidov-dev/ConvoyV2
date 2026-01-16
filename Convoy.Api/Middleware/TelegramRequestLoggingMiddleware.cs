using System.Text;
using System.Text.Json;
using Convoy.Service.Interfaces;

namespace Convoy.Api.Middleware;

public class TelegramRequestLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public TelegramRequestLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ITelegramService telegramService)
    {
        // Faqat location POST requestlarini Telegramga yuborish
        var shouldLog = ShouldLogRequest(context);
        
        if (!shouldLog)
        {
            await _next(context);
            return;
        }

        context.Request.EnableBuffering();

        // 🔹 BODY
        string body;
        using (var reader = new StreamReader(
                   context.Request.Body,
                   Encoding.UTF8,
                   leaveOpen: true))
        {
            body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
        }

        // 🔹 HEADERS
        var headers = context.Request.Headers
            .ToDictionary(h => h.Key, h => h.Value.ToString());

        var messageObject = new
        {
            Method = context.Request.Method,
            Path = context.Request.Path,
            Query = context.Request.QueryString.ToString(),
            Headers = headers,
            Body = TryParseJson(body),
            Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")
        };

        var json = JsonSerializer.Serialize(
            messageObject,
            new JsonSerializerOptions { WriteIndented = true });

        await telegramService.SendFormattedMessageAsync(
            $"<b>📥 Incoming Request</b>\n<pre>{EscapeHtml(json)}</pre>"
        );

        await _next(context);
    }

    /// <summary>
    /// Qaysi requestlarni Telegramga yuborish kerakligini aniqlaydi
    /// </summary>
    private static bool ShouldLogRequest(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";
        var method = context.Request.Method.ToUpper();

        // Faqat location POST requestlari
        if (method == "POST" && path.Contains("/api/location"))
        {
            return false;
        }

        // Agar boshqa endpoint'larni ham qo'shmoqchi bo'lsangiz, shu yerga qo'shing:
        // if (method == "POST" && path.Contains("/api/user"))
        // {
        //     return true;
        // }

        return false;
    }

    private static object TryParseJson(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return "(empty)";

        try
        {
            return JsonSerializer.Deserialize<object>(body)!;
        }
        catch
        {
            return body;
        }
    }

    private static string EscapeHtml(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }
}
