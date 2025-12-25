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
        // Faqat POST / PUT larni log qilamiz (xohlasang o‘zgartirasan)
        if (context.Request.Method is not ("POST" or "PUT"))
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
