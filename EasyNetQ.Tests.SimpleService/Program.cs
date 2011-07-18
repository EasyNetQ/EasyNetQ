using System;
using System.Threading;
using System.Threading.Tasks;

namespace EasyNetQ.Tests.SimpleService
{
    class Program
    {
        static void Main(string[] args)
        {
            var bus = RabbitHutch.CreateBus("localhost");
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
//            Console.WriteLine("Hit return to return response");
//            Console.ReadLine();

            Thread.Sleep(1000);

            Console.WriteLine("Returning response");
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
}
