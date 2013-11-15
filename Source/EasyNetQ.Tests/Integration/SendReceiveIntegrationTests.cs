// ReSharper disable InconsistentNaming

using System;
using System.Threading;
using NUnit.Framework;

namespace EasyNetQ.Tests.Integration
{
    [TestFixture]
    [Explicit("Requires a RabbitMQ broker on localhost")]
    public class SendReceiveIntegrationTests
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

        [Test]
        public void Should_be_able_to_send_and_receive_messages()
        {
            const string queue = "send_receive_test";

            bus.Receive<MyMessage>(queue, message => Console.WriteLine("MyMessage: {0}", message.Text));
            var cancel = bus.Receive<MyOtherMessage>(queue, message => Console.WriteLine("MyOtherMessage: {0}", message.Text));

            bus.Send(queue, new MyMessage{ Text = "Hello Widgets!" });
            bus.Send(queue, new MyOtherMessage { Text = "Hello Gadgets!" });

            Thread.Sleep(500);

            cancel.Dispose();
        }
    }
}

// ReSharper restore InconsistentNaming