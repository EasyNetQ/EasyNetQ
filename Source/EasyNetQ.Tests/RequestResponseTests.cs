// ReSharper disable InconsistentNaming
using System;
using System.Runtime.Serialization;
using System.Threading;
using NUnit.Framework;
using RabbitMQ.Client;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class RequestResponseTests
    {
        private IBus bus;

        [SetUp]
        public void SetUp()
        {
            bus = RabbitHutch.CreateBus("host=localhost");
            while(!bus.IsConnected) Thread.Sleep(10);
        }

        [TearDown]
        public void TearDown()
        {
            bus.Dispose();
        }

        // First start the EasyNetQ.Tests.SimpleService console app.
        [Test, Explicit("Needs a Rabbit instance on localhost to work")]
        public void Large_number_of_request_calls_should_not_create_a_large_number_of_open_channels()
        {
            var pool = new Semaphore(0, 500);
            for (int i = 0; i < 500; i++)
            {
                using (var publishChannel = bus.OpenPublishChannel())
                {
                    publishChannel.Request<TestRequestMessage, TestResponseMessage>(
                        new TestRequestMessage {Text = string.Format("Hello from client number: {0}! ", i)},
                        response =>
                            {
                                pool.Release();
                                Console.WriteLine(response.Text);
                            }
                        );
                }
            }
            var successfullyWaited = pool.WaitOne(TimeSpan.FromSeconds(40));
            Assert.True(successfullyWaited);
            Assert.AreEqual(1, ((RabbitAdvancedBus)bus.Advanced).OpenChannelCount);
        }

        // First start the EasyNetQ.Tests.SimpleService console app.
        // Run this test. You should see the SimpleService report that it's
        // responding and the response should appear here.
        [Test, Explicit("Needs a Rabbit instance on localhost to work")]
        public void Should_be_able_to_do_simple_request_response()
        {
            var request = new TestRequestMessage {Text = "Hello from the client! "};

            Console.WriteLine("Making request");
            using (var publishChannel = bus.OpenPublishChannel())
            {
                publishChannel.Request<TestRequestMessage, TestResponseMessage>(request, response =>
                    Console.WriteLine("Got response: '{0}'", response.Text));
            }

            Thread.Sleep(2000);
        }

        // First start the EasyNetQ.Tests.SimpleService console app.
        // Run this test. You should see the SimpleService report that it's
        // responding to 1000 messages and you should see the messages return here.
        [Test, Explicit("Needs a Rabbit instance on localhost to work")]
        public void Should_be_able_to_do_simple_request_response_lots()
        {
            for (int i = 0; i < 1000; i++)
            {
                var request = new TestRequestMessage { Text = "Hello from the client! " + i.ToString() };
                using (var publishChannel = bus.OpenPublishChannel())
                {
                    publishChannel.Request<TestRequestMessage, TestResponseMessage>(request, response =>
                        Console.WriteLine("Got response: '{0}'", response.Text));
                }
            }

            Thread.Sleep(3000);
        }

        // First start the EasyNetQ.Tests.SimpleService console app.
        // Run this test. You should see the SimpleService report that it's
        // responding and the response should appear here.
        [Test, Explicit("Needs a Rabbit instance on localhost to work")]
        public void Should_be_able_to_make_a_request_that_runs_async_on_the_server()
        {
            var request = new TestAsyncRequestMessage {Text = "Hello async from the client!"};

            Console.Out.WriteLine("Making request");
            using (var publishChannel = bus.OpenPublishChannel())
            {
                publishChannel.Request<TestAsyncRequestMessage, TestAsyncResponseMessage>(request,
                    response => Console.Out.WriteLine("response = {0}", response.Text));
            }

            Thread.Sleep(2000);
        }

        // First start the EasyNetQ.Tests.SimpleService console app.
        // Run this test. You should see 1000 response messages on the SimpleService
        // and then 1000 messages appear back here.
        [Test, Explicit("Needs a Rabbit instance on localhost to work")]
        public void Should_be_able_to_make_many_async_requests()
        {
            for (int i = 0; i < 1000; i++)
            {
                var request = new TestAsyncRequestMessage { Text = "Hello async from the client! " + i };

                using (var publishChannel = bus.OpenPublishChannel())
                {
                    publishChannel.Request<TestAsyncRequestMessage, TestAsyncResponseMessage>(request,
                        response =>
                            Console.Out.WriteLine("response = {0}", response.Text));
                }
            }
            Thread.Sleep(5000);
        }

        /// <summary>
        /// First start the EasyNetQ.Tests.SimpleService console app.
        /// Run this test. You should see an error message written to the error queue
        /// and an error logged
        /// </summary>
        [Test, Explicit("Needs a Rabbit instance on localhost to work")]
        public void Service_should_handle_sychronous_message_of_the_wrong_type()
        {
            const string routingKey = "EasyNetQ_Tests_TestRequestMessage:EasyNetQ_Tests_Messages";
            const string type = "not_the_type_you_are_expecting";

            MakeRpcRequest(type, routingKey);
        }

        /// <summary>
        /// First start the EasyNetQ.Tests.SimpleService console app.
        /// Run this test. You should see an error message written to the error queue
        /// and an error logged
        /// </summary>
        [Test, Explicit("Needs a Rabbit instance on localhost to work")]
        public void Service_should_handle_asychronous_message_of_the_wrong_type()
        {
            const string routingKey = "EasyNetQ_Tests_TestAsyncRequestMessage:EasyNetQ_Tests_Messages";
            const string type = "not_the_type_you_are_expecting";

            MakeRpcRequest(type, routingKey);
        }

        private static void MakeRpcRequest(string type, string routingKey)
        {
            var connectionFactory = new ConnectionFactory
            {
                HostName = "localhost"
            };
            using (var connection = connectionFactory.CreateConnection())
            using (var model = connection.CreateModel())
            {
                var properties = model.CreateBasicProperties();
                properties.Type = type;
                model.BasicPublish(
                    "easy_net_q_rpc", // exchange
                    routingKey, // routing key
                    false, // manditory
                    false, // immediate
                    properties,
                    new byte[0]);
            }
        }

        // First start the EasyNetQ.Tests.SimpleService console app.
        // Run this test. You should see the SimpleService report that it's
        // Thrown and a new error message in the error queue.
        [Test, Explicit("Needs a Rabbit instance on localhost to work")]
        public void Should_be_able_to_do_simple_request_response_that_throws_on_server()
        {
            var request = new TestRequestMessage
            {
                Text = "Hello from the client! ",
                CausesExceptionInServer = true
            };

            Console.WriteLine("Making request");
            using (var publishChannel = bus.OpenPublishChannel())
            {
                publishChannel.Request<TestRequestMessage, TestResponseMessage>(request, response =>
                    Console.WriteLine("Got response: '{0}'", response.Text));
            }

            Thread.Sleep(2000);
        }

        // First start the EasyNetQ.Tests.SimpleService console app.
        // Run this test. You should see the SimpleService report that it's
        // responding and the response should appear here with an exception report.
        // you should also see a new error message in the error queue.
        [Test, Explicit("Needs a Rabbit instance on localhost to work")]
        public void Should_be_able_to_do_simple_request_response_that_throws_on_response_consumer()
        {
            var request = new TestRequestMessage { Text = "Hello from the client! " };

            Console.WriteLine("Making request");
            using (var publishChannel = bus.OpenPublishChannel())
            {
                publishChannel.Request<TestRequestMessage, TestResponseMessage>(request, response =>
                {
                    Console.WriteLine("Got response: '{0}'", response.Text);
                    throw new SomeRandomException("Something very bad just happened!");
                });
            }

            Thread.Sleep(2000);
        }

        // First start the EasyNetQ.Tests.SimpleService console app.
        // Run this test. You should see the SimpleService report that it's
        // responding and the response should appear here.
        [Test, Explicit("Needs a Rabbit instance on localhost to work")]
        public void Should_be_able_to_do_simple_request_response_that_takes_a_long_time()
        {
            var request = new TestRequestMessage
            {
                Text = "Hello from the client! ",
                CausesServerToTakeALongTimeToRespond = true
            };

            Console.WriteLine("Making request");
            using (var publishChannel = bus.OpenPublishChannel())
            {
                publishChannel.Request<TestRequestMessage, TestResponseMessage>(request, response =>
                    Console.WriteLine("Got response: '{0}'", response.Text));
            }

            Thread.Sleep(7000);
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

// ReSharper restore InconsistentNaming