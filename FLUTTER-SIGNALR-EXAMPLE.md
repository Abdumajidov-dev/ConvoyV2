# Flutter'da SignalR Ishlatish - To'liq Qo'llanma

## 1. Package O'rnatish

```yaml
# pubspec.yaml
dependencies:
  signalr_netcore: ^1.3.7  # SignalR client for Flutter
  http: ^1.1.0
```

## 2. SignalR Service Yaratish

```dart
// lib/services/location_signalr_service.dart

import 'package:signalr_netcore/signalr_client.dart';

class LocationSignalRService {
  HubConnection? _hubConnection;
  String serverUrl = 'http://0.0.0.0:5084';  // Sizning API manzil

  // Callback functions - location kelganda nima qilish
  Function(Map<String, dynamic>)? onLocationReceived;
  Function(String)? onConnectionStateChanged;

  LocationSignalRService({
    this.onLocationReceived,
    this.onConnectionStateChanged,
  });

  /// SignalR'ga ulanish
  Future<void> connect({String? jwtToken}) async {
    try {
      // Hub connection yaratish
      _hubConnection = HubConnectionBuilder()
          .withUrl(
            '$serverUrl/hubs/location',
            options: HttpConnectionOptions(
              accessTokenFactory: jwtToken != null
                  ? () async => jwtToken
                  : null,  // JWT token kerak bo'lsa
            ),
          )
          .withAutomaticReconnect()  // Avtomatik qayta ulanish
          .build();

      // Connection state o'zgarishlarini kuzatish
      _hubConnection!.onclose((error) {
        print('SignalR disconnected: $error');
        onConnectionStateChanged?.call('Disconnected');
      });

      _hubConnection!.onreconnecting((error) {
        print('SignalR reconnecting: $error');
        onConnectionStateChanged?.call('Reconnecting');
      });

      _hubConnection!.onreconnected((connectionId) {
        print('SignalR reconnected: $connectionId');
        onConnectionStateChanged?.call('Connected');
      });

      // "LocationUpdated" event'ini tinglash
      _hubConnection!.on('LocationUpdated', _handleLocationUpdate);

      // Ulanish
      await _hubConnection!.start();
      print('SignalR connected successfully!');
      onConnectionStateChanged?.call('Connected');

    } catch (e) {
      print('SignalR connection error: $e');
      onConnectionStateChanged?.call('Error');
      rethrow;
    }
  }

  /// Location update kelganda
  void _handleLocationUpdate(List<Object?>? arguments) {
    if (arguments == null || arguments.isEmpty) return;

    try {
      final locationData = arguments[0] as Map<String, dynamic>;
      print('New location received: $locationData');

      // Callback orqali UI'ga xabar berish
      onLocationReceived?.call(locationData);
    } catch (e) {
      print('Error handling location update: $e');
    }
  }

  /// Bitta user'ni track qilish
  Future<void> joinUserTracking(int userId) async {
    try {
      await _hubConnection?.invoke('JoinUserTracking', args: [userId]);
      print('Joined tracking for user $userId');
    } catch (e) {
      print('Error joining user tracking: $e');
    }
  }

  /// User tracking'dan chiqish
  Future<void> leaveUserTracking(int userId) async {
    try {
      await _hubConnection?.invoke('LeaveUserTracking', args: [userId]);
      print('Left tracking for user $userId');
    } catch (e) {
      print('Error leaving user tracking: $e');
    }
  }

  /// Barcha user'larni track qilish
  Future<void> joinAllUsersTracking() async {
    try {
      await _hubConnection?.invoke('JoinAllUsersTracking');
      print('Joined tracking for all users');
    } catch (e) {
      print('Error joining all users tracking: $e');
    }
  }

  /// Barcha user'lar tracking'dan chiqish
  Future<void> leaveAllUsersTracking() async {
    try {
      await _hubConnection?.invoke('LeaveAllUsersTracking');
      print('Left tracking for all users');
    } catch (e) {
      print('Error leaving all users tracking: $e');
    }
  }

  /// Ulanishni yopish
  Future<void> disconnect() async {
    try {
      await _hubConnection?.stop();
      print('SignalR disconnected');
    } catch (e) {
      print('Error disconnecting: $e');
    }
  }

  /// Ulanish holati
  bool get isConnected =>
      _hubConnection?.state == HubConnectionState.Connected;
}
```

## 3. Flutter UI'da Ishlatish - Location Tracking Screen

