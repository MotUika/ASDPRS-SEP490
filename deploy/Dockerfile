# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ASDPRS-SEP490.sln ./
COPY ASDPRS-SEP490/ASDPRS-SEP490.csproj ASDPRS-SEP490/
COPY BussinessObject/BussinessObject.csproj BussinessObject/
COPY DataAccessLayer/DataAccessLayer.csproj DataAccessLayer/
COPY Repository/Repository.csproj Repository/
COPY Service/Service.csproj Service/

# Restore dependencies
RUN dotnet restore

# Copy all source code
COPY . .

# Build and publish
WORKDIR /src/ASDPRS-SEP490
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install dependencies for document processing
RUN apt-get update && apt-get install -y \
    libgdiplus \
    libc6-dev \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

EXPOSE 80
EXPOSE 443

ENTRYPOINT ["dotnet", "ASDPRS-SEP490.dll"]
