using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Tests.Tasks;
using EasyNetQ.Tests.Tasks.Tasks;
using Net.CommandLine;
using Serilog;

namespace EasyNetQ.Tests.Performance.Producer
{
    public class TestPerformanceParameters
    {
        public TestPerformanceParameters()
        {
            MessageSize = 1000;
        }
        public int PublishInterval { get; set; }
        public int MessageSize { get; set; }
    }

    public class TestPerformanceProducer : ICommandLineTask<TestPerformanceParameters>, IDisposable
    {
        private IBus bus;
        private Timer messageRateTimer;
        private bool cancelled;
        private Thread publishThread;

        public Task Run(TestPerformanceParameters args, CancellationToken cancellationToken)
        {
            var publishInterval = args.PublishInterval;
            var messageSize = args.MessageSize;

            Console.Out.WriteLine("publishInterval = {0}", publishInterval);
            Console.Out.WriteLine("messageSize = {0}", messageSize);

            bus = RabbitHutch.CreateBus("host=localhost;publisherConfirms=true;timeout=10;requestedHeartbeat=5;product=producer");

            var messageCount = 0;
            var faultMessageCount = 0;
            messageRateTimer = new Timer(state =>
            {
                Console.Out.WriteLine("messages per second = {0}", messageCount);
                Console.Out.WriteLine("fault messages per second = {0}", faultMessageCount);
                Interlocked.Exchange(ref messageCount, 0);
                Interlocked.Exchange(ref faultMessageCount, 0);
            }, null, 1000, 1000);

            cancelled = false;
            publishThread = new Thread(state =>
            {
                while (!cancelled)
                {
                    var text = new string('#', messageSize);
                    var message = new TestPerformanceMessage {Text = text};

                    try
                    {
                        bus.PublishAsync(message).ContinueWith(task =>
                        {
                            if (task.IsCompleted)
                            {
                                Interlocked.Increment(ref messageCount);
                            }
                            if (task.IsFaulted)
                            {
                                Interlocked.Increment(ref faultMessageCount);
                            }
                        });
                    }
                    catch (EasyNetQException easyNetQException)
                    {
                        Console.Out.WriteLine(easyNetQException.Message);
                        Thread.Sleep(1000);
                    }
                }
            });
            publishThread.Start();

            Console.Out.WriteLine("Timer running, enter to end");

            Console.ReadLine();

            return Task.FromResult(0);
        }

        public void Dispose()
        {
            Console.Out.WriteLine("Shutting down");

            cancelled = true;
            publishThread.Join();
            messageRateTimer.Dispose();
            bus.Dispose();
            Console.WriteLine("Shut down complete");
        }
    }
}
