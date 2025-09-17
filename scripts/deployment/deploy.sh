#!/bin/bash

# SalesAPI Professional Deployment Script
# Supports multiple environments: development, staging, production

set -euo pipefail  # Strict mode

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Default values
ENVIRONMENT="development"
BUILD_CONFIG="Debug"
SKIP_TESTS="false"
SKIP_BUILD="false"
FORCE_RECREATE="false"
VERBOSE="false"

# Script version
VERSION="1.0.0"

# Function to display usage
usage() {
    cat << EOF
?? SalesAPI Professional Deployment Script v$VERSION

Usage: $0 [OPTIONS]

OPTIONS:
    -e, --environment ENVIRONMENT   Target environment (development|staging|production)
    -c, --config CONFIG            Build configuration (Debug|Release)
    -s, --skip-tests              Skip running tests
    -b, --skip-build              Skip building the solution
    -f, --force                   Force recreate containers/services
    -v, --verbose                 Enable verbose output
    -h, --help                    Show this help message

ENVIRONMENTS:
    development     Local development with Docker Compose
    staging         Staging environment (Kubernetes)
    production      Production environment (Kubernetes)

EXAMPLES:
    $0 -e development                 # Deploy to development
    $0 -e production -c Release       # Deploy to production with Release build
    $0 -e staging --skip-tests        # Deploy to staging without running tests
    $0 --force --verbose              # Force recreate with verbose output

EOF
}

# Function to log messages
log() {
    local level=$1
    shift
    case $level in
        "INFO")  echo -e "${BLUE}[INFO]${NC} $*" ;;
        "WARN")  echo -e "${YELLOW}[WARN]${NC} $*" ;;
        "ERROR") echo -e "${RED}[ERROR]${NC} $*" ;;
        "SUCCESS") echo -e "${GREEN}[SUCCESS]${NC} $*" ;;
    esac
}

# Function to check prerequisites
check_prerequisites() {
    log "INFO" "Checking prerequisites..."
    
    # Check dotnet
    if ! command -v dotnet &> /dev/null; then
        log "ERROR" ".NET SDK is not installed"
        exit 1
    fi
    
    local dotnet_version=$(dotnet --version | cut -d '.' -f 1)
    if [ "$dotnet_version" -lt 8 ]; then
        log "ERROR" ".NET 8.0+ is required. Current version: $(dotnet --version)"
        exit 1
    fi
    
    # Check Docker for development environment
    if [ "$ENVIRONMENT" = "development" ]; then
        if ! command -v docker &> /dev/null; then
            log "ERROR" "Docker is not installed"
            exit 1
        fi
        
        if ! command -v docker-compose &> /dev/null; then
            log "ERROR" "Docker Compose is not installed"
            exit 1
        fi
    fi
    
    # Check kubectl for staging/production
    if [ "$ENVIRONMENT" = "staging" ] || [ "$ENVIRONMENT" = "production" ]; then
        if ! command -v kubectl &> /dev/null; then
            log "ERROR" "kubectl is not installed"
            exit 1
        fi
        
        if ! command -v helm &> /dev/null; then
            log "WARN" "Helm is not installed - using kubectl apply instead"
        fi
    fi
    
    log "SUCCESS" "Prerequisites check passed"
}

# Function to build the solution
build_solution() {
    if [ "$SKIP_BUILD" = "true" ]; then
        log "INFO" "Skipping build (--skip-build specified)"
        return
    fi
    
    log "INFO" "Building solution with $BUILD_CONFIG configuration..."
    
    # Clean previous builds
    log "INFO" "Cleaning previous builds..."
    dotnet clean SalesAPI.sln --configuration "$BUILD_CONFIG" --verbosity quiet
    
    # Build solution
    log "INFO" "Building solution..."
    if [ "$VERBOSE" = "true" ]; then
        dotnet build SalesAPI.sln --configuration "$BUILD_CONFIG"
    else
        dotnet build SalesAPI.sln --configuration "$BUILD_CONFIG" --verbosity quiet
    fi
    
    if [ $? -ne 0 ]; then
        log "ERROR" "Build failed"
        exit 1
    fi
    
    log "SUCCESS" "Build completed successfully"
}

# Function to run tests
run_tests() {
    if [ "$SKIP_TESTS" = "true" ]; then
        log "INFO" "Skipping tests (--skip-tests specified)"
        return
    fi
    
    log "INFO" "Running tests..."
    
    if [ "$VERBOSE" = "true" ]; then
        dotnet test --configuration "$BUILD_CONFIG" --no-build --logger "console;verbosity=detailed"
    else
        dotnet test --configuration "$BUILD_CONFIG" --no-build --verbosity quiet
    fi
    
    if [ $? -ne 0 ]; then
        log "ERROR" "Tests failed"
        exit 1
    fi
    
    log "SUCCESS" "All tests passed"
}

