using System;
using System.Threading;

namespace EasyNetQ.Tests.SimpleRequester
{
    class Program
    {
        static void Main(string[] args)
        {
            var bus = RabbitHutch.CreateBus("host=localhost");
            var count = 0;

            var timer = new Timer(x =>
            {
                try
                {
                    using (var publishChannel = bus.OpenPublishChannel())
                    {
                        publishChannel.Request<TestRequestMessage, TestResponseMessage>(
                            new TestRequestMessage { Text = string.Format("Hello from client number: {0}! ", count++) },
                            ResponseHandler);
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine("Exception thrown by Publish: {0}", exception.Message);
                }
            }, null, 1000, 1000);

            Console.Out.WriteLine("Timer running, ctrl-C to end");

            Console.CancelKeyPress += (source, cancelKeyPressArgs) =>
            {
                Console.Out.WriteLine("Shutting down");

                timer.Dispose();
                bus.Dispose();
                Console.WriteLine("Shut down complete");
            };

            var running = true;
            while (true)
            {
                Console.ReadKey();
                if (running)
                {
                    timer.Change(Timeout.Infinite, Timeout.Infinite);
                }
                else
                {
                    timer.Change(1000, 1000);
                }
                running = !running;
            }
        }

        static void ResponseHandler(TestResponseMessage response)
        {
            Console.WriteLine("Got Response: '{0}'", response.Text);
        }
    }
}
