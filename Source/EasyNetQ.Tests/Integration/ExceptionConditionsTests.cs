// ReSharper disable InconsistentNaming

using System;
using System.Threading;
using EasyNetQ.Management.Client;

namespace EasyNetQ.Tests
{
    /// <summary>
    /// Tests for various exception conditions
    /// </summary>
    public class ExceptionConditionsTests
    {
        public void Server_goes_away_after_connection_but_before_publish()
        {
            using(var bus = RabbitHutch.CreateBus("host=localhost"))
            {
                Console.WriteLine("Now kill the server");
                while(true)
                {
                    Thread.Sleep(2000);
                    try
                    {
                        bus.Publish(new MyMessage { Text = "Hello" });
                        Console.WriteLine("Published OK");
                    }
                    catch (EasyNetQException exception)
                    {
                        Console.WriteLine(exception);
                    }
                }
            }
        }

        private static void Reply<T>(ErrorTestBaseMessage message, IBus bus, string name)
            where T : ErrorTestBaseMessage, new()
        {
            Console.WriteLine("Subscriber {0} got: {1} {2}", name, message.Text, message.Id);
            Thread.Sleep(1000);
            while (!bus.IsConnected) Thread.Sleep(100);
            bus.Publish(new T { Text = "Hello From " + name, Id = ++message.Id });
        }

        /// <summary>
        /// Ping-pong between two EasyNetQ instances. Try stopping and starting RabbitMQ
        /// while this test is running.
        /// </summary>
        public void Server_goes_away_and_comes_back_during_subscription()
        {
            //Task.Factory.StartNew(OccasionallyKillConnections, TaskCreationOptions.LongRunning);

            Console.WriteLine("Creating busses");
            using (var busA = RabbitHutch.CreateBus("host=localhost"))
            using (var busB = RabbitHutch.CreateBus("host=localhost"))
            {
                Console.WriteLine("About to subscribe");

                // ping pong between busA and busB
                busB.Subscribe<FromA>("restarted", message => Reply<FromB>(message, busB, "B"));
                busA.Subscribe<FromB>("restarted_1", message => Reply<FromA>(message, busA, "A"));

                Console.WriteLine("Subscribed");

                busA.Publish(new FromA { Text = "Initial From A ", Id = 0 });

                while (true)
                {
                    Thread.Sleep(2000);
                }
            }
        }

        /// <summary>
        /// Simulates a consumer with many messages queued up in the internal consumer queue.
        /// </summary>
        public void Long_running_consumer_survives_broker_restart()
        {
            using (var publishBus = RabbitHutch.CreateBus("host=localhost;timeout=60"))
            using (var subscribeBus = RabbitHutch.CreateBus("host=localhost"))
            {
                subscribeBus.Subscribe<MyMessage>("longRunner", message =>
                    {
                        Console.Out.WriteLine("Got message: {0}", message.Text);
                        Thread.Sleep(2000);
                        Console.Out.WriteLine("Completed  : {0}", message.Text);
                    });

                var counter = 0;
                while (true)
                {
                    Thread.Sleep(1000);
                    publishBus.Publish(new MyMessage
                        {
                            Text = string.Format("Hello <{0}>", counter)
                        });
                    counter++;
                }
            }
        }

        public void Check()
        {
            CleanUp(new ManagementClient("http://localhost", "guest", "guest", 15672));
        }

        public void CleanUp(IManagementClient client)
        {
            foreach (var queue in client.GetQueues())
            {
                Console.Out.WriteLine("Deleting Queue: {0}", queue.Name);
                client.DeleteQueue(queue);
            }
            foreach (var exchange in client.GetExchanges())
            {
                if (!exchange.Name.StartsWith("amp."))
                {
                    Console.Out.WriteLine("Deleting Exchange: {0}", exchange.Name);
                    client.DeleteExchange(exchange);
                }
            }            
        }

        public void OccasionallyKillConnections()
        {
            OccasionallyKillConnections(new ManagementClient("http://localhost", "guest", "guest", 15672));
        }

        public void OccasionallyKillConnections(IManagementClient client)
        {
            while (true)
            {
                Thread.Sleep(3000);
                var connections = client.GetConnections();
                foreach (var connection in connections)
                {
                    Console.Out.WriteLine("\nKilling connection: {0}\n", connection.Name);
                    client.CloseConnection(connection);
                }
            }
        }

        public abstract class ErrorTestBaseMessage
        {
            public string Text { get; set; }
            public int Id { get; set; }
        }

        public class FromA : ErrorTestBaseMessage {}
        public class FromB : ErrorTestBaseMessage { }
    }
}
// ReSharper restore InconsistentNaming
