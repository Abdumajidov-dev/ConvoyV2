# SignalR User Active Status Tracking

Bu guide SignalR orqali user'larni active/inactive qilish funksiyasini tushuntiradi.

## Backend Implementation

### 1. LocationHub Changes

**File**: `Convoy.Api/Hubs/LocationHub.cs`

#### Key Features:
- ✅ **OnConnectedAsync**: Client ulanganida log qiladi
- ✅ **OnDisconnectedAsync**: Client uzilganida user'ni **INACTIVE** qiladi
- ✅ **RegisterUser(userId)**: Client bu metodini chaqirib user'ni **ACTIVE** qiladi

#### How it Works:

```csharp
// In-memory mapping: ConnectionId -> UserId
private static readonly ConcurrentDictionary<string, int> _connectionUserMap = new();

// Client ulanganida
public override async Task OnConnectedAsync()
{
    _logger.LogInformation("✅ SignalR client connected: {ConnectionId}", Context.ConnectionId);
    await base.OnConnectedAsync();
}

// Client RegisterUser(userId) chaqirganda
public async Task RegisterUser(int userId)
{
    // 1. User'ni active qilish
    await _userService.SetUserActiveStatusAsync(userId, true);

    // 2. ConnectionId -> UserId mapping saqlash
    _connectionUserMap[Context.ConnectionId] = userId;

    // 3. Client'ga tasdiqlash yuborish
    await Clients.Caller.SendAsync("UserRegistered", new {
        user_id = userId,
        is_active = true,
        message = "User successfully registered and marked as active"
    });
}

// Client uzilganida
public override async Task OnDisconnectedAsync(Exception? exception)
{
    // 1. ConnectionId bo'yicha userId topish
    if (_connectionUserMap.TryRemove(Context.ConnectionId, out var userId))
    {
        // 2. User'ni inactive qilish
        await _userService.SetUserActiveStatusAsync(userId, false);
        _logger.LogWarning("🔴 User marked as INACTIVE: user_id={UserId}", userId);
    }

    await base.OnDisconnectedAsync(exception);
}
```

### 2. UserService Implementation

**File**: `Convoy.Service/Services/UserService.cs`

```csharp
public async Task SetUserActiveStatusAsync(int userId, bool isActive)
{
    // user_id (PHP worker_id) bo'yicha user topish
    var user = await _context.Users
        .FirstOrDefaultAsync(u => u.UserId == userId);

    if (user == null)
    {
        _logger.LogWarning("User not found with user_id={UserId}", userId);
        return;
    }

    // Status yangilash
    user.IsActive = isActive;
    user.UpdatedAt = DateTimeExtensions.NowInApplicationTime();

    await _context.SaveChangesAsync();

    _logger.LogInformation("✅ User active status updated: user_id={UserId}, IsActive={IsActive}",
        userId, isActive);
}
```

---

## Flutter Integration

### Step 1: SignalR Connection Setup

```dart
import 'package:signalr_netcore/signalr_client.dart';

class LocationSignalRService {
  HubConnection? _hubConnection;
  final String hubUrl = "https://your-api.com/hubs/location";
  final int userId; // Current user's worker_id

  LocationSignalRService({required this.userId});

  Future<void> connect() async {
    // 1. Create hub connection
    _hubConnection = HubConnectionBuilder()
        .withUrl(hubUrl)
        .withAutomaticReconnect() // Auto-reconnect on disconnect
        .build();

    // 2. Listen for server events
    _setupListeners();

    // 3. Connect to hub
    await _hubConnection!.start();

    // 4. IMPORTANT: Register user after connection
    await registerUser();
  }

  Future<void> registerUser() async {
    try {
      // Call RegisterUser method on hub
      await _hubConnection!.invoke("RegisterUser", args: [userId]);
      print("🟢 User registered and marked as ACTIVE: userId=$userId");
    } catch (e) {
      print("❌ Error registering user: $e");
    }
  }

  void _setupListeners() {
    // Listen for registration confirmation
    _hubConnection!.on("UserRegistered", (arguments) {
      final data = arguments?[0];
      print("✅ User registration confirmed: $data");
      // data = {user_id: 5475, is_active: true, message: "..."}
    });

    // Listen for registration failure
    _hubConnection!.on("UserRegistrationFailed", (arguments) {
      final data = arguments?[0];
      print("❌ User registration failed: $data");
    });

    // Listen for location updates (existing functionality)
    _hubConnection!.on("LocationUpdated", (arguments) {
      final locationData = arguments?[0];
      print("📍 Location update received: $locationData");
    });
  }

  Future<void> disconnect() async {
    if (_hubConnection != null) {
      await _hubConnection!.stop();
      print("🔴 Disconnected from SignalR hub");
      // Backend will automatically mark user as INACTIVE
    }
  }
}
```

### Step 2: Usage in Flutter App

```dart
class MyApp extends StatefulWidget {
  @override
  _MyAppState createState() => _MyAppState();
}

class _MyAppState extends State<MyApp> with WidgetsBindingObserver {
  late LocationSignalRService _signalRService;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);

    // Initialize SignalR service with current user's ID
    _signalRService = LocationSignalRService(userId: 5475); // Replace with actual userId

    // Connect to hub
    _connectToHub();
  }

  Future<void> _connectToHub() async {
    try {
      await _signalRService.connect();
      print("✅ Connected to SignalR hub");
    } catch (e) {
      print("❌ Error connecting to hub: $e");
    }
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    // Handle app lifecycle
    if (state == AppLifecycleState.paused) {
      // App went to background - disconnect
      _signalRService.disconnect();
      print("📱 App paused - disconnecting from hub");
    } else if (state == AppLifecycleState.resumed) {
      // App returned to foreground - reconnect
      _connectToHub();
      print("📱 App resumed - reconnecting to hub");
    }
  }

  @override
  void dispose() {
    _signalRService.disconnect();
    WidgetsBinding.instance.removeObserver(this);
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      home: HomeScreen(),
    );
  }
}
```

