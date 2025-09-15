// This file has been refactored to separate concerns.
// 
// RealEventPublisher moved to: src/sales.api/Services/EventPublisher/RealEventPublisher.cs
// MockEventPublisher moved to: tests/SalesAPI.Tests.Professional/TestInfrastructure/Mocks/MockEventPublisher.cs
// Factory pattern available at: src/sales.api/Services/EventPublisher/EventPublisherFactory.cs
//
// This separation ensures that:
// 1. Production code doesn't contain mock implementations
// 2. Test code has access to enhanced mock capabilities
// 3. Factory pattern provides clean abstraction for different environments
//
// To use in production: Register RealEventPublisher in DI container
// To use in tests: Use MockEventPublisher from test infrastructure

// For backward compatibility, you can import the real implementation:
// using SalesApi.Services.EventPublisher;