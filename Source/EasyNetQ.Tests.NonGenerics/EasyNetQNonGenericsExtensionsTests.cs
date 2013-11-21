using EasyNetQ;
using EasyNetQ.ConnectionString;
using EasyNetQ.NonGenerics;
using EasyNetQ.Topology;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace EasyNetQ.Tests.NonGenerics
{
    [TestFixture]
    public class EasyNetQNonGenericsExtensionsTests
    {
        internal static string ConnectionString = "host=192.168.4.5";

        public class MyMessage
        {
            public Guid Id { get; set; }
        }

        private IBus NewBus(bool publisherConfirms)
        {
            var connectionStringParser = new ConnectionStringParser();
            var connectionConfiguration = connectionStringParser.Parse(ConnectionString) as ConnectionConfiguration;
            connectionConfiguration.PublisherConfirms = publisherConfirms;

            return RabbitHutch.CreateBus(connectionConfiguration, s => { });
        }

        [Test]
        [TestCase("Generics")]
        [TestCase("NonGenerics")]
        public void TestEasyNetQ(string messageTypeIdentificationMode)
        {
            var runtime = 1000;
            var receivedMessageCount = 0;
            var publishedMessageCount = 0;
            var requirePublishConfirmation = true;
            var subscriptionId = "TestEasyNetQ";

            using (var subscriptionBus = NewBus(requirePublishConfirmation))
            {
                try
                {
                    switch (messageTypeIdentificationMode)
                    {
                        case "Generics":
                            subscriptionBus.Subscribe<MyMessage>(
                                subscriptionId,
                                m =>
                                {
                                    receivedMessageCount++;
                                }
                            );
                            break;
                        case "NonGenerics":
                            subscriptionBus.Subscribe(
                                typeof(MyMessage),
                                subscriptionId,
                                m =>
                                {
                                    receivedMessageCount++;
                                }
                            );
                            break;
                    }

                    using (var publishBus = NewBus(requirePublishConfirmation))
                    {
                        var stopwatch = new Stopwatch();
                        stopwatch.Start();
                        while (stopwatch.ElapsedMilliseconds < runtime)
                        {
                            Task publishingTask = null;

                            var message = new MyMessage { Id = Guid.NewGuid() };
                            switch (messageTypeIdentificationMode)
                            {
                                case "Generics":
                                    publishingTask = publishBus.PublishAsync<MyMessage>(message);
                                    break;
                                case "NonGenerics":
                                    publishingTask = publishBus.PublishAsync(null, message);
                                    break;
                            }

                            if (requirePublishConfirmation)
                                publishingTask.Wait();

                            publishedMessageCount++;
                        }
                        stopwatch.Stop();
                    }
                }
                finally
                {
                    subscriptionBus.Advanced.QueueDelete(new Queue(subscriptionBus.QueueNameFor(typeof(MyMessage), subscriptionId), false));
                }
            }

            // Assert all messages sent were received
            publishedMessageCount.Should().Be(receivedMessageCount);
        }

        [Test]
        [TestCase("WithPublishConfirms", "Generics")]
        [TestCase("", "Generics")]
        [TestCase("WithPublishConfirms", "NonGenerics")]
        [TestCase("", "NonGenerics")]
        public void TestEasyNetQTransferRate(string publishConfirmationMode, string messageTypeIdentificationMode)
        {
            var runtime = 1000;
            var receivedMessageCount = 0;
            var publishedMessageCount = 0;
            var requirePublishConfirmation = publishConfirmationMode == "WithPublishConfirms";
            var subscriptionId = "TestEasyNetQTransferRate";

            using (var subscriptionBus = NewBus(requirePublishConfirmation))
            {
                try
                {
                    switch (messageTypeIdentificationMode)
                    {
                        case "Generics":
                            subscriptionBus.Subscribe<MyMessage>(
                                subscriptionId,
                                m =>
                                {
                                    receivedMessageCount++;
                                }
                            );
                            break;
                        case "NonGenerics":
                            subscriptionBus.Subscribe(
                                typeof(MyMessage),
                                subscriptionId,
                                m =>
                                {
                                    receivedMessageCount++;
                                }
                            );
                            break;
                    }

                    using (var publishBus = NewBus(requirePublishConfirmation))
                    {
                        var stopwatch = new Stopwatch();
                        stopwatch.Start();
                        while (stopwatch.ElapsedMilliseconds < runtime)
                        {
                            Task publishingTask = null;

                            var message = new MyMessage { Id = Guid.NewGuid() };
                            switch (messageTypeIdentificationMode)
                            {
                                case "Generics":
                                    publishingTask = publishBus.PublishAsync<MyMessage>(message);
                                    break;
                                case "NonGenerics":
                                    publishingTask = publishBus.PublishAsync(null, message);
                                    break;
                            }

                            if (requirePublishConfirmation)
                                publishingTask.Wait();

                            publishedMessageCount++;
                        }
                        stopwatch.Stop();
                    }
                }
                finally
                {
                    subscriptionBus.Advanced.QueueDelete(new Queue(subscriptionBus.QueueNameFor(typeof(MyMessage), subscriptionId), false));
                }
            }

            Console.WriteLine(String.Format("Published: {0}, Received: {1}, Run Time: {2}", publishedMessageCount, receivedMessageCount, runtime));

            if (requirePublishConfirmation)
            {
                receivedMessageCount.Should().Be(publishedMessageCount);
            }
        }
    }
}
