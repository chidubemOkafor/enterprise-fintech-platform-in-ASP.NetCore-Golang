# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY ["account.csproj", "."]
RUN dotnet restore "account.csproj" --disable-parallel

COPY . .
RUN dotnet build "account.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "account.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8082
ENTRYPOINT ["dotnet", "account.dll"]