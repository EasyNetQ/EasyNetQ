// ReSharper disable InconsistentNaming

using EasyNetQ.Tests.Integration;
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

            var info = new MessageReceivedInfo("consumerTag", 0, false, "exchange", "routingKey");

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

            var info = new MessageReceivedInfo("consumerTag", 0, false, "exchange", "routingKey");

            messageValidationStrategy.CheckMessageType<MyMessage>(body, properties, info);
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