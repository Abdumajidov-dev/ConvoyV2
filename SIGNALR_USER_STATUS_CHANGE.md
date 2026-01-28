# SignalR User Status Change & Disconnect

User o'zi status'ini `inactive` qilganda SignalR connection'ni ham yopish.

---

## Backend Implementation

### 1. Existing Endpoint

**Endpoint**: `PATCH /api/users/{id}/status?isActive=false`

**File**: `Convoy.Api/Controllers/UserController.cs`

Bu endpoint allaqachon mavjud va user statusni o'zgartiradi.

### 2. New SignalR Method: UnregisterUser

**File**: `Convoy.Api/Hubs/LocationHub.cs`

```csharp
/// <summary>
/// User o'zini inactive qilganda SignalR connection'ni yopish
/// Flutter client bu metodini chaqirishi kerak status false qilgandan keyin
/// </summary>
public async Task UnregisterUser(int userId)
{
    var connectionId = Context.ConnectionId;

    try
    {
        // 1. User'ni inactive qilish
        await _userService.SetUserActiveStatusAsync(userId, false);

        // 2. Connection -> UserId mapping o'chirish
        _connectionUserMap.TryRemove(connectionId, out _);

        _logger.LogInformation("🔴 User marked as INACTIVE (manual): user_id={UserId}", userId);

        // 3. Client'ga tasdiqlash yuborish
        await Clients.Caller.SendAsync("UserUnregistered", new {
            user_id = userId,
            is_active = false,
            message = "User successfully unregistered and marked as inactive"
        });

        // 4. Connection'ni yopish
        Context.Abort();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error unregistering user {UserId}", userId);
        await Clients.Caller.SendAsync("UserUnregistrationFailed", new {
            user_id = userId,
            error = ex.Message
        });
    }
}
```

---

## Flutter Integration

### Variant 1: SignalR orqali (Recommended)

Flutter client status false qilmoqchi bo'lganda, SignalR hub metodini chaqiradi:

```dart
class UserRepository {
  HubConnection? _hubConnection;

  // ... existing code ...

  /// User o'zini inactive qilish va SignalR disconnect
  Future<void> setUserInactive(int userId) async {
    try {
      if (!isConnected) {
        print('⚠️  SignalR not connected, skipping UnregisterUser');
        return;
      }

      print('🔴 Unregistering user: $userId');

      // SignalR hub metodini chaqirish
      await _hubConnection?.invoke('UnregisterUser', args: [userId]);

      print('✅ User unregistered, connection will be closed by server');

      // Server connection'ni o'zi yopadi (Context.Abort())
      // Lekin local state'ni ham tozalash yaxshi
      await Future.delayed(Duration(milliseconds: 500));
      _hubConnection = null;

    } catch (e, st) {
      if (kDebugMode) {
        print('❌ Unregister error: $e\n$st');
      }
      rethrow;
    }
  }

  // Listen for server events
  void _setupConnectionHandlers({required int userId}) {
    // ... existing handlers ...

    // ✅ Listen for unregister confirmation
    _hubConnection?.on('UserUnregistered', (arguments) {
      if (kDebugMode) {
        print('✅ UserUnregistered event received: ${arguments?.first}');
        print('🔴 Connection will be closed by server');
      }
    });

    _hubConnection?.on('UserUnregistrationFailed', (arguments) {
      if (kDebugMode) {
        print('❌ UserUnregistrationFailed event received: ${arguments?.first}');
      }
    });
  }
}
```

### Variant 2: REST API + Manual Disconnect

Agar REST API endpoint ishlatmoqchi bo'lsangiz:

```dart
class UserService {
  final ApiClient apiClient;
  final UserRepository signalRRepo;

  Future<void> setUserInactive(int userId) async {
    try {
      // 1. REST API orqali status false qilish
      print('🔄 Updating user status to inactive via API...');

      final response = await apiClient.patch(
        '/api/users/$userId/status',
        queryParameters: {'isActive': false},
      );

      if (response.statusCode == 200) {
        print('✅ User status updated to inactive');

        // 2. SignalR disconnect qilish
        print('🔴 Disconnecting from SignalR...');
        await signalRRepo.disconnectHub();

        print('✅ Disconnected from SignalR');
      } else {
        throw Exception('Failed to update user status: ${response.statusCode}');
      }
    } catch (e) {
      print('❌ Error setting user inactive: $e');
      rethrow;
    }
  }
}
```

### Variant 3: Hybrid (Recommended for Reliability)

