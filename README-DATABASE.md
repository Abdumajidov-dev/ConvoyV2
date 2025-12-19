# Database Setup - READY TO USE

## ✅ Database Tayyor!

Database va table'lar yaratildi:
- ✅ `users` table
- ✅ `locations` partitioned table
- ✅ Indexes
- ✅ Functions

**Faqat partition'larni yaratish qoldi!**

---

## 🎯 Option 1: PowerShell'da (TAVSIYA)

PowerShell oching va run qiling:

```powershell
cd C:\Users\jm7uz\Documents\Garant\canvoy

# Partition'larni yaratish
$env:PGPASSWORD="Danger124"
& "C:\Program Files\PostgreSQL\16\bin\psql.exe" -U postgres -d convoy_db -f create-partitions.sql
```

---

## 🎯 Option 2: CMD'da

CMD oching va run qiling:

```cmd
cd C:\Users\jm7uz\Documents\Garant\canvoy
create-partitions.bat
```

---

## 🎯 Option 3: pgAdmin (Eng Oson)

1. pgAdmin 4 ni oching
2. Databases → convoy_db → Query Tool
3. File → Open → `create-partitions.sql`
4. Execute (F5)

---

## ✅ Tekshirish

PowerShell yoki pgAdmin'da run qiling:

```sql
SELECT tablename FROM pg_tables WHERE tablename LIKE 'locations_%' ORDER BY tablename;
```

**Expected Output:**
```
 tablename
-----------
 locations_11_2025
 locations_12_2025
 locations_01_2026
 locations_02_2026
 locations_03_2026
```

---

## 🚀 Keyingi Qadam - Application Run

```powershell
cd Convoy.Api
dotnet run
```

Swagger: https://localhost:5001/swagger

---

## 📝 Test API

### Create Location (POST /api/location)

```json
{
  "userId": 1,
  "recordedAt": "2025-12-18T10:00:00Z",
  "latitude": 41.311151,
  "longitude": 69.279737,
  "isMoving": true,
  "batteryLevel": 80
}
```

### Get Last Locations (GET /api/location/user/1/last?count=10)

---

## 🐛 Agar Muammo Bo'lsa

### Partition yaratilmasa

pgAdmin Query Tool'da:

```sql
-- Manual partition yaratish
SELECT create_location_partition('2025-12-01'::DATE);
SELECT create_location_partition('2026-01-01'::DATE);
```

### Application ishlamasa

```sql
-- Partition'lar bormi?
SELECT COUNT(*) FROM pg_tables WHERE tablename LIKE 'locations_%';
```

Agar 0 bo'lsa, partition'lar yaratilmagan. Yuqoridagi manual command'larni run qiling.

---

## 📚 Files

- `database-setup.sql` - Barcha table'lar (✅ RUN qilindi)
- `create-partitions.sql` - Faqat partition'lar (❗ RUN qilish kerak)
- `create-partitions.bat` - Windows batch script
- `run-database-setup.ps1` - PowerShell script

---

Good luck! 🚀
