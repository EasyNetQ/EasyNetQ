using System;

namespace EasyNetQ.Tests.SimpleSaga
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Simple Saga starting");

            var bus = RabbitHutch.CreateRabbitBus("localhost");

            bus.Subscribe<StartMessage>("simpleSaga", startMessage =>
            {
                Console.WriteLine("StartMessage: {0}", startMessage.Text);
                var firstProcessedMessage = startMessage.Text + " - initial process ";
                var request = new TestRequestMessage {Text = firstProcessedMessage};
                bus.Request<TestRequestMessage, TestResponseMessage>(request, response =>
                {
                    Console.WriteLine("TestResponseMessage: {0}", response.Text);
                    var secondProcessedMessage = response.Text + " - final process ";
                    var endMessage = new EndMessage {Text = secondProcessedMessage};
                    bus.Publish(endMessage);
                });
            });

            Console.WriteLine("Hit return to quit");
            Console.ReadLine();
        }
    }
}
