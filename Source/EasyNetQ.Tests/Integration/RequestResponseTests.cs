// ReSharper disable InconsistentNaming

using Xunit;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Threading;
using FluentAssertions;

namespace EasyNetQ.Tests.Integration
{
    public class RequestResponseTests : IDisposable
    {
        private IBus bus;
        private const string defaultRpcExchange = "easy_net_q_rpc";

        private readonly Dictionary<Type, string> customRpcRequestConventionDictionary = new Dictionary<Type, string>()
        {
            {typeof (TestModifiedRequestExhangeRequestMessage), "ChangedRpcRequestExchange"}
        };

        private readonly Dictionary<Type, string> customRpcResponseConventionDictionary = new Dictionary<Type, string>()
        {
            {typeof (TestModifiedResponseExhangeResponseMessage), "ChangedRpcResponseExchange"}
        };

        public RequestResponseTests()
        {
            bus = RabbitHutch.CreateBus("host=localhost");
            bus.Advanced.Conventions.RpcRequestExchangeNamingConvention = type => customRpcRequestConventionDictionary.ContainsKey(type) ? customRpcRequestConventionDictionary[type] : defaultRpcExchange;
            bus.Advanced.Conventions.RpcResponseExchangeNamingConvention = type => customRpcResponseConventionDictionary.ContainsKey(type) ? customRpcResponseConventionDictionary[type] : defaultRpcExchange;
            bus.Respond<TestRequestMessage, TestResponseMessage>(req => new TestResponseMessage { Text = req.Text });
        }

        public void Dispose()
        {
            bus.Dispose();
        }

        // First start the EasyNetQ.Tests.SimpleService console app.
        [Fact][Explicit("Needs a Rabbit instance on localhost to work")]
        public void Large_number_of_request_calls_should_not_create_a_large_number_of_open_channels()
        {
            const int numberOfCalls = 100;

            var countdownEvent = new CountdownEvent(numberOfCalls);
            for (int i = 0; i < numberOfCalls; i++)
            {
                bus.RequestAsync<TestRequestMessage, TestResponseMessage>(
                    new TestRequestMessage {Text = string.Format("Hello from client number: {0}! ", i)})
                    .ContinueWith(
                        response =>
                            {
                                Console.WriteLine("Got response: " + response.Result.Text);
                                countdownEvent.Signal();
                            }
                    );
            }

            countdownEvent.Wait(1000);
            Console.WriteLine("Test end");
        }

        // First start the EasyNetQ.Tests.SimpleService console app.
        // Run this test. You should see the SimpleService report that it's
        // responding and the response should appear here.
        [Fact][Explicit("Needs a Rabbit instance on localhost to work")]
        public void Should_be_able_to_do_simple_request_response()
        {
            var request = new TestRequestMessage {Text = "Hello from the client! "};

            Console.WriteLine("Making request");
            var response = bus.Request<TestRequestMessage, TestResponseMessage>(request);

            Console.WriteLine("Got response: '{0}'", response.Text);
        }

        // First start the EasyNetQ.Tests.SimpleService console app.
        // Run this test. You should see the SimpleService report that it's
        // responding to 1000 messages and you should see the messages return here.
        [Fact][Explicit("Needs a Rabbit instance on localhost to work")]
        public void Should_be_able_to_do_simple_request_response_lots()
        {
            const int numberOfCalls = 5000;

            var countdownEvent = new CountdownEvent(numberOfCalls);
            var count = 0;

            for (int i = 0; i < numberOfCalls; i++)
            {
                var request = new TestRequestMessage { Text = "Hello from the client! " + i.ToString() };
                bus.RequestAsync<TestRequestMessage, TestResponseMessage>(request).ContinueWith(response =>
                {
                    Console.WriteLine("Got response: '{0}'", response.Result.Text);
                    count++;
                    countdownEvent.Signal();
                });
            }

            countdownEvent.Wait(15000);
            count.Should().Be(numberOfCalls);
        }

