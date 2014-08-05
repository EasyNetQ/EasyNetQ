using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using EasyNetQ.Loggers;
using EasyNetQ.Topology;
// ReSharper disable InconsistentNaming
using NUnit.Framework;

namespace EasyNetQ.Tests.Integration
{
    [TestFixture, Explicit("Requires a RabbitMQ instance on localhost")]
    public class ConsumeSingleTest
    {
        private IAdvancedBus advancedBus;

        [SetUp]
        public void SetUp()
        {
            advancedBus = RabbitHutch.CreateBus("host=localhost", x => x.Register<IEasyNetQLogger, NullLogger>()).Advanced;
        }

        [TearDown]
        public void TearDown()
        {
            advancedBus.Dispose();
        }

        [Test]
        public void Should_be_able_to_consume_a_single_message()
        {
            var queue = new Queue(Guid.NewGuid().ToString(), true);
            var result = advancedBus.ConsumeSingle(queue, TimeSpan.FromSeconds(5));
            var body = Encoding.UTF8.GetBytes("Hello World");

            advancedBus.Publish(Exchange.GetDefault(), queue.Name, false, false, new MessageProperties(), body);

            result.Wait();

            var message = Encoding.UTF8.GetString(result.Result.Message);

            Console.Out.WriteLine("message = {0}", message);
        }

        [Test]
        public void Should_be_able_to_single_consume_many_messages()
        {
            const int count = 1000;
            var countdown = new CountdownEvent(count);
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            Console.Out.WriteLine("Started");
            for (int i = 0; i < count; i++)
            {
                var publishedMessage = string.Format("Hello World {0}", i);
                var queue = new Queue(Guid.NewGuid().ToString(), true);
                var result = advancedBus.ConsumeSingle(queue,TimeSpan.FromSeconds(5));
                result.ContinueWith(task =>
                {
                    var consumedMessage = Encoding.UTF8.GetString(task.Result.Message);
                    consumedMessage.ShouldEqual(publishedMessage);
                    countdown.Signal();
                });

                var body = Encoding.UTF8.GetBytes(publishedMessage);
                advancedBus.Publish(Exchange.GetDefault(), queue.Name, false, false, new MessageProperties(), body);
            }

            countdown.Wait(TimeSpan.FromSeconds(10));
            stopwatch.Stop();
            Console.Out.WriteLine("Finished. Took {0}ms", stopwatch.ElapsedMilliseconds);
        }
    }
}

// ReSharper restore InconsistentNaming
