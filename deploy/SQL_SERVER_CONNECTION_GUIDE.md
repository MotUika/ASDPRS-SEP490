# 🔌 Connect to SQL Server from Local Machine

Port **1433** đã được mở trên VPS! Bạn có thể connect từ máy local.

---

## ✅ Thông Tin Kết Nối

### Connection Details:
```
Server: 160.25.232.199,1433
User: sa
Password: <YOUR_PASSWORD>
Database: LMS_ASDPRS
```

### Connection String:
```
Server=160.25.232.199,1433;Database=LMS_ASDPRS;User Id=sa;Password=<YOUR_PASSWORD>;TrustServerCertificate=True;MultipleActiveResultSets=True;
```

---

## 🔧 Cách Connect

### Option 1: SQL Server Management Studio (SSMS)

1. Mở SSMS
2. Connect to Server:
   - **Server type:** Database Engine
   - **Server name:** `160.25.232.199,1433`
   - **Authentication:** SQL Server Authentication
   - **Login:** `sa`
   - **Password:** `<YOUR_PASSWORD>`
3. Click **Connect**

### Option 2: Azure Data Studio

1. Mở Azure Data Studio
2. New Connection:
   - **Connection type:** Microsoft SQL Server
   - **Server:** `160.25.232.199,1433`
   - **Authentication type:** SQL Login
   - **User name:** `sa`
   - **Password:** `<YOUR_PASSWORD>`
   - **Database:** `LMS_ASDPRS` (optional)
   - **Trust server certificate:** Yes
3. Click **Connect**

### Option 3: Visual Studio / Rider

**Connection String:**
```
Server=160.25.232.199,1433;Database=LMS_ASDPRS;User Id=sa;Password=<YOUR_PASSWORD>;TrustServerCertificate=True;
```

Paste vào Server Explorer hoặc Database tool.

