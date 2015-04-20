// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.Threading;
using EasyNetQ.Loggers;
using NUnit.Framework;

namespace EasyNetQ.Tests.Integration
{
    [TestFixture, Explicit("Requires RabbitMQ instance to be running on localhost AND rabbitmq_delayed_message_exchange plugin to be installed.")]
    public class DelayedExchangeSchedulerTests
    {
        private IBus bus;
        private ConsoleLogger logger;

        [SetUp]
        public void SetUp()
        {
            logger = new ConsoleLogger();
            bus = RabbitHutch.CreateBus("host=localhost", x =>
                x.Register<IEasyNetQLogger>(_ => logger)
                .UseDelayedMessageExchange()
                 );
        }

        [TearDown]
        public void TearDown()
        {
            bus.Dispose();
        }

        [Test]
        [Explicit("Needs an instance of RabbitMQ on localhost to work.")]
        public void Should_be_able_to_schedule_a_message()
        {
            var autoResetEvent = new AutoResetEvent(false);

            bus.Subscribe<PartyInvitation>("schedulingTest1", message =>
            {
                Console.WriteLine("Got scheduled message: {0}", message.Text);
                autoResetEvent.Set();
            });


            var invitation = new PartyInvitation
            {
                Text = "Please come to my party",
                Date = new DateTime(2011, 5, 24)
            };

            bus.FuturePublish(DateTime.UtcNow.AddSeconds(5), invitation);

            if (!autoResetEvent.WaitOne(10000))
                Assert.Fail("Expected message to be delivered, but it wasn't.");
        }

        [Test]
        [Explicit("Needs an instance of RabbitMQ on localhost to work.")]
        public void When_published_regular_message_to_the_queue_and_then_published_delayed_message_then_should_receive_all_messages()
        {
            var resetEvent = new CountdownEvent(2);

            bus.Subscribe<PartyInvitation>("schedulingTest1", message =>
            {
                Console.WriteLine(message.Text);
                resetEvent.Signal();
            });


            var invitation1 = new PartyInvitation
            {
                Text = "Please come to my party",
                Date = new DateTime(2011, 5, 24)
            };
            bus.Publish(invitation1);

            var invitation2 = new PartyInvitation
            {
                Text = "Bring your friend with you!",
                Date = new DateTime(2011, 5, 24)
            };
            bus.FuturePublish(DateTime.UtcNow.AddSeconds(5), invitation2);

            if (!resetEvent.Wait(10000))
                Assert.Fail("Expected two messages to be delivered!");
        }


        [Test]
        [Explicit("Needs an instance of RabbitMQ on localhost to work.")]
        public void When_published_future_message_to_the_queue_and_then_published_regular_message_then_should_receive_all_messages()
        {
            var resetEvent = new CountdownEvent(2);

            bus.Subscribe<DelayedTestMessage>("schedulingTest1", message =>
            {
                resetEvent.Signal();
            });


            bus.FuturePublish(DateTime.UtcNow.AddSeconds(5), new DelayedTestMessage(1, true));
            bus.Publish(new DelayedTestMessage(2, false));


            if (!resetEvent.Wait(10000))
                Assert.Fail("Expected two messages to be delivered!");
        }


        [Test]
        [Explicit("Needs an instance of RabbitMQ on localhost to work.")]
        public void When_published_future_message_and_regular_message_then_should_recive_regular_message_first()
        {
            var resetEvent = new CountdownEvent(2);
            var receivedMessages = new List<DelayedTestMessage>();
            bus.Subscribe<DelayedTestMessage>("schedulingTest1", message =>
            {
                receivedMessages.Add(message);
                resetEvent.Signal();
            });


            bus.FuturePublish(DateTime.UtcNow.AddSeconds(5), new DelayedTestMessage(1, true));
            bus.Publish(new DelayedTestMessage(2, false));


            if (!resetEvent.Wait(10000))
                Assert.Fail("Expected two messages to be delivered!");

            Assert.IsFalse(receivedMessages[0].Delayed, "Expected first message to be a regular messages.");
            Assert.IsTrue(receivedMessages[1].Delayed, "Expected second messages to be a delayed message.");
        }

        [Test]
        [Explicit("Needs an instance of RabbitMQ on localhost to work.")]
        public void Should_receive_messages_in_order_of_future_publishing()
        {
            var resetEvent = new CountdownEvent(5);
            var receivedMessages = new List<DelayedTestMessage>();
            bus.Subscribe<DelayedTestMessage>("schedulingTest1", message =>
            {
                receivedMessages.Add(message);
                resetEvent.Signal();
            });


            for (var i = 5; i > 0; i--)
            {
                bus.FuturePublish(DateTime.UtcNow.AddSeconds(i), new DelayedTestMessage(i, true));
            }


            if (!resetEvent.Wait(10000))
                Assert.Fail("Expected five messages to be delivered!");

            for (var i = 0; i < 5; i++)
            {
                Assert.AreEqual(receivedMessages[i].Id, i+1);
            }
        }

        [Test]
        [Explicit("Needs an instance of RabbitMQ on localhost to work.")]
        [ExpectedException(typeof(NotImplementedException))]
        public void When_cancelling_a_message_then_fails()
        {
            bus.CancelFuturePublish("cancellationKey");
        }


        [Test]
        [Explicit("Needs an instance of RabbitMQ on localhost to work.")]
        [ExpectedException(typeof(NotImplementedException))]
        public void When_publishing_with_cancellation_key_then_fails()
        {
            bus.FuturePublish(DateTime.UtcNow.AddSeconds(1), "cancellationKey", new DelayedTestMessage(1, true));
        }
    }

    [Serializable]
    public class DelayedTestMessage
    {
        public int Id { get; set; }
        public bool Delayed { get; set; }

        public DelayedTestMessage()
        {}

        public DelayedTestMessage(int id, bool delayed)
        {
            Id = id;
            Delayed = delayed;
        }

        protected bool Equals(DelayedTestMessage other)
        {
            return Id == other.Id && Delayed.Equals(other.Delayed);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DelayedTestMessage) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Id*397) ^ Delayed.GetHashCode();
            }
        }
    }
}

// ReSharper restore InconsistentNaming