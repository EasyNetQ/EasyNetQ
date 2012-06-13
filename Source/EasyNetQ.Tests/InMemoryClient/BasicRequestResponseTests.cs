// ReSharper disable InconsistentNaming

using System;
using System.Threading;
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
                    response => actualResult = response.Text);
            }

            // give the bus a chance to deliver the message
            Thread.Sleep(100);

            actualResult.ShouldEqual(expectedResult);
        }

//        [Test]
//        public void Should_request_closures_work()
//        {
//            bus.Respond<TestRequestMessage, TestResponseMessage>(request =>
//            {
//                var response = new TestResponseMessage
//                {
//                    Text = request.Text + "--" + request.Text
//                };
//                return response;
//            });
//
//            var results = new string[3];
//
//            using (var channel = bus.OpenPublishChannel())
//            {
//                var requestTexts = new[] {"one", "two", "three"};
//                for (int i = 0; i < 3; i++)
//                {
//                    var request = new TestRequestMessage { Text = requestTexts[i] };
//                    var index = i;
//
//                    channel.Request<TestRequestMessage, TestResponseMessage>(
//                        request,
//                        response => 
//                        {
//                            results[index] = response.Text;
//                            Console.WriteLine("Got response '{0}' on index: {1}", response.Text, index);
//                        });
//                }
//            }
//
//            // give the bus a chance to deliver the message
//            Thread.Sleep(100);
//
//            results[0].ShouldEqual("one--one");
//            results[1].ShouldEqual("two--two");
//            results[2].ShouldEqual("three--three");
//        }
    }
}

// ReSharper restore InconsistentNaming