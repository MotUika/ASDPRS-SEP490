# ASDPRS-SEP490

ASDPRS-SEP490 là một hệ thống backend (ASP.NET Core 8.0) cho quản lý bài tập / điểm / chấm bài (Academic Submission & Delivery PRS). Dự án được tổ chức thành nhiều project (ASDPRS-SEP490 — web API, DataAccessLayer, BussinessObject, Repository, Service) và sử dụng Entity Framework Core với SQL Server.

Hiện tại repository chứa:
- Source code backend ASP.NET Core (.NET 8)
- Tập seed mẫu: `SampleDataSQL.sql`
- Hướng dẫn deploy & cấu hình DNS/SSL: `deploy/DNS_SSL_SETUP.md`

Lưu ý: kết quả tìm kiếm mã nguồn có thể chưa liệt kê tất cả file liên quan. Để xem mã nguồn đầy đủ, truy cập: https://github.com/MotUika/ASDPRS-SEP490

## Tính năng chính
- API RESTful (Swagger)
- Authentication: JWT + Google OAuth (thư viện đã thêm trong csproj)
- Entity Framework Core + SQL Server
- Một số cấu hình khởi tạo và seed dữ liệu (xem `ASDPRSContext`)

## Yêu cầu
- .NET 8 SDK
- SQL Server (local, Docker hoặc Azure SQL)
- Docker (nếu muốn chạy qua container)
- Một domain (nếu cần HTTPS / production)
- Công cụ: dotnet CLI, sqlcmd / SSMS

## Cấu trúc quan trọng
- ASDPRS-SEP490/: web API project
- DataAccessLayer/: context EF, migrations (sử dụng connection string `DbConnection` từ appsettings)
  - Lưu ý: `ASDPRSContextFactory` và `ASDPRSContext` đọc `appsettings.json` và tìm connection string có tên `DbConnection`. Nếu không có sẽ throw lỗi.
- SampleDataSQL.sql: dữ liệu mẫu để import vào DB
- deploy/DNS_SSL_SETUP.md: hướng dẫn cài SSL, nginx, certbot (dùng cho VPS + Docker)

## Thiết lập nhanh (development)
1. Clone repo:
   git clone https://github.com/MotUika/ASDPRS-SEP490.git
2. Vào thư mục gốc và build:
   dotnet build
3. Cấu hình secrets (xem phần "Secrets / Keys cần thiết" bên dưới)
4. Tạo database và chạy migrations / hoặc import `SampleDataSQL.sql`
5. Chạy ứng dụng:
   dotnet run --project ASDPRS-SEP490

## Secrets / Keys cần thiết và cách lấy / lưu trữ

Dưới đây là danh sách các secret / cấu hình mà dự án cần (dựa trên code và packages trong repository) và hướng dẫn chi tiết cách lấy/cấu hình chúng.

1) Connection string tới SQL Server (DbConnection)
- Tên trong appsettings: connectionStrings:DbConnection
- Mẫu connection string (SQL Server):
  - Local SQL Server (Windows auth): `Server=localhost;Database=ASDPRS;Trusted_Connection=True;`
  - SQL Server (SQL auth): `Server=your_server;Database=ASDPRS;User Id=sa;Password=Your_password123;TrustServerCertificate=True;`
  - Azure SQL: `Server=tcp:yourserver.database.windows.net,1433;Initial Catalog=ASDPRS;Persist Security Info=False;User ID=youruser;Password=yourpw;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;`
- Cách lấy:
  - Nếu dùng Azure SQL: tạo server + database trên Azure portal → phần "Connection strings" sẽ cho ví dụ.
  - Nếu dùng Docker: chạy SQL Server container (mcr.microsoft.com/mssql/server) và tạo DB bằng sqlcmd.
- Nơi lưu:
  - Development: appsettings.Development.json hoặc dotnet user-secrets
  - Production: biến môi trường, hoặc secrets store (Azure Key Vault, Docker secrets, Kubernetes secret)

