// ReSharper disable InconsistentNaming
using System;
using System.Threading;
using NUnit.Framework;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class RequestResponseTests
    {
        private IBus bus;

        [SetUp]
        public void SetUp()
        {
            bus = RabbitHutch.CreateBus("localhost");
            while(!bus.IsConnected) Thread.Sleep(10);
        }

        [TearDown]
        public void TearDown()
        {
            bus.Dispose();
        }

        // First start the EasyNetQ.Tests.SimpleService console app.
        // Run this test. You should see the SimpleService report that it's
        // responding and the response should appear here.
        [Test, Explicit("Needs a Rabbit instance on localhost to work")]
        public void Should_be_able_to_do_simple_request_response()
        {
            var request = new TestRequestMessage {Text = "Hello from the client! "};

            Console.WriteLine("Making request");
            bus.Request<TestRequestMessage, TestResponseMessage>(request, response => 
                Console.WriteLine("Got response: '{0}'", response.Text));

            while(true) Thread.Sleep(100);
        }

        // First start the EasyNetQ.Tests.SimpleService console app.
        // Run this test. You should see the SimpleService report that it's
        // responding and the response should appear here.
        [Test, Explicit("Needs a Rabbit instance on localhost to work")]
        public void Should_be_able_to_do_simple_request_response_lots()
        {
            for (int i = 0; i < 1000; i++)
            {
                var request = new TestRequestMessage { Text = "Hello from the client! " + i.ToString() };
                bus.Request<TestRequestMessage, TestResponseMessage>(request, response =>
                    Console.WriteLine("Got response: '{0}'", response.Text));
            }

            Thread.Sleep(1000);
        }

        [Test, Explicit("Needs a Rabbit instance on localhost to work")]
        public void Should_be_able_to_make_a_request_that_runs_async_on_the_server()
        {
            var request = new TestAsyncRequestMessage {Text = "Hello async from the client!"};

            Console.Out.WriteLine("Making request");
            bus.Request<TestAsyncRequestMessage, TestAsyncResponseMessage>(request, 
                response => Console.Out.WriteLine("response = {0}", response.Text));

            Thread.Sleep(2000);
        }
    }
}

// ReSharper restore InconsistentNaming