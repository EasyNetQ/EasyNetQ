// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using EasyNetQ.Loggers;
using EasyNetQ.SystemMessages;
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing.v0_9_1;
using Rhino.Mocks;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class DefaultConsumerErrorStrategyTests
    {
        private DefaultConsumerErrorStrategy consumerErrorStrategy;
        private IConnectionFactory connectionFactory;
        private ISerializer serializer;
        private IConventions conventions;

        [SetUp]
        public void SetUp()
        {
            connectionFactory = new ConnectionFactoryWrapper(new ConnectionConfiguration
            {
                Hosts = new List<IHostConfiguration>
                {
                    new HostConfiguration { Host = "localhost", Port = 5672 }
                },
                UserName = "guest",
                Password = "guest"
            }, new DefaultClusterHostSelectionStrategy<ConnectionFactoryInfo>());
            serializer = new JsonSerializer();
            conventions = new Conventions();
            consumerErrorStrategy = new DefaultConsumerErrorStrategy(connectionFactory, serializer, new ConsoleLogger(), conventions);
         
        }

        /// <summary>
        /// NOTE: Make sure the error queue is empty before running this test.
        /// </summary>
        [Test, Explicit("Requires a RabbitMQ instance on localhost")]
        public void Should_handle_an_exception_by_writing_to_the_error_queue()
        {
            const string originalMessage = "{ Text:\"Hello World\"}";
            var originalMessageBody = Encoding.UTF8.GetBytes(originalMessage);

            var exception = new Exception("I just threw!");

            var context = new ConsumerExecutionContext(
                (bytes, properties, arg3) => null,
                new MessageReceivedInfo
                {
                    RoutingKey = "originalRoutingKey",
                    Exchange = "orginalExchange",
                },
                new MessageProperties
                {
                    CorrelationId = "123",
                    AppId = "456"
                },
                originalMessageBody,
                MockRepository.GenerateStub<IBasicConsumer>()
                );

            consumerErrorStrategy.HandleConsumerError(context, exception);

            Thread.Sleep(100);

            // Now get the error message off the error queue and assert its properties
            using(var connection = connectionFactory.CreateConnection())
            using(var model = connection.CreateModel())
            {
                var getArgs = model.BasicGet(conventions.ErrorQueueNamingConvention(), true);
                if (getArgs == null)
                {
                    Assert.Fail("Nothing on the error queue");
                }
                else
                {
                    var message = serializer.BytesToMessage<Error>(getArgs.Body);

                    message.RoutingKey.ShouldEqual(context.Info.RoutingKey);
                    message.Exchange.ShouldEqual(context.Info.Exchange);
                    message.Message.ShouldEqual(originalMessage);
                    message.Exception.ShouldEqual("System.Exception: I just threw!");
                    message.DateTime.Date.ShouldEqual(DateTime.Now.Date);
                    message.BasicProperties.CorrelationId.ShouldEqual(context.Properties.CorrelationId);
                    message.BasicProperties.AppId.ShouldEqual(context.Properties.AppId);
                }
            }
        }

        [Test]
        public void Should_ack_after_exception_is_handled()
        {
            consumerErrorStrategy.PostExceptionAckStrategy()
                .ShouldEqual(PostExceptionAckStrategy.ShouldAck);
        }
    }
}

// ReSharper restore InconsistentNaming