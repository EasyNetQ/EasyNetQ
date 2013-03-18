// ReSharper disable InconsistentNaming

using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.InMemoryClient;
using NUnit.Framework;

namespace EasyNetQ.Tests.InMemoryClient
{
    [TestFixture]
    public class BasicRequestResponseTests
    {
        private IBus bus;

        [SetUp]
        public void SetUp()
        {
            bus = InMemoryRabbitHutch.CreateBus();
        }

        [TearDown]
        public void TearDown()
        {
            bus.Dispose();
        }

        [Test]
        public void Should_be_able_to_do_basic_request_response()
        {
            const string expectedResult = "Sending back 'Ninja!!'";
            var autoResetEvent = new AutoResetEvent(false);

            bus.Respond<TestRequestMessage, TestResponseMessage>(request =>
            {
                var response = new TestResponseMessage
                {
                    Text = string.Format("Sending back '{0}'", request.Text)
                };
                return response;
            });

            var actualResult = string.Empty;

            using (var channel = bus.OpenPublishChannel())
            {
                var request = new TestRequestMessage {Text = "Ninja!!"};
                channel.Request<TestRequestMessage, TestResponseMessage>(
                    request,
                    response => 
                    {
                        actualResult = response.Text;
                        autoResetEvent.Set();
                    });
            }

            // give the bus a chance to deliver the message
            autoResetEvent.WaitOne(1000);

            actualResult.ShouldEqual(expectedResult);
        }

        [Test]
        public void Should_be_able_to_do_basic_request_async()
        {
            const string expectedResult = "Sending back 'Ninja!!'";
            var autoResetEvent = new AutoResetEvent(false);

            bus.Respond<TestRequestMessage, TestResponseMessage>(request =>
            {
                var response = new TestResponseMessage
                {
                    Text = string.Format("Sending back '{0}'", request.Text)
                };
                return response;
            });

            var actualResult = string.Empty;

            using (var channel = bus.OpenPublishChannel())
            {
                var request = new TestRequestMessage { Text = "Ninja!!" };

                var responseTask = channel.RequestAsync<TestRequestMessage, TestResponseMessage>(request);

                responseTask.ContinueWith(t =>
                    {
                        actualResult = t.Result.Text;
                        autoResetEvent.Set();
                    });
            }

            // give the bus a chance to deliver the message
            autoResetEvent.WaitOne(1000);

            actualResult.ShouldEqual(expectedResult);
        }

        [Test]
        public void Should_be_able_to_do_basic_request_async_cancellation()
        {
            const string expectedResult = "Sending back 'Ninja!!'";
            var autoResetEvent = new AutoResetEvent(false);

            bus.Respond<TestRequestMessage, TestResponseMessage>(request =>
            {
                var response = new TestResponseMessage
                {
                    Text = string.Format("Sending back '{0}'", request.Text)
                };
                return response;
            });

            using (var channel = bus.OpenPublishChannel())
            {
                var request = new TestRequestMessage { Text = "Ninja!!" };

                var cancellationSource = new CancellationTokenSource();

                var responseTask = channel.RequestAsync<TestRequestMessage, TestResponseMessage>(request, cancellationSource.Token);

                responseTask.ContinueWith(t =>
                {
                    autoResetEvent.Set();
                }, TaskContinuationOptions.OnlyOnCanceled);

                cancellationSource.Cancel();
            }

            // give the bus a chance to deliver the message
            Assert.IsTrue(autoResetEvent.WaitOne(1000));
        }

        [Test]
        public void Should_request_closures_work()
        {
            var countdownEvent = new CountdownEvent(3);

            bus.Respond<TestRequestMessage, TestResponseMessage>(request =>
            {
                var response = new TestResponseMessage
                {
                    Text = request.Text + "--" + request.Text
                };
                return response;
            });

            var results = new string[3];

            using (var channel = bus.OpenPublishChannel())
            {
                var requestTexts = new[] {"one", "two", "three"};
                for (int i = 0; i < 3; i++)
                {
                    var request = new TestRequestMessage { Text = requestTexts[i] };
                    var index = i;

                    channel.Request<TestRequestMessage, TestResponseMessage>(
                        request,
                        response => 
                        {
                            results[index] = response.Text;
                            Console.WriteLine("Got response '{0}' on index: {1}", response.Text, index);
                            countdownEvent.Signal();
                        });
                }
            }

            // give the bus a chance to deliver the message
            countdownEvent.Wait(1000);

            results[0].ShouldEqual("one--one");
            results[1].ShouldEqual("two--two");
            results[2].ShouldEqual("three--three");
        }
    }
}

// ReSharper restore InconsistentNaming