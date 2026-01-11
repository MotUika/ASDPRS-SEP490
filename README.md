# ASDPRS-SEP490

Academic Submission & Delivery PRS (ASDPRS) — Backend API (ASP.NET Core 8.0)

ASDPRS-SEP490 is the backend service for managing academic submissions, grading, and score records. The API is implemented with ASP.NET Core 8 and Entity Framework Core and is organized into logical projects to separate the web API and data access layers.

Repository contents
- Backend source code: ASP.NET Core (.NET 8)
- Sample data for the database: `SampleDataSQL.sql`
- Deployment and DNS/SSL guide: `deploy/DNS_SSL_SETUP.md`
- Deployment helper files: `deploy/*`

For the full source tree, see: https://github.com/MotUika/ASDPRS-SEP490

## Key features
- RESTful API with Swagger documentation
- Authentication: JWT and optional Google OAuth sign-in
- Entity Framework Core with SQL Server support
- Initial configuration and seed data (see `ASDPRSContext`)

## Requirements
- .NET 8 SDK
- SQL Server (local instance, Docker container, or Azure SQL)
- Docker (optional — for containerized deployments)
- A domain name (recommended for HTTPS/production)
- Tools: dotnet CLI, sqlcmd or SSMS

## Important repository structure
- ASDPRS-SEP490/: Web API project (entry point)
- DataAccessLayer/: EF Core DbContext and migrations (reads connection string `DbConnection` from appsettings)
  - Note: `ASDPRSContextFactory` and `ASDPRSContext` read `appsettings.json` from the repository root. Missing `DbConnection` will cause startup errors.
- SampleDataSQL.sql: SQL file with sample data to import
- deploy/: Deployment guides and scripts (DNS, SSL, nginx, Docker)

## Quick setup (development)
1. Clone the repository:
   git clone https://github.com/MotUika/ASDPRS-SEP490.git
2. Build the solution:
   dotnet build
3. Configure secrets (see "Secrets / Keys required" below)
4. Create the database and run migrations, or import `SampleDataSQL.sql`
5. Run the API:
   dotnet run --project ASDPRS-SEP490

## Secrets / Keys required (what and how to store them)
Below is a consolidated list of configuration keys and recommended storage methods. Do not commit secrets into source control.

1) Database connection string (`ConnectionStrings:DbConnection`)
- Examples:
  - Local (Windows Auth): `Server=localhost;Database=ASDPRS;Trusted_Connection=True;`
  - SQL Auth: `Server=your_server;Database=ASDPRS;User Id=sa;Password=Your_password123;TrustServerCertificate=True;`
  - Azure SQL: `Server=tcp:yourserver.database.windows.net,1433;Initial Catalog=ASDPRS;Persist Security Info=False;User ID=youruser;Password=yourpw;Encrypt=True;TrustServerCertificate=False;`
- Store in:
  - Development: `appsettings.Development.json` (never commit) or dotnet user-secrets
  - Production: environment variables, Azure Key Vault, Docker secrets, or Kubernetes secrets

2) JWT signing key
- Used by JwtBearer authentication (`Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`)
- Generate a strong key (at least 32 bytes). Example:
  - openssl rand -base64 48
- Store in environment variables or a secrets manager (e.g., `Jwt__Key`).

3) Google OAuth Client ID & Client Secret (optional, for Google Sign-In)
- Create credentials in Google Cloud Console (OAuth consent screen → Credentials → OAuth client ID)
- Add authorized redirect URIs (e.g., `https://yourdomain.com/signin-google` or `http://localhost:5000/signin-google`)
- Store as `Authentication:Google:ClientId` and `Authentication:Google:ClientSecret` in secrets

4) SMTP credentials (if email is used)
- `Smtp:Host`, `Smtp:Port`, `Smtp:User`, `Smtp:Pass`
- Use provider-specific best practices (App Passwords for Gmail, SendGrid API keys, etc.)

5) Other API keys (AI services, cloud storage, etc.)
- Check project configuration for `SystemConfig` keys (e.g., AISummaryMaxTokens)
- Store provider keys in secure secrets storage

