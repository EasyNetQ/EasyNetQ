using System;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace EasyNetQ.Tests.SimpleService
{
    class Program
    {
        static void Main(string[] args)
        {
            var bus = RabbitHutch.CreateBus("host=localhost");
            bus.Respond<TestRequestMessage, TestResponseMessage>(HandleRequest);
            bus.RespondAsync<TestAsyncRequestMessage, TestAsyncResponseMessage>(HandleAsyncRequest);

            Console.WriteLine("Waiting to service requests");
            Console.WriteLine("Ctrl-C to exit");

            Console.CancelKeyPress += (source, cancelKeyPressArgs) =>
            {
                bus.Dispose();
                Console.WriteLine("Shut down complete");
            };

            while(true) Thread.Sleep(1000);
            
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
            Console.WriteLine("Handling request: {0}", request.Text);
            if (request.CausesExceptionInServer)
            {
                throw new SomeRandomException("Something terrible has just happened!");
            }
            return new TestResponseMessage{ Text = request.Text + " all done!" };
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
}
