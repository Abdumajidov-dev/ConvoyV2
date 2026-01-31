# Filtered Locations with Clustering & Stopped Time

## Overview

`POST /api/locations/multiple_users` endpoint endi **avtomatik clustering** bilan ishlaydi va faqat **muhim locationlar**ni qaytaradi.

## Qanday ishlaydi?

### Input: 43 ta location

```
User A: 43 ta location
├── 09:15 - 09:45 → 25 ta location (bir joyda)
├── 10:00 - 10:20 → 15 ta location (boshqa joyda)
└── 11:00 - 11:05 → 3 ta location (yana boshqa joyda)
```

### Processing: Clustering (10 metr radius)

1. **Cluster 1**: 25 ta location bir-biridan 10 metr ichida
2. **Cluster 2**: 15 ta location bir-biridan 10 metr ichida
3. **Cluster 3**: 3 ta location bir-biridan 10 metr ichida

### Output: Faqat 3 ta location (har cluster uchun 1 ta)

```json
"locations": [
  {
    "id": 10,
    "latitude": 40.470100,    // Cluster 1 markazi
    "longitude": 71.908897,
    "recorded_at": "2026-01-31T09:30:00",
    "stopped_time": 30,       // ← 30 daqiqa shu joyda turdi
    ...
  },
  {
    "id": 35,
    "latitude": 40.460578,    // Cluster 2 markazi
    "longitude": 71.905432,
    "recorded_at": "2026-01-31T10:10:00",
    "stopped_time": 20,       // ← 20 daqiqa
    ...
  },
  {
    "id": 40,
    "latitude": 40.450123,    // Cluster 3 markazi
    "longitude": 71.900000,
    "recorded_at": "2026-01-31T11:02:00",
    "stopped_time": 5,        // ← 5 daqiqa
    ...
  }
]
```

**Natija:** 43 ta → 3 ta location (14x kam data)

---

## API Request

```http
POST /api/locations/multiple_users
Content-Type: application/json

{
  "user_ids": [5277, 5475],
  "branch_guid": null,
  "date": "2026-01-31",
  "start_hour": null,
  "end_hour": null,
  "limit": 100
}
```

---

## API Response

```json
{
  "status": true,
  "message": "2 ta user (user_ids) uchun 2026-01-31 kunida 10 ta location olindi (clustering bilan)",
  "data": [
    {
      "id": 1,
      "name": "Тўлқин Тўраев",
      "phone": "+998901234567",
      "branch_guid": "...",
      "branch": null,
      "image": null,
      "is_active": true,
      "created_at": "2026-01-23T12:10:00",
      "updated_at": "2026-01-31T10:30:00",

      "locations": [
        {
          "id": 124,
          "user_id": 5277,
          "latitude": 40.367032,
          "longitude": 71.746694,
          "recorded_at": "2026-01-31T05:50:00+00:00",
          "accuracy": null,
          "speed": 4.00,
          "heading": null,
          "altitude": null,
          "activity_type": null,
          "activity_confidence": null,
          "is_moving": false,
          "battery_level": null,
          "is_charging": null,
          "distance_from_previous": 0.00,
          "stopped_time": 30,       // ← YANGI: 30 daqiqa shu joyda turdi
          "created_at": "2026-01-31T05:50:21+00:00"
        },
        {
          "id": 130,
          "user_id": 5277,
          "latitude": 40.360000,
          "longitude": 71.740000,
          "recorded_at": "2026-01-31T06:30:00+00:00",
          "stopped_time": 15,       // ← 15 daqiqa
          ...
        }
      ]
    },
    {
      "id": 2,
      "name": "Авазбек Абдумажидов",
      "locations": [...]
    }
  ]
}
```

---

## Response Structure

### UserWithLocationsDto

```typescript
{
  id: number,
  name: string,
  phone: string,
  branch_guid: string,
  branch: object | null,
  image: string | null,
  is_active: boolean,
  created_at: datetime,
  updated_at: datetime,

  // Faqat muhim locationlar (cluster markazlari)
  locations: [
    {
      ...LocationResponseDto,
      stopped_time: number | null  // ← Daqiqalarda
    }
  ]
}
```

### LocationResponseDto (with stopped_time)

Har bir location uchun yangi field:

- **`stopped_time`**: `number | null`
  - O'sha joyda qancha vaqt turganligini ko'rsatadi (daqiqalarda)
  - Cluster'dagi birinchi va oxirgi location orasidagi farq
  - Agar user harakat qilgan bo'lsa: `0` yoki `null`

---

## Clustering Logic

### Algorithm