        // First start the EasyNetQ.Tests.SimpleService console app.
        // Run this test. You should see the SimpleService report that it's
        // responding and the response should appear here.
        [Fact][Explicit("Needs a Rabbit instance on localhost to work")]
        public void Should_be_able_to_make_a_request_that_runs_async_on_the_server()
        {
            var autoResetEvent = new AutoResetEvent(false);
            var request = new TestAsyncRequestMessage {Text = "Hello async from the client!"};

            Console.Out.WriteLine("Making request");
            bus.RequestAsync<TestAsyncRequestMessage, TestAsyncResponseMessage>(request).ContinueWith(response =>
            {
                Console.Out.WriteLine("response = {0}", response.Result.Text);
                autoResetEvent.Set();
            });

            autoResetEvent.WaitOne(2000);
        }

        // First start the EasyNetQ.Tests.SimpleService console app.
        // Run this test. You should see the SimpleService report that it's
        // responding and the response should appear here.
        [Fact][Explicit("Needs a Rabbit instance on localhost to work")]
        public void Should_be_able_to_make_a_request_to_customly_defined_exchange()
        {
            var request = new TestModifiedRequestExhangeRequestMessage { Text = "Hello from the client to funky exchange!" };

            Console.Out.WriteLine("Making request");
            var response = bus.RequestAsync<TestModifiedRequestExhangeRequestMessage, TestModifiedRequestExhangeResponseMessage>(request);

            Console.Out.WriteLine("response = {0}", response.Result.Text);

        }

        // First start the EasyNetQ.Tests.SimpleService console app.
        // Run this test. You should see the SimpleService report that it's
        // responding and the response should appear here.
        [Fact][Explicit("Needs a Rabbit instance on localhost to work")]
        public void Should_be_able_to_make_a_request_and_receive_response_to_customly_defined_exchange()
        {
            var request = new TestModifiedResponseExhangeRequestMessage { Text = "Hello from the client! I Wanna receive response via funky exchange!" };

            Console.Out.WriteLine("Making request");
            var response = bus.RequestAsync<TestModifiedResponseExhangeRequestMessage, TestModifiedResponseExhangeResponseMessage>(request);

            Console.Out.WriteLine("response = {0}", response.Result.Text);

        }

        // First start the EasyNetQ.Tests.SimpleService console app.
        // Run this test. You should see 1000 response messages on the SimpleService
        // and then 1000 messages appear back here.
        [Fact][Explicit("Needs a Rabbit instance on localhost to work")]
        public void Should_be_able_to_make_many_async_requests()
        {
            const int numberOfCalls = 500;
            var countdownEvent = new CountdownEvent(numberOfCalls);
            var count = 0;

            for (int i = 0; i < 1000; i++)
            {
                var request = new TestAsyncRequestMessage { Text = "Hello async from the client! " + i };

                bus.RequestAsync<TestAsyncRequestMessage, TestAsyncResponseMessage>(request).ContinueWith(response =>
                {
                    Console.Out.WriteLine("response = {0}", response.Result.Text);
                    Interlocked.Increment(ref count);
                    countdownEvent.Signal();
                });
            }
            countdownEvent.Wait(10000);
            count.Should().Be(numberOfCalls);
        }

        /// <summary>
        /// First start the EasyNetQ.Tests.SimpleService console app.
        /// Run this test. You should see an error message written to the error queue
        /// and an error logged
        /// </summary>
        [Fact][Explicit("Needs a Rabbit instance on localhost to work")]
        public void Service_should_handle_sychronous_message_of_the_wrong_type()
        {
            const string routingKey = "EasyNetQ_Tests_TestRequestMessage, EasyNetQ_Tests_Messages";
            const string type = "not_the_type_you_are_expecting";

            MakeRpcRequest(type, routingKey);
        }

