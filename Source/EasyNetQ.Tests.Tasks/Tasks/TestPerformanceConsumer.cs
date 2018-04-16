using System;
using System.Threading;
using System.Threading.Tasks;
using Net.CommandLine;

namespace EasyNetQ.Tests.Performance.Consumer
{
    public class TestPerformanceConsumer : ICommandLineTask, IDisposable
    {
        private IBus bus;
        private Timer timer;

        public Task Run(CancellationToken cancellationToken)
        {
            bus = RabbitHutch.CreateBus("host=localhost;product=consumer");

            int messageCount = 0;
            timer = new Timer(state =>
            {
                Console.Out.WriteLine("messages per second = {0}", messageCount);
                Interlocked.Exchange(ref messageCount, 0);
            }, null, 1000, 1000);

            bus.Subscribe<TestPerformanceMessage>("consumer", message => Interlocked.Increment(ref messageCount));

            Console.WriteLine("press enter to exit");
            Console.ReadLine();

            return Task.FromResult(0);
        }

        public void Dispose()
        {
            Console.Out.WriteLine("Shutting down");
            bus.Dispose();
            timer.Dispose();
            Console.WriteLine("Shut down complete");
        }
    }
}
