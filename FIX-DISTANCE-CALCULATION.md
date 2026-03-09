# Distance Calculation Fix Guide

## Muammo

Ba'zi userlarning locationlari bor, lekin `distance_from_previous` maydoni **NULL yoki 0** bo'lib qolgan:
- User 3561: 186 ta location, **0 km** ❌
- User 9541: 1439 ta location, **0 km** ❌

## Sabab

Location yaratilganda `GetLastLocationsAsync()` bo'sh natija qaytargan yoki distance hisoblash logikasi ishlamagan.

## Yechim

Barcha locationlarning distance'ini qaytadan hisoblash uchun PostgreSQL function yaratildi.

---

## Step 1: Function'larni Yaratish

```bash
# Windows (PowerShell)
$env:PGPASSWORD='GarantDockerPass'
psql -h 10.21.61.51 -p 5432 -U postgres -d convoydb -f recalculate-distances.sql

# Linux/Mac
PGPASSWORD='GarantDockerPass' psql -h 10.21.61.51 -p 5432 -U postgres -d convoydb -f recalculate-distances.sql
```

---

## Step 2: Distance'ni Qaytadan Hisoblash

### Option 1: Bitta user, bitta kun

```sql
-- User 8558, 2026-03-05 kuni
SELECT * FROM recalculate_distances_for_user_and_date(8558, '2026-03-05', '2026-03-05');
```

**Result:**
```
updated_count | total_distance_meters
--------------+----------------------
776          | 20417.49
```

### Option 2: Barcha userlar, bitta kun

```sql
-- Barcha userlar, 2026-03-05 kuni
SELECT * FROM recalculate_all_distances_for_date('2026-03-05');
```

**Result:**
```
user_id | updated_count | total_distance_km
--------+---------------+-------------------
3561    | 186           | 12.34
8558    | 776           | 20.42
9541    | 1439          | 87.65
```

### Option 3: Bir necha kun

```bash
# 2026-03-01 dan 2026-03-07 gacha
SELECT * FROM recalculate_all_distances_for_date('2026-03-01');
SELECT * FROM recalculate_all_distances_for_date('2026-03-02');
SELECT * FROM recalculate_all_distances_for_date('2026-03-03');
SELECT * FROM recalculate_all_distances_for_date('2026-03-04');
SELECT * FROM recalculate_all_distances_for_date('2026-03-05');
SELECT * FROM recalculate_all_distances_for_date('2026-03-06');
SELECT * FROM recalculate_all_distances_for_date('2026-03-07');
```

---

## Step 3: Daily Report'larni Yangilash

Distance'lar qaytadan hisoblangandan keyin, hisobotlarni ham regenerate qilish kerak:

```sql
-- 2026-03-05 kuni uchun hisobotlarni yangilash
SELECT * FROM generate_daily_reports_for_date('2026-03-05');
```

**Result:**
```
user_id | report_id | distance_km
--------+-----------+-------------
3561    | 26955     | 12.34
8558    | 26952     | 20.42
9541    | 26956     | 87.65
```

---

## Step 4: Natijani Tekshirish

### 4.1. Distance'lar to'g'riligini tekshirish

```sql
SELECT
    user_id,
    COUNT(*) as location_count,
    SUM(distance_from_previous) as total_meters,
    ROUND(SUM(distance_from_previous) / 1000, 2) as total_km,
    MIN(recorded_at) as first_time,
    MAX(recorded_at) as last_time
FROM locations
WHERE user_id IN (3561, 8558, 9541)
  AND recorded_at >= '2026-03-05 00:00:00'
  AND recorded_at < '2026-03-06 00:00:00'
GROUP BY user_id
ORDER BY user_id;
```

**Kutilgan natija:**
```
user_id | location_count | total_meters | total_km | first_time           | last_time
--------+----------------+--------------+----------+----------------------+---------------------
3561    | 186            | 12340.56     | 12.34    | 2026-03-05 09:54:... | 2026-03-05 10:19:...
8558    | 776            | 20417.49     | 20.42    | 2026-03-05 04:22:... | 2026-03-05 17:30:...
9541    | 1439           | 87650.12     | 87.65    | 2026-03-05 03:28:... | 2026-03-05 09:29:...
```