```dart
// lib/screens/location_tracking_screen.dart

import 'package:flutter/material.dart';
import 'package:google_maps_flutter/google_maps_flutter.dart';
import '../services/location_signalr_service.dart';
import '../models/location_model.dart';

class LocationTrackingScreen extends StatefulWidget {
  final int userId;
  final String? jwtToken;

  const LocationTrackingScreen({
    Key? key,
    required this.userId,
    this.jwtToken,
  }) : super(key: key);

  @override
  State<LocationTrackingScreen> createState() => _LocationTrackingScreenState();
}

class _LocationTrackingScreenState extends State<LocationTrackingScreen> {
  late LocationSignalRService _signalRService;
  GoogleMapController? _mapController;

  // State variables
  String _connectionStatus = 'Disconnected';
  LatLng? _currentLocation;
  List<LatLng> _locationHistory = [];
  Map<String, dynamic>? _lastLocationData;

  @override
  void initState() {
    super.initState();
    _initializeSignalR();
  }

  /// SignalR'ni sozlash va ulanish
  void _initializeSignalR() async {
    _signalRService = LocationSignalRService(
      // Location kelganda nima qilish
      onLocationReceived: (locationData) {
        setState(() {
          _lastLocationData = locationData;

          // Koordinatalarni olish
          double lat = locationData['latitude'];
          double lng = locationData['longitude'];
          _currentLocation = LatLng(lat, lng);

          // History'ga qo'shish
          _locationHistory.add(_currentLocation!);

          // Xarita'ni yangi pozitsiyaga harakatlantirish
          _mapController?.animateCamera(
            CameraUpdate.newLatLng(_currentLocation!),
          );
        });
      },

      // Connection holati o'zgarganda
      onConnectionStateChanged: (status) {
        setState(() {
          _connectionStatus = status;
        });
      },
    );

    try {
      // SignalR'ga ulanish
      await _signalRService.connect(jwtToken: widget.jwtToken);

      // User'ni track qilishni boshlash
      await _signalRService.joinUserTracking(widget.userId);

      // Yoki barcha user'larni track qilish:
      // await _signalRService.joinAllUsersTracking();

    } catch (e) {
      print('Failed to initialize SignalR: $e');
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Connection failed: $e')),
      );
    }
  }

  @override
  void dispose() {
    _signalRService.disconnect();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('Live Tracking - User ${widget.userId}'),
        actions: [
          // Connection status indicator
          Padding(
            padding: EdgeInsets.all(8.0),
            child: Center(
              child: Container(
                padding: EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                decoration: BoxDecoration(
                  color: _connectionStatus == 'Connected'
                      ? Colors.green
                      : Colors.red,
                  borderRadius: BorderRadius.circular(20),
                ),
                child: Text(
                  _connectionStatus,
                  style: TextStyle(color: Colors.white, fontSize: 12),
                ),
              ),
            ),
          ),
        ],
      ),
      body: Column(
        children: [
          // Xarita
          Expanded(
            flex: 2,
            child: _currentLocation != null
                ? GoogleMap(
                    initialCameraPosition: CameraPosition(
                      target: _currentLocation!,
                      zoom: 15,
                    ),
                    onMapCreated: (controller) {
                      _mapController = controller;
                    },
                    markers: {
                      if (_currentLocation != null)
                        Marker(
                          markerId: MarkerId('current_location'),
                          position: _currentLocation!,
                          icon: BitmapDescriptor.defaultMarkerWithHue(
                            BitmapDescriptor.hueBlue,
                          ),
                          infoWindow: InfoWindow(
                            title: 'Current Position',
                            snippet: 'User ${widget.userId}',
                          ),
                        ),
                    },
                    polylines: {
                      if (_locationHistory.length > 1)
                        Polyline(
                          polylineId: PolylineId('route'),
                          points: _locationHistory,
                          color: Colors.blue,
                          width: 4,
                        ),
                    },
                  )
                : Center(
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        CircularProgressIndicator(),
                        SizedBox(height: 16),
                        Text('Waiting for location data...'),
                      ],
                    ),
                  ),
          ),

          // Location ma'lumotlari
          Expanded(
            flex: 1,
            child: _lastLocationData != null
                ? _buildLocationDetails()
                : Center(child: Text('No data yet')),
          ),
        ],
      ),
    );
  }

  /// Location ma'lumotlarini ko'rsatish
  Widget _buildLocationDetails() {
    return Container(
      padding: EdgeInsets.all(16),
      child: ListView(
        children: [
          _buildDetailRow('User ID', _lastLocationData!['userId'].toString()),
          _buildDetailRow('Latitude', _lastLocationData!['latitude'].toString()),
          _buildDetailRow('Longitude', _lastLocationData!['longitude'].toString()),
          _buildDetailRow('Speed', '${_lastLocationData!['speed'] ?? 0} m/s'),
          _buildDetailRow('Accuracy', '${_lastLocationData!['accuracy'] ?? 0} m'),
          _buildDetailRow('Battery', '${_lastLocationData!['batteryLevel'] ?? 0}%'),
          _buildDetailRow('Moving', _lastLocationData!['isMoving'] ? 'Yes' : 'No'),
          _buildDetailRow(
            'Distance from Previous',
            '${_lastLocationData!['distanceFromPrevious'] ?? 0} m',
          ),
          _buildDetailRow(
            'Recorded At',
            _lastLocationData!['recordedAt'].toString(),
          ),
        ],
      ),
    );
  }

  Widget _buildDetailRow(String label, String value) {
    return Padding(
      padding: EdgeInsets.symmetric(vertical: 4),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: TextStyle(fontWeight: FontWeight.bold)),
          Text(value),
        ],
      ),
    );
  }
}
```

