# Telegram Service Guide

Telegram bot orqali kanalga xabar yuborish service'i - to'liq qo'llanma.

## 📋 Overview

**TelegramService** - istalgan joydan Telegram kanaliga xabar yuborish imkonini beradi:
- ✅ Oddiy text xabarlar
- ✅ Formatted xabarlar (HTML/Markdown)
- ✅ Location ma'lumotlari
- ✅ Bulk location reports
- ✅ Custom data reports
- ✅ Alert/Warning xabarlari

---

## ⚙️ Configuration

### appsettings.json

```json
{
  "BotSettings": {
    "Telegram": {
      "BotToken": "YOUR_BOT_TOKEN",
      "ChannelId": "YOUR_CHANNEL_ID"
    }
  }
}
```

### Setup Qilish

#### 1. Telegram Bot Yaratish

```bash
# 1. Telegram'da @BotFather'ga yozing
# 2. /newbot buyrug'ini yuboring
# 3. Bot nomi va username'ni kiriting
# 4. Bot token'ni oling (masalan: 8514698197:AAF2gfXtFExW9bwmGQRNZQisod5ShAy167w)
```

#### 2. Kanal Yaratish va Bot Qo'shish

```bash
# 1. Telegram'da yangi kanal yarating
# 2. Bot'ni kanalga admin sifatida qo'shing
# 3. Kanal ID'sini oling:
#    - @username_to_id_bot'ga yozing
#    - Kanal link yuboring
#    - ID oling (masalan: -1003584246932)
```

#### 3. Configuration

```json
{
  "BotSettings": {
    "Telegram": {
      "BotToken": "8514698197:AAF2gfXtFExW9bwmGQRNZQisod5ShAy167w",
      "ChannelId": "-1003584246932"
    }
  }
}
```

---

## 🚀 Usage

### 1. Service Injection

```csharp
public class YourService
{
    private readonly ITelegramService _telegramService;

    public YourService(ITelegramService telegramService)
    {
        _telegramService = telegramService;
    }

    public async Task DoSomething()
    {
        await _telegramService.SendMessageAsync("Hello from Convoy!");
    }
}
```

### 2. Oddiy Text Xabar

```csharp
await _telegramService.SendMessageAsync("Test message");
```

**Telegram'dagi ko'rinish:**
```
Test message
```

### 3. Formatted Xabar (HTML)

```csharp
var message = @"<b>Bold text</b>
<i>Italic text</i>
<code>Code text</code>
<a href='https://google.com'>Link</a>";

await _telegramService.SendFormattedMessageAsync(message, "HTML");
```

**Telegram'dagi ko'rinish:**
```
Bold text
Italic text
Code text
Link
```

### 4. Location Ma'lumoti

```csharp
await _telegramService.SendLocationDataAsync(
    userId: 123,
    userName: "John Driver",
    latitude: 41.311151,
    longitude: 69.279737,
    recordedAt: DateTime.Now
);
```

**Telegram'dagi ko'rinish:**
```
📍 Yangi Lokatsiya

👤 User: John Driver (ID: 123)
🌍 Koordinatalar:
   • Latitude: 41.311151
   • Longitude: 69.279737
⏰ Vaqt: 25.12.2025 15:30:45

🗺 Google Maps'da ko'rish [link]
```

### 5. Bulk Location Report

```csharp
await _telegramService.SendBulkLocationDataAsync(
    userId: 123,
    userName: "John Driver",
    locationCount: 50,
    firstLocation: DateTime.Now.AddHours(-2),
    lastLocation: DateTime.Now
);
```

**Telegram'dagi ko'rinish:**
```
📊 Bulk Lokatsiya Ma'lumoti

👤 User: John Driver (ID: 123)
📍 Lokatsiyalar soni: 50
⏰ Davomiyligi: 2.0 soat
🕐 Birinchi: 25.12.2025 13:30:45
🕑 Oxirgi: 25.12.2025 15:30:45

✅ Ma'lumotlar muvaffaqiyatli saqlandi
```

### 6. Custom Data Report

```csharp
var data = new Dictionary<string, string>
{
    { "User ID", "123" },
    { "Action", "Location Created" },
    { "Count", "50" },
    { "Status", "Success" }
};

await _telegramService.SendDataAsync(data, "📊 Location Report");
```

**Telegram'dagi ko'rinish:**
```
📊 Location Report

• User ID: 123
• Action: Location Created
• Count: 50
• Status: Success

🕐 25.12.2025 15:30:45
```

### 7. Alert Xabarlari

```csharp
// INFO
await _telegramService.SendAlertAsync("Application started", "INFO");

// WARNING
await _telegramService.SendAlertAsync("High CPU usage detected", "WARNING");

// ERROR
await _telegramService.SendAlertAsync("Database connection failed", "ERROR");

// SUCCESS
await _telegramService.SendAlertAsync("Backup completed", "SUCCESS");
```

