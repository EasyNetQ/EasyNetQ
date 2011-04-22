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
            bus = RabbitHutch.CreateRabbitBus("localhost");
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

            Thread.Sleep(500);
        }

        [Test, Explicit("Needs a Rabbit instance on localhost to work")]
        public void Should_be_able_to_do_simple_request_response_lots()
        {
            const int howManyTimes = 10;
            for (int i = 0; i < howManyTimes; i++)
            {
                var request = new TestRequestMessage { Text = "Hello from the client! " + i.ToString() };
                Console.WriteLine("Making request");
                bus.Request<TestRequestMessage, TestResponseMessage>(request, response =>
                    Console.WriteLine("Got response: '{0}'", response.Text));
            }

            Thread.Sleep(500);
        }
    }
}

// ReSharper restore InconsistentNaming