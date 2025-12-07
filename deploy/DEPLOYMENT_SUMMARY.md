# ASDPRS Deployment & FTS Summary

## 1. Full-Text Search (FTS) Setup
We have transitioned from the standard Microsoft SQL Server image to a **custom Docker image** to ensure Full-Text Search is always available.

- **Dockerfile**: `deploy/Dockerfile.sql`
- **Image Name**: `asdprs-sqlserver-custom`
- **Verification Command**:
  ```sql
  SELECT FULLTEXTSERVICEPROPERTY('IsFullTextInstalled')
  -- Result: 1 (Installed)
  ```

## 2. Quick Deployment Workflow
To update the API code on the VPS, use the PowerShell script:

```powershell
.\deploy\quick-deploy.ps1
```

### What this script does:
1.  **Prepares Files**: Copies source code to a temporary local folder.
2.  **Cleans VPS**: **Deletes old source code** on the VPS (`/opt/asdprs/ASDPRS-SEP490`, etc.) to prevent file duplication errors (like duplicate Migrations).
3.  **Uploads**: Scps the new source code to the VPS.
4.  **Rebuilds API**: Runs `docker compose up -d --build --no-deps api` to rebuild and restart only the API container.

### Data Persistence
- **Database Data**: Stored in the Docker volume `sqlserver_data`. It is **NOT** affected by `quick-deploy.ps1`. Your data is safe even when the API is rebuilt or the code is replaced.

## 3. Troubleshooting
- **Check FTS Status**:
  ```bash
  docker exec -it asdprs-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'Asd2025!PrSqlSecure890' -C -Q "SELECT FULLTEXTSERVICEPROPERTY('IsFullTextInstalled')"
  ```
- **View API Logs**:
  ```bash
  docker compose -f /opt/asdprs/deploy/docker-compose.yml logs -f api
  ```