Status o'zgarganda har ikki yo'lni ishlatish:

```dart
class UserService {
  final ApiClient apiClient;
  final UserRepository signalRRepo;

  Future<void> setUserInactive(int userId) async {
    try {
      // Option A: SignalR orqali (agar connected bo'lsa)
      if (signalRRepo.isConnected) {
        print('🔄 Using SignalR to set user inactive...');
        await signalRRepo.setUserInactive(userId);
        print('✅ User set to inactive via SignalR');
      }
      // Option B: REST API orqali (agar SignalR yo'q bo'lsa)
      else {
        print('🔄 SignalR not connected, using REST API...');

        final response = await apiClient.patch(
          '/api/users/$userId/status',
          queryParameters: {'isActive': false},
        );

        if (response.statusCode == 200) {
          print('✅ User status updated via REST API');
        } else {
          throw Exception('Failed to update status: ${response.statusCode}');
        }
      }
    } catch (e) {
      print('❌ Error: $e');
      rethrow;
    }
  }
}
```

---

## Usage Examples

### Example 1: User clicks "Go Offline" button

```dart
class ProfileScreen extends StatelessWidget {
  final UserService userService;
  final int currentUserId;

  Future<void> _goOffline() async {
    try {
      // Show loading
      showDialog(context: context, builder: (_) => LoadingDialog());

      // Set user inactive
      await userService.setUserInactive(currentUserId);

      // Close loading
      Navigator.pop(context);

      // Show success message
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('You are now offline'))
      );

    } catch (e) {
      // Close loading
      Navigator.pop(context);

      // Show error
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Error: $e'))
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Column(
        children: [
          ElevatedButton(
            onPressed: _goOffline,
            child: Text('Go Offline'),
          ),
        ],
      ),
    );
  }
}
```

### Example 2: User logs out

```dart
Future<void> logout() async {
  try {
    // 1. Set user inactive (disconnect SignalR)
    await userService.setUserInactive(currentUserId);

    // 2. Call logout API (if exists)
    await authService.logout();

    // 3. Clear local storage
    await storage.clear();

    // 4. Navigate to login
    Navigator.of(context).pushReplacementNamed('/login');

  } catch (e) {
    print('Logout error: $e');
  }
}
```

### Example 3: App goes to background

```dart
class MyApp extends StatefulWidget {
  @override
  _MyAppState createState() => _MyAppState();
}

class _MyAppState extends State<MyApp> with WidgetsBindingObserver {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state == AppLifecycleState.paused) {
      // App went to background - disconnect SignalR
      print('📱 App paused - disconnecting SignalR');
      signalRRepo.disconnectHub();

      // Note: Don't set user inactive here, only disconnect
      // User will be marked inactive automatically by OnDisconnectedAsync
    }
    else if (state == AppLifecycleState.resumed) {
      // App returned to foreground - reconnect
      print('📱 App resumed - reconnecting SignalR');
      signalRRepo.connect(userId: currentUserId);
    }
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    super.dispose();
  }
}
```

---

## Flow Diagrams

### Variant 1: SignalR orqali

```
┌────────────────────────────────────────────────────────┐
│               Flutter Client                            │
└────────────────────────────────────────────────────────┘
                      │
                      │ 1. User clicks "Go Offline"
                      ▼
┌────────────────────────────────────────────────────────┐
│  hubConnection.invoke('UnregisterUser', args: [userId]) │
└────────────────────────────────────────────────────────┘
                      │
                      ▼
┌────────────────────────────────────────────────────────┐
│         LocationHub.UnregisterUser(userId)              │
│  - SetUserActiveStatusAsync(userId, false)              │
│  - Remove from _connectionUserMap                       │
│  - Send "UserUnregistered" event                        │
│  - Context.Abort() → Close connection                   │
└────────────────────────────────────────────────────────┘
                      │
                      ▼
┌────────────────────────────────────────────────────────┐
│           Flutter receives events:                      │
│  - "UserUnregistered": {user_id, is_active: false}      │
│  - Connection closed by server                          │
└────────────────────────────────────────────────────────┘
```

### Variant 2: REST API orqali