## 4. Location Model

```dart
// lib/models/location_model.dart

class LocationModel {
  final int id;
  final int userId;
  final DateTime recordedAt;
  final double latitude;
  final double longitude;
  final double? accuracy;
  final double? speed;
  final double? heading;
  final double? altitude;
  final String? activityType;
  final int? activityConfidence;
  final bool isMoving;
  final int? batteryLevel;
  final bool isCharging;
  final double? distanceFromPrevious;
  final DateTime createdAt;

  LocationModel({
    required this.id,
    required this.userId,
    required this.recordedAt,
    required this.latitude,
    required this.longitude,
    this.accuracy,
    this.speed,
    this.heading,
    this.altitude,
    this.activityType,
    this.activityConfidence,
    required this.isMoving,
    this.batteryLevel,
    required this.isCharging,
    this.distanceFromPrevious,
    required this.createdAt,
  });

  factory LocationModel.fromJson(Map<String, dynamic> json) {
    return LocationModel(
      id: json['id'],
      userId: json['userId'],
      recordedAt: DateTime.parse(json['recordedAt']),
      latitude: json['latitude'].toDouble(),
      longitude: json['longitude'].toDouble(),
      accuracy: json['accuracy']?.toDouble(),
      speed: json['speed']?.toDouble(),
      heading: json['heading']?.toDouble(),
      altitude: json['altitude']?.toDouble(),
      activityType: json['activityType'],
      activityConfidence: json['activityConfidence'],
      isMoving: json['isMoving'],
      batteryLevel: json['batteryLevel'],
      isCharging: json['isCharging'],
      distanceFromPrevious: json['distanceFromPrevious']?.toDouble(),
      createdAt: DateTime.parse(json['createdAt']),
    );
  }
}
```

## 5. Ishlatish - Main.dart

```dart
import 'package:flutter/material.dart';
import 'screens/location_tracking_screen.dart';

void main() {
  runApp(MyApp());
}

class MyApp extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Convoy GPS Tracking',
      home: LocationTrackingScreen(
        userId: 1,  // Track qilinadigan user ID
        jwtToken: 'your-jwt-token-here',  // Auth uchun
      ),
    );
  }
}
```

## SignalR Methods va Events

### Server Methods (Flutter'dan chaqirish):

```dart
// Bitta user'ni track qilish
await _signalRService.joinUserTracking(123);

// User tracking'dan chiqish
await _signalRService.leaveUserTracking(123);

// Barcha user'larni track qilish
await _signalRService.joinAllUsersTracking();

// Barcha'dan chiqish
await _signalRService.leaveAllUsersTracking();
```

### Server Events (Server'dan keladi, Flutter tinglaydi):

```dart
// "LocationUpdated" event'ini tinglash
_hubConnection.on('LocationUpdated', (arguments) {
  // arguments[0] - yangi location data
  var locationData = arguments[0] as Map<String, dynamic>;
  print('New location: $locationData');
});
```

## Qanday Ishlaydi - Step by Step

1. **Client ulanadi**: Flutter app SignalR hub'ga ulanadi (`/hubs/location`)
2. **Group'ga qo'shiladi**: `joinUserTracking(123)` - User 123'ni track qilish
3. **Server'da location yaratiladi**: Biror qurilma POST qiladi location'ni
4. **LocationService broadcast qiladi**: Server avtomatik SignalR orqali yuboradi
5. **Flutter event oladi**: `LocationUpdated` event keladi
6. **UI yangilanadi**: Xarita va ma'lumotlar real-time o'zgaradi

## Advantages (Afzalliklar)

- **Real-time**: Polling kerak emas, server avtomatik yuboradi
- **Efficient**: Faqat o'zgarish bo'lganda data keladi
- **Bidirectional**: Ikki tomonlama aloqa
- **Auto-reconnect**: Ulanish uzilsa, avtomatik qayta ulanadi
- **Groups**: Faqat kerakli user'larni track qilish

## Polling vs SignalR

```dart
// ESKI USUL - Polling (yomon):
Timer.periodic(Duration(seconds: 5), (timer) async {
  // Har 5 sekundda so'rash
  var response = await http.get('/api/locations/latest');
  // 99% vaqtda yangi data yo'q, lekin baribir so'raydi
});

// YANGI USUL - SignalR (yaxshi):
_hubConnection.on('LocationUpdated', (data) {
  // Faqat yangi data bo'lgandagina keladi
  // Server avtomatik yuboradi
});
```

## Testing

API ishlab turganida:
```
Server: http://0.0.0.0:5084
SignalR Hub: http://0.0.0.0:5084/hubs/location
```

Flutter'da serverUrl o'zgartiring:
```dart
String serverUrl = 'http://YOUR_IP:5084';  // Localhost emas, real IP
```
