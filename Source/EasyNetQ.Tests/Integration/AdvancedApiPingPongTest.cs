// ReSharper disable InconsistentNaming

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Text;
using System.Threading.Tasks;
using EasyNetQ.Management.Client;
using EasyNetQ.Topology;
using Xunit;

namespace EasyNetQ.Tests.Integration
{
    [Explicit]
    public class AdvancedApiPingPongTest : IDisposable
    {
        private readonly IBus[] buses = new IBus[2];
        private readonly IQueue[] queues = new IQueue[2];
        private readonly IExchange[] exchanges = new IExchange[2];

        private const string routingKey = "x";
        private const string messageText = "Hello World:0";

        private const long rallyLength = 10000;
        private long rallyCount;

        public AdvancedApiPingPongTest()
        {
            rallyCount = 0;
            for (int i = 0; i < 2; i++)
            {
                buses[i] = RabbitHutch.CreateBus("host=localhost");
                var name = string.Format("advanced_ping_pong_{0}", i);

                exchanges[i] = buses[i].Advanced.ExchangeDeclare(name, "direct");
                queues[i] = buses[i].Advanced.QueueDeclare(name);
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

        [Fact][Explicit("Requires a RabbitMQ instance on localhost.")]
        public void Ping_pong_with_advanced_consumers()
        {
            IntermittentDisconnection();

            Consume(0, 1);
            Consume(1, 0);

            // kick off
            var properties = new MessageProperties
                {
                    CorrelationId = "0"
                };
            var body = Encoding.UTF8.GetBytes(messageText);
            buses[1].Advanced.Publish(exchanges[1], routingKey, false, properties, body);

            while (Interlocked.Read(ref rallyCount) < rallyLength)
            {
                Thread.Sleep(100);
            }
        }

        public void Consume(int from, int to)
        {
            buses[from].Advanced.Consume(queues[from], (body, properties, info) => Task.Factory.StartNew(() =>
                {
                    Console.Out.WriteLine("Consumer {0}: '{1}'", from, Encoding.UTF8.GetString(body));
                    Thread.Sleep(1000);
                    var nextMessage = GenerateNextMessage(body);

                    if (IsDuplicateMessage(nextMessage.CorrelationId))
                    {
                        Console.Out.WriteLine("\n>>>>>>>> DUPLICATE MESSAGE DETECTED: {0} <<<<<<<<<<\n", 
                            nextMessage.CorrelationId);
                        return;
                    }

                    var publishProperties = new MessageProperties
                        {
                            CorrelationId = nextMessage.CorrelationId.ToString()
                        };

                    var published = false;
                    while (!published)
                    {
                        try
                        {
                            buses[from].Advanced.Publish(exchanges[to], routingKey, false, publishProperties, nextMessage.Body);
                            published = true;
                        }
                        catch (Exception)
                        {
                            Console.Out.WriteLine("\n>>>>>>>> PUBLISH FAILED, RETRYING <<<<<<<<<<\n");
                            Thread.Sleep(100);
                            // retry if connection fails
                        }
                    }
                    Interlocked.Increment(ref rallyCount);
                }));
        }

        private class Message
        {
            public Message(byte[] body, int correlationId)
            {
                Body = body;
                CorrelationId = correlationId;
            }

            public byte[] Body { get; private set; }
            public int CorrelationId { get; private set; }
        }

        private Message GenerateNextMessage(byte[] previousMessageBody)
        {
            var bodyText = Encoding.UTF8.GetString(previousMessageBody);
            var bodyElements = bodyText.Split(':');
            var textPart = bodyElements[0];
            var count = int.Parse(bodyElements[1]);
            var nextBodyText = textPart + ":" + (++count);
            return new Message(Encoding.UTF8.GetBytes(nextBodyText), count);
        }

        private readonly ConcurrentDictionary<int, object> correlationIds = new ConcurrentDictionary<int, object>();

        private bool IsDuplicateMessage(int correlationId)
        {
            return !correlationIds.TryAdd(correlationId, null);
        }

        private void IntermittentDisconnection()
        {
            const int secondsBetweenDisconnection = 2;

            Task.Factory.StartNew(() =>
                {
                    var client = new ManagementClient("http://localhost", "guest", "guest", 15672);
                    while (true)
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(secondsBetweenDisconnection)); 
                        var connections = client.GetConnections();
                        foreach (var connection in connections)
                        {
                            client.CloseConnection(connection);
                        }
                    }
                }, TaskCreationOptions.LongRunning);
        }
    }
}

// ReSharper restore InconsistentNaming