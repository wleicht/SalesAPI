# Troubleshooting Guide for SalesAPI Manual Tests

## Common Issues and Solutions

### 1. Services Not Running

**Problem**: Tests fail with connection errors or "Service not responding"

**Solutions**:
```bash
# Check if services are running on expected ports
netstat -an | grep -E "(5000|5001|6000)"

# On Windows
netstat -an | findstr "5000 5001 6000"

# Start services manually if needed
cd src/gateway && dotnet run
cd src/inventory.api && dotnet run  
cd src/sales.api && dotnet run
```

**Alternative**: Use Docker Compose
```bash
docker-compose up -d
```

### 2. Authentication Token Expired

**Problem**: Tests fail with 401 Unauthorized after running for a while

**Solutions**:
```bash
# Re-run authentication tests to get fresh tokens
./scripts/manual-tests/01_authentication_tests.sh

# Or restart the full test suite
./scripts/manual-tests/run_manual_tests.sh
```

**Token Expiry**: Default JWT tokens expire after 1 hour

### 3. Database Connection Issues

**Problem**: Tests fail with database-related errors

**Solutions**:
```bash
# Check SQL Server is running
docker ps | grep mssql

# Start SQL Server container if needed
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=Your_password123" \
  -p 1433:1433 --name sqlserver -d mcr.microsoft.com/mssql/server:2022-latest

# Check connection strings in appsettings.json files
grep -r "ConnectionStrings" src/*/appsettings.json
```

### 4. RabbitMQ Connection Issues

**Problem**: Event-driven tests fail with messaging errors

**Solutions**:
```bash
# Check RabbitMQ is running
docker ps | grep rabbitmq

# Start RabbitMQ container if needed
docker run -d --name rabbitmq \
  -p 5672:5672 -p 15672:15672 \
  -e RABBITMQ_DEFAULT_USER=admin \
  -e RABBITMQ_DEFAULT_PASS=admin123 \
  rabbitmq:3-management

# Access RabbitMQ management UI
# http://localhost:15672 (admin/admin123)
```

### 5. Concurrency Test Failures

**Problem**: Concurrent tests report inconsistent results

**Causes**:
- Resource contention
- Timing issues
- Race conditions in test execution

**Solutions**:
```bash
# Run concurrency tests with more conservative settings
# Modify the test to use smaller quantities or fewer concurrent requests

# Check system resources
top  # Linux/macOS
taskmgr  # Windows

# Run tests one at a time instead of in parallel
./scripts/manual-tests/run_manual_tests.sh --skip-concurrency
```

### 6. Test Data Conflicts

**Problem**: Tests fail due to conflicting test data

**Solutions**:
```bash
# Clear test databases
# This depends on your database setup - typically:

# Reset databases (if using in-memory or test databases)
# Or use unique GUIDs for test data to avoid conflicts

# Check for leftover test data
curl -H "Authorization: Bearer $ADMIN_TOKEN" http://localhost:6000/inventory/products
```

### 7. Port Conflicts

**Problem**: Services fail to start due to port conflicts

**Solutions**:
```bash
# Find processes using the ports
lsof -i :5000  # Linux/macOS
lsof -i :5001
lsof -i :6000

# On Windows
netstat -ano | findstr ":5000"
netstat -ano | findstr ":5001"  
netstat -ano | findstr ":6000"

# Kill conflicting processes or change ports in configuration
```

### 8. JSON Processing Issues

**Problem**: Tests fail to parse JSON responses

**Solutions**:
```bash
# Install jq for better JSON handling
# Ubuntu/Debian
sudo apt-get install jq

# macOS
brew install jq

# Windows (using chocolatey)
choco install jq

# Or use alternative parsing methods in the scripts
```

### 9. Permission Issues

**Problem**: Cannot write to results directory or execute scripts

**Solutions**:
```bash
# Make scripts executable
chmod +x scripts/manual-tests/*.sh

# Create results directory with proper permissions
mkdir -p scripts/manual-tests/results
chmod 755 scripts/manual-tests/results

# On Windows, ensure PowerShell execution policy allows scripts
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### 10. SSL/TLS Certificate Issues

**Problem**: HTTPS requests fail with certificate errors

**Solutions**:
```bash
# For development, you might need to bypass SSL verification
# This is NOT recommended for production

# In curl commands, add -k flag to ignore SSL errors
curl -k https://localhost:6000/gateway/status

# Or configure proper development certificates
dotnet dev-certs https --trust
```

## Debugging Commands

### Check Service Health
```bash
curl -i http://localhost:6000/gateway/status
curl -i http://localhost:5000/health
curl -i http://localhost:5001/health
```

### Verify Authentication
```bash
# Get token
TOKEN=$(curl -s -X POST http://localhost:6000/auth/token \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}' | \
  jq -r '.accessToken')

echo "Token: $TOKEN"

# Test authenticated request
curl -H "Authorization: Bearer $TOKEN" http://localhost:6000/inventory/products
```

### Check Database Connectivity
```bash
# This depends on your setup, but generally:
sqlcmd -S localhost -U sa -P "Your_password123" -Q "SELECT 1"
```

### Monitor Logs
```bash
# Check application logs
tail -f logs/*.log

# Or check console output of running services
```

## Environment-Specific Issues

### Windows-Specific
- Use PowerShell instead of bash scripts when possible
- Check Windows Defender or antivirus blocking executables
- Ensure proper line endings (CRLF vs LF) in scripts

### Linux/macOS-Specific
- Check firewall settings (iptables, ufw)
- Verify sufficient file descriptors limit
- Ensure proper case sensitivity in file paths

### Docker-Specific
- Check container logs: `docker logs <container_name>`
- Verify network connectivity between containers
- Ensure proper port mappings

## Getting Help

If you continue to experience issues:

1. **Check Logs**: Always check application logs first
2. **Verify Configuration**: Ensure all configuration files are correct
3. **Test Individual Components**: Test each service independently
4. **Check Dependencies**: Verify all required services are running
5. **Review Documentation**: Check API documentation for changes

## Reporting Issues

When reporting issues, please include:
- Operating system and version
- .NET version
- Docker version (if using containers)
- Full error messages
- Steps to reproduce
- Test results output
- Relevant log files