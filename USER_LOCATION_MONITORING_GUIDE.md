# User Location Monitoring & Notification System

User'larning location post qilish holatini kuzatish va admin'larga notification yuborish tizimi.

---

## 📋 Overview

Bu tizim hodimlarni nazorat qilish uchun yaratilgan. Agar hodim ma'lum vaqt ichida location post qilmasa, admin'larga avtomatik notification yuboriladi.

### Key Features

- ⏰ **Har 1 minutda tekshirish** - `CheckLocationCreatedBackrounService` har minutda ishga tushadi
- 📊 **Offline duration tracking** - User qancha vaqtdan beri offline ekanligini kuzatadi
- 🔔 **Multi-threshold notifications** - 20, 40, 60, 80, 100, 120 daqiqada notification yuboradi
- 👥 **Admin targeting** - Faqat `role='admin_unduruv'` bo'lgan user'larga yuboriladi
- 📱 **Firebase Cloud Messaging** - Push notification orqali xabar boradi
- 💾 **Notification history** - Barcha notification'lar database'da saqlanadi

---

## 🏗️ Architecture

### Components

1. **CheckLocationCreatedBackrounService** (Background Service)
   - Har 1 minutda ishga tushadi
   - Barcha active user'larni tekshiradi
   - Offline duration'ni hisoblab, notification yuboradi

2. **NotificationService**
   - Firebase Cloud Messaging orqali push notification yuboradi
   - Admin'lar ro'yxatini oladi va barcha uchun notification yuboradi

3. **DeviceTokenService**
   - User'larning device token'larini saqlaydi va yangilaydi
   - Token'lar orqali push notification yuboriladi

### Database Tables

#### 1. device_tokens
```sql
CREATE TABLE device_tokens (
    id BIGSERIAL PRIMARY KEY,
    user_id BIGINT NOT NULL,
    token VARCHAR(500) NOT NULL,
    device_system VARCHAR(20) NOT NULL, -- "android", "ios"
    model VARCHAR(100) NOT NULL,
    device_id VARCHAR(100) NOT NULL,
    is_physical_device BOOLEAN NOT NULL DEFAULT true,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ,
    delete_at TIMESTAMPTZ
);
```

#### 2. user_status_reports
```sql
CREATE TABLE user_status_reports (
    id BIGSERIAL PRIMARY KEY,
    user_id BIGINT NOT NULL,
    last_location_time TIMESTAMPTZ,           -- So'nggi location post qilgan vaqt
    last_notified_at TIMESTAMPTZ,             -- So'nggi notification yuborilgan vaqt
    offline_duration_minutes INTEGER NOT NULL DEFAULT 0,
    is_notified BOOLEAN NOT NULL DEFAULT false,
    notification_count INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ,
    delete_at TIMESTAMPTZ
);
```

#### 3. admin_notifications
```sql
CREATE TABLE admin_notifications (
    id BIGSERIAL PRIMARY KEY,
    user_id BIGINT NOT NULL,                  -- Qaysi user haqida
    admin_user_id BIGINT NOT NULL,            -- Qaysi admin'ga
    notification_type VARCHAR(50) NOT NULL,   -- "user_offline"
    title VARCHAR(200) NOT NULL,
    message VARCHAR(1000) NOT NULL,
    offline_duration_minutes INTEGER NOT NULL DEFAULT 0,
    is_sent BOOLEAN NOT NULL DEFAULT false,
    sent_at TIMESTAMPTZ,
    is_read BOOLEAN NOT NULL DEFAULT false,
    read_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ,
    delete_at TIMESTAMPTZ
);
```

---

## 🔄 Workflow

### 1. Background Service Flow

```
┌──────────────────────────────────────────────────────────────────┐
│        CheckLocationCreatedBackrounService (Every 1 min)          │
└──────────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌──────────────────────────────────────────────────────────────────┐
│  1. Get all active users (role != 'admin_unduruv')               │
└──────────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌──────────────────────────────────────────────────────────────────┐
│  2. For each user:                                                │
│     - Get last location from database                             │
│     - Calculate offline duration (NOW - last_location_time)       │
│     - Update user_status_reports table                            │
└──────────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌──────────────────────────────────────────────────────────────────┐
│  3. Check if notification needed:                                 │
│     - Is offline >= 20 minutes?                                   │
│     - Is new threshold reached? (20, 40, 60, 80, 100, 120)       │
│     - Has notification been sent for current threshold?           │
└──────────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌──────────────────────────────────────────────────────────────────┐
│  4. Send notification to all admins                               │
│     - Get all admin users (role = 'admin_unduruv')               │
│     - Get their device tokens                                     │
│     - Send FCM push notification                                  │
│     - Save notification log to admin_notifications table          │
└──────────────────────────────────────────────────────────────────┘
```

