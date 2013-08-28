// ReSharper disable InconsistentNaming
using System;
using System.Threading;

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
                        using (var publishChannel = bus.OpenPublishChannel())
                        {
                            publishChannel.Publish(new MyMessage { Text = "Hello" });
                        }

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
            Thread.Sleep(2000);
            while (!bus.IsConnected) Thread.Sleep(100);
            using (var publishChannel = bus.OpenPublishChannel())
            {
                publishChannel.Publish(new T { Text = "Hello From " + name, Id = ++message.Id });
            }
        }

        /// <summary>
        /// Ping-pong between two EasyNetQ instances. Try stopping and starting RabbitMQ
        /// while this test is running.
        /// </summary>
        public void Server_goes_away_and_comes_back_during_subscription()
        {
            Console.WriteLine("Creating busses");
            using (var busA = RabbitHutch.CreateBus("host=localhost"))
            using (var busB = RabbitHutch.CreateBus("host=localhost"))
            {
                Console.WriteLine("About to subscribe");

                // ping pong between busA and busB
                busB.Subscribe<FromA>("restarted", message => Reply<FromB>(message, busB, "B"));
                busA.Subscribe<FromB>("restarted_1", message => Reply<FromA>(message, busA, "A"));

                Console.WriteLine("Subscribed");

                while(!busB.IsConnected) Thread.Sleep(100);
                using (var publishChannel = busA.OpenPublishChannel())
                {
                    publishChannel.Publish(new FromA { Text = "Initial From A ", Id = 0 });
                }

                while (true)
                {
                    Thread.Sleep(2000);
                }
            }
        }

        [Serializable]
        public abstract class ErrorTestBaseMessage
        {
            public string Text { get; set; }
            public int Id { get; set; }
        }

        [Serializable]
        public class FromA : ErrorTestBaseMessage {}
        [Serializable]
        public class FromB : ErrorTestBaseMessage { }
    }
}
// ReSharper restore InconsistentNaming
