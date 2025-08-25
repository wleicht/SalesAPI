#!/bin/bash
# Quick start script for SalesAPI with Docker Compose
# Usage: ./start.sh

set -e

echo "?? Starting SalesAPI Complete Environment"
echo "========================================="

# Check if Docker and Docker Compose are available
if ! command -v docker &> /dev/null; then
    echo "? Docker is not installed. Please install Docker first."
    exit 1
fi

if ! command -v docker compose &> /dev/null; then
    echo "? Docker Compose is not available. Please install Docker Compose first."
    exit 1
fi

echo "? Docker and Docker Compose are available"

# Stop any existing containers
echo "?? Stopping any existing containers..."
docker compose down --remove-orphans 2>/dev/null || true

# Build and start all services
echo "?? Building and starting all services..."
docker compose up --build -d

# Wait for services to be healthy
echo "? Waiting for services to be healthy..."
echo "   This may take 2-3 minutes for first-time setup..."

# Function to wait for service health
wait_for_service() {
    local service=$1
    local max_attempts=60
    local attempt=1
    
    echo "   Waiting for $service..."
    while [ $attempt -le $max_attempts ]; do
        if docker compose ps --filter "status=running" --filter "health=healthy" | grep -q $service; then
            echo "   ? $service is healthy"
            return 0
        fi
        
        if [ $((attempt % 10)) -eq 0 ]; then
            echo "   ? Still waiting for $service... (${attempt}s)"
        fi
        
        sleep 1
        attempt=$((attempt + 1))
    done
    
    echo "   ? $service failed to become healthy"
    return 1
}

# Wait for infrastructure services first
wait_for_service "salesapi-sqlserver"
wait_for_service "salesapi-rabbitmq"

# Apply database migrations
echo "?? Applying database migrations..."
docker compose run --rm migration || {
    echo "? Migration failed. Checking if databases exist..."
    # Try to create databases if they don't exist
    docker compose exec sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'Your_password123' -Q "
    IF NOT EXISTS(SELECT * FROM sys.databases WHERE name = 'InventoryDb')
    CREATE DATABASE InventoryDb;
    IF NOT EXISTS(SELECT * FROM sys.databases WHERE name = 'SalesDb')
    CREATE DATABASE SalesDb;
    " || echo "??  Database creation attempted, continuing..."
}

# Wait for application services
wait_for_service "salesapi-inventory"
wait_for_service "salesapi-sales" 
wait_for_service "salesapi-gateway"

echo ""
echo "?? SalesAPI is ready!"
echo "==================="
echo ""
echo "?? Available endpoints:"
echo "   ?? Gateway:     http://localhost:6000"
echo "   ?? Inventory:   http://localhost:5000" 
echo "   ?? Sales:       http://localhost:5001"
echo "   ?? RabbitMQ UI: http://localhost:15672 (admin/admin123)"
echo ""
echo "?? Management commands:"
echo "   View logs:      docker compose logs -f"
echo "   Stop services:  docker compose down"
echo "   Restart:        docker compose restart"
echo ""
echo "?? Test the system:"
echo "   curl http://localhost:6000/health"
echo "   curl http://localhost:6000/inventory/products"
echo ""
echo "?? More info in README.md"