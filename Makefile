# Makefile for SalesAPI Docker Operations
# Usage: make help

.PHONY: help build up down logs clean test migrate status restart

# Default target
help: ## Show this help message
	@echo "SalesAPI Docker Management Commands"
	@echo "=================================="
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | sort | awk 'BEGIN {FS = ":.*?## "}; {printf "\033[36m%-15s\033[0m %s\n", $$1, $$2}'


# Build and deployment commands
build: ## Build all Docker images
	@echo "?? Building all Docker images..."
	docker compose build

up: ## Start all services (build if needed)
	@echo "?? Starting all services..."
	docker compose up --build -d
	@echo "? Waiting for services to be ready..."
	@sleep 30
	@make status

down: ## Stop all services
	@echo "?? Stopping all services..."
	docker compose down

restart: ## Restart all services
	@echo "?? Restarting all services..."
	docker compose restart

# Database commands
migrate: ## Run database migrations
	@echo "?? Running database migrations..."
	docker compose run --rm migration

reset-db: ## Reset databases (DESTRUCTIVE)
	@echo "??  WARNING: This will destroy all data!"
	@read -p "Are you sure? [y/N] " -n 1 -r; echo; if [[ $$REPLY =~ ^[Yy]$$ ]]; then \
		docker compose down -v; \
		docker volume rm -f $$(docker volume ls -q | grep salesapi) 2>/dev/null || true; \
		echo "???  Database volumes removed"; \
	fi

# Monitoring commands
logs: ## Show logs from all services
	docker compose logs -f

logs-gateway: ## Show Gateway logs
	docker compose logs -f gateway

logs-inventory: ## Show Inventory API logs
	docker compose logs -f inventory

logs-sales: ## Show Sales API logs
	docker compose logs -f sales

status: ## Show status of all services
	@echo "?? Service Status:"
	@docker compose ps
	@echo ""
	@echo "?? Health Checks:"
	@curl -s --max-time 5 http://localhost:6000/health > /dev/null && echo "? Gateway: Healthy" || echo "? Gateway: Unhealthy"
	@curl -s --max-time 5 http://localhost:5000/health > /dev/null && echo "? Inventory: Healthy" || echo "? Inventory: Unhealthy"
	@curl -s --max-time 5 http://localhost:5001/health > /dev/null && echo "? Sales: Healthy" || echo "? Sales: Unhealthy"

# Testing commands
test: ## Run integration tests (requires running services)
	@echo "?? Running integration tests..."
	@if ! curl -s --max-time 5 http://localhost:6000/health > /dev/null; then \
		echo "? Services not running. Start with 'make up' first."; \
		exit 1; \
	fi
	dotnet test tests/endpoint.tests/endpoint.tests.csproj --logger "console;verbosity=normal"

test-reservations: ## Run stock reservation tests only
	@echo "?? Running stock reservation tests..."
	dotenv run dotnet test tests/endpoint.tests/endpoint.tests.csproj --filter "StockReservationTests" --logger "console;verbosity=detailed"

test-events: ## Run event-driven tests only
	@echo "?? Running event-driven tests..."
	dotenv run dotnet test tests/endpoint.tests/endpoint.tests.csproj --filter "EventDrivenTests" --logger "console;verbosity=detailed"

# Development commands
dev: ## Start in development mode with override
	@echo "?? Starting in development mode..."
	docker compose -f docker-compose.yml -f docker-compose.override.yml up --build -d

observability: ## Start with observability stack
	@echo "?? Starting with observability stack..."
	docker compose -f docker-compose-observability-simple.yml up --build -d

# Cleanup commands
clean: ## Clean up containers, networks, and images
	@echo "?? Cleaning up Docker resources..."
	docker compose down --remove-orphans
	docker system prune -f
	@echo "? Cleanup completed"

clean-all: ## Clean everything including volumes (DESTRUCTIVE)
	@echo "??  WARNING: This will remove all data and images!"
	@read -p "Are you sure? [y/N] " -n 1 -r; echo; if [[ $$REPLY =~ ^[Yy]$$ ]]; then \
		docker compose down -v --remove-orphans; \
		docker system prune -af; \
		docker volume prune -f; \
		echo "???  Everything cleaned"; \
	fi

# Quick start
quick-start: build up migrate status ## Complete setup from scratch
	@echo ""
	@echo "?? SalesAPI is ready!"
	@echo "Available at:"
	@echo "  Gateway:    http://localhost:6000"
	@echo "  Inventory:  http://localhost:5000"
	@echo "  Sales:      http://localhost:5001"
	@echo "  RabbitMQ:   http://localhost:15672 (admin/admin123)"

# Production commands
prod: ## Start in production mode
	@echo "?? Starting in production mode..."
	docker compose -f docker-compose.yml up --build -d

# Backup and restore
backup: ## Backup databases
	@echo "?? Creating database backup..."
	@mkdir -p backups
	@if docker compose ps --format json | grep -q '"Service":"sqlserver".*"State":"running"'; then \
		docker compose exec -T sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'Your_password123' -Q "BACKUP DATABASE InventoryDb TO DISK = '/tmp/inventory_backup.bak'" || echo "?? Inventory backup failed"; \
		docker compose exec -T sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'Your_password123' -Q "BACKUP DATABASE SalesDb TO DISK = '/tmp/sales_backup.bak'" || echo "?? Sales backup failed"; \
		docker cp $$(docker compose ps -q sqlserver):/tmp/inventory_backup.bak ./backups/inventory_$$(date +%Y%m%d_%H%M%S).bak 2>/dev/null || echo "?? Could not copy inventory backup"; \
		docker cp $$(docker compose ps -q sqlserver):/tmp/sales_backup.bak ./backups/sales_$$(date +%Y%m%d_%H%M%S).bak 2>/dev/null || echo "?? Could not copy sales backup"; \
		echo "? Backup process completed. Check ./backups/"; \
	else \
		echo "? SQL Server container is not running"; \
	fi