### 2. Notification Threshold Logic

```javascript
Thresholds: [20, 40, 60, 80, 100, 120] // minutes

Example:
- User A so'nggi location: 10:00
- Current time: 10:25
- Offline duration: 25 minutes
- Threshold: 20 minutes (eng yaqin threshold)
- Action: Send notification ✅

- Current time: 10:45
- Offline duration: 45 minutes
- Threshold: 40 minutes
- Action: Send notification ✅ (yangi threshold)

- Current time: 10:50
- Offline duration: 50 minutes
- Threshold: 40 minutes (hali 60'ga yetmagan)
- Action: Skip ❌ (allaqachon 40 uchun yuborilgan)
```

---

## 📱 Firebase Setup

### 1. Create Firebase Project

1. Go to [Firebase Console](https://console.firebase.google.com/)
2. Create new project or select existing one
3. Add Android/iOS app
4. Download `google-services.json` (Android) or `GoogleService-Info.plist` (iOS)

### 2. Generate Service Account Key

1. Go to Project Settings → Service Accounts
2. Click "Generate new private key"
3. Save as `firebase-adminsdk.json`
4. Place in `Convoy.Api/` directory

### 3. Backend Configuration

```json
// appsettings.json
{
  "Firebase": {
    "ProjectId": "your-firebase-project-id",
    "CredentialsPath": "firebase-adminsdk.json"
  }
}
```

---

## 🔧 Setup Instructions

### 1. Database Migration

**Option A: Using EF Core (Development)**
```bash
dotnet ef database update --project Convoy.Data --startup-project Convoy.Api
```

**Option B: Using SQL Script (Production)**
```bash
psql -U postgres -d convoydb -f add-notification-system.sql
```

### 2. Firebase Setup

```bash
# 1. Place firebase-adminsdk.json in Convoy.Api/
# 2. Update appsettings.json with Firebase config
# 3. Restart application
```

### 3. Verify Installation

```sql
-- Check if tables are created
SELECT tablename FROM pg_tables
WHERE tablename IN ('device_tokens', 'user_status_reports', 'admin_notifications');

-- Check background service logs
-- Look for: "🔄 CheckLocationCreatedBackrounService started at..."
```

---

## 🚀 API Endpoints

### Save Device Token (Login qilgandan keyin)

```http
POST /api/auth/save_device_token
Authorization: Bearer {token}
Content-Type: application/json

{
  "user_id": 5475,
  "device_info": {
    "device_token": "cA7x...FCM-token-here...9zY",
    "device_system": "android",
    "model": "Samsung Galaxy S21",
    "device_id": "unique-device-id-123",
    "is_physical_device": true
  }
}
```

**Response:**
```json
{
  "status": true,
  "message": "Device token saqlandi",
  "data": null
}
```

---

## 📊 Monitoring & Queries

### Check User Status Reports

```sql
SELECT
    u.user_id,
    u.name,
    usr.last_location_time,
    usr.offline_duration_minutes,
    usr.is_notified,
    usr.notification_count,
    usr.last_notified_at
FROM user_status_reports usr
JOIN users u ON usr.user_id = u.id
WHERE u.is_active = true
ORDER BY usr.offline_duration_minutes DESC;
```

### Check Recent Notifications

```sql
SELECT
    an.*,
    u.name as user_name,
    a.name as admin_name
FROM admin_notifications an
JOIN users u ON an.user_id = u.id
JOIN users a ON an.admin_user_id = a.id
WHERE an.created_at >= NOW() - INTERVAL '24 hours'
ORDER BY an.created_at DESC
LIMIT 50;
```

### Check Device Tokens

```sql
SELECT
    u.user_id,
    u.name,
    dt.device_system,
    dt.model,
    dt.is_active,
    dt.created_at
FROM device_tokens dt
JOIN users u ON dt.user_id = u.id
WHERE dt.is_active = true
ORDER BY dt.created_at DESC;
```

### Find Users Without Recent Locations

```sql
SELECT
    u.user_id,
    u.name,
    l.recorded_at as last_location,
    EXTRACT(EPOCH FROM (NOW() - l.recorded_at)) / 60 as minutes_ago
FROM users u
LEFT JOIN LATERAL (
    SELECT recorded_at
    FROM locations
    WHERE user_id = u.user_id
    ORDER BY recorded_at DESC
    LIMIT 1
) l ON true
WHERE u.is_active = true
  AND u.role != 'admin_unduruv'
  AND (l.recorded_at IS NULL OR l.recorded_at < NOW() - INTERVAL '20 minutes')
ORDER BY minutes_ago DESC NULLS FIRST;
```

---

## 🧪 Testing

### 1. Test Device Token Saving

```bash
# Login and get token
curl -X POST http://localhost:5084/api/auth/verify_otp \
  -H "Content-Type: application/json" \
  -d '{"phone_number": "901234567", "otp_code": "1111"}'

# Save device token
curl -X POST http://localhost:5084/api/auth/save_device_token \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "user_id": 5475,
    "device_info": {
      "device_token": "test-fcm-token-123",
      "device_system": "android",
      "model": "Test Device",
      "device_id": "test-device-id",
      "is_physical_device": false
    }
  }'
```

### 2. Test Background Service

```bash
# Check logs for service start
# Expected log: "🔄 CheckLocationCreatedBackrounService started at..."

# Wait 1-2 minutes
# Expected log: "🔍 Checking user locations at..."

# If user offline > 20 min:
# Expected log: "🚨 NOTIFICATION YUBORILDI: User ... - 20 daqiqadan beri offline"
```

### 3. Simulate Offline User

```sql
-- Set user's last location to 25 minutes ago
UPDATE locations
SET recorded_at = NOW() - INTERVAL '25 minutes'
WHERE user_id = 5475
ORDER BY recorded_at DESC
LIMIT 1;

-- Wait 1-2 minutes for background service to run
-- Check admin_notifications table for new notification
```

---

## 🐛 Troubleshooting

### Problem: Notification yuborilmayapti

**Checks:**
1. Firebase Admin SDK initialized correctly?
   ```bash
   # Check logs for: "Firebase Admin SDK initialized successfully"
   ```

2. Admin'lar mavjudmi?
   ```sql
   SELECT * FROM users WHERE role = 'admin_unduruv' AND is_active = true;
   ```

3. Admin'larda device token bormi?
   ```sql
   SELECT dt.* FROM device_tokens dt
   JOIN users u ON dt.user_id = u.id
   WHERE u.role = 'admin_unduruv' AND dt.is_active = true;
   ```

4. Background service ishlayaptimi?
   ```bash
   # Check logs for periodic messages:
   # "🔍 Checking user locations at..."
   ```

### Problem: Background service ishlamayapti

**Solution:**
```bash
# 1. Check if service is registered in Program.cs
# builder.Services.AddHostedService<CheckLocationCreatedBackrounService>();

# 2. Check logs for startup error
# Look for exceptions during service initialization

# 3. Restart application
dotnet run --project Convoy.Api
```

### Problem: User offline lekin notification yo'q

**Possible reasons:**
1. User's last location < 20 minutes ago
2. Notification already sent for current threshold
3. User role = 'admin_unduruv' (admins are excluded)
4. User is_active = false

**Debug query:**
```sql
SELECT
    u.user_id,
    u.name,
    u.role,
    u.is_active,
    usr.last_location_time,
    usr.offline_duration_minutes,
    usr.is_notified,
    usr.last_notified_at
FROM user_status_reports usr
JOIN users u ON usr.user_id = u.id
WHERE u.user_id = 5475;
```

---

## 📝 Best Practices

### 1. Device Token Management

- ✅ Login qilganda token saqlash
- ✅ Logout qilganda token'ni deactivate qilish
- ✅ Har safar app ochilganda token yangilash
- ❌ Bir device uchun bir nechta active token

### 2. Notification Strategy

- ✅ Threshold'lar: 20, 40, 60, 80, 100, 120 minutes
- ✅ Har bir threshold uchun faqat bir marta notification
- ❌ Bir minutda bir nechta marta tekshirish
- ❌ Test notification'larni production'da qoldirish

### 3. Database Maintenance

```sql
-- Old notifications cleanup (har oyda)
DELETE FROM admin_notifications
WHERE created_at < NOW() - INTERVAL '3 months';

-- Inactive device tokens cleanup
DELETE FROM device_tokens
WHERE is_active = false
  AND updated_at < NOW() - INTERVAL '6 months';
```

---

## 🎯 Future Improvements

- [ ] Web dashboard for notification management
- [ ] Email notification qo'shish
- [ ] SMS notification fallback
- [ ] Custom notification thresholds per user
- [ ] Notification sound/vibration settings
- [ ] Read receipts tracking
- [ ] Bulk notification management
- [ ] Notification templates

---

## 📚 Related Documentation

- **Firebase Cloud Messaging**: https://firebase.google.com/docs/cloud-messaging
- **PostgreSQL Partitioning**: See `CLAUDE.md`
- **Background Services**: See `CLAUDE.md` - Creating New Background Service
- **Device Token Service**: `Convoy.Service/Services/DeviceTokens/DeviceTokenService.cs`

---

## 🆘 Support

**Issues:** Report bugs in project repository
**Logs:** Check `Convoy.Api` logs for detailed error messages
**Database:** Use queries above to debug notification flow
