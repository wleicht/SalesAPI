#!/bin/bash
# Docker management utility for SalesAPI
# Usage: ./docker-manage.sh [command]

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Detect available compose files and build compose command
detect_compose_files() {
    local compose_files=""
    
    # Always include main compose file if it exists
    if [ -f "docker-compose.yml" ]; then
        compose_files="-f docker-compose.yml"
    fi
    
    # Include infrastructure file for database and rabbitmq
    if [ -f "docker-compose.infrastructure.yml" ]; then
        compose_files="${compose_files} -f docker-compose.infrastructure.yml"
    fi
    
    # Include observability file for prometheus
    if [ -f "docker-compose.observability.yml" ]; then
        compose_files="${compose_files} -f docker-compose.observability.yml"
    fi
    
    # Include observability-simple if main observability is not available
    if [ -f "docker-compose-observability-simple.yml" ] && [ ! -f "docker-compose.observability.yml" ]; then
        compose_files="${compose_files} -f docker-compose-observability-simple.yml"
    fi
    
    echo "${compose_files}"
}

# Get compose files to use
COMPOSE_FILES=$(detect_compose_files)

# Function to print colored output
print_status() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

print_header() {
    echo -e "${BLUE}=== $1 ===${NC}"
}

# Check if Docker is running
check_docker() {
    if ! docker info > /dev/null 2>&1; then
        print_error "Docker is not running. Please start Docker first."
        exit 1
    fi
}

# Validate compose files
validate_compose() {
    print_status "Validating Docker Compose files..."
    print_status "Using compose files: ${COMPOSE_FILES}"
    
    if ! docker compose ${COMPOSE_FILES} config > /dev/null 2>&1; then
        print_error "Docker Compose configuration is invalid. Running diagnostics..."
        echo ""
        echo "Checking individual compose files:"
        
        # Check each file individually
        for file in docker-compose.yml docker-compose.infrastructure.yml docker-compose.observability.yml docker-compose-observability-simple.yml; do
            if [ -f "$file" ]; then
                if docker compose -f "$file" config > /dev/null 2>&1; then
                    print_status "? $file is valid"
                else
                    print_error "? $file has errors:"
                    docker compose -f "$file" config 2>&1 | head -10
                fi
            fi
        done
        
        return 1
    fi
    print_status "? Docker Compose configuration is valid"
}

# Show help
show_help() {
    echo "SalesAPI Docker Management Utility"
    echo "Usage: $0 [command]"
    echo ""
    echo "Commands:"
    echo "  start           Start all services"
    echo "  stop            Stop all services (including infrastructure)"
    echo "  stop-apps       Stop only application services"
    echo "  restart         Restart all services"
    echo "  status          Show service status"
    echo "  logs            Show logs from all services"
    echo "  logs-follow     Follow logs from all services"
    echo "  migrate         Run database migrations"
    echo "  test            Run integration tests"
    echo "  clean           Clean up containers and networks"
    echo "  reset           Reset everything (DESTRUCTIVE)"
    echo "  backup          Backup databases"
    echo "  health          Check service health"
    echo "  urls            Show service URLs"
    echo "  validate        Validate compose files"
    echo "  help            Show this help"
    echo ""
    echo "Detected compose files: ${COMPOSE_FILES}"
}

# Start services
start_services() {
    print_header "Starting SalesAPI Services"
    check_docker
    validate_compose
    
    print_status "Building and starting containers..."
    docker compose ${COMPOSE_FILES} up --build -d
    
    print_status "Waiting for services to be ready..."
    sleep 30
    
    print_status "Running database migrations..."
    docker compose ${COMPOSE_FILES} run --rm migration || {
        print_warning "Migration failed, but continuing..."
    }
    
    show_status
    show_urls
}

# Stop all services (including infrastructure)
stop_services() {
    print_header "Stopping All SalesAPI Services"
    check_docker
    
    print_status "Stopping all containers (apps + infrastructure + observability)..."
    
    # Try to stop using detected compose files
    if ! docker compose ${COMPOSE_FILES} down 2>/dev/null; then
        print_warning "Compose stop failed, trying individual compose files..."
        
        # Try each compose file individually
        for file in docker-compose.yml docker-compose.infrastructure.yml docker-compose.observability.yml docker-compose-observability-simple.yml; do
            if [ -f "$file" ]; then
                print_status "Stopping services from $file..."
                docker compose -f "$file" down 2>/dev/null || true
            fi
        done
    fi
    
    # Also stop any remaining salesapi containers
    print_status "Stopping any remaining SalesAPI containers..."
    docker ps --filter "name=salesapi-" --format "{{.Names}}" | xargs -r docker stop 2>/dev/null || true
    
    print_status "All SalesAPI services stopped"
}

