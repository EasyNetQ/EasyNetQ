// ReSharper disable InconsistentNaming

using System;
using System.Diagnostics;
using System.Threading;
using EasyNetQ.Loggers;
using NUnit.Framework;

namespace EasyNetQ.Tests.Integration
{
    [TestFixture]
    public class PublishSubscribeWithPublisherConfirms
    {
        private IBus bus;

        [SetUp]
        public void SetUp()
        {
            bus = RabbitHutch.CreateBus("host=localhost", x => x.Register<IEasyNetQLogger>(_ => new DelegateLogger()));
        }

        [TearDown]
        public void TearDown()
        {
            bus.Dispose();
        }

        [Test, Explicit("needs a RabbitMQ broker on localhost to run")]
        public void Should_acknowledge_publication_with_publisher_confirms()
        {
            var wait = new AutoResetEvent(false);
            var confirmed = false;

            using (var channel = bus.OpenPublishChannel(x => x.WithPublisherConfirms()))
            {
                var message = new MyMessage {Text = "Hello Confirm!"};

                channel.Publish(message, x => 
                    x.OnSuccess(() =>
                    {
                        confirmed = true;
                        wait.Set();
                    })
                    .OnFailure(() => wait.Set()));

                wait.WaitOne(2000);
            }

            confirmed.ShouldBeTrue();
        }

        [Test, Explicit("needs a RabbitMQ broker on localhost to run")]
        [ExpectedException(typeof(EasyNetQException))]
        public void Should_throw_if_callbacks_are_not_set()
        {
            using (var channel = bus.OpenPublishChannel(x => x.WithPublisherConfirms()))
            {
                var message = new MyMessage { Text = "Hello Confirm!" };
                channel.Publish(message);
            }
        }

        [Test, Explicit("needs a RabbitMQ broker on localhost to run")]
        public void Should_run_a_batch_nicely()
        {
            const int batchSize = 10000;
            var callbackCount = 0;
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            using (var channel = bus.OpenPublishChannel(x => x.WithPublisherConfirms()))
            {
                for (int i = 0; i < batchSize; i++)
                {
                    var message = new MyMessage {Text = string.Format("Hello Message {0}", i)};
                    channel.Publish(message, x => 
                        x.OnSuccess(() => {
                            callbackCount++;
                        })
                        .OnFailure(() =>
                        {
                            callbackCount++;
                        }));
                }

                // wait until all the publications have been acknowleged.
                while (callbackCount < batchSize)
                {
                    if (stopwatch.Elapsed.Seconds > 10)
                    {
                        throw new ApplicationException("Aborted batch with timeout");
                    }
                    Thread.Sleep(10);
                }
            }
        }
    }
}

// ReSharper restore InconsistentNaming