# SalesAPI - Deployment Guide

## Overview

This guide provides complete instructions for deploying SalesAPI across different environments, from local development to production Kubernetes deployment.

## Prerequisites

### Local Development
- .NET 8.0 SDK
- Docker Desktop
- SQL Server or Docker container
- RabbitMQ or Docker container

### Production Deployment
- Kubernetes cluster
- Helm 3.x
- kubectl configured
- Container registry (Docker Hub, ACR, etc.)

## Local Deployment

### Quick Setup with Docker Compose

```bash
# Start all services
docker-compose up -d

# Verify status
docker-compose ps

# View logs
docker-compose logs -f

# Stop services
docker-compose down
```

### Manual Step-by-Step Setup

#### 1. Start Infrastructure Services
```bash
# Start SQL Server
docker run -e "ACCEPT_EULA=Y" \
  -e "SA_PASSWORD=Your_password123" \
  -p 1433:1433 \
  --name salesapi-sqlserver \
  -d mcr.microsoft.com/mssql/server:2022-latest

# Start RabbitMQ
docker run -d \
  --name salesapi-rabbitmq \
  -p 5672:5672 \
  -p 15672:15672 \
  -e RABBITMQ_DEFAULT_USER=admin \
  -e RABBITMQ_DEFAULT_PASS=admin123 \
  rabbitmq:3-management
```

#### 2. Build Application
```bash
# Compile solution
dotnet build SalesAPI.sln --configuration Release

# Run tests
dotnet test --configuration Release --no-build
```

#### 3. Run Microservices
```bash
# Terminal 1 - Gateway
cd src/gateway && dotnet run --configuration Release --urls "http://localhost:6000"

# Terminal 2 - Sales API
cd src/sales.api && dotnet run --configuration Release --urls "http://localhost:5001"

# Terminal 3 - Inventory API
cd src/inventory.api && dotnet run --configuration Release --urls "http://localhost:5000"
```

## Docker Deployment

### Building Images

```bash
# Build Gateway
docker build -f src/gateway/Dockerfile -t salesapi/gateway:latest .

# Build Sales API
docker build -f src/sales.api/Dockerfile -t salesapi/sales-api:latest .

# Build Inventory API
docker build -f src/inventory.api/Dockerfile -t salesapi/inventory-api:latest .
```

### Docker Compose Configuration

**docker-compose.yml**:
```yaml
version: '3.8'

services:
  gateway:
    image: salesapi/gateway:latest
    ports:
      - "6000:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Server=sqlserver;Database=GatewayDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True
    depends_on:
      - sqlserver
      - rabbitmq

  sales-api:
    image: salesapi/sales-api:latest
    ports:
      - "5001:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Server=sqlserver;Database=SalesDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True
      - ConnectionStrings__RabbitMQ=amqp://admin:admin123@rabbitmq:5672/
    depends_on:
      - sqlserver
      - rabbitmq

  inventory-api:
    image: salesapi/inventory-api:latest
    ports:
      - "5000:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Server=sqlserver;Database=InventoryDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True
      - ConnectionStrings__RabbitMQ=amqp://admin:admin123@rabbitmq:5672/
    depends_on:
      - sqlserver
      - rabbitmq

  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=Your_password123
    ports:
      - "1433:1433"
    volumes:
      - sqlserver_data:/var/opt/mssql

  rabbitmq:
    image: rabbitmq:3-management
    environment:
      - RABBITMQ_DEFAULT_USER=admin
      - RABBITMQ_DEFAULT_PASS=admin123
    ports:
      - "5672:5672"
      - "15672:15672"
    volumes:
      - rabbitmq_data:/var/lib/rabbitmq

volumes:
  sqlserver_data:
  rabbitmq_data:
```

## Kubernetes Deployment

### Namespace and Resources

```bash
# Create namespace
kubectl create namespace salesapi

# Apply configurations
kubectl apply -f infrastructure/kubernetes/
```

### Helm Chart Deployment (Recommended)

```bash
# Install with Helm
helm install salesapi ./charts/salesapi --namespace salesapi

# Upgrade
helm upgrade salesapi ./charts/salesapi --namespace salesapi

# Status
helm status salesapi --namespace salesapi
```

**values.yaml example**:
```yaml
global:
  registry: "your-registry.com"
  tag: "latest"
  pullPolicy: "Always"

gateway:
  replicaCount: 3
  service:
    type: LoadBalancer
    port: 6000

salesApi:
  replicaCount: 2
  service:
    port: 5001

inventoryApi:
  replicaCount: 2
  service:
    port: 5000

sqlserver:
  enabled: true
  persistence:
    size: 10Gi
  password: "Your_password123"

rabbitmq:
  enabled: true
  persistence:
    size: 5Gi
  auth:
    username: admin
    password: admin123
```

### Ingress and TLS

```yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: salesapi-ingress
  namespace: salesapi
  annotations:
    kubernetes.io/ingress.class: nginx
    cert-manager.io/cluster-issuer: letsencrypt-prod
spec:
  tls:
  - hosts:
    - api.yourcompany.com
    secretName: salesapi-tls
  rules:
  - host: api.yourcompany.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: gateway-service
            port:
              number: 6000
```

## Production Security Configuration

