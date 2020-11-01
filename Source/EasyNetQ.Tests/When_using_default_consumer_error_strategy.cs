// ReSharper disable InconsistentNaming;

using System;
using System.Text;
using System.Threading.Tasks;
using EasyNetQ.Consumer;
using EasyNetQ.Internals;
using EasyNetQ.Tests.Mocking;
using NSubstitute;
using RabbitMQ.Client;
using Xunit;

namespace EasyNetQ.Tests
{
    public class When_using_default_consumer_error_strategy
    {
        public When_using_default_consumer_error_strategy()
        {
            var customConventions = new Conventions(new DefaultTypeNameSerializer())
            {
                ErrorQueueNamingConvention = info => "CustomEasyNetQErrorQueueName",
                ErrorExchangeNamingConvention = info => "CustomErrorExchangePrefixName." + info.RoutingKey
            };

            mockBuilder = new MockBuilder();

            var connectionConfiguration = new ConnectionConfiguration();
            var connection = new PersistentConnection(connectionConfiguration, mockBuilder.ConnectionFactory, new EventBus());

            errorStrategy = new DefaultConsumerErrorStrategy(
                connection,
                new JsonSerializer(),
                customConventions,
                new DefaultTypeNameSerializer(),
                new DefaultErrorMessageSerializer(),
                connectionConfiguration
            );

            const string originalMessage = "";
            var originalMessageBody = Encoding.UTF8.GetBytes(originalMessage);

            var context = new ConsumerExecutionContext(
                (bytes, properties, info, cancellation) => Task.FromResult(AckStrategies.Ack),
                new MessageReceivedInfo("consumerTag", 0, false, "orginalExchange", "originalRoutingKey", "queue"),
                new MessageProperties
                {
                    CorrelationId = string.Empty,
                    AppId = string.Empty
                },
                originalMessageBody
            );

            try
            {
                errorAckStrategy = errorStrategy.HandleConsumerError(context, new Exception());
                cancelAckStrategy = errorStrategy.HandleConsumerCancelled(context);
            }
            catch (Exception)
            {
                // swallow
            }
        }

        private DefaultConsumerErrorStrategy errorStrategy;
        private MockBuilder mockBuilder;
        private AckStrategy errorAckStrategy;
        private AckStrategy cancelAckStrategy;

        [Fact]
        public void Should_Ack_canceled_message()
        {
            Assert.Same(AckStrategies.NackWithRequeue, cancelAckStrategy);
        }

        [Fact]
        public void Should_Ack_failed_message()
        {
            Assert.Same(AckStrategies.Ack, errorAckStrategy);
        }

        [Fact]
        public void Should_use_exchange_name_from_custom_names_provider()
        {
            mockBuilder.Channels[0].Received().ExchangeDeclare("CustomErrorExchangePrefixName.originalRoutingKey", "direct", true);
        }

        [Fact]
        public void Should_use_queue_name_from_custom_names_provider()
        {
            mockBuilder.Channels[0].Received().QueueDeclare("CustomEasyNetQErrorQueueName", true, false, false, null);
        }
    }
}