```
┌────────────────────────────────────────────────────────┐
│               Flutter Client                            │
└────────────────────────────────────────────────────────┘
                      │
                      │ 1. User clicks "Go Offline"
                      ▼
┌────────────────────────────────────────────────────────┐
│  PATCH /api/users/{id}/status?isActive=false            │
└────────────────────────────────────────────────────────┘
                      │
                      ▼
┌────────────────────────────────────────────────────────┐
│       UserController.ChangeUserStatus()                 │
│  - UpdateStatusAsync(id, false)                         │
│  - Response: {status: true, ...}                        │
└────────────────────────────────────────────────────────┘
                      │
                      ▼
┌────────────────────────────────────────────────────────┐
│         Flutter receives response                       │
│  - Manually disconnect SignalR                          │
│  - await signalRRepo.disconnectHub()                    │
└────────────────────────────────────────────────────────┘
```

---

## Testing

### Test 1: SignalR orqali inactive qilish

```dart
// Flutter
await signalRRepo.setUserInactive(5475);

// Expected backend logs:
// 🔴 User marked as INACTIVE (manual): user_id=5475, connection=abc123
// ❌ SignalR client disconnected: abc123
```

### Test 2: REST API orqali inactive qilish

```bash
# cURL
curl -X PATCH "http://10.100.104.128:5084/api/users/1/status?isActive=false" \
  -H "Authorization: Bearer {token}"

# Response:
{
  "status": true,
  "message": "User statusi muvaffaqiyatli o'zgartirildi",
  "data": true
}
```

### Test 3: Database tekshirish

```sql
SELECT user_id, name, is_active, updated_at
FROM users
WHERE user_id = 5475;

-- Expected:
-- user_id | name  | is_active | updated_at
-- 5475    | Avaz  | false     | 2026-01-28 13:00:00
```

---

## Important Notes

### 1. SignalR UnregisterUser vs Manual Disconnect

| Method | User Status | SignalR Connection | When to Use |
|--------|-------------|-------------------|-------------|
| `UnregisterUser()` | ✅ Set to `false` | ✅ Closed by server | User explicitly goes offline |
| `disconnectHub()` | ✅ Set to `false` (by `OnDisconnectedAsync`) | ✅ Closed by client | App closes, network issue |
| REST API + disconnect | ✅ Set to `false` | ✅ Closed by client | SignalR not connected |

### 2. Context.Abort() vs stop()

- **`Context.Abort()`** (Backend): Immediately closes connection from server side
- **`stop()`** (Flutter): Gracefully closes connection from client side

Both trigger `OnDisconnectedAsync`, but `Abort()` is instant.

### 3. Race Conditions

If user clicks "Go Offline" multiple times quickly:

```dart
bool _isDisconnecting = false;

Future<void> _goOffline() async {
  if (_isDisconnecting) {
    print('⚠️  Already disconnecting, skipping...');
    return;
  }

  _isDisconnecting = true;

  try {
    await userService.setUserInactive(currentUserId);
  } finally {
    _isDisconnecting = false;
  }
}
```

### 4. Background App Behavior

**Recommendation**: Don't set user inactive when app goes to background, only disconnect:

```dart
// ❌ BAD - User shown as offline when app in background
await userService.setUserInactive(userId);

// ✅ GOOD - Just disconnect, OnDisconnectedAsync will mark inactive
await signalRRepo.disconnectHub();
```

---

## API Reference

### SignalR Hub Methods

| Method | Parameters | Description |
|--------|-----------|-------------|
| `RegisterUser` | `int userId` | Mark user ACTIVE |
| `UnregisterUser` | `int userId` | Mark user INACTIVE + close connection |

### SignalR Events

| Event | Payload | Description |
|-------|---------|-------------|
| `UserRegistered` | `{user_id, is_active, message}` | User marked active |
| `UserUnregistered` | `{user_id, is_active, message}` | User marked inactive |
| `UserUnregistrationFailed` | `{user_id, error}` | Unregister failed |

### REST API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/users/{id}/status?isActive=false` | PATCH | Set user inactive |

---

## Summary

✅ **Backend**:
- `LocationHub.UnregisterUser(userId)` - Set user inactive + close connection
- `UserController.ChangeUserStatus(id, false)` - REST API alternative

✅ **Flutter**:
- **Variant 1**: `hubConnection.invoke('UnregisterUser')` (Recommended)
- **Variant 2**: REST API + `disconnectHub()`
- **Variant 3**: Hybrid approach (try SignalR, fallback to REST)

✅ **When to Use**:
- User clicks "Go Offline" → Use `UnregisterUser()`
- User logs out → Use `UnregisterUser()` or REST API
- App closes → Automatic via `OnDisconnectedAsync`
- App background → Just `disconnectHub()`, don't mark inactive
