using System;

namespace EasyNetQ.Tests.SimpleService
{
    class Program
    {
        static void Main(string[] args)
        {
            var messageQueue = RabbitHutch.CreateBus("localhost");
            messageQueue.Respond<TestRequestMessage, TestResponseMessage>(HandleRequest);

            Console.WriteLine("Waiting to service requests");
            Console.WriteLine("Hit return to exit");
            Console.ReadLine();

            messageQueue.Dispose();
        }

        public static TestResponseMessage HandleRequest(TestRequestMessage request)
        {
            Console.WriteLine("Handling request: {0}", request.Text);
            return new TestResponseMessage{ Text = request.Text + " all done!" };
        }
    }
}