### Step 3: Manual Disconnect (e.g., Logout)

```dart
Future<void> logout() async {
  // 1. Disconnect from SignalR
  await _signalRService.disconnect();
  print("🔴 User logged out - marked as INACTIVE");

  // 2. Clear local data
  // 3. Navigate to login screen
}
```

---

## Flow Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                      Flutter Client                          │
└─────────────────────────────────────────────────────────────┘
                            │
                            │ 1. Connect to hub
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                LocationHub.OnConnectedAsync()                │
│  Log: "✅ SignalR client connected: {ConnectionId}"         │
└─────────────────────────────────────────────────────────────┘
                            │
                            │ 2. Flutter calls RegisterUser(userId)
                            ▼
┌─────────────────────────────────────────────────────────────┐
│              LocationHub.RegisterUser(userId)                │
│  - SetUserActiveStatusAsync(userId, true)                    │
│  - Save: _connectionUserMap[connectionId] = userId           │
│  - Send: "UserRegistered" event to client                    │
│  Log: "🟢 User marked as ACTIVE: user_id={userId}"          │
└─────────────────────────────────────────────────────────────┘
                            │
                            │ 3. User is now ACTIVE
                            │
                    [User uses app...]
                            │
                            │ 4. Flutter disconnects (app closed/background)
                            ▼
┌─────────────────────────────────────────────────────────────┐
│            LocationHub.OnDisconnectedAsync()                 │
│  - Get userId from _connectionUserMap[connectionId]          │
│  - SetUserActiveStatusAsync(userId, false)                   │
│  - Remove from map                                           │
│  Log: "🔴 User marked as INACTIVE: user_id={userId}"        │
└─────────────────────────────────────────────────────────────┘
```

---

## Testing

### 1. Test User Registration (Flutter)

```dart
// After connecting
await _signalRService.registerUser();

// Expected backend log:
// 🟢 User marked as ACTIVE: user_id=5475, connection=abc123
```

### 2. Test User Disconnection (Close App)

```dart
// Close Flutter app or call disconnect()
await _signalRService.disconnect();

// Expected backend log:
// 🔴 User marked as INACTIVE: user_id=5475, connection=abc123
```

### 3. Check Database

```sql
-- Check user status in database
SELECT user_id, name, is_active, updated_at
FROM users
WHERE user_id = 5475;

-- Expected:
-- user_id | name               | is_active | updated_at
-- 5475    | Avaz               | true/false| 2026-01-28 12:30:00
```

---

## Important Notes

1. **CRITICAL**: Flutter client MUST call `RegisterUser(userId)` after connecting to hub
   - Without this call, disconnect won't mark user as inactive (no mapping exists)

2. **Auto-reconnect**: Use `.withAutomaticReconnect()` in Flutter to handle network issues
   - When reconnected, call `RegisterUser(userId)` again

3. **App Lifecycle**: Handle app going to background/foreground:
   - **Paused**: Disconnect from hub (user marked inactive)
   - **Resumed**: Reconnect and call RegisterUser (user marked active)

4. **Connection ID Mapping**:
   - In-memory dictionary stores `ConnectionId -> UserId`
   - Cleared when user disconnects
   - Not persisted (app restart clears all mappings)

5. **Multiple Devices**:
   - Same user can connect from multiple devices
   - Each connection tracked separately
   - User marked inactive only when ALL connections closed

---

## Troubleshooting

### Problem: User not marked inactive on disconnect

**Cause**: Flutter didn't call `RegisterUser(userId)` after connecting

**Solution**:
```dart
await _hubConnection!.start();
await registerUser(); // ← Add this!
```

### Problem: User marked inactive immediately after connect

**Cause**: Network issue causing rapid connect/disconnect

**Solution**: Use automatic reconnect:
```dart
HubConnectionBuilder()
    .withUrl(hubUrl)
    .withAutomaticReconnect() // ← Add this!
    .build();
```

### Problem: Multiple users marked inactive when one disconnects

**Cause**: Bug in ConnectionId -> UserId mapping

**Solution**: Check backend logs for connection ID mismatches

---

## API Reference

### Hub Methods (Call from Flutter)

| Method | Parameters | Description |
|--------|-----------|-------------|
| `RegisterUser` | `int userId` | Register user and mark as ACTIVE |
| `JoinUserTracking` | `int userId` | Join user's location tracking group |
| `LeaveUserTracking` | `int userId` | Leave user's location tracking group |
| `JoinAllUsersTracking` | - | Join all users tracking group |
| `LeaveAllUsersTracking` | - | Leave all users tracking group |

### Hub Events (Receive in Flutter)

| Event | Payload | Description |
|-------|---------|-------------|
| `UserRegistered` | `{user_id, is_active, message}` | User successfully registered |
| `UserRegistrationFailed` | `{user_id, error}` | Registration failed |
| `LocationUpdated` | `LocationResponseDto` | New location received |

---

## Summary

✅ **Backend**:
- `LocationHub.RegisterUser(userId)` → Mark user ACTIVE
- `LocationHub.OnDisconnectedAsync()` → Mark user INACTIVE
- `UserService.SetUserActiveStatusAsync()` → Update database

✅ **Flutter**:
- Connect to hub
- Call `RegisterUser(userId)` after connection
- Handle app lifecycle (pause/resume)
- Disconnect on logout

✅ **Database**:
- `users.is_active` automatically updated
- Query active users: `SELECT * FROM users WHERE is_active = true`
