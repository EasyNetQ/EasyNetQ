// ReSharper disable InconsistentNaming

using System;
using System.Threading;
using Xunit;

namespace EasyNetQ.Tests.Integration
{
    [Explicit("Requires a RabbitMQ broker on localhost")]
    public class SendReceiveIntegrationTests : IDisposable
    {
        private IBus bus;

        public SendReceiveIntegrationTests()
        {
            bus = RabbitHutch.CreateBus("host=localhost");
        }

        public void Dispose()
        {
            bus.Dispose();
        }

        [Fact]
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

        [Fact]
        public void Should_be_able_to_handle_a_long_running_consumer()
        {
            const string queue = "send_receive_test";
            var are = new AutoResetEvent(false);
            var waitTime = TimeSpan.FromMinutes(2);

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

            var signalReceived = are.WaitOne(waitTime);
            Assert.True(signalReceived, $"Expected reset event within {waitTime.TotalSeconds} seconds");
        }
    }
}

// ReSharper restore InconsistentNaming