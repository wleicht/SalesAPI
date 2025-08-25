# Multi-stage Dockerfile for Sales API
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/sales.api/sales.api.csproj", "src/sales.api/"]
COPY ["src/buildingblocks.contracts/buildingblocks.contracts.csproj", "src/buildingblocks.contracts/"]
COPY ["src/buildingblocks.events/buildingblocks.events.csproj", "src/buildingblocks.events/"]

# Restore dependencies
RUN dotnet restore "src/sales.api/sales.api.csproj"

# Copy source code
COPY . .

# Build the application
WORKDIR "/src/src/sales.api"
RUN dotnet build "sales.api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "sales.api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Install curl for health check
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Health check for Sales API
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "sales.api.dll"]