2) JWT Signing Key (symmetric key)
- Sử dụng cho `JwtBearer` (dự án có reference tới Microsoft.AspNetCore.Authentication.JwtBearer)
- Key name thường: `Jwt:Key` / `Jwt:Issuer` / `Jwt:Audience` (kiểm tra appsettings trong repo nếu có — nếu không, bạn phải thêm)
- Cách tạo:
  - Sinh key dài ít nhất 32 bytes base64:
    - openssl rand -base64 48
    - hoặc PowerShell: [Convert]::ToBase64String((New-Object Byte[] 48 | %{ Get-Random -Maximum 256 }))
  - Lưu key ở nơi an toàn.
- Nơi lưu:
  - Development: dotnet user-secrets set "Jwt:Key" "your-generated-key"
  - Production: biến môi trường (e.g., JWT__Key trong ASP.NET Core), hoặc secrets manager.

3) Google OAuth Client ID & Client Secret (nếu bật Google Sign-In)
- Tên package trong project: Microsoft.AspNetCore.Authentication.Google
- Cách tạo:
  1. Vào Google Cloud Console (https://console.cloud.google.com)
  2. Tạo project → OAuth consent screen (cấu hình tên, email, scope cơ bản)
  3. Credentials → Create Credentials → OAuth client ID → chọn Web application
  4. Thêm Authorized redirect URIs (ví dụ: https://yourdomain.com/signin-google hoặc http://localhost:5000/signin-google tùy cấu hình)
  5. Sao chép Client ID và Client Secret
- Nơi lưu:
  - appsettings (không commit) hoặc biến môi trường: `Authentication:Google:ClientId`, `Authentication:Google:ClientSecret`
- Lưu ý: redirect URI phải trùng khớp với cấu hình middleware Google trong app.

4) SMTP credentials (nếu dự án gửi email: reset password, notifications)
- Thông tin: SMTP host, port, username, password, enable SSL/TLS
- Cách lấy:
  - Gmail: bật "App Passwords" cho tài khoản (nếu bật 2FA) hoặc dùng SendGrid / Mailgun.
  - SendGrid: tạo API key trên SendGrid dashboard.
- Nơi lưu:
  - `Smtp:Host`, `Smtp:Port`, `Smtp:User`, `Smtp:Pass`
  - Production: secrets manager hoặc biến môi trường

5) Các API keys khác (ví dụ AI summarization, cloud storage)
- Repo có SystemConfig keys liên quan tới AI (AISummaryMaxTokens) → nếu dùng dịch vụ AI (OpenAI, etc.), bạn cần API key tương ứng.
- Cách lấy:
  - Đăng ký trên provider (OpenAI, Azure OpenAI, Google Cloud AI) → lấy API key
- Lưu trữ như các secret khác.

6) SSL / Domain / DNS / Certbot
- Nếu deploy lên VPS theo `deploy/DNS_SSL_SETUP.md`, bạn cần:
  - 1 domain, trỏ A record về IP máy chủ.
  - Sử dụng certbot để cấp chứng chỉ Let's Encrypt.
  - Trong nhiều cấu hình, bạn cũng cần API key cho Cloudflare nếu sử dụng Cloudflare DNS API.

7) Docker / CI secrets (GitHub Actions)
- Nếu sử dụng CI/CD, đặt secrets trong GitHub repository settings → Secrets and variables → Actions:
  - Ví dụ: DB_CONNECTION_STRING, JWT_KEY, GOOGLE_CLIENT_ID, GOOGLE_CLIENT_SECRET, SMTP_PASSWORD, AI_API_KEY
- Khi dùng GitHub Actions, tham chiếu chúng bằng secrets.DB_CONNECTION_STRING, v.v.

8) Cách an toàn để lưu trữ & dùng secrets (tùy môi trường)
- Development (local):
  - dotnet user-secrets (không commit):
    dotnet user-secrets init
    dotnet user-secrets set "ConnectionStrings:DbConnection" "Server=...;Database=...;..."
    dotnet user-secrets set "Jwt:Key" "..."
  - Hoặc dùng environment variables:
    Windows PowerShell:
      $env:ConnectionStrings__DbConnection="..."
    Linux/mac:
      export ConnectionStrings__DbConnection="..."