# Stop only application services (keep infrastructure running)
stop_apps_only() {
    print_header "Stopping Application Services Only"
    check_docker
    
    print_status "Stopping application containers (keeping infrastructure)..."
    
    # Stop only app services
    if [ -f "docker-compose.yml" ]; then
        docker compose -f docker-compose.yml down 2>/dev/null || true
    fi
    
    # Stop specific app containers
    for container in salesapi-gateway salesapi-inventory salesapi-sales; do
        if docker ps --filter "name=$container" --format "{{.Names}}" | grep -q "$container"; then
            print_status "Stopping $container..."
            docker stop "$container" 2>/dev/null || true
        fi
    done
    
    print_status "Application services stopped (infrastructure still running)"
}

# Restart services
restart_services() {
    print_header "Restarting SalesAPI Services"
    check_docker
    validate_compose
    
    docker compose ${COMPOSE_FILES} restart
    print_status "All services restarted"
    sleep 10
    show_status
}

# Show status
show_status() {
    print_header "Service Status"
    
    # Show status from compose files
    if ! docker compose ${COMPOSE_FILES} ps 2>/dev/null; then
        print_warning "Compose status failed, showing manual container status..."
    fi
    
    # Always show manual status for SalesAPI containers
    echo ""
    print_header "SalesAPI Containers"
    docker ps --filter "name=salesapi-" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" 2>/dev/null || true
    
    echo ""
    check_health
}

# Show logs
show_logs() {
    print_header "Service Logs"
    docker compose ${COMPOSE_FILES} logs
}

# Follow logs
follow_logs() {
    print_header "Following Service Logs (Ctrl+C to stop)"
    docker compose ${COMPOSE_FILES} logs -f
}

# Run migrations
run_migrations() {
    print_header "Running Database Migrations"
    check_docker
    validate_compose
    
    docker compose ${COMPOSE_FILES} run --rm migration
    print_status "Migrations completed"
}

# Run tests
run_tests() {
    print_header "Running Integration Tests"
    
    # Check if services are running
    if ! curl -s --max-time 5 http://localhost:6000/health > /dev/null 2>&1; then
        print_error "Services are not running. Start them first with: $0 start"
        exit 1
    fi
    
    print_status "Running all integration tests..."
    dotnet test tests/endpoint.tests/endpoint.tests.csproj --logger "console;verbosity=normal"
}

# Clean up
clean_up() {
    print_header "Cleaning Up"
    
    # Clean using compose files
    for file in docker-compose.yml docker-compose.infrastructure.yml docker-compose.observability.yml docker-compose-observability-simple.yml; do
        if [ -f "$file" ]; then
            docker compose -f "$file" down --remove-orphans 2>/dev/null || true
        fi
    done
    
    docker system prune -f
    print_status "Cleanup completed"
}

# Reset everything
reset_all() {
    print_header "Resetting Everything"
    print_warning "This will destroy all data and containers!"
    read -p "Are you sure? [y/N] " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        # Force cleanup regardless of compose file issues
        docker ps --filter "name=salesapi-" --format "{{.Names}}" | xargs -r docker rm -f || true
        docker volume ls --filter "name=salesapi" --format "{{.Name}}" | xargs -r docker volume rm || true
        
        # Clean using all compose files
        for file in docker-compose.yml docker-compose.infrastructure.yml docker-compose.observability.yml docker-compose-observability-simple.yml; do
            if [ -f "$file" ]; then
                docker compose -f "$file" down -v --remove-orphans 2>/dev/null || true
            fi
        done
        
        docker system prune -af
        docker volume prune -f
        print_status "Everything has been reset"
    else
        print_status "Reset cancelled"
    fi
}

