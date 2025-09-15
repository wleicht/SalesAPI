using FluentAssertions;
using SalesApi.Configuration.Constants;
using SalesAPI.Tests.Professional.TestInfrastructure.Configuration;
using Xunit;

namespace SalesAPI.Tests.Professional.Configuration
{
    /// <summary>
    /// Tests to validate that all configuration constants have valid and reasonable values.
    /// These tests help ensure that magic numbers elimination didn't introduce invalid values.
    /// </summary>
    public class ConfigurationValidationTests
    {
        [Fact]
        public void NetworkConstants_Ports_ShouldHaveValidValues()
        {
            // Assert - All ports should be in valid range
            NetworkConstants.Ports.SalesApi.Should().BeInRange(1024, 65535, "SalesApi port should be in valid range");
            NetworkConstants.Ports.InventoryApi.Should().BeInRange(1024, 65535, "InventoryApi port should be in valid range");
            NetworkConstants.Ports.Gateway.Should().BeInRange(1024, 65535, "Gateway port should be in valid range");
            NetworkConstants.Ports.ContainerInternal.Should().BeInRange(1024, 65535, "ContainerInternal port should be in valid range");
            NetworkConstants.Ports.RabbitMQ.Should().BeInRange(1024, 65535, "RabbitMQ port should be in valid range");
            NetworkConstants.Ports.SqlServer.Should().BeInRange(1024, 65535, "SqlServer port should be in valid range");
        }

        [Fact]
        public void NetworkConstants_Ports_ShouldBeUnique()
        {
            // Arrange
            var ports = new[]
            {
                NetworkConstants.Ports.SalesApi,
                NetworkConstants.Ports.InventoryApi,
                NetworkConstants.Ports.Gateway,
                NetworkConstants.Ports.RabbitMQ
                // Note: ContainerInternal and SqlServer are expected to be reused
            };

            // Assert - External ports should be unique
            ports.Should().OnlyHaveUniqueItems("External ports should not conflict");
        }

        [Fact]
        public void NetworkConstants_Hosts_ShouldNotBeEmpty()
        {
            // Assert
            NetworkConstants.Hosts.Localhost.Should().NotBeNullOrEmpty("Localhost should be defined");
            NetworkConstants.Hosts.DockerInternal.Should().NotBeNullOrEmpty("DockerInternal should be defined");
            NetworkConstants.Hosts.InventoryService.Should().NotBeNullOrEmpty("InventoryService should be defined");
            NetworkConstants.Hosts.SalesService.Should().NotBeNullOrEmpty("SalesService should be defined");
            NetworkConstants.Hosts.GatewayService.Should().NotBeNullOrEmpty("GatewayService should be defined");
        }

        [Fact]
        public void NetworkConstants_Timeouts_ShouldHaveReasonableValues()
        {
            // Assert - Timeouts should be positive and reasonable
            NetworkConstants.Timeouts.HealthCheckInterval.Should().BeGreaterThan(0, "Health check interval should be positive");
            NetworkConstants.Timeouts.HealthCheckInterval.Should().BeLessThan(300, "Health check interval should be reasonable");
            
            NetworkConstants.Timeouts.HealthCheckTimeout.Should().BeGreaterThan(0, "Health check timeout should be positive");
            NetworkConstants.Timeouts.HealthCheckTimeout.Should().BeLessThan(NetworkConstants.Timeouts.HealthCheckInterval, 
                "Health check timeout should be less than interval");
            
            NetworkConstants.Timeouts.HealthCheckRetries.Should().BeGreaterThan(0, "Health check retries should be positive");
            NetworkConstants.Timeouts.HealthCheckRetries.Should().BeLessThan(10, "Health check retries should be reasonable");
            
            NetworkConstants.Timeouts.HealthCheckStartPeriod.Should().BeGreaterThan(0, "Health check start period should be positive");
            NetworkConstants.Timeouts.HttpClientTimeout.Should().BeGreaterThan(0, "HTTP client timeout should be positive");
            NetworkConstants.Timeouts.DatabaseTimeout.Should().BeGreaterThan(0, "Database timeout should be positive");
        }