**Telegram'dagi ko'rinish:**
```
ℹ️ INFO

Application started

🕐 25.12.2025 15:30:45
```

```
⚠️ WARNING

High CPU usage detected

🕐 25.12.2025 15:30:45
```

```
🔴 ERROR

Database connection failed

🕐 25.12.2025 15:30:45
```

```
✅ SUCCESS

Backup completed

🕐 25.12.2025 15:30:45
```

---

## 🎯 Real-World Examples

### Example 1: LocationService Integration

**Already implemented!** LocationService'da location create bo'lganda avtomatik Telegram'ga yuboriladi.

```csharp
// Convoy.Service/Services/LocationService.cs

// Bitta location
if (responseDtos.Count == 1)
{
    var loc = responseDtos[0];
    await _telegramService.SendLocationDataAsync(
        dto.UserId,
        $"User {dto.UserId}",
        loc.Latitude,
        loc.Longitude,
        loc.RecordedAt
    );
}
// Bulk locations
else
{
    var firstLoc = responseDtos.First();
    var lastLoc = responseDtos.Last();
    await _telegramService.SendBulkLocationDataAsync(
        dto.UserId,
        $"User {dto.UserId}",
        responseDtos.Count,
        firstLoc.RecordedAt,
        lastLoc.RecordedAt
    );
}
```

### Example 2: OTP Service Integration

```csharp
public class OtpService : IOtpService
{
    private readonly ITelegramService _telegramService;

    public async Task<bool> SendOtpAsync(string phoneNumber, string otpCode)
    {
        // OTP yuborilganda Telegram'ga xabar
        await _telegramService.SendDataAsync(new Dictionary<string, string>
        {
            { "Phone", phoneNumber },
            { "OTP Code", otpCode },
            { "Sent At", DateTime.Now.ToString() }
        }, "📱 OTP Sent");

        // ... send SMS logic
    }
}
```

### Example 3: Error Monitoring

```csharp
public class GlobalExceptionHandler
{
    private readonly ITelegramService _telegramService;

    public async Task HandleException(Exception ex)
    {
        await _telegramService.SendAlertAsync(
            $"Exception: {ex.Message}\nStack: {ex.StackTrace}",
            "ERROR"
        );
    }
}
```

### Example 4: Daily Report

```csharp
public class DailyReportService : BackgroundService
{
    private readonly ITelegramService _telegramService;
    private readonly ILocationRepository _locationRepository;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Har kuni 23:00 da
            var now = DateTime.Now;
            var targetTime = new DateTime(now.Year, now.Month, now.Day, 23, 0, 0);

            if (now > targetTime)
                targetTime = targetTime.AddDays(1);

            await Task.Delay(targetTime - now, stoppingToken);

            // Kunlik statistika
            var stats = await GetDailyStats();

            await _telegramService.SendDataAsync(new Dictionary<string, string>
            {
                { "Date", DateTime.Today.ToString("dd.MM.yyyy") },
                { "Total Locations", stats.TotalLocations.ToString() },
                { "Active Users", stats.ActiveUsers.ToString() },
                { "Total Distance", $"{stats.TotalDistance:F2} km" }
            }, "📊 Kunlik Hisobot");
        }
    }
}
```

---

## 🧪 Testing

### Test Controller

Telegram service'ni test qilish uchun endpoint'lar:

```bash
# 1. Oddiy xabar
curl -X GET "http://localhost:5084/api/telegram-test/send-simple?message=Hello"

# 2. Location ma'lumoti
curl -X POST http://localhost:5084/api/telegram-test/send-location \
  -H "Content-Type: application/json" \
  -d '{
    "user_id": 123,
    "user_name": "John Driver",
    "latitude": 41.311151,
    "longitude": 69.279737,
    "recorded_at": "2025-12-25T15:30:00Z"
  }'

# 3. Bulk report
curl -X POST http://localhost:5084/api/telegram-test/send-bulk \
  -H "Content-Type: application/json" \
  -d '{
    "user_id": 123,
    "user_name": "John Driver",
    "location_count": 50,
    "first_location": "2025-12-25T13:30:00Z",
    "last_location": "2025-12-25T15:30:00Z"
  }'

# 4. Custom data
curl -X POST http://localhost:5084/api/telegram-test/send-data \
  -H "Content-Type: application/json" \
  -d '{
    "User ID": "123",
    "Action": "Test",
    "Status": "Success"
  }'

# 5. Alert
curl -X POST http://localhost:5084/api/telegram-test/send-alert \
  -H "Content-Type: application/json" \
  -d '{
    "message": "Test alert message",
    "level": "INFO"
  }'
```

### Expected Results

Har bir test uchun Telegram kanalingizda xabar paydo bo'lishi kerak.

