# Multiple Users Locations with Automatic Clustering

## Overview

`POST /api/locations/multiple_users` endpoint endi avtomatik ravishda locationlarni **clustering** (gruppalash) qiladi.

## Changes

### 1. DTO Update

`UserWithLocationsDto` ga yangi property qo'shildi:

```csharp
public class UserWithLocationsDto
{
    // ... existing properties ...

    // Oddiy locations (eski format, backward compatibility)
    [JsonPropertyName("locations")]
    public List<LocationResponseDto> Locations { get; set; } = new();

    // Clusterlangan locations (yangi format)
    [JsonPropertyName("clusters")]
    public List<ClusteredLocationDto>? Clusters { get; set; }
}
```

### 2. Service Logic

`LocationService.GetMultipleUsersLocationsAsync()` endi har bir user uchun:
1. ✅ Locationlarni oladi
2. ✅ `LocationClusteringService` orqali 10 metr oralig'ida groupalaydi
3. ✅ Har bir cluster uchun `stopped_time` hisoblab beradi

### 3. Clustering Algorithm

- **Radius**: 10 metr
- **Logic**: Ketma-ket locationlar 10 metr ichida bo'lsa, bir cluster'ga kiradi
- **Stopped Time**: Cluster'da qancha vaqt turganligini ko'rsatadi (daqiqalarda)

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

## API Response

```json
{
  "status": true,
  "message": "2 ta user (user_ids) uchun 2026-01-31 kunida 43 ta location olindi (5 ta cluster)",
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

      // Oddiy locations array (backward compatibility)
      "locations": [
        {
          "id": 23,
          "user_id": 5277,
          "latitude": 40.47010090,
          "longitude": 71.90889770,
          "recorded_at": "2026-01-23T04:10:24+00:00",
          "speed": 0.02,
          "is_moving": false,
          "distance_from_previous": 0.00,
          "stopped_time": null,
          "created_at": "2026-01-23T12:10:26+00:00"
        },
        // ... boshqa locationlar
      ],

      // Clusterlangan locations (yangi format)
      "clusters": [
        {
          "cluster_id": 1,
          "user_id": 5277,
          "latitude": 40.470100,    // Cluster markazi (o'rtacha)
          "longitude": 71.908897,
          "start_time": "2026-01-31T09:15:00",
          "end_time": "2026-01-31T09:45:00",
          "stopped_time": 30,       // 30 daqiqa o'sha joyda
          "location_count": 25,     // Cluster ichida 25 ta location
          "locations": null         // Optional: cluster ichidagi locationlar
        },
        {
          "cluster_id": 2,
          "user_id": 5277,
          "latitude": 40.460578,
          "longitude": 71.905432,
          "start_time": "2026-01-31T10:00:00",
          "end_time": "2026-01-31T10:20:00",
          "stopped_time": 20,       // 20 daqiqa
          "location_count": 15,
          "locations": null
        }
      ]
    },
    {
      "id": 2,
      "name": "Авазбек Абдумажидов",
      "locations": [...],
      "clusters": [...]
    }
  ]
}
```

## Response Fields

### `locations` (array)
- **Type**: `LocationResponseDto[]`
- **Purpose**: Backward compatibility
- **Content**: Barcha locationlar vaqt bo'yicha tartiblangan

### `clusters` (array)
- **Type**: `ClusteredLocationDto[]`
- **Purpose**: Yangi clustering format
- **Content**: 10 metr oralig'ida groupalangan locationlar

Each cluster contains:
- `cluster_id`: Cluster raqami (1, 2, 3, ...)
- `latitude`, `longitude`: Cluster markazi (o'rtacha koordinatalar)
- `start_time`: Cluster'ga birinchi kirgan vaqt
- `end_time`: Cluster'dan oxirgi chiqgan vaqt
- `stopped_time`: O'sha joyda turgan vaqt (daqiqalarda)
- `location_count`: Cluster ichidagi locationlar soni
- `locations`: (optional) Cluster ichidagi barcha locationlar

## Use Cases

### Frontend Map Display

1. **Oddiy view** - `locations` array'dan foydalaning
2. **Cluster view** - `clusters` array'dan foydalaning (kam marker, tezroq render)
3. **Detail view** - Cluster bosilganda `locations` array'ni ko'rsating

### Analytics

- `stopped_time` orqali user qayerda qancha vaqt turganini bilish
- Cluster count orqali user nechta joyga borgan
- Location count vs Cluster count - user harakatini tahlil qilish

## Migration Guide

### Eski kod (locations only):
```dart
for (var user in response.data) {
  for (var location in user.locations) {
    // Plot location on map
  }
}
```

### Yangi kod (clusters):
```dart
for (var user in response.data) {
  // Option 1: Show clusters
  for (var cluster in user.clusters ?? []) {
    // Plot cluster marker
    // Show stopped_time as label
  }

  // Option 2: Show all locations (backward compatible)
  for (var location in user.locations) {
    // Plot location on map
  }
}
```

## Performance

- **Without clustering**: 1000 locationlar = 1000 markers (slow)
- **With clustering**: 1000 locationlar = ~50 clusters = 50 markers (fast)

## Configuration

Clustering radius (hozirda 10 metr):
```csharp
// LocationClusteringService.cs
private const double ClusterRadiusMeters = 10.0;
```

## Backward Compatibility

✅ `locations` array hali ham mavjud
✅ Eski API clientlar ishlay beradi
✅ Yangi clientlar `clusters` dan foydalanishi mumkin

## Testing

```bash
# Test with Postman/REST Client
POST http://localhost:5084/api/locations/multiple_users

{
  "user_ids": [5277, 5475],
  "date": "2026-01-31",
  "start_hour": null,
  "end_hour": null,
  "limit": 100
}
```

Check response:
- ✅ `locations` array bor
- ✅ `clusters` array bor
- ✅ Message'da cluster count ko'rinadi
- ✅ Har bir cluster'da `stopped_time` bor

## Future Enhancements

- [ ] Configurable cluster radius (query parameter)
- [ ] Cluster detail on demand (include locations in cluster)
- [ ] Different clustering algorithms (DBSCAN, K-means)
- [ ] Client-side clustering option
