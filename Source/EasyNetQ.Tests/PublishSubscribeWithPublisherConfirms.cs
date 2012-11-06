// ReSharper disable InconsistentNaming

using System.Threading;
using NUnit.Framework;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class PublishSubscribeWithPublisherConfirms
    {
        private IBus bus;

        [SetUp]
        public void SetUp()
        {
            bus = RabbitHutch.CreateBus("host=localhost");
        }

        [TearDown]
        public void TearDown()
        {
            bus.Dispose();
        }

        [Test, Explicit("needs a RabbitMQ broker on localhost to run")]
        public void Should_acknowledge_publication_with_publisher_confirms()
        {
            var wait = new AutoResetEvent(false);
            var confirmed = false;

            using (var channel = bus.OpenPublishChannel(x => x.WithPublisherConfirms()))
            {
                var message = new MyMessage {Text = "Hello Confirm!"};

                channel.Publish(message, x => 
                    x.OnSuccess(() =>
                    {
                        confirmed = true;
                        wait.Set();
                    })
                    .OnFailure(() => wait.Set()));

                wait.WaitOne(2000);
            }

            confirmed.ShouldBeTrue();
        }
    }
}

// ReSharper restore InconsistentNaming