1. **Input**: Barcha locationlar (vaqt bo'yicha tartiblangan)
2. **Grouping**: 10 metr ichidagi locationlarni bir cluster'ga birlashtirish
3. **Representative**: Har bir cluster uchun markazga eng yaqin locationni tanlash
4. **Stopped Time**: Cluster ichidagi vaqt farqini hisoblash
5. **Output**: Faqat representative locationlar (stopped_time bilan)

### Example

```
Input (25 locations in same place):
├── 09:15:00 → Location 1 (40.470100, 71.908897)
├── 09:16:00 → Location 2 (40.470105, 71.908900) [8m from prev]
├── 09:17:00 → Location 3 (40.470102, 71.908895) [5m from prev]
├── ...
└── 09:45:00 → Location 25 (40.470098, 71.908892) [7m from prev]

Processing:
- All 25 locations within 10m radius → 1 cluster
- Cluster center: (40.470100, 71.908897)
- Closest to center: Location 1
- Stopped time: 09:45 - 09:15 = 30 minutes

Output (1 location):
{
  ...Location 1 data,
  stopped_time: 30
}
```

---

## Benefits

### 1. Performance

- **43 locations → 10 locations** = 4.3x kam data
- Tezroq network transfer
- Kam memory consumption
- Tezroq map rendering

### 2. User Experience

- Kam marker'lar map'da (10 o'rniga 3)
- Har bir marker ma'noli (cluster representative)
- Stopped time ko'rsatish (user qayerda qancha turdi)

### 3. Data Quality

- Faqat muhim locationlar
- Duplicate/noise locationlar filterlangan
- Real user harakati ko'rinadi

---

## Frontend Integration

### Flutter Map Display

```dart
// Response'dan locationlarni olish
final response = await api.getMultipleUsersLocations(request);

for (var user in response.data) {
  for (var location in user.locations) {
    // Har bir location cluster representative
    // Stopped time mavjud

    addMarker(
      position: LatLng(location.latitude, location.longitude),
      infoWindow: InfoWindow(
        title: user.name,
        snippet: 'Stopped: ${location.stoppedTime ?? 0} min',
      ),
      icon: location.stoppedTime > 20
        ? redMarker    // Uzoq turgan joylar qizil
        : blueMarker,  // Qisqa turgan joylar ko'k
    );
  }
}
```

### Polyline (Route)

```dart
// Locationlarni bog'lash
List<LatLng> routePoints = user.locations
  .map((loc) => LatLng(loc.latitude, loc.longitude))
  .toList();

Polyline(
  polylineId: PolylineId('user_${user.id}_route'),
  points: routePoints,
  color: Colors.blue,
  width: 3,
);
```

### Statistics

```dart
// User statistics
int totalStops = user.locations.length;
int totalStoppedMinutes = user.locations
  .map((l) => l.stoppedTime ?? 0)
  .reduce((a, b) => a + b);

print('$totalStops ta joyda bo\'lgan');
print('Jami $totalStoppedMinutes daqiqa turgan');
```

---

## Configuration

### Cluster Radius

Default: 10 metr

```csharp
// LocationClusteringService.cs:13
private const double ClusterRadiusMeters = 10.0;
```

O'zgartirish uchun bu qiymatni edit qiling.

### Limit

Request'da limit parametri (har bir user uchun):

```json
{
  "limit": 100  // Maksimal 100 ta original location oladi
}
```

Clustering'dan keyin natija kamroq bo'ladi (masalan 10-15 ta).

---

## Comparison

### Old vs New

| Aspekt | Oldingi (without clustering) | Yangi (with clustering) |
|--------|------------------------------|-------------------------|
| Response size | 43 ta location | 10 ta location |
| Data size | ~15 KB | ~3.5 KB |
| Map markers | 43 ta marker | 10 ta marker |
| Stopped time | ❌ Yo'q | ✅ Har birida bor |
| User insight | Kam ma'lumotli | Ko'proq ma'lumotli |
| Performance | Sekin | Tez |

---

## Troubleshooting

### Problem: Locationlar kam qaytaradi

**Sabab:** Bu normal - clustering'dan keyin kamroq location qaytaradi.

**Yechim:** Agar original locationlar kerak bo'lsa:
1. Limit'ni oshiring: `"limit": 500`
2. Yoki alohida endpoint ishlating (clustering'siz)

### Problem: stopped_time 0 yoki null

**Sabab:**
- User harakat qilgan (bir joyda turmagan)
- Yoki cluster faqat 1 ta location'dan iborat

**Yechim:** Bu normal behavior - harakat qilayotgan locationlar uchun stopped_time 0 bo'ladi.

### Problem: Bir xil joyda 2 ta marker

**Sabab:** 10 metrdan ko'proq masofa bor.

**Yechim:** Cluster radius'ni oshiring (masalan 20 metr):
```csharp
private const double ClusterRadiusMeters = 20.0;
```

---

## Testing

### Postman/REST Client

```http
POST http://localhost:5084/api/locations/multiple_users

{
  "user_ids": [5277, 5475],
  "date": "2026-01-31",
  "start_hour": null,
  "end_hour": null,
  "limit": 100
}
```

### Expected Response

- ✅ `locations` array kamroq elementlar bilan
- ✅ Har bir location'da `stopped_time` bor
- ✅ Message'da "(clustering bilan)" yozuvi
- ✅ Barcha location'lar muhim joylarni ko'rsatadi

---

## Summary

✅ **Qisqacha:**
- 43 ta location → 10 ta location (clustering bilan)
- Har birida `stopped_time` bor
- Faqat muhim joylar qaytaradi
- Tez, samarali, ma'lumotli

✅ **Files changed:**
1. `UserWithLocationsDto` - clusters array o'chirildi
2. `LocationClusteringService` - `GetFilteredLocationsWithStoppedTime()` qo'shildi
3. `LocationService` - filtered locations ishlatiladi

✅ **Backward compatibility:**
- Response format bir xil (locations array)
- Faqat kam location qaytaradi
- stopped_time yangi field (optional)
