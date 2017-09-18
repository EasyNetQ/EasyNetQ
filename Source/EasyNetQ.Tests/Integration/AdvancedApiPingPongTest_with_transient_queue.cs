// ReSharper disable InconsistentNaming

using System;
using System.Threading;
using System.Text;
using System.Threading.Tasks;
using EasyNetQ.Topology;
using Xunit;

namespace EasyNetQ.Tests.Integration
{
    [Explicit]
    public class AdvancedApiPingPongTest_with_transient_queue : IDisposable
    {
        private readonly IBus[] buses = new IBus[2];
        private readonly IQueue[] queues = new IQueue[2];
        private readonly IExchange[] exchanges = new IExchange[2];

        private const string routingKey = "x";
        private const string messageText = "Hello World:0";

        private const long rallyLength = 1000;
        private long rallyCount;

        public AdvancedApiPingPongTest_with_transient_queue()
        {
            rallyCount = 0;
            for (int i = 0; i < 2; i++)
            {
                buses[i] = RabbitHutch.CreateBus("host=localhost");
                var name = string.Format("advanced_ping_pong_{0}", i);

                exchanges[i] = buses[i].Advanced.ExchangeDeclare(name, "direct");

                // declaring a queue without specifying the name creates a transient, server named queue.
                queues[i] = buses[i].Advanced.QueueDeclare(); 

                buses[i].Advanced.QueuePurge(queues[i]);
                buses[i].Advanced.Bind(exchanges[i], queues[i], routingKey);
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < 2; i++)
            {
                buses[i].Dispose();
            }
        }

        [Fact][Explicit]
        public void Ping_pong_with_advances_consumers()
        {
            Consume(0, 1);
            Consume(1, 0);

            // kick off
            var properties = new MessageProperties
                {
                    CorrelationId = "ping pong test"
                };
            var body = Encoding.UTF8.GetBytes(messageText);
            buses[1].Advanced.Publish(exchanges[1], routingKey, false, properties, body);

            while (Interlocked.Read(ref rallyCount) < rallyLength)
            {
                Thread.Sleep(100);
            }
        }

        void Consume(int from, int to)
        {
            buses[from].Advanced.Consume(queues[from], (body, properties, info) => Task.Factory.StartNew(() =>
                {
                    Console.Out.WriteLine("Consumer {0}: '{1}'", from, Encoding.UTF8.GetString(body));
                    Thread.Sleep(500);
                    var publishProperties = new MessageProperties
                        {
                            CorrelationId = properties.CorrelationId ?? "no id present"
                        };
                    var publishBody = GenerateNextMessage(body);
                    buses[from].Advanced.Publish(exchanges[to], routingKey, false, publishProperties, publishBody);

                    Interlocked.Increment(ref rallyCount);
                }));
        }

        public byte[] GenerateNextMessage(byte[] previousMessageBody)
        {
            var bodyText = Encoding.UTF8.GetString(previousMessageBody);
            var bodyElements = bodyText.Split(':');
            var textPart = bodyElements[0];
            var count = int.Parse(bodyElements[1]);
            var nextBodyText = textPart + ":" + (++count);
            return Encoding.UTF8.GetBytes(nextBodyText);
        }
    }
}

// ReSharper restore InconsistentNaming