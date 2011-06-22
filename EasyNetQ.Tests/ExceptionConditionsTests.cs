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
            using(var bus = RabbitHutch.CreateBus("localhost"))
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

        public void Server_goes_away_and_comes_back_during_subscription()
        {
            Console.WriteLine("Creating busses");
            using(var busA = RabbitHutch.CreateBus("localhost"))
            using(var busB = RabbitHutch.CreateBus("localhost"))
            {
                Console.WriteLine("About to subscribe");
                busB.Subscribe<FromA>("restarted", message => 
                    Console.WriteLine("Subscriber B got: {0}", message.Text));
                busA.Subscribe<FromB>("restarted", message => 
                    Console.WriteLine("Subscriber A got: {0}", message.Text));
                Console.WriteLine("Subscribed");

                var count = 0;
                while (true)
                {
                    Thread.Sleep(2000);
                    try
                    {
                        count++;
                        busA.Publish(new FromA { Text = "Hello" + (count) });
                        busB.Publish(new FromB { Text = "Hello" + (count) });
                        Console.WriteLine("Published {0} OK", count);
                    }
                    catch (EasyNetQException exception)
                    {
                        Console.WriteLine("No Publish '{0}'", exception.Message);
                    }
                }
            }
        }

        [Serializable]
        public abstract class ErrorTestBaseMessage
        {
            public string Text { get; set; }
        }

        [Serializable]
        public class FromA : ErrorTestBaseMessage {}
        [Serializable]
        public class FromB : ErrorTestBaseMessage { }
    }
}
// ReSharper restore InconsistentNaming
