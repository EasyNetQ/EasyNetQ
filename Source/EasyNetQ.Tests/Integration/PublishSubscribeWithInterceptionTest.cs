using System;
using System.Threading;
using EasyNetQ.Interception;
using EasyNetQ.Tests.Integration.Scheduling;
using Xunit;

namespace EasyNetQ.Tests.Integration
{
    public class PublishSubscribeWithInterceptionTest : IDisposable
    {
        public PublishSubscribeWithInterceptionTest()
        {
            bus = RabbitHutch.CreateBus("host=localhost", x => x.EnableInterception(r => r.EnableGZipCompression().EnableTripleDESEncryption(Convert.FromBase64String("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"), Convert.FromBase64String("aaaaaaaaaaa="))));
        }

        public void Dispose()
        {
            if (bus != null) bus.Dispose();
        }

        private IBus bus;


        [Fact]
        [Explicit("Needs an instance of RabbitMQ on localhost to work")]
        public void Should_be_able_to_get_a_message()
        {
            var autoResetEvent = new AutoResetEvent(false);

            bus.Subscribe<PartyInvitation>("Should_be_able_to_get_a_message", message =>
                {
                    Console.WriteLine("Got message: {0}", message.Text);
                    autoResetEvent.Set();
                });

            var invitation = new PartyInvitation
                {
                    Text = "Please come to my party",
                    Date = new DateTime(2011, 5, 24)
                };

            bus.Publish(invitation);

            if (! autoResetEvent.WaitOne(100000))
                Assert.True(false);
        }
    }
}