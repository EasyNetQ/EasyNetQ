﻿// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;

using EasyNetQ.SystemMessages;
using EasyNetQ.Topology;

namespace EasyNetQ.Tests
{
    using EasyNetQ.Loggers;

    [TestFixture]
    public class ConsumerErrorConditionsTests
    {
        private IBus bus;

        [SetUp]
        public void SetUp()
        {
            bus = RabbitHutch.CreateBus(new ConnectionConfiguration() {Hosts = new HostConfiguration[] {new HostConfiguration() { Host = "localhost"} }},
                reg => { reg.Register<IEasyNetQLogger>(p => new ConsoleLogger()); });
        }

        [TearDown]
        public void TearDown()
        {
            bus.Dispose();
        }

        // run this test. You should see the following on the console:
        //
        //    ERROR: Exception thrown by subscription calback.
        //	    Exchange:    'EasyNetQ_Tests_MyErrorTestMessage:EasyNetQ_Tests'
        //	    Routing Key: 'EasyNetQ_Tests_MyErrorTestMessage:EasyNetQ_Tests'
        //	    Redelivered: 'False'
        //    Message:
        //    {"Id":444,"Name":"I cause an error. Naughty me!"}
        //    BasicProperties:
        //    (content-type=_, content-encoding=_, headers=_, delivery-mode=2, priority=_, correlation-id=_, reply-to=_, expiration=_, message-id=_, timestamp=_, type=_, user-id=_, app-id=_, cluster-id=_)
        //    Exception:
        //    System.Exception: Hello Error Handler!
        //       at EasyNetQ.Tests.ConsumerErrorConditionsTests.<Should_log_exceptions_thrown_by_subscribers>b__1(MyErrorTestMessage message) in C:\Source\Mike.AmqpSpike\EasyNetQ.Tests\ConsumerErrorConditionsTests.cs:line 51
        //       at EasyNetQ.RabbitBus.<>c__DisplayClass2`1.<Subscribe>b__1(String consumerTag, UInt64 deliveryTag, Boolean redelivered, String exchange, String routingKey, IBasicProperties properties, Byte[] body) in C:\Source\Mike.AmqpSpike\EasyNetQ\RabbitBus.cs:line 154
        //       at EasyNetQ.QueueingConsumerFactory.HandleMessageDelivery(BasicDeliverEventArgs basicDeliverEventArgs) in C:\Source\Mike.AmqpSpike\EasyNetQ\QueueingConsumerFactory.cs:line 78
        //
        [Test, Explicit("Needs a RabbitMQ instance on localhost to run")]
        public void Should_log_exceptions_thrown_by_subscribers()
        {
            bus.Subscribe<MyErrorTestMessage>("exceptionTest", message =>
            {
                throw new Exception("Hello Error Handler!");
            });    

            // give the subscription a chance to complete
            Thread.Sleep(500);

            bus.Publish(new MyErrorTestMessage { Id = 444, Name = "I cause an error. Naughty me!" });

            // give the publish a chance to get to rabbit and back
            Thread.Sleep(1000);
        }

        [Test, Explicit("Needs a RabbitMQ instance on localhost to run")]
        public void Should_wrap_error_messages_correctly()
        {
            var typeNameSerializer = bus.Advanced.Container.Resolve<ITypeNameSerializer>();
            var serializer = bus.Advanced.Container.Resolve<ISerializer>();
            var conventions = bus.Advanced.Container.Resolve<IConventions>();

            var errorQueue = new Queue(conventions.ErrorQueueNamingConvention(), false);
            bus.Advanced.QueuePurge(errorQueue);

            var typeName = typeNameSerializer.Serialize(typeof(MyErrorTestMessage));
            var exchange = this.bus.Advanced.ExchangeDeclare(typeName, ExchangeType.Topic);

            bus.Subscribe<MyErrorTestMessage>("exceptionTest", _ =>
            {
                throw new Exception("Hello Error Handler!");
            });

            // give the subscription a chance to complete
            Thread.Sleep(500);

            var message = new MyErrorTestMessage { Id = 444, Name = "I cause an error. Naughty me!" };
            var headers = new Dictionary<string, object>()
                          {
                              { "AString", "ThisIsAString" },
                              { "AnInt", 123 }
                          };
            var correlationId = Guid.NewGuid().ToString();

            var props = new MessageProperties()
            {
                Type = typeName,
                Headers = headers,
                CorrelationId = correlationId
            };

             bus.Advanced.Publish(exchange, typeNameSerializer.Serialize(typeof(MyErrorTestMessage)), true, props, serializer.MessageToBytes(message));

            // give the publish a chance to get to rabbit and back
            // also allow the DefaultConsumerErrorStrategy time to spin up its connection
            Thread.Sleep(1000);

            var errorMessage = this.bus.Advanced.Get<Error>(errorQueue);
            errorMessage.MessageAvailable.ShouldBeTrue();

            var error = errorMessage.Message.Body;
            Console.WriteLine(error.ToString());


            error.BasicProperties.Type.ShouldEqual(typeNameSerializer.Serialize(typeof(MyErrorTestMessage)));
            error.BasicProperties.CorrelationId.ShouldEqual(correlationId);
            error.BasicProperties.Headers.ShouldEqual(headers);
            error.BasicProperties.Headers["AString"].ShouldEqual("ThisIsAString");
            error.BasicProperties.Headers["AnInt"].ShouldEqual(123);
        }
    }

    public class MyErrorTestMessage
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}

// ReSharper restore InconsistentNaming