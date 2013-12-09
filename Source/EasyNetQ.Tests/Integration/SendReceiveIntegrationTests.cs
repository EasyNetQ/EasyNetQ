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

            bus.Receive(queue, x => x
                .Add<MyMessage>(message => Console.WriteLine("MyMessage: {0}", message.Text))
                .Add<MyOtherMessage>(message => Console.WriteLine("MyOtherMessage: {0}", message.Text)));

            bus.Send(queue, new MyOtherMessage { Text = "Hello Gadgets!" });
            bus.Send(queue, new MyMessage { Text = "Hello Widgets!" });

            Thread.Sleep(500);
        }

        [Test]
        public void Should_be_able_to_handle_a_long_running_consumer()
        {
            const string queue = "send_receive_test";
            var are = new AutoResetEvent(false);

            bus.Receive(queue, x => x.Add<MyMessage>(message =>
                {
                    Console.Out.WriteLine("Got message {0}, now working");
                    Thread.Sleep(TimeSpan.FromMinutes(1));
                    Console.Out.WriteLine("Completed working, should be sending ACK");
                    are.Set();
                }));

//            bus.Receive<MyMessage>(queue, message =>
//                {
//                    Console.Out.WriteLine("Got message {0}, now working");
//                    Thread.Sleep(TimeSpan.FromMinutes(1));
//                    Console.Out.WriteLine("Completed working, should be sending ACK");
//                    are.Set();
//                });

            bus.Send(queue, new MyMessage { Text = "Hello Widgets!" });

            are.WaitOne();
        }
    }
}

// ReSharper restore InconsistentNaming