FROM mcr.microsoft.com/mssql/server:2022-latest

# Switch to root to install packages
USER root

# Install prerequisites and Full-Text Search
RUN apt-get update && \
    apt-get install -y curl gnupg && \
    # Add Microsoft Repository
    curl -fsSL https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor -o /usr/share/keyrings/microsoft-prod.gpg && \
    curl -fsSL https://packages.microsoft.com/config/ubuntu/22.04/mssql-server-2022.list | tee /etc/apt/sources.list.d/mssql-server-2022.list && \
    apt-get update && \
    # Install FTS
    apt-get install -y mssql-server-fts && \
    # Cleanup to keep image small
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Switch back to mssql user
USER mssql
