using System;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Loggers;

namespace EasyNetQ.Tests.SimpleService
{
    class Program
    {
        static void Main(string[] args)
        {
            var bus = RabbitHutch.CreateBus("host=localhost",
                x => x.Register<IEasyNetQLogger>(_ => new NoDebugLogger()));
            bus.Respond<TestRequestMessage, TestResponseMessage>(HandleRequest);
            bus.RespondAsync<TestAsyncRequestMessage, TestAsyncResponseMessage>(HandleAsyncRequest);

            Console.WriteLine("Waiting to service requests");
            Console.WriteLine("Ctrl-C to exit");

            Console.CancelKeyPress += (source, cancelKeyPressArgs) =>
            {
                bus.Dispose();
                Console.WriteLine("Shut down complete");
            };

            Thread.Sleep(Timeout.Infinite);
            
        }

        private static Task<TestAsyncResponseMessage> HandleAsyncRequest(TestAsyncRequestMessage request)
        {
            Console.Out.WriteLine("Got aysnc request '{0}'", request.Text);

            return RunDelayed(1000, () =>
            {
                Console.Out.WriteLine("Sending response");
                return new TestAsyncResponseMessage {Text = request.Text + " ... completed."};
            });
        }

        public static TestResponseMessage HandleRequest(TestRequestMessage request)
        {
            // Console.WriteLine("Handling request: {0}", request.Text);
            if (request.CausesServerToTakeALongTimeToRespond)
            {
                Console.Out.WriteLine("Taking a long time to respond...");
                Thread.Sleep(5000);
                Console.Out.WriteLine("... responding");
            }
            if (request.CausesExceptionInServer)
            {
                if (request.ExceptionInServerMessage != null)
                    throw new SomeRandomException(request.ExceptionInServerMessage);
                throw new SomeRandomException("Something terrible has just happened!");
            }
            return new TestResponseMessage{ Id = request.Id, Text = request.Text + " all done!" };
        }

        private static Task<T> RunDelayed<T>(int millisecondsDelay, Func<T> func)
        {
            if (func == null)
            {
                throw new ArgumentNullException("func");
            }
            if (millisecondsDelay < 0)
            {
                throw new ArgumentOutOfRangeException("millisecondsDelay");
            }

            var taskCompletionSource = new TaskCompletionSource<T>();

            var timer = new Timer(self =>
            {
                ((Timer)self).Dispose();
                try
                {
                    var result = func();
                    taskCompletionSource.SetResult(result);
                }
                catch (Exception exception)
                {
                    taskCompletionSource.SetException(exception);
                }
            });
            timer.Change(millisecondsDelay, millisecondsDelay);

            return taskCompletionSource.Task;
        }
    }

    [Serializable]
    public class SomeRandomException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public SomeRandomException() {}
        public SomeRandomException(string message) : base(message) {}
        public SomeRandomException(string message, Exception inner) : base(message, inner) {}

        protected SomeRandomException(
            SerializationInfo info,
            StreamingContext context) : base(info, context) {}
    }

    public class NoDebugLogger : IEasyNetQLogger
    {
        private readonly ConsoleLogger logger = new ConsoleLogger();

        public void DebugWrite(string format, params object[] args)
        {
            
        }

        public void InfoWrite(string format, params object[] args)
        {
            
        }

        public void ErrorWrite(string format, params object[] args)
        {
            logger.ErrorWrite(format, args);
        }

        public void ErrorWrite(Exception exception)
        {
            logger.ErrorWrite(exception);
        }
    }
}
