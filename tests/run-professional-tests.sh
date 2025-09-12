#!/bin/bash

# SalesAPI Professional Test Suite Runner
# Execute este script para rodar todos os testes profissionais

echo "?? SalesAPI Professional Test Suite"
echo "==================================="
echo

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Test counters
TOTAL_TESTS=0
TOTAL_PASSED=0
TOTAL_FAILED=0
TOTAL_TIME=0

# Function to run tests and capture results
run_test_project() {
    local PROJECT_NAME=$1
    local PROJECT_PATH=$2
    
    echo -e "${BLUE}?? Running $PROJECT_NAME...${NC}"
    
    # Run tests and capture output
    START_TIME=$(date +%s)
    
    if dotnet test "$PROJECT_PATH" --verbosity quiet --logger "console;verbosity=minimal" > test_output.tmp 2>&1; then
        END_TIME=$(date +%s)
        DURATION=$((END_TIME - START_TIME))
        
        # Extract test counts from output
        PASSED=$(grep -o "Passed:.*" test_output.tmp | grep -o '[0-9]*' | head -1 || echo "0")
        FAILED=$(grep -o "Failed:.*" test_output.tmp | grep -o '[0-9]*' | head -1 || echo "0")
        
        if [ -z "$PASSED" ]; then PASSED=0; fi
        if [ -z "$FAILED" ]; then FAILED=0; fi
        
        TOTAL_TESTS=$((TOTAL_TESTS + PASSED + FAILED))
        TOTAL_PASSED=$((TOTAL_PASSED + PASSED))
        TOTAL_FAILED=$((TOTAL_FAILED + FAILED))
        TOTAL_TIME=$((TOTAL_TIME + DURATION))
        
        if [ "$FAILED" -eq 0 ]; then
            echo -e "   ${GREEN}? $PASSED tests passed${NC} (${DURATION}s)"
        else
            echo -e "   ${RED}? $FAILED tests failed${NC}, ${GREEN}$PASSED passed${NC} (${DURATION}s)"
        fi
    else
        echo -e "   ${RED}? Build/Test execution failed${NC}"
        cat test_output.tmp
        return 1
    fi
    
    rm -f test_output.tmp
    echo
}

echo "?? Building Test Infrastructure..."
dotnet build tests/SalesAPI.Tests.Professional/TestInfrastructure/TestInfrastructure.csproj --verbosity quiet
if [ $? -ne 0 ]; then
    echo -e "${RED}? Failed to build TestInfrastructure${NC}"
    exit 1
fi
echo -e "${GREEN}? TestInfrastructure built successfully${NC}"
echo

# Run each test project
run_test_project "Domain Tests" "tests/SalesAPI.Tests.Professional/Domain.Tests/Domain.Tests.csproj"
run_test_project "Infrastructure Tests" "tests/SalesAPI.Tests.Professional/Infrastructure.Tests/Infrastructure.Tests.csproj"  
run_test_project "Integration Tests" "tests/SalesAPI.Tests.Professional/Integration.Tests/Integration.Tests.csproj"

echo "?? TEST SUMMARY"
echo "==============="

if [ $TOTAL_FAILED -eq 0 ]; then
    echo -e "${GREEN}?? All tests passed!${NC}"
    echo -e "   Total: ${GREEN}$TOTAL_PASSED tests${NC}"
    echo -e "   Time:  ${BLUE}${TOTAL_TIME}s${NC}"
    echo
    echo -e "${GREEN}? Professional test suite completed successfully!${NC}"
    exit 0
else
    echo -e "${RED}? Some tests failed${NC}"
    echo -e "   Passed: ${GREEN}$TOTAL_PASSED${NC}"
    echo -e "   Failed: ${RED}$TOTAL_FAILED${NC}"
    echo -e "   Total:  $TOTAL_TESTS tests"
    echo -e "   Time:   ${BLUE}${TOTAL_TIME}s${NC}"
    echo
    echo -e "${YELLOW}??  Please check the failed tests above${NC}"
    exit 1
fi