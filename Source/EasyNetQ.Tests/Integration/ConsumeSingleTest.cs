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
            var result = advancedBus.ConsumeSingle();
            var body = Encoding.UTF8.GetBytes("Hello World");

            advancedBus.Publish(Exchange.GetDefault(), result.Queue.Name, false, false, new MessageProperties(), body);

            result.MessageTask.Wait();

            var message = Encoding.UTF8.GetString(result.MessageTask.Result.Message);

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

                var result = advancedBus.ConsumeSingle();
                result.MessageTask.ContinueWith(task =>
                {
                    var consumedMessage = Encoding.UTF8.GetString(task.Result.Message);
                    consumedMessage.ShouldEqual(publishedMessage);
                    countdown.Signal();
                });

                var body = Encoding.UTF8.GetBytes(publishedMessage);
                advancedBus.Publish(Exchange.GetDefault(), result.Queue.Name, false, false, new MessageProperties(), body);
            }

            countdown.Wait(TimeSpan.FromSeconds(10));
            stopwatch.Stop();
            Console.Out.WriteLine("Finished. Took {0}ms", stopwatch.ElapsedMilliseconds);
        }
    }
}

// ReSharper restore InconsistentNaming
