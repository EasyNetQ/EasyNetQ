// ReSharper disable InconsistentNaming

using System;
using NUnit.Framework;
using RabbitMQ.Client.Events;
using Rhino.Mocks;

namespace EasyNetQ.Tests.ConsumeTests
{
    [TestFixture]
    public class When_an_error_occurs_in_the_message_handler : ConsumerTestBase
    {
        private Exception exception;

        protected override void AdditionalSetUp()
        {
            exception = new ApplicationException("I've had a bad day :(");
            StartConsumer((body, properties, info) =>
                {
                    throw exception;
                });
            DeliverMessage();
        }

        [Test]
        public void Should_write_an_error_message_to_the_log()
        {
            const string errorMessage = "Exception thrown by subscription calback.";

            MockBuilder.Logger.AssertWasCalled(x => x.ErrorWrite(
                Arg<string>.Matches(msg => msg.StartsWith(errorMessage)),
                Arg<object[]>.Is.Anything));
        }

        [Test]
        public void Should_invoke_the_error_strategy()
        {
            ConsumerErrorStrategy.AssertWasCalled(x => x.HandleConsumerError(
                Arg<BasicDeliverEventArgs>.Matches(args => args.ConsumerTag == ConsumerTag &&
                                                           args.DeliveryTag == DeliverTag &&
                                                           args.Exchange == "the_exchange" &&
                                                           args.Body == OriginalBody),
                Arg<Exception>.Matches(ex => ex.InnerException == exception)
                ));
        }

        [Test]
        public void Should_ack()
        {
            MockBuilder.Channels[0].AssertWasCalled(x => x.BasicAck(DeliverTag, false));
        }
    }
}

// ReSharper restore InconsistentNaming