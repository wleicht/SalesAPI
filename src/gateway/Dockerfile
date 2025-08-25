# Multi-stage Dockerfile for Gateway API
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/gateway/gateway.csproj", "src/gateway/"]
COPY ["src/buildingblocks.contracts/buildingblocks.contracts.csproj", "src/buildingblocks.contracts/"]

# Restore dependencies
RUN dotnet restore "src/gateway/gateway.csproj"

# Copy source code
COPY . .

# Build the application
WORKDIR "/src/src/gateway"
RUN dotnet build "gateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "gateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Install curl for health check
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Health check for Gateway
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "gateway.dll"]