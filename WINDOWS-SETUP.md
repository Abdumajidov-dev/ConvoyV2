# Windows uchun PostgreSQL Setup

## 🎯 Variant 1: pgAdmin (Eng Oson)

### 1. pgAdmin ochish
- pgAdmin 4 ni oching
- Serverlarga ulanish (postgres user)

### 2. Database yaratish
- Right-click on "Databases" → Create → Database
- Database name: `convoy_db`
- Save

### 3. SQL Script ishga tushirish
- `convoy_db` ni tanlang
- Tools → Query Tool (yoki F5)
- File → Open → `database-setup.sql` ni tanlang
- Execute button (yoki F5) bosing

### 4. Natijani tekshiring
```sql
-- Partition'lar
SELECT tablename FROM pg_tables WHERE tablename LIKE 'locations_%' ORDER BY tablename;

-- Users table
SELECT * FROM users;
```

---

## 🎯 Variant 2: psql (Command Line)

### 1. PostgreSQL bin folder topish

PostgreSQL qayerda o'rnatilganini toping:

```
C:\Program Files\PostgreSQL\16\bin\psql.exe
C:\Program Files\PostgreSQL\15\bin\psql.exe
C:\Program Files\PostgreSQL\14\bin\psql.exe
```

### 2. To'liq path bilan ishlatish

```powershell
# PostgreSQL 16 (version'ni o'zgartirishingiz mumkin)
& "C:\Program Files\PostgreSQL\16\bin\psql.exe" -U postgres -d convoy_db -f database-setup.sql

# Yoki database yaratish
& "C:\Program Files\PostgreSQL\16\bin\psql.exe" -U postgres -c "CREATE DATABASE convoy_db;"
& "C:\Program Files\PostgreSQL\16\bin\psql.exe" -U postgres -d convoy_db -f database-setup.sql
```

### 3. PATH'ga qo'shish (Optional)

**PowerShell (Admin):**
```powershell
$env:Path += ";C:\Program Files\PostgreSQL\16\bin"
[Environment]::SetEnvironmentVariable("Path", $env:Path, [EnvironmentVariableTarget]::Machine)
```

Yoki manual:
1. System Properties → Environment Variables
2. System Variables → Path → Edit
3. New → `C:\Program Files\PostgreSQL\16\bin`
4. OK → OK
5. PowerShell'ni restart qiling

---

## 🎯 Variant 3: DBeaver / DataGrip

### DBeaver
1. New Connection → PostgreSQL
2. Database: convoy_db
3. SQL Editor → Open → `database-setup.sql`
4. Execute Script (Ctrl+Alt+X)

### DataGrip
1. New → Data Source → PostgreSQL
2. Database: convoy_db
3. Open SQL file → Execute

---

## 🎯 Variant 4: Docker PostgreSQL

Agar Docker ishlatayotgan bo'lsangiz:

```powershell
# PostgreSQL container run qilish
docker run --name convoy-postgres `
  -e POSTGRES_PASSWORD=Danger124 `
  -e POSTGRES_DB=convoy_db `
  -p 5432:5432 `
  -d postgres:16

# SQL script copy qilish
docker cp database-setup.sql convoy-postgres:/database-setup.sql

# Script ishga tushirish
docker exec -it convoy-postgres psql -U postgres -d convoy_db -f /database-setup.sql
```

---

## ✅ Tavsiya Qilinadi

**pgAdmin ishlatish** - Eng oson va vizual.

Yoki PostgreSQL bin folder'ni topib, to'liq path bilan psql ishlatish.
