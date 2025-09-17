# Messaging Architecture

## Overview

This messaging architecture is designed to be professional, scalable, and follow industry best practices, **without fake implementations in production code**.

## Architecture by Environment

### Production Environment
- ? **RealEventPublisher** with Rebus/RabbitMQ
- ? Configured via `appsettings.Production.json`
- ? Environment-specific support
- ? **Messaging.Enabled = true**

### Development Environment  
- ? **NullEventPublisher** when messaging disabled
- ? **RealEventPublisher** when messaging enabled
- ? Flexible configuration via `appsettings.Development.json`
- ? **Messaging.Enabled = false** (default for local development)

### Testing Environment
- ? **MockEventPublisher** isolated in test infrastructure
- ? Located in `tests/*/TestInfrastructure/Mocks/`
- ? **MessagingTestFixture** for test configuration
- ? **Completely separated** from production code

## Configuration

### Sales API

#### Production (`appsettings.Production.json`)
```json
{
  "Messaging": {
    "Enabled": true,
    "ConnectionString": "amqp://user:password@localhost:5672",
    "Workers": 5
  }
}
```

#### Development (`appsettings.Development.json`)
```json
{
  "Messaging": {
    "Enabled": false,
    "ConnectionString": "amqp://admin:admin123@localhost:5672/",
    "Workers": 1
  }
}
```

### Inventory API

#### Production (`appsettings.Production.json`)
```json
{
  "Messaging": {
    "Enabled": true,
    "ConnectionString": "amqp://user:password@localhost:5672",
    "Workers": 5
  }
}
```

#### Development (`appsettings.Development.json`)
```json
{
  "Messaging": {
    "Enabled": false,
    "ConnectionString": "amqp://admin:admin123@localhost:5672/",
    "Workers": 1
  }
}
```

## Implementations

### Production Implementations
| Service | Implementation | Location | Usage |
|---------|----------------|----------|-------|
| **Sales API** | `RealEventPublisher` | `src/sales.api/Services/EventPublisher/RealEventPublisher.cs` | Production with Rebus |
| **Inventory API** | `InventoryEventPublisher` | `src/inventory.api/Configuration/MessagingConfiguration.cs` | Production with Rebus |

### Null Object Pattern (Messaging Disabled)
| Service | Implementation | Location | Usage |
|---------|----------------|----------|-------|
| **Sales API** | `NullEventPublisher` | `src/sales.api/Configuration/MessagingConfiguration.cs` | Messaging disabled |
| **Inventory API** | `NullEventPublisher` | `src/inventory.api/Configuration/MessagingConfiguration.cs` | Messaging disabled |

### Test Implementations
| Service | Implementation | Location | Usage |
|---------|----------------|----------|-------|
| **Tests** | `MockEventPublisher` | `tests/SalesAPI.Tests.Professional/TestInfrastructure/Mocks/` | Testing only |
| **Test Fixture** | `MessagingTestFixture` | `tests/SalesAPI.Tests.Professional/TestInfrastructure/Fixtures/` | Test configuration |

## Architecture Principles

### Clean Code
- **No fake implementations in production**
- **Clear separation** between production and test code
- **Null Object Pattern** for disabled messaging scenarios
- **Factory Pattern** for implementation creation

### Flexible Configuration
- **Environment-based configuration** via appsettings
- **Enable/disable** via configuration
- **Environment-specific parameters** (workers, connection strings)
- **Multi-environment support**

### Testability
- **MockEventPublisher** isolated in tests
- **MessagingTestFixture** for test configuration
- **Event capture and verification** in tests
- **Automatic cleanup** between tests

## Message Flow

### Sales API ? Inventory API
```
Sales API (Order Confirmed)
    ? RealEventPublisher
    ? Rebus/RabbitMQ
    ? OrderConfirmedEvent
Inventory API (Order Processing)
```

### Test Environment
```
Test Method
    ? MockEventPublisher
    ? Event Capture (In-Memory)
    ? Test Assertions
Test Verification
```

## Architecture Validations

### Removed Implementations
- ~~`DummyEventPublisher.cs`~~ (Sales API)
- ~~`DummyEventPublisher.cs`~~ (Inventory API)
- ~~`MockEventPublisher.cs`~~ (Sales API - moved to tests)
- ~~`DevMockEventPublisher`~~ (EventPublisherFactory)

### Current Implementations
- `RealEventPublisher` (Sales API)
- `InventoryEventPublisher` (Inventory API)
- `NullEventPublisher` (both, when messaging disabled)
- `MockEventPublisher` (tests only)

## Usage Guidelines

### For Production
1. **Configure** `Messaging.Enabled = true`
2. **Set** RabbitMQ connection string
3. **Configure** workers as needed
4. **Deploy** with production settings

### For Development
1. **Keep** `Messaging.Enabled = false` for local development
2. **Enable** `Messaging.Enabled = true` to test with RabbitMQ
3. **Configure** local RabbitMQ if needed

### For Testing
1. **Use** `MessagingTestFixture` in tests
2. **Verify** captured events via `MockEventPublisher`
3. **Clean** events between tests with `ClearPublishedEvents()`

## Benefits Achieved

| Aspect | Benefit |
|--------|---------|
| **Quality** | Zero fake implementations in production |
| **Maintenance** | Centralized and flexible configuration |
| **Testability** | Isolated and robust test infrastructure |
| **Scalability** | Support for multiple environments and configurations |
| **Professionalism** | Production-ready architecture |

---

*Documentation updated: December 2024*  
*Architecture implemented following industry best practices*