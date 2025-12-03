# Hướng dẫn Deploy ASDPRS lên VPS Ubuntu 22.04

## Thông tin VPS
- IP: 160.25.232.199
- OS: Ubuntu 22.04
- User: root
- Password: Gm4Gp8mYJGpQ20Jt
- Cấu hình: 2 CPU - 4 RAM - 30 Disk

---

## Cách 1: One-Click Deploy từ Windows (Khuyến nghị)

Chạy PowerShell script để upload và deploy tự động:

```powershell
.\deploy\full-deploy.ps1
```

Script sẽ tự động:
1. Upload project lên VPS
2. Cài đặt Docker
3. Build và start services

Khi được hỏi password, nhập: `Gm4Gp8mYJGpQ20Jt`

---

## Cách 2: Upload rồi Deploy thủ công

### Bước 1: Upload project lên VPS

**Windows PowerShell:**
```powershell
.\deploy\upload-to-vps.ps1
```

**Hoặc dùng scp thủ công:**
```bash
scp -r . root@160.25.232.199:/opt/asdprs
# Password: Gm4Gp8mYJGpQ20Jt
```

### Bước 2: SSH vào VPS và chạy Quick Deploy

```bash
ssh root@160.25.232.199
# Password: Gm4Gp8mYJGpQ20Jt

cd /opt/asdprs/deploy/scripts
chmod +x *.sh
./00-quick-deploy.sh
```

Xong! API sẽ chạy tại http://160.25.232.199

---

## Cách 2: Deploy từng bước

### Bước 1: Upload project (như trên)

### Bước 2: SSH vào VPS

```bash
ssh root@160.25.232.199
# Password: Gm4Gp8mYJGpQ20Jt
```

### Bước 3: Setup VPS

```bash
cd /opt/asdprs/deploy/scripts
chmod +x *.sh
./01-setup-vps.sh
```

### Bước 4: Deploy ứng dụng

```bash
./02-deploy-app.sh
```

---

## Quản lý Services

Sau khi deploy, sử dụng các lệnh sau:

```bash
# Xem trạng thái
asdprs status

# Xem logs
asdprs logs
asdprs logs-api
asdprs logs-db

# Restart services
asdprs restart

# Rebuild API (sau khi update code)
asdprs rebuild

# Stop all
asdprs stop

# Start all
asdprs start
```

Hoặc dùng docker compose trực tiếp:
```bash
cd /opt/asdprs
docker compose -f deploy/docker-compose.yml ps
docker compose -f deploy/docker-compose.yml logs -f
```

---

## Backup Database

```bash
cd /opt/asdprs/deploy/scripts

# Tạo backup
./04-backup-restore.sh backup

# Xem danh sách backup
./04-backup-restore.sh list

# Restore từ backup
./04-backup-restore.sh restore
```

---

## Setup SSL (Optional)

Nếu có domain trỏ về VPS:

```bash
cd /opt/asdprs/deploy/scripts
./05-ssl-setup.sh your-domain.com
```

---

## Cấu trúc thư mục trên VPS

```
/opt/asdprs/
├── deploy/
│   ├── docker-compose.yml
│   ├── Dockerfile
│   ├── nginx.conf
│   ├── ssl/                  # SSL certificates
│   └── scripts/
│       ├── 00-quick-deploy.sh
│       ├── 01-setup-vps.sh
│       ├── 02-deploy-app.sh
│       ├── 03-manage-services.sh
│       ├── 04-backup-restore.sh
│       └── 05-ssl-setup.sh
├── backups/                  # Database backups
├── ASDPRS-SEP490/           # Source code
├── BussinessObject/
├── DataAccessLayer/
├── Repository/
└── Service/
```

---

## URLs

- API: http://160.25.232.199
- Swagger: http://160.25.232.199/swagger
- SignalR Hub: ws://160.25.232.199/notificationHub

---

## Troubleshooting

### Xem logs khi có lỗi
```bash
asdprs logs-api
asdprs logs-db
```

### SQL Server không start được
```bash
# Check memory (SQL Server cần ít nhất 2GB)
free -h

# Restart SQL Server
docker restart asdprs-sqlserver
```

### API không connect được database
```bash
# Check SQL Server đã ready chưa
docker exec asdprs-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourStrong@Passw0rd123" -C -Q "SELECT 1"

# Restart API
docker restart asdprs-api
```

### Rebuild hoàn toàn
```bash
asdprs clean
./02-deploy-app.sh
```

---

## Lưu ý bảo mật

1. **Đổi mật khẩu VPS** sau khi deploy
2. **Đổi mật khẩu SQL Server** trong `docker-compose.yml` và các scripts
3. **Setup SSL/HTTPS** nếu có domain
4. **Không commit** các file chứa credentials lên git
5. **Backup database** định kỳ
