#!/bin/bash
# Setup script for SalesAPI
# Usage: ./scripts/setup.sh

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

echo "??? Setting up SalesAPI environment..."
echo "Project root: $PROJECT_ROOT"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

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

# Check prerequisites
print_header "Checking Prerequisites"

# Check Docker
if ! command -v docker &> /dev/null; then
    print_error "Docker is not installed. Please install Docker first."
    echo "Download from: https://docs.docker.com/get-docker/"
    exit 1
fi
print_status "? Docker is available"

# Check Docker Compose
if ! docker compose version &> /dev/null; then
    print_error "Docker Compose is not available. Please install Docker Compose first."
    echo "Download from: https://docs.docker.com/compose/install/"
    exit 1
fi
print_status "? Docker Compose is available"

# Check .NET SDK
if ! command -v dotnet &> /dev/null; then
    print_error ".NET SDK is not installed. Please install .NET 8.0 SDK first."
    echo "Download from: https://dotnet.microsoft.com/download"
    exit 1
fi

# Check .NET version
DOTNET_VERSION=$(dotnet --version 2>/dev/null || echo "unknown")
if [[ $DOTNET_VERSION == 8.* ]]; then
    print_status "? .NET SDK $DOTNET_VERSION is available"
else
    print_warning "?? .NET SDK version is $DOTNET_VERSION, but .NET 8.0 is recommended"
fi

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    print_error "Docker daemon is not running. Please start Docker first."
    exit 1
fi
print_status "? Docker daemon is running"

print_header "Setting Up Project"

# Make scripts executable
print_status "Making scripts executable..."
chmod +x "$SCRIPT_DIR"/*.sh 2>/dev/null || true
print_status "? Scripts made executable"

# Validate Docker Compose files
print_status "Validating Docker Compose configuration..."
cd "$PROJECT_ROOT"

# Check main compose file
if [ -f "docker/compose/docker-compose.yml" ]; then
    if docker compose -f docker/compose/docker-compose.yml config > /dev/null 2>&1; then
        print_status "? Main compose file is valid"
    else
        print_error "? Main compose file has errors"
        docker compose -f docker/compose/docker-compose.yml config
        exit 1
    fi
else
    print_error "? Main compose file not found at docker/compose/docker-compose.yml"
    exit 1
fi

# Check observability compose file
if [ -f "docker/compose/docker-compose-observability-simple.yml" ]; then
    if docker compose -f docker/compose/docker-compose-observability-simple.yml config > /dev/null 2>&1; then
        print_status "? Observability compose file is valid"
    else
        print_error "? Observability compose file has errors"
        docker compose -f docker/compose/docker-compose-observability-simple.yml config
        exit 1
    fi
else
    print_error "? Observability compose file not found"
    exit 1
fi

print_header "Project Information"
print_status "Project structure:"
echo "  ?? docker/compose/          - Docker Compose files"
echo "  ?? docker/observability/    - Observability configurations"
echo "  ?? scripts/                 - Management scripts"
echo "  ?? src/                     - Source code"
echo "  ?? tests/                   - Test projects"
echo ""

print_status "Available commands:"
echo "  ./scripts/docker-manage.sh start     - Start all services"
echo "  ./scripts/docker-manage.sh stop      - Stop all services"
echo "  ./scripts/docker-manage.sh status    - Check service status"
echo "  make -C scripts up                   - Alternative start command"
echo "  make -C scripts down                 - Alternative stop command"
echo ""

print_header "Quick Health Check"

# Try to build images to verify Dockerfiles
print_status "Testing Docker build process..."
if docker compose -f docker/compose/docker-compose.yml build --dry-run > /dev/null 2>&1; then
    print_status "? Docker build configuration is valid"
else
    print_warning "?? Docker build test failed - this might be normal if dependencies are missing"
fi

print_header "Setup Complete!"
print_status "?? SalesAPI environment setup completed successfully!"
echo ""
echo "Next steps:"
echo "  1. Start services:    ./scripts/docker-manage.sh start"
echo "  2. Check status:      ./scripts/docker-manage.sh status"
echo "  3. View URLs:         ./scripts/docker-manage.sh urls"
echo "  4. Run tests:         ./scripts/docker-manage.sh test"
echo "  5. Stop services:     ./scripts/docker-manage.sh stop"
echo ""
echo "Or use Make commands:"
echo "  1. Start services:    make -C scripts up"
echo "  2. Check status:      make -C scripts status"
echo "  3. Run tests:         make -C scripts test"
echo "  4. Stop services:     make -C scripts down"
echo ""
echo "For help: ./scripts/docker-manage.sh help"
echo "For Make help: make -C scripts help"