        /// <summary>
        /// First start the EasyNetQ.Tests.SimpleService console app.
        /// Run this test. You should see an error message written to the error queue
        /// and an error logged
        /// </summary>
        [Fact][Explicit("Needs a Rabbit instance on localhost to work")]
        public void Service_should_handle_asychronous_message_of_the_wrong_type()
        {
            const string routingKey = "EasyNetQ_Tests_TestAsyncRequestMessage, EasyNetQ_Tests_Messages";
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
                    false, // mandatory
                    properties,
                    new byte[0]);
            }
        }

        // First start the EasyNetQ.Tests.SimpleService console app.
        // Run this test. You should see the SimpleService report that it's
        // Thrown and a new error message in the error queue.
        [Fact][Explicit("Needs a Rabbit instance on localhost to work")]
        public void Should_be_able_to_do_simple_request_response_that_throws_on_server()
        {
            var request = new TestRequestMessage
            {
                Text = "Hello from the client! ",
                CausesExceptionInServer = true
            };

            Console.WriteLine("Making request");
            bus.RequestAsync<TestRequestMessage, TestResponseMessage>(request).ContinueWith(response =>
                Console.WriteLine("Got response: '{0}'", response.Result.Text));

            Thread.Sleep(500);
        }

        // First start the EasyNetQ.Tests.SimpleService console app.
        // Run this test. You should see the SimpleService report that it's
        // Thrown, a new error message in the error queue and an EasyNetQResponderException
        // exception should be thrown by the consumer as a response.
        [Fact][Explicit("Needs a Rabbit instance on localhost to work")]
        public void Should_throw_an_exception_at_consumer_on_simple_request_response_that_throws_on_server()
        {
            Assert.Throws<EasyNetQResponderException>(() =>
            {
                var request = new TestRequestMessage
                {
                    Text = "Hello from the client! ",
                    CausesExceptionInServer = true,
                    ExceptionInServerMessage = "This should be the original exception message!"
                };

                Console.WriteLine("Making request");
                try
                {
                    bus.RequestAsync<TestRequestMessage, TestResponseMessage>(request).Wait(1000);
                }
                catch (AggregateException e)
                {
                    throw e.InnerException;
                }
            }); //, "This should be the original exception message!");
        }

        // First start the EasyNetQ.Tests.SimpleService console app.
        // Run this test. You should see the SimpleService report that it's
        // responding and the response should appear here with an exception report.
        // you should also see a new error message in the error queue.
        [Fact][Explicit("Needs a Rabbit instance on localhost to work")]
        public void Should_be_able_to_do_simple_request_response_that_throws_on_response_consumer()
        {
            var autoResetEvent = new AutoResetEvent(false);
            var request = new TestRequestMessage { Text = "Hello from the client! " };

            Console.WriteLine("Making request");
            bus.RequestAsync<TestRequestMessage, TestResponseMessage>(request).ContinueWith(response =>
            {
                Console.WriteLine("Got response: '{0}'", response.Result.Text);
                autoResetEvent.Set();
                throw new SomeRandomException("Something very bad just happened!");
            });

            autoResetEvent.WaitOne(1000);
        }

        // First start the EasyNetQ.Tests.SimpleService console app.
        // Run this test. You should see the SimpleService report that it's
        // responding and the response should appear here.
        [Fact][Explicit("Needs a Rabbit instance on localhost to work")]
        public void Should_be_able_to_do_simple_request_response_that_takes_a_long_time()
        {
            var autoResetEvent = new AutoResetEvent(false);

            var request = new TestRequestMessage
            {
                Text = "Hello from the client! ",
                CausesServerToTakeALongTimeToRespond = true
            };

            Console.WriteLine("Making request");
            bus.RequestAsync<TestRequestMessage, TestResponseMessage>(request).ContinueWith(response =>
            {
                Console.WriteLine("Got response: '{0}'", response.Result.Text);
                autoResetEvent.Set();
            });

            autoResetEvent.WaitOne(10000);
        }

    }

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
    }
}

// ReSharper restore InconsistentNaming