# Function to deploy to development environment
deploy_development() {
    log "INFO" "Deploying to development environment..."
    
    # Check if services are already running
    if [ "$FORCE_RECREATE" = "true" ]; then
        log "INFO" "Force recreating containers..."
        docker-compose down --remove-orphans
        docker-compose up -d --build --force-recreate
    else
        # Check if containers exist
        if docker-compose ps | grep -q "Up"; then
            log "INFO" "Services are already running. Use --force to recreate."
            docker-compose up -d
        else
            log "INFO" "Starting services..."
            docker-compose up -d --build
        fi
    fi
    
    # Wait for services to be ready
    log "INFO" "Waiting for services to be ready..."
    sleep 15
    
    # Run health checks
    run_health_checks_development
}

# Function to deploy to staging environment
deploy_staging() {
    log "INFO" "Deploying to staging environment..."
    
    # Build Docker images
    build_docker_images "staging"
    
    # Apply Kubernetes manifests
    kubectl apply -f infrastructure/kubernetes/staging/ --namespace salesapi-staging
    
    # Wait for deployment
    kubectl rollout status deployment/gateway-deployment --namespace salesapi-staging --timeout=300s
    kubectl rollout status deployment/sales-api-deployment --namespace salesapi-staging --timeout=300s
    kubectl rollout status deployment/inventory-api-deployment --namespace salesapi-staging --timeout=300s
    
    log "SUCCESS" "Staging deployment completed"
    
    # Run health checks
    run_health_checks_kubernetes "salesapi-staging"
}

# Function to deploy to production environment
deploy_production() {
    log "INFO" "Deploying to production environment..."
    
    # Additional safety checks for production
    if [ "$BUILD_CONFIG" != "Release" ]; then
        log "ERROR" "Production deployments must use Release configuration"
        exit 1
    fi
    
    if [ "$SKIP_TESTS" = "true" ]; then
        log "ERROR" "Cannot skip tests for production deployment"
        exit 1
    fi
    
    # Confirm production deployment
    echo -e "${YELLOW}??  You are about to deploy to PRODUCTION environment!${NC}"
    read -p "Are you sure you want to continue? (yes/no): " confirm
    if [ "$confirm" != "yes" ]; then
        log "INFO" "Production deployment cancelled"
        exit 0
    fi
    
    # Build Docker images
    build_docker_images "production"
    
    # Deploy with Helm if available
    if command -v helm &> /dev/null; then
        log "INFO" "Deploying with Helm..."
        helm upgrade --install salesapi ./charts/salesapi \
            --namespace salesapi-production \
            --create-namespace \
            --values ./charts/salesapi/values-production.yaml \
            --wait --timeout 10m
    else
        # Fallback to kubectl
        log "INFO" "Deploying with kubectl..."
        kubectl apply -f infrastructure/kubernetes/production/ --namespace salesapi-production
        
        # Wait for deployment
        kubectl rollout status deployment/gateway-deployment --namespace salesapi-production --timeout=600s
        kubectl rollout status deployment/sales-api-deployment --namespace salesapi-production --timeout=600s
        kubectl rollout status deployment/inventory-api-deployment --namespace salesapi-production --timeout=600s
    fi
    
    log "SUCCESS" "Production deployment completed"
    
    # Run health checks
    run_health_checks_kubernetes "salesapi-production"
    
    # Run smoke tests
    run_smoke_tests_production
}

# Function to build Docker images
build_docker_images() {
    local env=$1
    log "INFO" "Building Docker images for $env..."
    
    local tag="latest"
    if [ "$env" = "production" ]; then
        tag=$(date +%Y%m%d-%H%M%S)
    fi
    
    # Build images
    docker build -f src/gateway/Dockerfile -t salesapi/gateway:$tag . || exit 1
    docker build -f src/sales.api/Dockerfile -t salesapi/sales-api:$tag . || exit 1
    docker build -f src/inventory.api/Dockerfile -t salesapi/inventory-api:$tag . || exit 1
    
    log "SUCCESS" "Docker images built with tag: $tag"
}

# Function to run health checks for development
run_health_checks_development() {
    log "INFO" "Running health checks for development environment..."
    
    local services=("gateway:6000" "sales-api:5001" "inventory-api:5000")
    local failed=0
    
    for service in "${services[@]}"; do
        local name=$(echo $service | cut -d':' -f1)
        local port=$(echo $service | cut -d':' -f2)
        local url="http://localhost:$port/health"
        
        log "INFO" "Checking $name health..."
        
        local max_attempts=30
        local attempt=1
        while [ $attempt -le $max_attempts ]; do
            if curl -f -s "$url" >/dev/null 2>&1; then
                log "SUCCESS" "$name is healthy"
                break
            fi
            
            if [ $attempt -eq $max_attempts ]; then
                log "ERROR" "$name health check failed"
                ((failed++))
                break
            fi
            
            sleep 2
            ((attempt++))
        done
    done
    
    if [ $failed -eq 0 ]; then
        log "SUCCESS" "All health checks passed"
    else
        log "ERROR" "$failed service(s) failed health checks"
        exit 1
    fi
}