# Backup databases
backup_databases() {
    print_header "Backing Up Databases"
    
    # Create backup directory
    mkdir -p backups
    TIMESTAMP=$(date +%Y%m%d_%H%M%S)
    
    print_status "Creating database backups..."
    
    # Check if SQL Server container is running (try both names)
    sql_container=""
    if docker ps --filter "name=salesapi-sqlserver" --filter "status=running" | grep -q salesapi-sqlserver; then
        sql_container="salesapi-sqlserver"
    elif docker ps --filter "name=sqlserver-dev" --filter "status=running" | grep -q sqlserver-dev; then
        sql_container="sqlserver-dev"
        print_warning "Using external sqlserver-dev container for backup"
    else
        print_error "No SQL Server container is running. Start services first."
        return 1
    fi
    
    # Backup Inventory DB
    docker exec "$sql_container" /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'Your_password123' -Q \
        "BACKUP DATABASE InventoryDb TO DISK = '/tmp/inventory_backup.bak'" || {
        print_warning "Inventory backup failed, but continuing..."
    }
    docker cp "$sql_container":/tmp/inventory_backup.bak "./backups/inventory_${TIMESTAMP}.bak" 2>/dev/null || {
        print_warning "Could not copy inventory backup file"
    }
    
    # Backup Sales DB
    docker exec "$sql_container" /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'Your_password123' -Q \
        "BACKUP DATABASE SalesDb TO DISK = '/tmp/sales_backup.bak'" || {
        print_warning "Sales backup failed, but continuing..."
    }
    docker cp "$sql_container":/tmp/sales_backup.bak "./backups/sales_${TIMESTAMP}.bak" 2>/dev/null || {
        print_warning "Could not copy sales backup file"
    }
    
    print_status "Backup process completed. Check ./backups/ directory"
    ls -la backups/ 2>/dev/null || true
}

# Check service health
check_health() {
    print_header "Health Checks"
    
    services=("Gateway:6000" "Inventory:5000" "Sales:5001")
    
    for service in "${services[@]}"; do
        name=$(echo $service | cut -d: -f1)
        port=$(echo $service | cut -d: -f2)
        
        if curl -s --max-time 5 "http://localhost:${port}/health" > /dev/null 2>&1; then
            print_status "? $name: Healthy"
        else
            print_error "? $name: Unhealthy or unreachable"
        fi
    done
    
    # Check infrastructure services
    echo ""
    print_header "Infrastructure Status"
    
    # Check RabbitMQ
    if docker ps --filter "name=salesapi-rabbitmq" --filter "status=running" | grep -q salesapi-rabbitmq; then
        print_status "? RabbitMQ: Running"
    else
        print_error "? RabbitMQ: Not running"
    fi
    
    # Check SQL Server
    if docker ps --filter "name=salesapi-sqlserver" --filter "status=running" | grep -q salesapi-sqlserver; then
        print_status "? SQL Server: Running (salesapi-sqlserver)"
    elif docker ps --filter "name=sqlserver-dev" --filter "status=running" | grep -q sqlserver-dev; then
        print_status "? SQL Server: Running (external sqlserver-dev)"
    else
        print_error "? SQL Server: Not running"
    fi
    
    # Check Prometheus
    if docker ps --filter "name=salesapi-prometheus" --filter "status=running" | grep -q salesapi-prometheus; then
        print_status "? Prometheus: Running"
    else
        print_warning "??  Prometheus: Not running"
    fi
}

# Show service URLs
show_urls() {
    print_header "Service URLs"
    echo "?? Gateway:     http://localhost:6000"
    echo "?? Inventory:   http://localhost:5000"
    echo "?? Sales:       http://localhost:5001"
    echo "?? RabbitMQ UI: http://localhost:15672 (admin/admin123)"
    echo "?? Prometheus:  http://localhost:9090"
    echo ""
    echo "?? Quick test: curl http://localhost:6000/health"
}

# Validate compose files
validate_compose_files() {
    validate_compose
}

# Main script logic
case "${1:-help}" in
    start)
        start_services
        ;;
    stop)
        stop_services
        ;;
    stop-apps)
        stop_apps_only
        ;;
    restart)
        restart_services
        ;;
    status)
        show_status
        ;;
    logs)
        show_logs
        ;;
    logs-follow)
        follow_logs
        ;;
    migrate)
        run_migrations
        ;;
    test)
        run_tests
        ;;
    clean)
        clean_up
        ;;
    reset)
        reset_all
        ;;
    backup)
        backup_databases
        ;;
    health)
        check_health
        ;;
    urls)
        show_urls
        ;;
    validate)
        validate_compose_files
        ;;
    help|*)
        show_help
        ;;
esac