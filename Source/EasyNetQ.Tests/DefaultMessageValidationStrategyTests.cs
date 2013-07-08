// ReSharper disable InconsistentNaming

using System;
using NUnit.Framework;
using Rhino.Mocks;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class DefaultMessageValidationStrategyTests
    {
        private IMessageValidationStrategy messageValidationStrategy;

        [SetUp]
        public void SetUp()
        {
            var logger = MockRepository.GenerateStub<IEasyNetQLogger>();

            messageValidationStrategy = 
                new DefaultMessageValidationStrategy(logger, TypeNameSerializer.Serialize);
        }

        [Test]
        public void Should_pass_when_expected_type_name_matches_message_type_name()
        {
            var body = new byte[0];
            var properties = new MessageProperties
                {
                    Type = "EasyNetQ_Tests_MyMessage:EasyNetQ_Tests"
                };

            var info = new MessageReceivedInfo();

            messageValidationStrategy.CheckMessageType<MyMessage>(body, properties, info);
        }

        [Test, ExpectedException(typeof(EasyNetQInvalidMessageTypeException))]
        public void Should_fail_when_expected_type_name_does_match_message_type_name()
        {
            var body = new byte[0];
            var properties = new MessageProperties
            {
                Type = "EasyNetQ_Tests_MyMessage:EasyNetQ_Tests_XXX"
            };

            var info = new MessageReceivedInfo();

            messageValidationStrategy.CheckMessageType<MyMessage>(body, properties, info);
        }

        [Test]
        [Explicit("Requires a RabbitMQ server on localhost")]
        public void Should_be_able_to_configure_EasyNetQ_to_allow_message_with_a_blank_type_field()
        {
            var are = new System.Threading.AutoResetEvent(false);
            var validation = new NullMessageValidator();
            var bus = RabbitHutch.CreateBus("host=localhost", r => 
                r.Register<IMessageValidationStrategy>(x => validation));

            bus.Subscribe<MyMessage>("null_validation_test", message =>
                {
                    Console.Out.WriteLine("Got message: {0}", message.Text);
                    are.Set();
                });

            // now use the basic client API to publish some JSON to the message type exchange ...
            var factory = new RabbitMQ.Client.ConnectionFactory
                {
                    HostName = "localhost"
                };
            using (var connection = factory.CreateConnection())
            using( var channel = connection.CreateModel())
            {
                const string exchange = "EasyNetQ_Tests_MyMessage:EasyNetQ_Tests";
                const string routingKey = "#";
                const string bodyString = "{ Text: \"Hello from Mr Raw :)\" }";
                var body = System.Text.Encoding.UTF8.GetBytes(bodyString);
                var properties = channel.CreateBasicProperties();
                channel.BasicPublish(exchange, routingKey, properties, body);
            }

            are.WaitOne(1000);
        }
    }

    public class NullMessageValidator : IMessageValidationStrategy
    {
        public void CheckMessageType<TMessage>(byte[] body, MessageProperties properties, MessageReceivedInfo messageReceivedInfo)
        {
            // Does nothing
        }
    }
}