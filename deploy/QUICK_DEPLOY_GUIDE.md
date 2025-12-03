# 🚀 Quick Deploy Guide

Scripts để deploy code mới lên VPS nhanh chóng mà không cần config lại.

---

## 📋 Scripts

### 1. `quick-deploy.ps1` (Windows)
Deploy từ máy local lên VPS - Upload code + Rebuild API

### 2. `rebuild-api.sh` (VPS)
Rebuild API từ code đã có trên VPS

---

## 🎯 Option 1: Deploy từ Windows (Full Update)

**Khi nào dùng:** Bạn đã sửa code trên máy local và muốn deploy lên VPS

```powershell
.\deploy\quick-deploy.ps1
```

### Script sẽ làm:
1. ✅ Copy source code (CHỈ ASDPRS-SEP490 + dependencies)
2. ✅ Upload lên VPS `/opt/asdprs`
3. ✅ Rebuild API container với code mới
4. ✅ Restart API (SQL Server và Nginx không động)
5. ✅ Cleanup temp files

### Thời gian: ~2-3 phút

### Output mong đợi:
```
==========================================
  Quick Deploy - Code Update Only
==========================================

[1/4] Preparing files...
Project: C:\Working\ASDPRS-SEP490
Copying API source code...
✓ Files prepared

[2/4] Uploading to VPS...
Password: Gm4Gp8mYJGpQ20Jt

ASDPRS-SEP490/...
BussinessObject/...
✓ Upload complete

[3/4] Rebuilding API...
Rebuilding API container...
Building api...
[+] Running 1/1
 ✔ Container asdprs-api  Started
Waiting for API to be ready...
✓ API rebuilt and restarted

[4/4] Cleaning up...
✓ Cleanup complete

==========================================
  Deployment Complete!
==========================================

API is now running with the latest code!
```

---

## ⚡ Option 2: Rebuild trên VPS (Quick Rebuild)

**Khi nào dùng:** 
- Code đã có trên VPS (đã upload rồi)
- Chỉ cần rebuild lại (code không đổi nhưng muốn rebuild clean)
- Restart API với code hiện tại

```bash
# SSH vào VPS
ssh root@160.25.232.199

# Chạy rebuild script
cd /opt/asdprs/deploy/scripts
chmod +x rebuild-api.sh
./rebuild-api.sh
```

### Script sẽ làm:
1. ✅ Stop API container
2. ✅ Rebuild API (no cache)
3. ✅ Start API container
4. ✅ Show status

### Thời gian: ~1 phút

---

## 🆚 So sánh các phương pháp deploy

| Method | Time | Khi nào dùng | Upload code? | Rebuild? | Restart? |
|--------|------|--------------|--------------|----------|----------|
| **quick-deploy.ps1** | 2-3 min | Code mới từ Windows | ✅ | ✅ | ✅ |
| **rebuild-api.sh** | 1 min | Code đã trên VPS, chỉ rebuild | ❌ | ✅ | ✅ |
| **docker restart** | 10 sec | Chỉ restart, không rebuild | ❌ | ❌ | ✅ |

---

## 🔄 Workflow Deploy Thông Thường

### Lần đầu deploy:
```powershell
# 1. Full deployment (đã làm rồi)
.\deploy\full-deploy.ps1

# 2. Setup domain (đã làm rồi)
# SSH vào VPS và chạy setup-api-domain.sh
```

### Lần sau deploy code mới:
```powershell
# CHỈ CẦN CHẠY:
.\deploy\quick-deploy.ps1
```

---

## 📦 File Structure được Upload

`quick-deploy.ps1` chỉ upload các folder cần thiết:

```
/opt/asdprs/
├── ASDPRS-SEP490/          ← API source
├── BussinessObject/        ← Business logic
├── DataAccessLayer/        ← Data access
├── Repository/             ← Repositories  
├── Service/                ← Services
└── ASDPRS-SEP490.sln       ← Solution file
```

**KHÔNG upload:**
- `bin/`, `obj/` (build artifacts)
- `.git/` (git history)
- `.vs/` (Visual Studio cache)
- `node_modules/`
- `deploy/` (config không đổi)

---

## ✅ Verify Deployment

### Check API status:
```bash
ssh root@160.25.232.199
docker compose -f /opt/asdprs/deploy/docker-compose.yml ps
```

Expected:
```
NAME                STATUS              PORTS
asdprs-api          Up X minutes        0.0.0.0:5000->80/tcp
asdprs-nginx        Up X hours          0.0.0.0:80->80/tcp, 0.0.0.0:443->443/tcp
asdprs-sqlserver    Up X hours (healthy) 0.0.0.0:1433->1433/tcp
```

### Test API:
```bash
# From VPS
curl http://localhost:5000

# From browser
https://api.fasm.site
https://api.fasm.site/swagger
```

### View logs:
```bash
# Real-time logs
docker compose -f /opt/asdprs/deploy/docker-compose.yml logs -f api

# Last 100 lines
docker compose -f /opt/asdprs/deploy/docker-compose.yml logs --tail=100 api
```

---

## 🐛 Troubleshooting

### API không start sau deploy:

```bash
# Check logs
docker logs asdprs-api

# Check SQL Server connection
docker exec asdprs-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "YourStrong@Passw0rd123" -C -Q "SELECT 1"

# Restart API
docker restart asdprs-api
```

### Upload bị lỗi:

```powershell
# Check VPS connectivity
ssh root@160.25.232.199 "echo OK"

# Manual upload nếu cần
scp -r C:\Working\ASDPRS-SEP490\ASDPRS-SEP490 root@160.25.232.199:/opt/asdprs/
```

### Rebuild lỗi:

```bash
# Clean rebuild
docker compose -f /opt/asdprs/deploy/docker-compose.yml down api
docker compose -f /opt/asdprs/deploy/docker-compose.yml build --no-cache api
docker compose -f /opt/asdprs/deploy/docker-compose.yml up -d api
```

---

## 💡 Tips

1. **Trước khi deploy:** Test code trên local trước
2. **Sau deploy:** Check logs ngay để đảm bảo API start OK
3. **Nếu có lỗi:** Rollback bằng cách deploy lại code cũ
4. **Database migrations:** Nếu có thay đổi DB, chạy migrations trước khi deploy
5. **Backup:** Backup database trước khi deploy major changes

---

## 🎯 Quick Commands Summary

```powershell
# Deploy code mới từ Windows
.\deploy\quick-deploy.ps1

# Rebuild trên VPS
ssh root@160.25.232.199 "cd /opt/asdprs/deploy/scripts && ./rebuild-api.sh"

# View logs
ssh root@160.25.232.199 "docker logs -f asdprs-api"

# Restart API only
ssh root@160.25.232.199 "docker restart asdprs-api"

# Check status
ssh root@160.25.232.199 "docker compose -f /opt/asdprs/deploy/docker-compose.yml ps"
```

---

**Đơn giản nhất:** Chỉ cần chạy `.\deploy\quick-deploy.ps1` mỗi khi có code mới! 🚀
