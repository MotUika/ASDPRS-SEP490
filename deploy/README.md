# 📚 ASDPRS Deployment Documentation

Tài liệu deployment cho ASDPRS API trên VPS Ubuntu 22.04.

---

## 🚀 Quick Start

### Deploy code mới (thường dùng):
```powershell
.\deploy\quick-deploy.ps1
```

### Restart API:
```powershell
.\deploy\restart-api.ps1
```

---

## 📖 Documentation Files

### Core Guides:
- **README.md** ← Bạn đang đọc file này
- **DEPLOY_GUIDE.md** - Hướng dẫn deployment đầy đủ
- **QUICK_DEPLOY_GUIDE.md** - Hướng dẫn deploy nhanh

### Configuration Guides:
- **DNS_SSL_SETUP.md** - Setup domain và SSL
- **SQL_SERVER_CONNECTION_GUIDE.md** - Connect SQL Server từ local
- **SQL_PASSWORD_INFO.md** - Thông tin password SQL Server

---

## 🎯 Common Tasks

### Deploy Code Mới
```powershell
.\deploy\quick-deploy.ps1
```

### Change SQL Password
```powershell
.\deploy\change-sql-password.ps1
```

### Restart Services
```bash
ssh root@160.25.232.199
docker compose -f /opt/asdprs/deploy/docker-compose.yml restart
```

### View Logs
```bash
ssh root@160.25.232.199
docker logs -f asdprs-api
```

---

## 🔑 Server Info

| Item | Value |
|------|-------|
| **VPS IP** | 160.25.232.199 |
| **User** | root |
| **Password** | Gm4Gp8mYJGpQ20Jt |
| **Domain** | api.fasm.site |
| **SQL Port** | 1433 |
| **SQL User** | sa |
| **SQL Password** | Asd#2024!Pr$Sql@Secure890 |

---

## 📁 Directory Structure

```
deploy/
├── README.md                      ← This file
├── DEPLOY_GUIDE.md               ← Full deployment guide
├── QUICK_DEPLOY_GUIDE.md         ← Quick deployment
├── DNS_SSL_SETUP.md              ← Domain & SSL setup
├── SQL_SERVER_CONNECTION_GUIDE.md ← SQL connection info
├── SQL_PASSWORD_INFO.md          ← SQL password details
│
├── Scripts (PowerShell):
│   ├── quick-deploy.ps1          ⭐ Deploy code mới
│   ├── restart-api.ps1           ⭐ Restart API
│   ├── change-sql-password.ps1   🔐 Change SQL password
│   ├── full-deploy.ps1           🏗️ Full deployment
│   └── upload-to-vps.ps1         📤 Upload only
│
├── Scripts (Bash - on VPS):
│   └── scripts/
│       ├── setup-api-domain.sh       🌐 Setup domain + SSL
│       ├── rebuild-api.sh            🔨 Rebuild API
│       ├── cleanup-wrong-uploads.sh  🧹 Cleanup
│       └── open-sql-port.sh          🔓 Open SQL port
│
└── Config Files:
    ├── docker-compose.yml            🐳 Main config
    ├── docker-compose-fe-be.yml      🐳 FE+BE config
    ├── nginx.conf                    ⚙️ Nginx config
    └── Dockerfile                    📦 API Dockerfile
```

---

## 🔗 URLs

| Service | URL |
|---------|-----|
| **API** | https://api.fasm.site |
| **Swagger** | https://api.fasm.site/swagger |
| **SQL Server** | 160.25.232.199,1433 |

---

## 📞 Need Help?

Xem documentation chi tiết:
- Deploy issues → **QUICK_DEPLOY_GUIDE.md**
- Domain/SSL → **DNS_SSL_SETUP.md**
- Database → **SQL_SERVER_CONNECTION_GUIDE.md**
- Full guide → **DEPLOY_GUIDE.md**

---

**Last Updated:** 2025-12-03  
**Status:** ✅ Production Ready
