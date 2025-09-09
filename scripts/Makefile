# Makefile for SalesAPI Docker Operations
# Usage: make help

# Project paths
PROJECT_ROOT := $(shell dirname $(realpath $(firstword $(MAKEFILE_LIST))))
COMPOSE_DIR := $(PROJECT_ROOT)/../docker/compose

.PHONY: help build up down logs clean test migrate status restart

# Default target
help: ## Show this help message
	@echo "SalesAPI Docker Management Commands"
	@echo "=================================="
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | sort | awk 'BEGIN {FS = ":.*?## "}; {printf "\033[36m%-15s\033[0m %s\n", $$1, $$2}'

# Build and deployment commands
build: ## Build all Docker images
	@echo "??? Building all Docker images..."
	@cd $(PROJECT_ROOT)/.. && docker compose -f $(COMPOSE_DIR)/docker-compose.yml build

up: ## Start all services (build if needed)
	@echo "?? Starting all services..."
	@cd $(PROJECT_ROOT)/.. && docker compose -f $(COMPOSE_DIR)/docker-compose.yml up --build -d
	@echo "? Waiting for services to be ready..."
	@sleep 30
	@$(MAKE) status

down: ## Stop all services
	@echo "?? Stopping all services..."
	@cd $(PROJECT_ROOT)/.. && docker compose -f $(COMPOSE_DIR)/docker-compose.yml down

restart: ## Restart all services
	@echo "?? Restarting all services..."
	@cd $(PROJECT_ROOT)/.. && docker compose -f $(COMPOSE_DIR)/docker-compose.yml restart

# Database commands
migrate: ## Run database migrations
	@echo "?? Running database migrations..."
	@cd $(PROJECT_ROOT)/.. && docker compose -f $(COMPOSE_DIR)/docker-compose.yml run --rm migration

reset-db: ## Reset databases (DESTRUCTIVE)
	@echo "??  WARNING: This will destroy all data!"
	@read -p "Are you sure? [y/N] " -n 1 -r; echo; if [[ $$REPLY =~ ^[Yy]$$ ]]; then \
		cd $(PROJECT_ROOT)/.. && docker compose -f $(COMPOSE_DIR)/docker-compose.yml down -v; \
		docker volume rm -f $$(docker volume ls -q | grep salesapi) 2>/dev/null || true; \
		echo "???  Database volumes removed"; \
	fi

# Monitoring commands
logs: ## Show logs from all services
	@cd $(PROJECT_ROOT)/.. && docker compose -f $(COMPOSE_DIR)/docker-compose.yml logs -f

logs-gateway: ## Show Gateway logs
	@cd $(PROJECT_ROOT)/.. && docker compose -f $(COMPOSE_DIR)/docker-compose.yml logs -f gateway

logs-inventory: ## Show Inventory API logs
	@cd $(PROJECT_ROOT)/.. && docker compose -f $(COMPOSE_DIR)/docker-compose.yml logs -f inventory

logs-sales: ## Show Sales API logs
	@cd $(PROJECT_ROOT)/.. && docker compose -f $(COMPOSE_DIR)/docker-compose.yml logs -f sales

status: ## Show status of all services
	@echo "?? Service Status:"
	@cd $(PROJECT_ROOT)/.. && docker compose -f $(COMPOSE_DIR)/docker-compose.yml ps
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
	@cd $(PROJECT_ROOT)/.. && dotnet test tests/endpoint.tests/endpoint.tests.csproj --logger "console;verbosity=normal"

test-reservations: ## Run stock reservation tests only
	@echo "?? Running stock reservation tests..."
	@cd $(PROJECT_ROOT)/.. && dotnet test tests/endpoint.tests/endpoint.tests.csproj --filter "StockReservationTests" --logger "console;verbosity=detailed"

test-events: ## Run event-driven tests only
	@echo "?? Running event-driven tests..."
	@cd $(PROJECT_ROOT)/.. && dotnet test tests/endpoint.tests/endpoint.tests.csproj --filter "EventDrivenTests" --logger "console;verbosity=detailed"

# Development commands
dev: ## Start in development mode with override
	@echo "?? Starting in development mode..."
	@cd $(PROJECT_ROOT)/.. && docker compose -f $(COMPOSE_DIR)/docker-compose.yml -f $(COMPOSE_DIR)/docker-compose.override.yml up --build -d

observability: ## Start with observability stack
	@echo "?? Starting with observability stack..."
	@cd $(PROJECT_ROOT)/.. && docker compose -f $(COMPOSE_DIR)/docker-compose-observability-simple.yml up --build -d

# Cleanup commands
clean: ## Clean up containers, networks, and images
	@echo "?? Cleaning up Docker resources..."
	@cd $(PROJECT_ROOT)/.. && docker compose -f $(COMPOSE_DIR)/docker-compose.yml down --remove-orphans
	@docker system prune -f
	@echo "? Cleanup completed"

clean-all: ## Clean everything including volumes (DESTRUCTIVE)
	@echo "??  WARNING: This will remove all data and images!"
	@read -p "Are you sure? [y/N] " -n 1 -r; echo; if [[ $$REPLY =~ ^[Yy]$$ ]]; then \
		cd $(PROJECT_ROOT)/.. && docker compose -f $(COMPOSE_DIR)/docker-compose.yml down -v --remove-orphans; \
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
	@cd $(PROJECT_ROOT)/.. && docker compose -f $(COMPOSE_DIR)/docker-compose.yml up --build -d

# Backup and restore
backup: ## Backup databases
	@echo "?? Creating database backup..."
	@mkdir -p $(PROJECT_ROOT)/../backups
	@if docker compose -f $(COMPOSE_DIR)/docker-compose.yml ps --format json | grep -q '"Service":"sqlserver".*"State":"running"'; then \
		cd $(PROJECT_ROOT)/.. && docker compose -f $(COMPOSE_DIR)/docker-compose.yml exec -T sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'Your_password123' -Q "BACKUP DATABASE InventoryDb TO DISK = '/tmp/inventory_backup.bak'" || echo "?? Inventory backup failed"; \
		cd $(PROJECT_ROOT)/.. && docker compose -f $(COMPOSE_DIR)/docker-compose.yml exec -T sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'Your_password123' -Q "BACKUP DATABASE SalesDb TO DISK = '/tmp/sales_backup.bak'" || echo "?? Sales backup failed"; \
		cd $(PROJECT_ROOT)/.. && docker cp $$(docker compose -f $(COMPOSE_DIR)/docker-compose.yml ps -q sqlserver):/tmp/inventory_backup.bak ./backups/inventory_$$(date +%Y%m%d_%H%M%S).bak 2>/dev/null || echo "?? Could not copy inventory backup"; \
		cd $(PROJECT_ROOT)/.. && docker cp $$(docker compose -f $(COMPOSE_DIR)/docker-compose.yml ps -q sqlserver):/tmp/sales_backup.bak ./backups/sales_$$(date +%Y%m%d_%H%M%S).bak 2>/dev/null || echo "?? Could not copy sales backup"; \
		echo "? Backup process completed. Check ./backups/"; \
	else \
		echo "? SQL Server container is not running"; \
	fi