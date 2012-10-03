// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using EasyNetQ.Loggers;
using EasyNetQ.SystemMessages;
using NUnit.Framework;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing.v0_9_1;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class DefaultConsumerErrorStrategyTests
    {
        private DefaultConsumerErrorStrategy consumerErrorStrategy;
        private IConnectionFactory connectionFactory;
        private ISerializer serializer;

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
            consumerErrorStrategy = new DefaultConsumerErrorStrategy(connectionFactory, serializer, new ConsoleLogger());
        }

        /// <summary>
        /// NOTE: Make sure the error queue is empty before running this test.
        /// </summary>
        [Test, Explicit("Requires a RabbitMQ instance on localhost")]
        public void Should_handle_an_exception_by_writing_to_the_error_queue()
        {
            const string originalMessage = "{ Text:\"Hello World\"}";
            var originalMessageBody = Encoding.UTF8.GetBytes(originalMessage);

            var deliverArgs = new BasicDeliverEventArgs
            {
                RoutingKey = "originalRoutingKey",
                Exchange = "orginalExchange",
                Body = originalMessageBody,
                BasicProperties = new BasicProperties
                {
                    CorrelationId = "123",
                    AppId = "456"
                }
            };
            var exception = new Exception("I just threw!");

            consumerErrorStrategy.HandleConsumerError(deliverArgs, exception);

            Thread.Sleep(100);

            // Now get the error message off the error queue and assert its properties
            using(var connection = connectionFactory.CreateConnection())
            using(var model = connection.CreateModel())
            {
                var getArgs = model.BasicGet(DefaultConsumerErrorStrategy.EasyNetQErrorQueue, true);
                if (getArgs == null)
                {
                    Assert.Fail("Nothing on the error queue");
                }
                else
                {
                    var message = serializer.BytesToMessage<Error>(getArgs.Body);

                    message.RoutingKey.ShouldEqual(deliverArgs.RoutingKey);
                    message.Exchange.ShouldEqual(deliverArgs.Exchange);
                    message.Message.ShouldEqual(originalMessage);
                    message.Exception.ShouldEqual("System.Exception: I just threw!");
                    message.DateTime.Date.ShouldEqual(DateTime.Now.Date);
                    message.BasicProperties.CorrelationId.ShouldEqual(deliverArgs.BasicProperties.CorrelationId);
                    message.BasicProperties.AppId.ShouldEqual(deliverArgs.BasicProperties.AppId);
                }
            }
        }
    }
}

// ReSharper restore InconsistentNaming