### Secrets Management
```bash
# Create secrets for production
kubectl create secret generic salesapi-secrets \
  --from-literal=sql-password="Your_secure_password_123!" \
  --from-literal=jwt-key="Your_super_secure_jwt_key_256_bits_minimum" \
  --from-literal=rabbitmq-password="Your_secure_rabbitmq_pass" \
  --namespace salesapi
```

### Environment Variables
```yaml
env:
- name: ConnectionStrings__DefaultConnection
  valueFrom:
    secretKeyRef:
      name: salesapi-secrets
      key: sql-connection-string
- name: Jwt__Key
  valueFrom:
    secretKeyRef:
      name: salesapi-secrets
      key: jwt-key
```

## Monitoring and Observability

### Health Checks
```yaml
livenessProbe:
  httpGet:
    path: /health
    port: 8080
  initialDelaySeconds: 30
  periodSeconds: 10

readinessProbe:
  httpGet:
    path: /health/ready
    port: 8080
  initialDelaySeconds: 5
  periodSeconds: 5
```

### Resource Management
```yaml
resources:
  requests:
    memory: "256Mi"
    cpu: "250m"
  limits:
    memory: "512Mi"
    cpu: "500m"
```

### Horizontal Pod Autoscaler
```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: gateway-hpa
  namespace: salesapi
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: gateway-deployment
  minReplicas: 2
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
```

## CI/CD Pipeline

### GitHub Actions Example
```yaml
name: Deploy SalesAPI

on:
  push:
    branches: [ main ]

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    
    - name: Build
      run: dotnet build --configuration Release
    
    - name: Test
      run: dotnet test --configuration Release --no-build
    
    - name: Build Docker Images
      run: |
        docker build -f src/gateway/Dockerfile -t ${{ secrets.REGISTRY }}/salesapi-gateway:${{ github.sha }} .
        docker build -f src/sales.api/Dockerfile -t ${{ secrets.REGISTRY }}/salesapi-sales:${{ github.sha }} .
        docker build -f src/inventory.api/Dockerfile -t ${{ secrets.REGISTRY }}/salesapi-inventory:${{ github.sha }} .
    
    - name: Push to Registry
      run: |
        echo ${{ secrets.REGISTRY_PASSWORD }} | docker login ${{ secrets.REGISTRY }} -u ${{ secrets.REGISTRY_USERNAME }} --password-stdin
        docker push ${{ secrets.REGISTRY }}/salesapi-gateway:${{ github.sha }}
        docker push ${{ secrets.REGISTRY }}/salesapi-sales:${{ github.sha }}
        docker push ${{ secrets.REGISTRY }}/salesapi-inventory:${{ github.sha }}
    
    - name: Deploy to Kubernetes
      uses: azure/k8s-deploy@v1
      with:
        manifests: |
          infrastructure/kubernetes/
        images: |
          ${{ secrets.REGISTRY }}/salesapi-gateway:${{ github.sha }}
          ${{ secrets.REGISTRY }}/salesapi-sales:${{ github.sha }}
          ${{ secrets.REGISTRY }}/salesapi-inventory:${{ github.sha }}
```

## Deployment Validation

### Smoke Tests
```bash
# Health check validation
curl -f http://your-domain.com/health || exit 1

# Basic functionality test
curl -f http://your-domain.com/inventory/products || exit 1
```

### Load Testing
```bash
# Apache Bench
ab -n 1000 -c 10 http://your-domain.com/inventory/products

# K6 Load Test
k6 run scripts/load-tests/api-load-test.js
```

## Deployment Checklist

### Pre-Deployment
- [ ] All tests passing
- [ ] Build successful
- [ ] Security vulnerabilities resolved
- [ ] Production configurations validated
- [ ] Current environment backed up

### Deployment
- [ ] Deployment executed successfully
- [ ] Health checks passing
- [ ] Smoke tests completed
- [ ] Logs being generated correctly
- [ ] Metrics being collected

### Post-Deployment
- [ ] Monitoring active
- [ ] Alerts configured
- [ ] Performance within expected ranges
- [ ] Critical functionality tested
- [ ] Rollback plan documented

## Troubleshooting

### Common Issues

#### Database Connection Failures
```bash
# Verify connectivity
kubectl exec -it pod-name -- ping sqlserver-service

# Check secrets
kubectl get secret salesapi-secrets -o yaml
```

#### Messaging Issues
```bash
# Check RabbitMQ
kubectl port-forward svc/rabbitmq 15672:15672
# Access: http://localhost:15672
```

#### Performance Issues
```bash
# Check resource usage
kubectl top pods -n salesapi

# Check HPA status
kubectl get hpa -n salesapi
```

### Useful Commands
```bash
# View logs from all pods
kubectl logs -l app=salesapi -n salesapi --tail=100

# Restart deployment
kubectl rollout restart deployment/gateway-deployment -n salesapi

# Check events
kubectl get events -n salesapi --sort-by='.lastTimestamp'
```

## Environment Matrix

| Environment | Configuration | Observability | Backup |
|-------------|---------------|---------------|--------|
| **Development** | Local/Docker | Basic logs | Not required |
| **Staging** | Kubernetes | Prometheus + Grafana | Daily |
| **Production** | Kubernetes + Helm | Full monitoring | Continuous |

---

**Status: Production Ready Deployment Guide**