- Production:
  - Azure: Azure Key Vault + Managed Identity
  - AWS: Secrets Manager or Parameter Store
  - Docker: Docker secrets (swarm) hoặc docker-compose with .env (không commit)
  - Kubernetes: K8s Secret
  - GitHub Actions: repository / org secrets for CI/CD

## Ví dụ appsettings.Development.json (không lưu secrets thật)
Lưu ý: không commit bản có secrets thật.
```json
{
  "ConnectionStrings": {
    "DbConnection": "Server=localhost;Database=ASDPRS;Trusted_Connection=True;"
  },
  "Jwt": {
    "Key": "REPLACE_WITH_A_STRONG_SECRET",
    "Issuer": "ASDPRS",
    "Audience": "ASDPRS_Audience"
  },
  "Authentication": {
    "Google": {
      "ClientId": "YOUR_GOOGLE_CLIENT_ID",
      "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET"
    }
  },
  "Smtp": {
    "Host": "smtp.example.com",
    "Port": 587,
    "User": "noreply@example.com",
    "Pass": "SMTP_PASSWORD"
  }
}
```

## Chạy migrations / khởi tạo DB
- Nếu muốn dùng EF migrations: tạo migration & update database
  dotnet ef migrations add InitialCreate --project DataAccessLayer --startup-project ASDPRS-SEP490
  dotnet ef database update --project DataAccessLayer --startup-project ASDPRS-SEP490
- Hoặc import `SampleDataSQL.sql` bằng SSMS / sqlcmd:
  sqlcmd -S <server> -U <user> -P <password> -i SampleDataSQL.sql

## Deployment
- Có hướng dẫn cấu hình DNS + SSL + nginx trong `deploy/DNS_SSL_SETUP.md`.
  Xem file: https://github.com/MotUika/ASDPRS-SEP490/blob/master/deploy/DNS_SSL_SETUP.md
- Nếu deploy bằng Docker, hãy đặt secrets vào Docker environment hoặc Docker secrets và đảm bảo `appsettings.Production.json`/environment variables đọc đúng.

## Một số điểm lưu ý từ code
- `ASDPRSContextFactory` và `ASDPRSContext` đọc `appsettings.json` từ thư mục gốc (base path: Path.Combine(Directory.GetCurrentDirectory(), "..", "ASDPRS-SEP490")). Vì vậy đảm bảo file appsettings json nằm đúng chỗ khi chạy design-time tools hoặc migrations.
  - Xem: DataAccessLayer/ASDPRSContextFactory.cs — https://github.com/MotUika/ASDPRS-SEP490/blob/master/DataAccessLayer/ASDPRSContextFactory.cs
  - Xem: DataAccessLayer/ASDPRSContext.cs — https://github.com/MotUika/ASDPRS-SEP490/blob/master/DataAccessLayer/ASDPRSContext.cs
- Nếu thiếu `DbConnection` trong appsettings.json, ứng dụng sẽ ném lỗi (vì code kiểm tra và throw).

## Bảo mật và best-practices
- Không bao giờ commit appsettings có chứa secrets vào git.
- Dùng environment variables hoặc secret managers cho production.
- Hạn chế quyền cho account DB (không dùng sa root cho production).
- Đặt độ dài và entropy cao cho JWT key.
- Giới hạn redirect URIs cho OAuth trên console provider.

---

Nếu bạn muốn, tôi có thể:
- Tạo mẫu `appsettings.Development.json` và `appsettings.Production.json` an toàn (không chứa secrets).
- Viết hướng dẫn chi tiết tạo Google OAuth (từng bước) hoặc script để tạo JWT key.
- Hướng dẫn cấu hình GitHub Actions để inject secrets khi deploy.

Tài nguyên tham khảo trong repo:
- Source code: https://github.com/MotUika/ASDPRS-SEP490
- deploy/DNS_SSL_SETUP.md: https://github.com/MotUika/ASDPRS-SEP490/blob/master/deploy/DNS_SSL_SETUP.md
- SampleDataSQL.sql: https://github.com/MotUika/ASDPRS-SEP490/blob/master/SampleDataSQL.sql
- DataAccessLayer/ASDPRSContextFactory.cs: https://github.com/MotUika/ASDPRS-SEP490/blob/master/DataAccessLayer/ASDPRSContextFactory.cs

```
