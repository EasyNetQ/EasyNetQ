// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using EasyNetQ.Consumer;
using EasyNetQ.SystemMessages;
using FluentAssertions;
using NSubstitute;
using RabbitMQ.Client;
using Xunit;

namespace EasyNetQ.Tests.Integration
{
    public class DefaultConsumerErrorStrategyTests
    {
        private DefaultConsumerErrorStrategy consumerErrorStrategy;
        private IConnectionFactory connectionFactory;
        private ISerializer serializer;
        private IConventions conventions;

        public DefaultConsumerErrorStrategyTests()
        {
            var configuration = new ConnectionConfiguration
            {
                Hosts = new List<HostConfiguration>
                {
                    new HostConfiguration { Host = "localhost", Port = 5672 }
                },
                UserName = "guest",
                Password = "guest"
            };

            configuration.Validate();

            var typeNameSerializer = new DefaultTypeNameSerializer();
            var errorMessageSerializer = new DefaultErrorMessageSerializer();
            connectionFactory = new ConnectionFactoryWrapper(configuration, new RandomClusterHostSelectionStrategy<ConnectionFactoryInfo>());
            serializer = new JsonSerializer();
            conventions = new Conventions(typeNameSerializer);
            consumerErrorStrategy = new DefaultConsumerErrorStrategy(
                connectionFactory, 
                serializer, 
                conventions,
                typeNameSerializer,
                errorMessageSerializer
            );
        }

        /// <summary>
        /// NOTE: Make sure the error queue is empty before running this test.
        /// </summary>
        [Fact][Explicit("Requires a RabbitMQ instance on localhost")]
        public void Should_handle_an_exception_by_writing_to_the_error_queue()
        {
            const string originalMessage = "{ Text:\"Hello World\"}";
            var originalMessageBody = Encoding.UTF8.GetBytes(originalMessage);

            var exception = new Exception("I just threw!");

            var context = new ConsumerExecutionContext(
                (bytes, properties, arg3) => null,
                new MessageReceivedInfo("consumertag", 0, false, "orginalExchange", "originalRoutingKey", "queue"),
                new MessageProperties
                {
                    CorrelationId = "123",
                    AppId = "456"
                },
                originalMessageBody
            );

            consumerErrorStrategy.HandleConsumerError(context, exception);

            Thread.Sleep(100);

            // Now get the error message off the error queue and assert its properties
            using(var connection = connectionFactory.CreateConnection())
            using(var model = connection.CreateModel())
            {
                var getArgs = model.BasicGet(conventions.ErrorQueueNamingConvention(new MessageReceivedInfo()), true);
                if (getArgs == null)
                {
                    Assert.True(false, "Nothing on the error queue");
                }
                else
                {
                    var message = serializer.BytesToMessage<Error>(getArgs.Body);

                    message.RoutingKey.Should().Be(context.Info.RoutingKey);
                    message.Exchange.Should().Be(context.Info.Exchange);
                    message.Message.Should().Be(originalMessage);
                    message.Exception.Should().Be("System.Exception: I just threw!");
                    message.DateTime.Date.Should().Be(DateTime.UtcNow.Date);
                    message.BasicProperties.CorrelationId.Should().Be(context.Properties.CorrelationId);
                    message.BasicProperties.AppId.Should().Be(context.Properties.AppId);
                }
            }
        }

        [Fact]
        public void Should_not_reconnect_if_has_been_disposed()
        {
            const string originalMessage = "{ Text:\"Hello World\"}";
            var originalMessageBody = Encoding.UTF8.GetBytes(originalMessage);

            var exception = new Exception("I just threw!");

            var context = new ConsumerExecutionContext(
                (bytes, properties, arg3) => null,
                new MessageReceivedInfo("consumertag", 0, false, "orginalExchange", "originalRoutingKey", "queue"),
                new MessageProperties
                {
                    CorrelationId = "123",
                    AppId = "456"
                },
                originalMessageBody
            );

            connectionFactory = Substitute.For<IConnectionFactory>();

            consumerErrorStrategy = new DefaultConsumerErrorStrategy(
                connectionFactory,
                Substitute.For<ISerializer>(),
                Substitute.For<IConventions>(),
                Substitute.For<ITypeNameSerializer>(),
                Substitute.For<IErrorMessageSerializer>());

            consumerErrorStrategy.Dispose();

            var ackStrategy = consumerErrorStrategy.HandleConsumerError(context, exception);

            connectionFactory.DidNotReceive().CreateConnection();

            Assert.Equal(AckStrategies.NackWithRequeue, ackStrategy);
        }
    }
}

// ReSharper restore InconsistentNaming