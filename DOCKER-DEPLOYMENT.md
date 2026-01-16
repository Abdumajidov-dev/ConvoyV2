# Docker Deployment Guide - AVTOMATIK SETUP!

## 🚀 1-Komanda Deploy (Eng Oson)

```bash
docker-compose up -d
```

**Hammasi avtomatik:**
- ✅ PostgreSQL container yaratiladi
- ✅ Database yaratiladi
- ✅ SQL script avtomatik run qiladi (database-setup.sql)
- ✅ Partition'lar yaratiladi
- ✅ API ishga tushadi

---

## 📋 To'liq Qadamlar

### 1. Prerequisites

```bash
# Docker o'rnatilgan bo'lishi kerak
docker --version
docker-compose --version
```

### 2. Clone/Download Project

```bash
cd C:\Users\jm7uz\Documents\Garant\canvoy
```

### 3. Run

```bash
# Build va run (birinchi marta)
docker-compose up -d --build

# Yoki faqat run (keyingi safar)
docker-compose up -d
```

### 4. Tekshirish

```bash
# Container'lar ishlab turganini tekshiring
docker-compose ps

# Logs ko'rish
docker-compose logs -f api
docker-compose logs -f postgres

# API health check
curl http://localhost:8080/api/location/user/1/last?count=10
```

### 5. Swagger

Browser: http://localhost:8080/swagger

---

## 🔍 Qanday Ishlaydi?

### PostgreSQL Container

1. PostgreSQL 16 image yuklanadi
2. Database yaratiladi: `convoy_db`
3. `/docker-entrypoint-initdb.d/` folder'dagi SQL scriptlar avtomatik run qiladi
4. `database-setup.sql` → tables, indexes, functions yaratiladi

### API Container

1. .NET 8 app build qilinadi
2. PostgreSQL tayyor bo'lishini kutadi (healthcheck)
3. DatabaseInitializer ishlaydi
4. PartitionMaintenanceService partition'lar yaratadi
5. API ishga tushadi

---

## 👥 Teamwork - Boshqa Dasturchi Qanday Ishlatadi?

### Developer 1 (Siz)

```bash
# .gitignore'ga qo'shing (agar yo'q bo'lsa)
echo "bin/" >> .gitignore
echo "obj/" >> .gitignore
echo ".vs/" >> .gitignore

# Git'ga push qiling
git add .
git commit -m "Add Docker support"
git push origin main
```

### Developer 2 (Boshqa dasturchi)

```bash
# Loyihani clone qiladi
git clone https://github.com/your-repo/convoy.git
cd convoy

# Bitta komanda - hamma narsa avtomatik!
docker-compose up -d --build

# API tayyor!
# http://localhost:8080/swagger
```

**MANUAL SQL RUN KERAK EMAS!** ✅

---

## 🛠️ Docker Commands

### Start/Stop

```bash
# Start
docker-compose up -d

# Stop
docker-compose down

# Stop va delete volumes
docker-compose down -v
```

### Logs

```bash
# Barcha logs
docker-compose logs -f

# Faqat API
docker-compose logs -f api

# Faqat PostgreSQL
docker-compose logs -f postgres
```

### Rebuild

```bash
# Code o'zgarsa, rebuild qiling
docker-compose up -d --build
```

### Database Access

```bash
# PostgreSQL'ga kirish
docker exec -it convoy-postgres psql -U postgres -d convoy_db

# SQL commands
\dt  -- tables ko'rish
SELECT tablename FROM pg_tables WHERE tablename LIKE 'locations_%';
\q   -- exit
```

---

## 🌐 Server Deploy (Production)

### Linux Server (Ubuntu/CentOS)

```bash
# Server'ga kirish
ssh user@your-server.com

# Docker o'rnatish
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# Docker Compose o'rnatish
sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose

# Loyihani ko'chirish
git clone https://github.com/your-repo/convoy.git
cd convoy

# Production environment
export ASPNETCORE_ENVIRONMENT=Production

# Run
sudo docker-compose up -d --build

# Nginx reverse proxy (optional)
# Port 80 -> 8080
```

### Environment Variables

Production uchun `docker-compose.prod.yml`:

```yaml
version: '3.8'

services:
  postgres:
    environment:
      POSTGRES_PASSWORD: ${DB_PASSWORD}  # Secret
    volumes:
      - /var/lib/convoy/postgres:/var/lib/postgresql/data

  api:
    environment:
      - ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=convoy_db;Username=postgres;Password=${DB_PASSWORD}
```

Run:
```bash
DB_PASSWORD=super_secret_password docker-compose -f docker-compose.prod.yml up -d
```

---

## 📊 Database Backup

```bash
# Backup
docker exec convoy-postgres pg_dump -U postgres convoy_db > backup_$(date +%Y%m%d).sql

# Restore
docker exec -i convoy-postgres psql -U postgres convoy_db < backup_20251218.sql
```

---

## 🔐 Security Best Practices

1. **Change passwords** - `docker-compose.yml` va `appsettings.json`
2. **Use secrets** - Docker secrets yoki environment variables
3. **HTTPS** - Nginx reverse proxy bilan
4. **Firewall** - Faqat kerakli portlar (80, 443)
5. **Regular updates** - Docker images va packages

---

## ❓ Troubleshooting

### Port busy

```bash
# Port 5432 band bo'lsa
sudo lsof -i :5432
sudo kill -9 <PID>
```

### Database connection error

```bash
# PostgreSQL tayyor emasligini kutish
docker-compose logs postgres

# Manual partition yaratish (agar kerak bo'lsa)
docker exec -it convoy-postgres psql -U postgres -d convoy_db -c "SELECT create_location_partition('2025-12-01'::DATE);"
```

### Container restart

```bash
docker-compose restart api
docker-compose restart postgres
```

---

## 🎉 Summary

### Local Development
```bash
docker-compose up -d
```

### New Team Member
```bash
git clone ...
docker-compose up -d --build
```

### Production Server
```bash
git clone ...
docker-compose -f docker-compose.prod.yml up -d
```

**NO MANUAL SQL NEEDED!** ✅

---

## 📁 Files

- `Dockerfile` - API image
- `docker-compose.yml` - Local/Dev
- `.dockerignore` - Ignore files
- `database-setup.sql` - Avtomatik run qilinadi

---

Ready to deploy! 🚀