# Function to run health checks for Kubernetes
run_health_checks_kubernetes() {
    local namespace=$1
    log "INFO" "Running health checks for Kubernetes environment..."
    
    # Wait for pods to be ready
    kubectl wait --for=condition=ready pod -l app=salesapi --namespace $namespace --timeout=300s
    
    # Check service endpoints
    local services=(gateway sales-api inventory-api)
    for service in "${services[@]}"; do
        log "INFO" "Checking $service health..."
        
        # Port forward and check health
        kubectl port-forward svc/$service-service 8080:80 --namespace $namespace &
        local pf_pid=$!
        sleep 5
        
        if curl -f -s "http://localhost:8080/health" >/dev/null 2>&1; then
            log "SUCCESS" "$service is healthy"
        else
            log "ERROR" "$service health check failed"
            kill $pf_pid 2>/dev/null || true
            exit 1
        fi
        
        kill $pf_pid 2>/dev/null || true
        sleep 2
    done
    
    log "SUCCESS" "All Kubernetes health checks passed"
}

# Function to run smoke tests for production
run_smoke_tests_production() {
    log "INFO" "Running production smoke tests..."
    
    if [ -f "./scripts/testing/smoke-tests.sh" ]; then
        ./scripts/testing/smoke-tests.sh
    else
        log "WARN" "Smoke test script not found, skipping..."
    fi
}

# Function to display deployment summary
show_deployment_summary() {
    log "SUCCESS" "?? Deployment completed successfully!"
    echo ""
    echo "=================================="
    echo "?? DEPLOYMENT SUMMARY"
    echo "=================================="
    echo "Environment: $ENVIRONMENT"
    echo "Build Config: $BUILD_CONFIG"
    echo "Timestamp: $(date)"
    echo ""
    
    case $ENVIRONMENT in
        "development")
            echo "?? Service URLs:"
            echo "  Gateway:     http://localhost:6000"
            echo "  Sales API:   http://localhost:5001"
            echo "  Inventory:   http://localhost:5000"
            echo ""
            echo "?? Documentation:"
            echo "  Gateway Swagger:    http://localhost:6000/swagger"
            echo "  Sales Swagger:      http://localhost:5001/swagger"
            echo "  Inventory Swagger:  http://localhost:5000/swagger"
            ;;
        "staging"|"production")
            echo "??  Kubernetes Deployment:"
            echo "  Namespace: salesapi-$ENVIRONMENT"
            echo "  View pods: kubectl get pods -n salesapi-$ENVIRONMENT"
            echo "  View logs: kubectl logs -l app=salesapi -n salesapi-$ENVIRONMENT"
            ;;
    esac
    
    echo ""
    echo "???  Management Commands:"
    echo "  Health Check:  curl http://localhost:6000/health"
    echo "  View Logs:     docker-compose logs -f  # development"
    echo "  Stop Services: docker-compose down     # development"
    echo ""
    echo "?? Deployment successful - system ready for use!"
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -e|--environment)
            ENVIRONMENT="$2"
            shift 2
            ;;
        -c|--config)
            BUILD_CONFIG="$2"
            shift 2
            ;;
        -s|--skip-tests)
            SKIP_TESTS="true"
            shift
            ;;
        -b|--skip-build)
            SKIP_BUILD="true"
            shift
            ;;
        -f|--force)
            FORCE_RECREATE="true"
            shift
            ;;
        -v|--verbose)
            VERBOSE="true"
            shift
            ;;
        -h|--help)
            usage
            exit 0
            ;;
        *)
            log "ERROR" "Unknown option: $1"
            usage
            exit 1
            ;;
    esac
done

# Validate environment
case $ENVIRONMENT in
    "development"|"staging"|"production")
        ;;
    *)
        log "ERROR" "Invalid environment: $ENVIRONMENT"
        log "ERROR" "Valid environments: development, staging, production"
        exit 1
        ;;
esac

# Set build configuration based on environment
if [ "$ENVIRONMENT" = "production" ] && [ "$BUILD_CONFIG" = "Debug" ]; then
    BUILD_CONFIG="Release"
    log "INFO" "Automatically setting build configuration to Release for production"
fi

# Main execution flow
main() {
    echo "?? SalesAPI Professional Deployment Script v$VERSION"
    echo "=================================================="
    echo ""
    
    log "INFO" "Starting deployment to $ENVIRONMENT environment..."
    log "INFO" "Build configuration: $BUILD_CONFIG"
    
    # Execute deployment steps
    check_prerequisites
    build_solution
    run_tests
    
    case $ENVIRONMENT in
        "development")
            deploy_development
            ;;
        "staging")
            deploy_staging
            ;;
        "production")
            deploy_production
            ;;
    esac
    
    show_deployment_summary
}

# Trap to cleanup on exit
trap 'log "WARN" "Deployment interrupted"' INT TERM

# Run main function
main "$@"