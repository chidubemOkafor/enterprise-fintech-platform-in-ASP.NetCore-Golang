# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY ["auth.csproj", "."]
RUN dotnet restore "auth.csproj" --disable-parallel

COPY . .
RUN dotnet build "auth.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "auth.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage (match build version: 10.0)
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8081
ENTRYPOINT ["dotnet", "auth.dll"]