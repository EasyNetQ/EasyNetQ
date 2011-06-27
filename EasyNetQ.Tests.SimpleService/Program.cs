using System;
using System.Threading;

namespace EasyNetQ.Tests.SimpleService
{
    class Program
    {
        static void Main(string[] args)
        {
            var messageQueue = RabbitHutch.CreateBus("localhost");
            messageQueue.Respond<TestRequestMessage, TestResponseMessage>(HandleRequest);

            Console.WriteLine("Waiting to service requests");
            Console.WriteLine("Ctrl-C to exit");
            
            while(true) Thread.Sleep(1000);
            messageQueue.Dispose();
        }

        public static TestResponseMessage HandleRequest(TestRequestMessage request)
        {
            Console.WriteLine("Handling request: {0}", request.Text);
            Console.WriteLine("Hit return to return response");
            Console.ReadLine();

            Console.WriteLine("Returning response");
            return new TestResponseMessage{ Text = request.Text + " all done!" };
        }
    }
}
