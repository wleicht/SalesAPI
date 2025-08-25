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

# Show help
show_help() {
    echo "SalesAPI Docker Management Utility"
    echo "Usage: $0 [command]"
    echo ""
    echo "Commands:"
    echo "  start           Start all services"
    echo "  stop            Stop all services"
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
    echo "  help            Show this help"
}

# Start services
start_services() {
    print_header "Starting SalesAPI Services"
    check_docker
    
    print_status "Building and starting containers..."
    docker compose up --build -d
    
    print_status "Waiting for services to be ready..."
    sleep 30
    
    print_status "Running database migrations..."
    docker compose run --rm migration || {
        print_warning "Migration failed, but continuing..."
    }
    
    show_status
    show_urls
}

# Stop services
stop_services() {
    print_header "Stopping SalesAPI Services"
    docker compose down
    print_status "All services stopped"
}

# Restart services
restart_services() {
    print_header "Restarting SalesAPI Services"
    docker compose restart
    print_status "All services restarted"
    sleep 10
    show_status
}

# Show status
show_status() {
    print_header "Service Status"
    docker compose ps
    echo ""
    check_health
}

# Show logs
show_logs() {
    print_header "Service Logs"
    docker compose logs
}

# Follow logs
follow_logs() {
    print_header "Following Service Logs (Ctrl+C to stop)"
    docker compose logs -f
}

# Run migrations
run_migrations() {
    print_header "Running Database Migrations"
    docker compose run --rm migration
    print_status "Migrations completed"
}

# Run tests
run_tests() {
    print_header "Running Integration Tests"
    
    # Check if services are running
    if ! curl -s http://localhost:6000/health > /dev/null; then
        print_error "Services are not running. Start them first with: $0 start"
        exit 1
    fi
    
    print_status "Running all integration tests..."
    dotnet test tests/endpoint.tests/endpoint.tests.csproj --logger "console;verbosity=normal"
}

# Clean up
clean_up() {
    print_header "Cleaning Up"
    docker compose down --remove-orphans
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
        docker compose down -v --remove-orphans
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
    
    # Backup Inventory DB
    docker compose exec -T sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'Your_password123' -Q \
        "BACKUP DATABASE InventoryDb TO DISK = '/tmp/inventory_backup.bak'"
    docker cp salesapi-sqlserver:/tmp/inventory_backup.bak "./backups/inventory_${TIMESTAMP}.bak"
    
    # Backup Sales DB
    docker compose exec -T sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'Your_password123' -Q \
        "BACKUP DATABASE SalesDb TO DISK = '/tmp/sales_backup.bak'"
    docker cp salesapi-sqlserver:/tmp/sales_backup.bak "./backups/sales_${TIMESTAMP}.bak"
    
    print_status "Backups created in ./backups/"
    ls -la backups/
}

# Check service health
check_health() {
    print_header "Health Checks"
    
    services=("Gateway:6000" "Inventory:5000" "Sales:5001")
    
    for service in "${services[@]}"; do
        name=$(echo $service | cut -d: -f1)
        port=$(echo $service | cut -d: -f2)
        
        if curl -s "http://localhost:${port}/health" > /dev/null; then
            print_status "? $name: Healthy"
        else
            print_error "? $name: Unhealthy"
        fi
    done
}

# Show service URLs
show_urls() {
    print_header "Service URLs"
    echo "?? Gateway:     http://localhost:6000"
    echo "?? Inventory:   http://localhost:5000"
    echo "?? Sales:       http://localhost:5001"
    echo "?? RabbitMQ UI: http://localhost:15672 (admin/admin123)"
    echo ""
    echo "?? Quick test: curl http://localhost:6000/health"
}

# Main script logic
case "${1:-help}" in
    start)
        start_services
        ;;
    stop)
        stop_services
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
    help|*)
        show_help
        ;;
esac