        [Fact]
        public void SecurityConstants_Development_ShouldHaveValidValues()
        {
            // Assert
            SecurityConstants.Development.DefaultRabbitMQUsername.Should().NotBeNullOrEmpty("RabbitMQ username should be defined");
            SecurityConstants.Development.DefaultRabbitMQPassword.Should().NotBeNullOrEmpty("RabbitMQ password should be defined");
            SecurityConstants.Development.DefaultSqlPassword.Should().NotBeNullOrEmpty("SQL password should be defined");
            SecurityConstants.Development.DefaultSqlUsername.Should().NotBeNullOrEmpty("SQL username should be defined");
            SecurityConstants.Development.DevJwtKey.Should().NotBeNullOrEmpty("JWT key should be defined");
            SecurityConstants.Development.DevJwtKey.Length.Should().BeGreaterOrEqualTo(SecurityConstants.Jwt.MinimumKeyLength, 
                "JWT key should meet minimum length requirement");
        }

        [Fact]
        public void SecurityConstants_EnvironmentKeys_ShouldBeValidEnvironmentVariableNames()
        {
            // Assert - Environment variable names should follow conventions
            SecurityConstants.EnvironmentKeys.RabbitMQUsername.Should().MatchRegex(@"^[A-Z_]+$", "Should be uppercase with underscores");
            SecurityConstants.EnvironmentKeys.RabbitMQPassword.Should().MatchRegex(@"^[A-Z_]+$", "Should be uppercase with underscores");
            SecurityConstants.EnvironmentKeys.SqlPassword.Should().MatchRegex(@"^[A-Z_]+$", "Should be uppercase with underscores");
            SecurityConstants.EnvironmentKeys.SqlUsername.Should().MatchRegex(@"^[A-Z_]+$", "Should be uppercase with underscores");
            SecurityConstants.EnvironmentKeys.JwtKey.Should().MatchRegex(@"^[A-Z_]+$", "Should be uppercase with underscores");
            SecurityConstants.EnvironmentKeys.JwtIssuer.Should().MatchRegex(@"^[A-Z_]+$", "Should be uppercase with underscores");
            SecurityConstants.EnvironmentKeys.JwtAudience.Should().MatchRegex(@"^[A-Z_]+$", "Should be uppercase with underscores");
        }

        [Fact]
        public void SecurityConstants_Jwt_ShouldHaveValidDefaults()
        {
            // Assert
            SecurityConstants.Jwt.DefaultExpiryDays.Should().BeGreaterThan(0, "Default expiry should be positive");
            SecurityConstants.Jwt.DefaultExpiryDays.Should().BeLessThan(365, "Default expiry should be reasonable");
            SecurityConstants.Jwt.MinimumKeyLength.Should().BeGreaterThan(0, "Minimum key length should be positive");
            SecurityConstants.Jwt.DefaultIssuer.Should().NotBeNullOrEmpty("Default issuer should be defined");
            SecurityConstants.Jwt.DefaultAudience.Should().NotBeNullOrEmpty("Default audience should be defined");
        }

        [Fact]
        public void SecurityConstants_Database_ShouldHaveValidValues()
        {
            // Assert
            SecurityConstants.Database.SalesDbName.Should().NotBeNullOrEmpty("Sales DB name should be defined");
            SecurityConstants.Database.InventoryDbName.Should().NotBeNullOrEmpty("Inventory DB name should be defined");
            SecurityConstants.Database.SqlServerConnectionTemplate.Should().NotBeNullOrEmpty("Connection template should be defined");
            SecurityConstants.Database.SqlServerConnectionTemplate.Should().Contain("{0}", "Template should have server placeholder");
            SecurityConstants.Database.SqlServerConnectionTemplate.Should().Contain("{1}", "Template should have database placeholder");
            SecurityConstants.Database.SqlServerConnectionTemplate.Should().Contain("{2}", "Template should have username placeholder");
            SecurityConstants.Database.SqlServerConnectionTemplate.Should().Contain("{3}", "Template should have password placeholder");
        }

