using System;
using System.Text;
using EasyNetQ.Consumer;
using NSubstitute;
using Xunit;

namespace EasyNetQ.Tests.ConsumeTests
{
    public class DefaultConsumerErrorStrategyTests
    {
        [Fact]
        public void Should_nack_with_requeue_if_has_been_disposed()
        {
            const string originalMessage = "{ Text:\"Hello World\"}";
            var originalMessageBody = Encoding.UTF8.GetBytes(originalMessage);

            var exception = new Exception("I just threw!");

            var context = new ConsumerExecutionContext(
                (bytes, properties, arg3, cancellationToken) => null,
                new MessageReceivedInfo("consumertag", 0, false, "orginalExchange", "originalRoutingKey", "queue"),
                new MessageProperties
                {
                    CorrelationId = "123",
                    AppId = "456"
                },
                originalMessageBody
            );

            var consumerErrorStrategy = new DefaultConsumerErrorStrategy(
                Substitute.For<IPersistentConnection>(),
                Substitute.For<ISerializer>(),
                Substitute.For<IConventions>(),
                Substitute.For<ITypeNameSerializer>(),
                Substitute.For<IErrorMessageSerializer>());

            consumerErrorStrategy.Dispose();

            var ackStrategy = consumerErrorStrategy.HandleConsumerError(context, exception);

            Assert.Equal(AckStrategies.NackWithRequeue, ackStrategy);
        }
    }
}
