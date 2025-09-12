using FluentAssertions;
using SalesAPI.Tests.Professional.TestInfrastructure.Messaging;
using Xunit;
using Microsoft.Extensions.Logging;

namespace SalesAPI.Tests.Professional.Infrastructure.Tests.Messaging
{
    /// <summary>
    /// Infrastructure tests for event publishing using simple messaging infrastructure.
    /// Tests message delivery, serialization, and basic event processing without complex event creation.
    /// </summary>
    public class EventPublishingTests : IAsyncLifetime
    {
        private readonly TestMessagingFactory _messagingFactory;

        public EventPublishingTests()
        {
            var loggerFactory = new LoggerFactory();
            var logger = loggerFactory.CreateLogger<TestMessagingFactory>();
            _messagingFactory = new TestMessagingFactory("EventPublishingTests", logger);
        }

        public async Task InitializeAsync()
        {
            await Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            _messagingFactory.Dispose();
            await Task.CompletedTask;
        }

        [Fact]
        public async Task FakeBus_PublishMessage_ShouldStoreMessage()
        {
            // Arrange
            var bus = _messagingFactory.CreateFakeBus();
            var testMessage = new TestMessage
            {
                Id = Guid.NewGuid(),
                Content = "Test message content",
                Timestamp = DateTime.UtcNow
            };

            // Act
            await bus.Publish(testMessage);

            // Assert
            var publishedMessages = bus.GetPublishedMessages<TestMessage>();
            publishedMessages.Should().HaveCount(1);
            publishedMessages.First().Id.Should().Be(testMessage.Id);
            publishedMessages.First().Content.Should().Be("Test message content");
        }

        [Fact]
        public async Task FakeBus_PublishMultipleMessages_ShouldStoreAllMessages()
        {
            // Arrange
            var bus = _messagingFactory.CreateFakeBus();
            var messages = new[]
            {
                new TestMessage { Id = Guid.NewGuid(), Content = "Message 1" },
                new TestMessage { Id = Guid.NewGuid(), Content = "Message 2" },
                new TestMessage { Id = Guid.NewGuid(), Content = "Message 3" }
            };

            // Act
            foreach (var message in messages)
            {
                await bus.Publish(message);
            }

            // Assert
            var publishedMessages = bus.GetPublishedMessages<TestMessage>();
            publishedMessages.Should().HaveCount(3);
            publishedMessages.Select(m => m.Content).Should().BeEquivalentTo(new[] { "Message 1", "Message 2", "Message 3" });
        }

        [Fact]
        public async Task FakeBus_GetAllPublishedMessages_ShouldReturnMixedTypes()
        {
            // Arrange
            var bus = _messagingFactory.CreateFakeBus();
            var testMessage = new TestMessage { Id = Guid.NewGuid(), Content = "Test Message" };
            var anotherMessage = new AnotherTestMessage { Name = "Another Message", Value = 42 };

            // Act
            await bus.Publish(testMessage);
            await bus.Publish(anotherMessage);

            // Assert
            var allMessages = bus.GetAllPublishedMessages();
            allMessages.Should().HaveCount(2);
            allMessages.Should().ContainItemsAssignableTo<TestMessage>();
            allMessages.Should().ContainItemsAssignableTo<AnotherTestMessage>();
        }

        [Fact]
        public async Task MessageCollector_RecordAndRetrieve_ShouldWorkCorrectly()
        {
            // Arrange
            var collector = _messagingFactory.CreateMessageCollector();
            var testMessage = new TestMessage { Id = Guid.NewGuid(), Content = "Collected Message" };

            // Act
            collector.RecordMessage(testMessage);

            // Assert
            var messages = collector.GetMessages<TestMessage>();
            messages.Should().HaveCount(1);
            messages.First().Content.Should().Be("Collected Message");
            
            collector.GetMessageCount<TestMessage>().Should().Be(1);
            await Task.CompletedTask;
        }

        [Fact]
        public async Task MessageCollector_Clear_ShouldRemoveAllMessages()
        {
            // Arrange
            var collector = _messagingFactory.CreateMessageCollector();
            collector.RecordMessage(new TestMessage { Id = Guid.NewGuid(), Content = "Message 1" });
            collector.RecordMessage(new TestMessage { Id = Guid.NewGuid(), Content = "Message 2" });

            // Act
            collector.Clear();

            // Assert
            collector.GetMessageCount<TestMessage>().Should().Be(0);
            collector.GetMessages<TestMessage>().Should().BeEmpty();
            await Task.CompletedTask;
        }

        [Fact]
        public async Task FakeBus_ClearMessages_ShouldRemoveAllStoredMessages()
        {
            // Arrange
            var bus = _messagingFactory.CreateFakeBus();
            await bus.Publish(new TestMessage { Id = Guid.NewGuid(), Content = "Message 1" });
            await bus.Publish(new TestMessage { Id = Guid.NewGuid(), Content = "Message 2" });

            // Act
            bus.ClearMessages();

            // Assert
            bus.GetPublishedMessages<TestMessage>().Should().BeEmpty();
            bus.GetAllPublishedMessages().Should().BeEmpty();
        }

        [Fact]
        public async Task MessageCollector_GetSpecificMessage_ShouldFilterCorrectly()
        {
            // Arrange
            var collector = _messagingFactory.CreateMessageCollector();
            var message1 = new TestMessage { Id = Guid.NewGuid(), Content = "First" };
            var message2 = new AnotherTestMessage { Name = "Second", Value = 100 };
            var message3 = new TestMessage { Id = Guid.NewGuid(), Content = "Third" };

            collector.RecordMessage(message1);
            collector.RecordMessage(message2);
            collector.RecordMessage(message3);

            // Act & Assert
            var testMessages = collector.GetMessages<TestMessage>();
            testMessages.Should().HaveCount(2);
            testMessages.Select(m => m.Content).Should().BeEquivalentTo(new[] { "First", "Third" });

            var anotherMessages = collector.GetMessages<AnotherTestMessage>();
            anotherMessages.Should().HaveCount(1);
            anotherMessages.First().Name.Should().Be("Second");
            
            await Task.CompletedTask;
        }

        [Fact]
        public void MessagingFactory_CreateMultipleCollectors_ShouldCreateIndependentInstances()
        {
            // Arrange & Act
            var collector1 = _messagingFactory.CreateMessageCollector();
            var collector2 = _messagingFactory.CreateMessageCollector();

            collector1.RecordMessage(new TestMessage { Id = Guid.NewGuid(), Content = "Collector 1" });
            collector2.RecordMessage(new TestMessage { Id = Guid.NewGuid(), Content = "Collector 2" });

            // Assert
            collector1.GetMessageCount<TestMessage>().Should().Be(1);
            collector2.GetMessageCount<TestMessage>().Should().Be(1);
            
            collector1.GetMessages<TestMessage>().First().Content.Should().Be("Collector 1");
            collector2.GetMessages<TestMessage>().First().Content.Should().Be("Collector 2");
        }

        #region Test Message Classes

        /// <summary>
        /// Simple test message class for messaging infrastructure testing.
        /// </summary>
        public class TestMessage
        {
            public Guid Id { get; set; }
            public string Content { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        }

        /// <summary>
        /// Another test message class for type differentiation testing.
        /// </summary>
        public class AnotherTestMessage
        {
            public string Name { get; set; } = string.Empty;
            public int Value { get; set; }
        }

        #endregion
    }
}