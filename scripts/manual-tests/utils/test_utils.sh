#!/bin/bash

# SalesAPI Manual Tests - Utility Functions
# Common functions used across all test scripts

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Test result variables
TEST_COUNT=0
PASSED_COUNT=0
FAILED_COUNT=0
CURRENT_TEST=""
RESULTS_DIR="scripts/manual-tests/results"
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
RESULTS_FILE="$RESULTS_DIR/test_results_$TIMESTAMP.json"
SUMMARY_FILE="$RESULTS_DIR/test_summary_$TIMESTAMP.txt"
FAILED_LOG="$RESULTS_DIR/failed_tests.log"

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CONFIG_FILE="$SCRIPT_DIR/../config/endpoints.json"

# Create results directory if it doesn't exist
mkdir -p "$RESULTS_DIR"

# Initialize results file
echo '{"testRun": {"timestamp": "'$TIMESTAMP'", "tests": []}}' > "$RESULTS_FILE"

# Logging functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

log_test() {
    echo -e "${PURPLE}[TEST]${NC} $1"
}

# Test execution functions
start_test() {
    CURRENT_TEST="$1"
    TEST_COUNT=$((TEST_COUNT + 1))
    log_test "Starting: $CURRENT_TEST"
}

pass_test() {
    PASSED_COUNT=$((PASSED_COUNT + 1))
    log_success "PASSED: $CURRENT_TEST"
    record_test_result "PASSED" "$1"
}

fail_test() {
    FAILED_COUNT=$((FAILED_COUNT + 1))
    log_error "FAILED: $CURRENT_TEST - $1"
    echo "$(date '+%Y-%m-%d %H:%M:%S') - FAILED: $CURRENT_TEST - $1" >> "$FAILED_LOG"
    record_test_result "FAILED" "$1"
}

skip_test() {
    log_warning "SKIPPED: $CURRENT_TEST - $1"
    record_test_result "SKIPPED" "$1"
}

# Record test result to JSON file
record_test_result() {
    local status="$1"
    local message="$2"
    local temp_file="/tmp/test_results_temp.json"
    
    # Create test result object
    local test_result="{
        \"name\": \"$CURRENT_TEST\",
        \"status\": \"$status\",
        \"message\": \"$message\",
        \"timestamp\": \"$(date -Iseconds)\"
    }"
    
    # Add to results file (this is a simple approach, in production you'd use jq)
    if command -v jq > /dev/null; then
        jq ".testRun.tests += [$test_result]" "$RESULTS_FILE" > "$temp_file" && mv "$temp_file" "$RESULTS_FILE"
    fi
}

# HTTP helper functions
make_request() {
    local method="$1"
    local url="$2"
    local headers="$3"
    local data="$4"
    local expected_status="$5"
    
    local curl_cmd="curl -s -w '%{http_code}:%{time_total}' -X $method"
    
    if [ -n "$headers" ]; then
        while IFS= read -r header; do
            curl_cmd="$curl_cmd -H '$header'"
        done <<< "$headers"
    fi
    
    if [ -n "$data" ]; then
        curl_cmd="$curl_cmd -d '$data'"
    fi
    
    curl_cmd="$curl_cmd '$url'"
    
    local response
    response=$(eval $curl_cmd 2>/dev/null)
    
    if [ $? -ne 0 ]; then
        echo "ERROR:CURL_FAILED"
        return 1
    fi
    
    local http_code="${response##*:}"
    local time_total="${response%:*}"
    time_total="${time_total##*:}"
    local body="${response%:*:*}"
    
    echo "$http_code|$time_total|$body"
}

# Authentication helper
get_auth_token() {
    local username="$1"
    local password="$2"
    local gateway_url="$3"
    
    local response
    response=$(make_request "POST" "$gateway_url/auth/token" \
        "Content-Type: application/json" \
        "{\"username\":\"$username\",\"password\":\"$password\"}" \
        "200")
    
    local http_code="${response%%|*}"
    local body="${response##*|}"
    
    if [ "$http_code" = "200" ] && command -v jq > /dev/null; then
        echo "$body" | jq -r '.accessToken' 2>/dev/null
    else
        echo ""
    fi
}

