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
            using(var publishBus = RabbitHutch.CreateBus("localhost"))
            using(var subcribeBus = RabbitHutch.CreateBus("localhost"))
            {
                Console.WriteLine("About to subscribe");
                subcribeBus.Subscribe<MyMessage>("restarted", message => 
                    Console.WriteLine("Subscriber got: {0}", message.Text));
                Console.WriteLine("Subscribed");

                var count = 0;
                while (true)
                {
                    Thread.Sleep(2000);
                    try
                    {
                        var message = new MyMessage {Text = "Hello" + (count++)};
                        publishBus.Publish(message);
                        Console.WriteLine("Published {0} OK", message.Text);
                    }
                    catch (EasyNetQException exception)
                    {
                        Console.WriteLine("No Publish '{0}'", exception.Message);
                    }
                }
            }
        }
    }
}
// ReSharper restore InconsistentNaming
