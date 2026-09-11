# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Build context is services/ (set in docker-compose.yml), so paths are
# relative to that. Copy the csproj files first so the restore layer caches
# independently of source edits.
COPY ["auth/auth.csproj", "auth/"]
COPY ["Contracts/Contracts.csproj", "Contracts/"]
RUN dotnet restore "auth/auth.csproj" --disable-parallel

COPY . .
RUN dotnet build "auth/auth.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "auth/auth.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage (match build version: 10.0)
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "auth.dll"]
