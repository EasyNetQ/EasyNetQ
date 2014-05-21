using System;
using System.Threading;
using EasyNetQ.Loggers;

namespace EasyNetQ.Tests.SimpleRequester
{
    class Program
    {
        private static readonly IBus bus = RabbitHutch.CreateBus("host=localhost",
            x => x.Register(_ => new NoDebugLogger()));

        private static long count = 0;

        private static readonly ILatencyRecorder latencyRecorder = new LatencyRecorder();
        private const int publishIntervalMilliseconds = 10;

        static void Main(string[] args)
        {
            var timer = new Timer(OnTimer, null, publishIntervalMilliseconds, publishIntervalMilliseconds);

            Console.Out.WriteLine("Timer running, ctrl-C to end");

            Console.CancelKeyPress += (source, cancelKeyPressArgs) =>
            {
                Console.Out.WriteLine("Shutting down");

                timer.Dispose();
                bus.Dispose();
                latencyRecorder.Dispose();

                Console.WriteLine("Shut down complete");
            };

            Thread.Sleep(Timeout.Infinite);
        }

        private static readonly object requestLock = new object();

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
    }

    public class NoDebugLogger : IEasyNetQLogger
    {
        private readonly ConsoleLogger consoleLogger = new ConsoleLogger();

        public void DebugWrite(string format, params object[] args)
        {
            // do nothing
        }

        public void InfoWrite(string format, params object[] args)
        {
            // do nothing
        }

        public void ErrorWrite(string format, params object[] args)
        {
            consoleLogger.ErrorWrite(format, args);
        }

        public void ErrorWrite(Exception exception)
        {
            consoleLogger.ErrorWrite(exception);
        }
    }
}