6) SSL / Domain / DNS / Certbot
- If you follow `deploy/DNS_SSL_SETUP.md`, you will need:
  - A domain pointing to your server (A record)
  - Certbot or another ACME client to obtain Let's Encrypt certificates
  - Optional DNS provider API key (e.g., Cloudflare) if automating DNS validation

7) CI/CD and Docker
- Set repository or organization secrets for CI pipelines (e.g., GitHub Actions): DB_CONNECTION_STRING, JWT_KEY, GOOGLE_CLIENT_ID, GOOGLE_CLIENT_SECRET, SMTP_PASSWORD, AI_API_KEY
- Reference secrets in your workflow files via secrets.*

Best practices for secret storage
- Local development:
  - dotnet user-secrets:
    dotnet user-secrets init
    dotnet user-secrets set "ConnectionStrings:DbConnection" "Server=...;Database=...;..."
    dotnet user-secrets set "Jwt:Key" "..."
  - Or use environment variables:
    - Windows PowerShell:
      $env:ConnectionStrings__DbConnection="..."
    - Linux/mac:
      export ConnectionStrings__DbConnection="..."
- Production:
  - Azure: Azure Key Vault + Managed Identity
  - AWS: Secrets Manager or Parameter Store
  - Docker: Docker secrets or environment variables (avoid committing .env)
  - Kubernetes: Kubernetes Secret
  - GitHub Actions: repository/organization secrets for CI/CD

## Example appsettings.Development.json (placeholder values — do not commit real secrets)
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

## Running EF Core migrations / initializing the database
- Use Entity Framework migrations:
  dotnet ef migrations add InitialCreate --project DataAccessLayer --startup-project ASDPRS-SEP490
  dotnet ef database update --project DataAccessLayer --startup-project ASDPRS-SEP490
- Or import sample data with SSMS / sqlcmd:
  sqlcmd -S <server> -U <user> -P <password> -i SampleDataSQL.sql

## Deployment
- See `deploy/DNS_SSL_SETUP.md` for DNS and SSL configuration: https://github.com/MotUika/ASDPRS-SEP490/blob/master/deploy/DNS_SSL_SETUP.md
- For Docker-based deployments, inject secrets via Docker environment variables or Docker secrets and ensure `appsettings.Production.json` or environment variables are read correctly.
- The deploy folder includes scripts and a docker-compose configuration for production deployments.

## Notable implementation details
- `ASDPRSContextFactory` and `ASDPRSContext` read `appsettings.json` from the repository root (base path: Path.Combine(Directory.GetCurrentDirectory(), "..", "ASDPRS-SEP490")). Ensure configuration files are accessible from that path when running migrations or tools.
  - See: DataAccessLayer/ASDPRSContextFactory.cs — https://github.com/MotUika/ASDPRS-SEP490/blob/master/DataAccessLayer/ASDPRSContextFactory.cs
  - See: DataAccessLayer/ASDPRSContext.cs — https://github.com/MotUika/ASDPRS-SEP490/blob/master/DataAccessLayer/ASDPRSContext.cs
- If `DbConnection` is missing from configuration, the application will throw an error on startup.

## Security and best practices
- Never commit configuration files that contain secrets.
- Use environment variables or a secrets manager for production credentials.
- Limit database account privileges (avoid using `sa` in production).
- Use a high-entropy JWT signing key.
- Restrict OAuth redirect URIs to trusted domains.

---

If helpful, I can also:
- Create safe example `appsettings.Development.json` and `appsettings.Production.json` templates (without secrets).
- Add a step-by-step Google OAuth setup guide.
- Provide a GitHub Actions example workflow that injects secrets for deployment.

Resources
- Repository: https://github.com/MotUika/ASDPRS-SEP490
- Deployment DNS/SSL guide: https://github.com/MotUika/ASDPRS-SEP490/blob/master/deploy/DNS_SSL_SETUP.md
- Sample data: https://github.com/MotUika/ASDPRS-SEP490/blob/master/SampleDataSQL.sql
- Data access code references:
  - https://github.com/MotUika/ASDPRS-SEP490/blob/master/DataAccessLayer/ASDPRSContextFactory.cs
  - https://github.com/MotUika/ASDPRS-SEP490/blob/master/DataAccessLayer/ASDPRSContext.cs