# Check if service is running
check_service() {
    local service_name="$1"
    local url="$2"
    
    local response
    response=$(curl -s -w '%{http_code}' -o /dev/null "$url" --max-time 5)
    
    if [ "$response" = "200" ]; then
        log_success "$service_name is running"
        return 0
    else
        log_error "$service_name is not responding (HTTP: $response)"
        return 1
    fi
}

# Load configuration from JSON file
load_config() {
    if [ ! -f "$CONFIG_FILE" ]; then
        log_error "Configuration file not found: $CONFIG_FILE"
        return 1
    fi
    
    if ! command -v jq > /dev/null; then
        log_warning "jq not found. Some features may be limited."
        return 1
    fi
    
    return 0
}

# Get configuration value
get_config_value() {
    local path="$1"
    if command -v jq > /dev/null && [ -f "$CONFIG_FILE" ]; then
        jq -r "$path" "$CONFIG_FILE" 2>/dev/null
    else
        echo ""
    fi
}

# Generate test summary
generate_summary() {
    local total=$TEST_COUNT
    local passed=$PASSED_COUNT
    local failed=$FAILED_COUNT
    local success_rate=0
    
    if [ $total -gt 0 ]; then
        success_rate=$(( (passed * 100) / total ))
    fi
    
    echo "============================================" > "$SUMMARY_FILE"
    echo "SalesAPI Manual Tests Summary" >> "$SUMMARY_FILE"
    echo "============================================" >> "$SUMMARY_FILE"
    echo "Timestamp: $(date)" >> "$SUMMARY_FILE"
    echo "Total Tests: $total" >> "$SUMMARY_FILE"
    echo "Passed: $passed" >> "$SUMMARY_FILE"
    echo "Failed: $failed" >> "$SUMMARY_FILE"
    echo "Success Rate: $success_rate%" >> "$SUMMARY_FILE"
    echo "============================================" >> "$SUMMARY_FILE"
    
    if [ $failed -gt 0 ]; then
        echo "" >> "$SUMMARY_FILE"
        echo "Failed Tests:" >> "$SUMMARY_FILE"
        if [ -f "$FAILED_LOG" ]; then
            cat "$FAILED_LOG" >> "$SUMMARY_FILE"
        fi
    fi
    
    # Display summary
    echo
    log_info "============================================"
    log_info "SalesAPI Manual Tests Summary"
    log_info "============================================"
    log_info "Total Tests: $total"
    if [ $passed -gt 0 ]; then
        log_success "Passed: $passed"
    fi
    if [ $failed -gt 0 ]; then
        log_error "Failed: $failed"
    fi
    log_info "Success Rate: $success_rate%"
    log_info "============================================"
    log_info "Detailed results: $RESULTS_FILE"
    log_info "Summary: $SUMMARY_FILE"
    
    if [ $failed -gt 0 ]; then
        log_info "Failed tests log: $FAILED_LOG"
        return 1
    fi
    
    return 0
}

# Cleanup function
cleanup_test_data() {
    log_info "Cleaning up test data..."
    # This would implement cleanup logic
    # For now, just log the action
}

# Wait for user input
wait_for_user() {
    echo
    read -p "Press Enter to continue..."
    echo
}

# Check prerequisites
check_prerequisites() {
    log_info "Checking prerequisites..."
    
    # Check if curl is available
    if ! command -v curl > /dev/null; then
        log_error "curl is required but not installed"
        return 1
    fi
    
    # Check if jq is available (optional but recommended)
    if ! command -v jq > /dev/null; then
        log_warning "jq is recommended for JSON processing but not required"
    fi
    
    # Load configuration
    if ! load_config; then
        log_error "Failed to load configuration"
        return 1
    fi
    
    log_success "Prerequisites check completed"
    return 0
}