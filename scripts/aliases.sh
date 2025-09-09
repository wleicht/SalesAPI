# SalesAPI Docker Aliases
# Add these aliases to your ~/.bashrc or ~/.zshrc for convenience
# Usage: source scripts/aliases.sh

# Docker management aliases
alias dapi-start="./scripts/docker-manage.sh start"
alias dapi-stop="./scripts/docker-manage.sh stop"
alias dapi-status="./scripts/docker-manage.sh status"
alias dapi-logs="./scripts/docker-manage.sh logs-follow"
alias dapi-health="./scripts/docker-manage.sh health"
alias dapi-urls="./scripts/docker-manage.sh urls"
alias dapi-test="./scripts/docker-manage.sh test"
alias dapi-clean="./scripts/docker-manage.sh clean"
alias dapi-reset="./scripts/docker-manage.sh reset"

# Make aliases
alias dapi-make-up="make -C scripts up"
alias dapi-make-down="make -C scripts down"
alias dapi-make-status="make -C scripts status"
alias dapi-make-test="make -C scripts test"
alias dapi-make-help="make -C scripts help"

# Quick development aliases
alias dapi-dev="make -C scripts dev"
alias dapi-obs="make -C scripts observability"
alias dapi-quick="make -C scripts quick-start"

echo "? SalesAPI aliases loaded!"
echo "Available commands:"
echo "  dapi-start    - Start all services"
echo "  dapi-stop     - Stop all services" 
echo "  dapi-status   - Check service status"
echo "  dapi-logs     - Follow logs"
echo "  dapi-test     - Run tests"
echo "  dapi-health   - Health check"
echo "  dapi-urls     - Show service URLs"
echo ""
echo "For full command list: ./scripts/docker-manage.sh help"