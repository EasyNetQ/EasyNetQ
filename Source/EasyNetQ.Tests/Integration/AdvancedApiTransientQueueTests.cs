// ReSharper disable InconsistentNaming

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Topology;
using Xunit;

namespace EasyNetQ.Tests.Integration
{
    [Explicit]
    public class AdvancedApiTransientQueueTests : IDisposable
    {
        private IBus bus;

        public AdvancedApiTransientQueueTests()
        {
            bus = RabbitHutch.CreateBus("host=localhost");
        }

        public void Dispose()
        {
            bus.Dispose();
        }

        [Fact][Explicit]
        public void Does_transient_queue_cause_channel_to_close_after_consuming_one_message()
        {
            var queue = bus.Advanced.QueueDeclare();

            Console.WriteLine($"declared queue: {queue.Name}");

            bus.Advanced.Consume(queue, (body, properties, info) => Task.Factory.StartNew(() =>
                {
                    var message = Encoding.UTF8.GetString(body);
                    Console.Out.WriteLine("message = '{0}'", message);
                }));

            Thread.Sleep(5000);

            var body1 = Encoding.UTF8.GetBytes("Publish 1");
            bus.Advanced.Publish(Exchange.GetDefault(), queue.Name, false, new MessageProperties(), body1);

            Thread.Sleep(5000);

            var body2 = Encoding.UTF8.GetBytes("Publish 2");
            bus.Advanced.Publish(Exchange.GetDefault(), queue.Name, false, new MessageProperties(), body2);

            Thread.Sleep(1000);
        }
    }
}

// ReSharper restore InconsistentNaming