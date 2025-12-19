# EF Core Migrations + SQL Script Hybrid Approach

## Strategiya

- **User entity** → EF Core migrations
- **Location partitioned table** → SQL script (manual)

## Setup

### 1️⃣ EF Core Migrations (User Entity)

```bash
cd Convoy.Data

# Initial migration (User table)
dotnet ef migrations add InitialCreate --startup-project ../Convoy.Api

# Database update (faqat User table yaratiladi)
dotnet ef database update --startup-project ../Convoy.Api
```

### 2️⃣ Manual SQL Script (Location Partitioned Table)

```bash
# Partitioned table va function'lar uchun
psql -U postgres -d convoy_db -f database-setup-locations-only.sql
```

## Fayllar

### `Convoy.Data/Migrations/xxxxx_InitialCreate.cs`

EF Core avtomatik yaratadi - faqat User table.

### `database-setup-locations-only.sql` (yangi fayl)

Faqat Location partitioned table va functions.

---

## Afzalliklari

✅ **User table** - EF Core migration (version control, rollback)
✅ **Location table** - SQL script (partition support)
✅ **Ikkalasi** - alohida manage qilinadi

## Kamchiliklari

⚠️ Ikkita migration system (EF + SQL)
⚠️ Manual coordination kerak

---

## Alternative: 100% SQL Script

Agar **barcha table'lar** uchun SQL script ishlatsangiz:

**Afzalliklari:**
- Bir xil approach
- Partition'lar uchun to'liq nazorat
- PostgreSQL-specific features

**Kamchiliklari:**
- EF migrations'ning version control yo'q
- Rollback qo'lda qilish kerak

---

## Tavsiya

1. **Small project** → 100% SQL script (`database-setup.sql`)
2. **Large project** → Hybrid (EF migrations + SQL for partitions)

Sizning holatingiz uchun: **100% SQL script yaxshiroq** chunki:
- Location bu asosiy table (partitioned)
- User oddiy table (kam o'zgaradi)
- SQL script bilan to'liq nazorat

---

## Xulosa

**EF migrations ishlatish MUMKIN emas** Location uchun.
**SQL script MAJBURIY** partitioned table'lar uchun.

Foydalaning: `database-setup.sql` ✅
