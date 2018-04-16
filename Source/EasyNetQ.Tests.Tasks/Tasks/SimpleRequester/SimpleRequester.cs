using System;
using System.Threading;
using System.Threading.Tasks;
using Net.CommandLine;

namespace EasyNetQ.Tests.SimpleRequester
{
    public class SimpleRequester : ICommandLineTask, IDisposable
    {
        private static readonly IBus bus = RabbitHutch.CreateBus("host=localhost");

        private static long count = 0;

        private static readonly ILatencyRecorder latencyRecorder = new LatencyRecorder();
        private const int publishIntervalMilliseconds = 10;

        public Task Run(CancellationToken cancellationToken)
        {

            timer = new Timer(OnTimer, null, publishIntervalMilliseconds, publishIntervalMilliseconds);

            Console.Out.WriteLine("Timer running, ctrl-C to end");

            Console.ReadLine();

            return Task.FromResult(0);
        }

        private static readonly object requestLock = new object();
        private static Timer timer;

        static void OnTimer(object state)
        {
            try
            {
                lock (requestLock)
                {
                    bus.RequestAsync<TestRequestMessage, TestResponseMessage>(
                        new TestRequestMessage
                        {
                            Id = count,
                            Text = string.Format("Hello from client number: {0}! ", count)
                        }).ContinueWith(t => ResponseHandler(t.Result));
                    latencyRecorder.RegisterRequest(count);
                    count++;
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Exception thrown by Publish: {0}", exception.Message);
            }
        }

        static void ResponseHandler(TestResponseMessage response)
        {
            Console.WriteLine("Response: {0}", response.Text);
            latencyRecorder.RegisterResponse(response.Id);
        }

        public void Dispose()
        {
            Console.Out.WriteLine("Shutting down");

            timer.Dispose();
            bus.Dispose();
            latencyRecorder.Dispose();

            Console.WriteLine("Shut down complete");
        }
    }
}
