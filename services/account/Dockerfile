# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Build context is services/ (set in docker-compose.yml), so paths are
# relative to that. Copy the csproj files first so the restore layer caches
# independently of source edits.
COPY ["account/account.csproj", "account/"]
COPY ["Contracts/Contracts.csproj", "Contracts/"]
RUN dotnet restore "account/account.csproj" --disable-parallel

# Brings in Contracts/Protos/*.proto, which the gRPC codegen needs.
COPY . .
RUN dotnet build "account/account.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "account/account.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage (match build version: 10.0)
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app
COPY --from=publish /app/publish .
# 8080 = REST (HTTP/1.1), 8081 = gRPC (HTTP/2)
EXPOSE 8080 8081
ENTRYPOINT ["dotnet", "account.dll"]