        [Fact]
        public void TestConstants_Performance_ShouldHaveReasonableValues()
        {
            // Assert
            TestConstants.Performance.MaxExecutionTimeMs.Should().BeGreaterThan(0, "Max execution time should be positive");
            TestConstants.Performance.MaxExecutionTimeMs.Should().BeLessThan(60000, "Max execution time should be reasonable (< 1 minute)");
            
            TestConstants.Performance.BulkInsertRecords.Should().BeGreaterThan(0, "Bulk insert records should be positive");
            TestConstants.Performance.BulkInsertRecords.Should().BeLessThan(1000, "Bulk insert records should be reasonable");
            
            TestConstants.Performance.MaxTestTimeoutMs.Should().BeGreaterThan(TestConstants.Performance.MaxExecutionTimeMs, 
                "Test timeout should be greater than max execution time");
            
            TestConstants.Performance.MaxDatabaseOperationMs.Should().BeGreaterThan(0, "Database operation time should be positive");
            TestConstants.Performance.MaxMessagePublishMs.Should().BeGreaterThan(0, "Message publish time should be positive");
            TestConstants.Performance.RetryDelayMs.Should().BeGreaterThan(0, "Retry delay should be positive");
            TestConstants.Performance.MaxRetries.Should().BeGreaterThan(0, "Max retries should be positive");
        }

        [Fact]
        public void TestConstants_TestData_ShouldHaveValidBusinessValues()
        {
            // Assert
            TestConstants.TestData.SamplePrice1.Should().BeGreaterThan(0, "Sample price 1 should be positive");
            TestConstants.TestData.SamplePrice2.Should().BeGreaterThan(0, "Sample price 2 should be positive");
            TestConstants.TestData.SampleQuantity.Should().BeGreaterThan(0, "Sample quantity should be positive");
            TestConstants.TestData.AlternativeQuantity.Should().BeGreaterThan(0, "Alternative quantity should be positive");
            
            TestConstants.TestData.SampleDiscountPercent.Should().BeInRange(0m, 1m, "Discount should be between 0 and 1");
            TestConstants.TestData.SampleTaxRate.Should().BeInRange(0m, 1m, "Tax rate should be between 0 and 1");
            
            TestConstants.TestData.MinPrice.Should().BeGreaterThan(0, "Min price should be positive");
            TestConstants.TestData.MaxPrice.Should().BeGreaterThan(TestConstants.TestData.MinPrice, "Max price should be greater than min price");
            TestConstants.TestData.MinQuantity.Should().BeGreaterThan(0, "Min quantity should be positive");
            TestConstants.TestData.MaxQuantity.Should().BeGreaterThan(TestConstants.TestData.MinQuantity, "Max quantity should be greater than min quantity");
        }

        [Fact]
        public void TestConstants_ExpectedResults_ShouldMatchCalculations()
        {
            // Arrange & Act
            var calculatedPrice1Total = TestConstants.TestData.SampleQuantity * TestConstants.TestData.SamplePrice1;
            var calculatedPrice2Total = TestConstants.TestData.AlternativeQuantity * TestConstants.TestData.SamplePrice2;
            var calculatedGrandTotal = calculatedPrice1Total + calculatedPrice2Total;

            // Assert - Expected results should match actual calculations
            TestConstants.ExpectedResults.ComplexPrice1Total.Should().Be(calculatedPrice1Total, 
                "Expected result should match calculated value for price 1");
            TestConstants.ExpectedResults.ComplexPrice2Total.Should().Be(calculatedPrice2Total, 
                "Expected result should match calculated value for price 2");
            TestConstants.ExpectedResults.ComplexGrandTotal.Should().Be(calculatedGrandTotal, 
                "Expected grand total should match calculated grand total");
        }
    }
}