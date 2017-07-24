// ReSharper disable InconsistentNaming

using System;
using EasyNetQ.Tests.ProducerTests.Very.Long.Namespace.Certainly.Longer.Than.The255.Char.Length.That.RabbitMQ.Likes.That.Will.Certainly.Cause.An.AMQP.Exception.If.We.Dont.Do.Something.About.It.And.Stop.It.From.Happening;
using Xunit;

namespace EasyNetQ.Tests.Integration
{
    public class When_a_message_with_a_long_type_name_is_published : IDisposable
    {
        private IBus bus;

        public When_a_message_with_a_long_type_name_is_published()
        {
            bus = RabbitHutch.CreateBus("host=localhost");
        }

        public void Dispose()
        {
            bus.Dispose();
        }

        [Fact]
        [Explicit("Requires a broker on localhost to run")]
        public void Should_not_throw_when_over_long_message_is_published()
        {
            var message =
                new MessageWithVeryVEryVEryLongNameThatWillMostCertainlyBreakAmqpsSilly255CharacterNameLimitThatIsAlmostCertainToBeReachedWithGenericTypes();

            message.Text = "Some Text";

            bus.Publish(message);
        }
    }
}

// ReSharper restore InconsistentNaming