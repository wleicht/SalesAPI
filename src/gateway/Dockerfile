# Multi-stage Dockerfile for Gateway API
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Instalar ferramentas necessárias para health check e debugging
RUN apt-get update && \
    apt-get install -y curl netcat-traditional && \
    rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files primeiro para melhor cache das dependências
COPY ["src/gateway/gateway.csproj", "src/gateway/"]
COPY ["src/buildingblocks.contracts/buildingblocks.contracts.csproj", "src/buildingblocks.contracts/"]

# Restore dependencies com verbose para debug
RUN dotnet restore "src/gateway/gateway.csproj" --verbosity minimal

# Copy source code
COPY . .

# Build com configurações otimizadas
WORKDIR "/src/src/gateway"
RUN dotnet build "gateway.csproj" -c Release -o /app/build --no-restore

FROM build AS publish
RUN dotnet publish "gateway.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Health check melhorado com mais tempo para startup
HEALTHCHECK --interval=30s --timeout=15s --start-period=90s --retries=5 \
  CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "gateway.dll"]