---

## ⚠️ Important Notes

### 1. Configuration Validation

Service avtomatik ravishda config'ni tekshiradi:

```csharp
// Agar BotToken yoki ChannelId bo'lmasa, service disabled bo'ladi
if (!_isEnabled)
{
    _logger.LogWarning("Telegram service is disabled. Skipping message send.");
    return false;
}
```

### 2. Error Handling

Service barcha xatoliklarni log qiladi va false qaytaradi:

```csharp
try
{
    // Send message logic
}
catch (Exception ex)
{
    _logger.LogError(ex, "❌ Error sending Telegram message");
    return false;
}
```

### 3. Non-Blocking

LocationService'da Telegram yuborish asynchronous va non-blocking:

```csharp
// Main operation'ni block qilmaydi
try
{
    await _telegramService.SendLocationDataAsync(...);
}
catch (Exception ex)
{
    _logger.LogWarning(ex, "Failed to send Telegram notification");
    // Continue execution
}
```

### 4. HTML Formatting

HTML tag'lar supported:
- `<b>bold</b>` - **bold**
- `<i>italic</i>` - *italic*
- `<code>code</code>` - `code`
- `<a href='url'>link</a>` - [link](url)
- `<pre>preformatted</pre>` - preformatted text

### 5. Rate Limiting

Telegram API rate limit: 30 message/second. Service buni avtomatik handle qiladi.

---

## 🔧 Customization

### Custom Message Templates

```csharp
public class CustomTelegramService
{
    private readonly ITelegramService _telegramService;

    public async Task SendOrderNotification(Order order)
    {
        var message = $@"🛒 <b>Yangi Buyurtma</b>

📦 <b>Order ID:</b> {order.Id}
👤 <b>Customer:</b> {order.CustomerName}
💰 <b>Total:</b> ${order.Total:F2}
📍 <b>Location:</b> {order.DeliveryAddress}

⏰ {DateTime.Now:dd.MM.yyyy HH:mm:ss}";

        await _telegramService.SendFormattedMessageAsync(message);
    }
}
```

### Environment-Specific Channels

```json
// appsettings.Development.json
{
  "BotSettings": {
    "Telegram": {
      "BotToken": "DEV_BOT_TOKEN",
      "ChannelId": "DEV_CHANNEL_ID"
    }
  }
}

// appsettings.Production.json
{
  "BotSettings": {
    "Telegram": {
      "BotToken": "PROD_BOT_TOKEN",
      "ChannelId": "PROD_CHANNEL_ID"
    }
  }
}
```

---

## 🐛 Troubleshooting

### Problem: "Telegram service is disabled"

**Reason**: BotToken yoki ChannelId configured emas

**Solution**:
```json
{
  "BotSettings": {
    "Telegram": {
      "BotToken": "YOUR_BOT_TOKEN",
      "ChannelId": "YOUR_CHANNEL_ID"
    }
  }
}
```

### Problem: "Chat not found"

**Reason**: Bot kanalga admin sifatida qo'shilmagan

**Solution**:
1. Kanalga kiring
2. Bot'ni admin sifatida qo'shing
3. "Post Messages" ruxsatini bering

### Problem: "Unauthorized"

**Reason**: Bot token noto'g'ri

**Solution**:
1. @BotFather'ga /token buyrug'ini yuboring
2. Yangi token oling
3. appsettings.json'ni yangilang

---

## 📊 Best Practices

### ✅ DO

1. **Non-blocking calls** - Telegram'ga yuborishni asynchronous qiling
2. **Error handling** - Exception'larni handle qiling
3. **Logging** - Barcha xatolarni log qiling
4. **Environment-specific** - Dev va Prod uchun har xil channel
5. **Meaningful messages** - Aniq va tushunarli xabarlar yuboring

### ❌ DON'T

1. **Block main flow** - Telegram failure main operation'ni to'xtatmasin
2. **Send secrets** - Sensitive data yuborishdan saqlaning
3. **Spam** - Juda ko'p xabar yuborishdan saqlaning
4. **Large messages** - Telegram message limit: 4096 characters

---

## 🎯 Summary

**TelegramService** - flexible va reusable service:
- ✅ Easy integration - istalgan joyda ishlatish mumkin
- ✅ Multiple message types - location, data, alerts
- ✅ Auto-configuration - appsettings.json'dan o'qiydi
- ✅ Error handling - barcha xatoliklar handled
- ✅ Non-blocking - asynchronous calls

**Already integrated:**
- ✅ LocationService - location create bo'lganda Telegram'ga yuboriladi

**Ready to use in:**
- AuthService (OTP sent notifications)
- ErrorHandler (error monitoring)
- BackgroundServices (daily reports)
- Controllers (custom notifications)

---

Happy Coding! 🚀
