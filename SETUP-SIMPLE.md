# Database Setup - Oddiy Yo'l

## pgAdmin orqali (3 qadam)

### 1. pgAdmin 4 ni oching

### 2. Query Tool ochish
- Servers → PostgreSQL 16 → Databases → convoy_db
- Right-click on **convoy_db** → Query Tool

### 3. SQL Script ishga tushirish
- Query Tool'da:
- **File** → **Open**
- `C:\Users\jm7uz\Documents\Garant\canvoy\database-setup.sql` ni tanlang
- **Execute** button (⚡ icon) yoki **F5** bosing

### 4. Natija

Ko'rishingiz kerak:
```
CREATE TABLE
CREATE TABLE
CREATE INDEX
CREATE INDEX
CREATE FUNCTION
create_location_partition
-------------------------
Created: locations_11_2025
Created: locations_12_2025
Created: locations_01_2026
...
Database setup completed successfully!
```

### 5. Tekshirish

Query Tool'da ishga tushiring:

```sql
-- Partition'lar
SELECT tablename FROM pg_tables
WHERE tablename LIKE 'locations_%'
ORDER BY tablename;

-- Users table
SELECT * FROM users;

-- Location count
SELECT COUNT(*) FROM pg_tables WHERE tablename LIKE 'locations_%';
```

**Expected:** 5 ta partition (11_2025, 12_2025, 01_2026, 02_2026, 03_2026)

---

## Keyin

```powershell
cd Convoy.Api
dotnet run
```

Swagger: https://localhost:5001/swagger

---

## Muammo?

Agar pgAdmin'da error chiqsa, screenshot yuboring.