### 4.2. Hisobotlarni tekshirish

```sql
SELECT
    user_id,
    report_date,
    total_distance_km,
    location_count
FROM daily_distance_reports
WHERE user_id IN (3561, 8558, 9541)
  AND report_date = '2026-03-05'
ORDER BY user_id;
```

**Kutilgan natija:**
```
user_id | report_date  | total_distance_km | location_count
--------+--------------+-------------------+----------------
3561    | 2026-03-05   | 12.34             | 186
8558    | 2026-03-05   | 20.42             | 776
9541    | 2026-03-05   | 87.65             | 1439
```

---

## Complete Script (Copy-Paste Ready)

```sql
-- 1. Functionlarni tekshirish
SELECT routine_name, routine_type
FROM information_schema.routines
WHERE routine_schema = 'public'
  AND routine_name IN ('calculate_distance', 'recalculate_distances_for_user_and_date', 'recalculate_all_distances_for_date')
ORDER BY routine_name;

-- 2. Distance'larni qaytadan hisoblash (2026-03-05)
SELECT * FROM recalculate_all_distances_for_date('2026-03-05');

-- 3. Hisobotlarni yangilash
SELECT * FROM generate_daily_reports_for_date('2026-03-05');

-- 4. Natijani tekshirish
SELECT
    user_id,
    COUNT(*) as location_count,
    ROUND(SUM(distance_from_previous) / 1000, 2) as total_km
FROM locations
WHERE user_id IN (3561, 8558, 9541)
  AND recorded_at >= '2026-03-05 00:00:00'
  AND recorded_at < '2026-03-06 00:00:00'
GROUP BY user_id
ORDER BY user_id;

-- 5. Final check: Hisobotlar
SELECT user_id, report_date, total_distance_km, location_count
FROM daily_distance_reports
WHERE user_id IN (3561, 8558, 9541)
  AND report_date = '2026-03-05'
ORDER BY user_id;
```

---

## Kelajakda Muammoning Oldini Olish

### 1. LocationService'da Debug Logging

`LocationService.cs:152` da log bor, lekin uni tekshiring:
```csharp
var lastLocations = await _locationRepository.GetLastLocationsAsync(userId, 1);
_logger.LogInformation("🔍 DEBUG: GetLastLocationsAsync returned {Count} locations for UserId={UserId}",
    lastLocations.Count(), userId);
```

Agar `Count = 0` bo'lsa, muammo `GetLastLocationsAsync()`'da.

### 2. Partition Muammosi

Ehtimol, partition yo'q bo'lgani uchun `GetLastLocationsAsync()` bo'sh natija qaytargan. Yangi oyda locationlar yaratilganda partition yo'q bo'lsa, query ishlamaydi.

**Yechim:** `PartitionMaintenanceService` har doim ishlayotganini tekshiring:
```bash
# API loglarida qidirish
grep "PartitionMaintenanceService" Convoy.Api/logs/*.log
```

### 3. Bulk Insert'da Distance Hisoblash

`PostMultipleLocationsAsync` metodida distance to'g'ri hisoblanayotganini tekshiring:
```csharp
// Har bir location uchun distance hisoblash
foreach (var item in ordered)
{
    if (previousLocation != null)
    {
        var distance = _locationRepository.CalculateDistance(...);
        distanceFromPrevious = (decimal)distance;
    }

    previousLocation = location; // IMPORTANT: Keyingi aylanish uchun
}
```

---

## Summary

1. ✅ `recalculate-distances.sql` scriptni run qiling
2. ✅ `recalculate_all_distances_for_date('2026-03-05')` - distance'larni qaytadan hisoblang
3. ✅ `generate_daily_reports_for_date('2026-03-05')` - hisobotlarni yangilang
4. ✅ Natijani tekshiring (locations va daily_distance_reports)
5. ✅ Kelajakda PartitionMaintenanceService ishlayotganini monitoring qiling

---

## Contacts

Agar muammo davom etsa, quyidagilarni tekshiring:
1. API logs: `LocationService` - "GetLastLocationsAsync" qidiruvi
2. Database logs: `pg_stat_statements` - slow queries
3. Partition list: `SELECT tablename FROM pg_tables WHERE tablename LIKE 'locations_%';`
