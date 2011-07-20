// ReSharper disable InconsistentNaming

using System;
using System.Threading;
using EasyNetQ.Loggers;
using NUnit.Framework;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class ConsumerErrorConditionsTests
    {
        private IBus bus;

        [SetUp]
        public void SetUp()
        {
            bus = RabbitHutch.CreateBus("localhost", "guest", "guest", new ConsoleLogger());

            // wait for rabbit to connect
            while (!bus.IsConnected)
            {
                Thread.Sleep(10);
            }
        }

        [TearDown]
        public void TearDown()
        {
            bus.Dispose();
        }

        // run this test. You should see the following on the console:
        //
        //ERROR: Exception thrown by subscription calback.
        //	Exchange:    'EasyNetQ_Tests_MyErrorTestMessage:EasyNetQ_Tests'
        //	Routing Key: 'EasyNetQ_Tests_MyErrorTestMessage:EasyNetQ_Tests'
        //	Redelivered: 'False'
        //Message:
        //{"Id":444,"Name":"I cause an error. Naughty me!"}
        //Exception:
        //System.Exception: Hello Error Handler!
        //   at EasyNetQ.Tests.ConsumerErrorConditionsTests.<Should_log_exceptions_thrown_by_subscribers>b__2(MyErrorTestMessage message) in C:\Source\Mike.AmqpSpike\EasyNetQ.Tests\ConsumerErrorConditionsTests.cs:line 42
        //   at EasyNetQ.RabbitBus.<>c__DisplayClass2`1.<Subscribe>b__1(String consumerTag, UInt64 deliveryTag, Boolean redelivered, String exchange, String routingKey, IBasicProperties properties, Byte[] body) in C:\Source\Mike.AmqpSpike\EasyNetQ\RabbitBus.cs:line 154
        //   at EasyNetQ.QueueingConsumerFactory.HandleMessageDelivery(BasicDeliverEventArgs basicDeliverEventArgs) in C:\Source\Mike.AmqpSpike\EasyNetQ\QueueingConsumerFactory.cs:line 75
        [Test, Explicit("Needs a RabbitMQ instance on localhost to run")]
        public void Should_log_exceptions_thrown_by_subscribers()
        {
            bus.Subscribe<MyErrorTestMessage>("exceptionTest", message =>
            {
                throw new Exception("Hello Error Handler!");
            });    

            // give the subscription a chance to complete
            Thread.Sleep(100);

            bus.Publish(new MyErrorTestMessage { Id = 444, Name = "I cause an error. Naughty me!"});

            // give the publish a chance to get to rabbit and back
            Thread.Sleep(100);
        }
    }

    public class MyErrorTestMessage
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}

// ReSharper restore InconsistentNaming