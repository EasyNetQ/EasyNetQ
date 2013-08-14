// ReSharper disable InconsistentNaming

using System;
using System.Threading;
using EasyNetQ.InMemoryClient;
using NUnit.Framework;

namespace EasyNetQ.Tests.InMemoryClient
{
    [TestFixture]
    public class BasicPublishSubscribeTests
    {
        private IBus bus;
        
        private InMemoryRabbitHutch inMemoryRabbitHutch;

        [SetUp]
        public void SetUp()
        {
            inMemoryRabbitHutch = new InMemoryRabbitHutch();
            bus = inMemoryRabbitHutch.CreateBus();
        }

        [TearDown]
        public void TearDown()
        {
            bus.Dispose();
        }

        [Test]
        public void Should_be_able_to_publish()
        {
            var publishedMessage = new MyMessage {Text = "Hello There Fido!"};
            using (var channel = bus.OpenPublishChannel())
            {
                channel.Publish(publishedMessage);
            }
        }

        [Test]
        public void Should_be_able_to_subscribe()
        {
            bus.Subscribe<MyMessage>("subscriberId", message => Console.WriteLine("Got message: {0}", message.Text));
        }

        [Test]
        public void Should_be_able_to_resubscribe_in_case_queue_is_deleted_or_primary_node_is_down()
        {
            const string subscriberTag = "testConsumerCancelNotification";
            const string queueName = "EasyNetQ_Tests_MyMessage:EasyNetQ_Tests_testConsumerCancelNotification";
            
            bus.Subscribe<MyMessage>(subscriberTag, message => { });

            var originalQueueId = inMemoryRabbitHutch.ConnectionFactory.CurrentConnection.Queues[queueName].Id;

            inMemoryRabbitHutch.ConnectionFactory.CurrentConnection.DeleteQueue(queueName);


            for (int i = 0; i < 10; i++)
            {
                if (inMemoryRabbitHutch.ConnectionFactory.CurrentConnection.Queues.ContainsKey(queueName))
                {
                    break;
                }
                Thread.Sleep(100); // Give time for background thread to reregister queue
            }

            var currentQueueId = inMemoryRabbitHutch.ConnectionFactory.CurrentConnection.Queues[queueName].Id;

            Assert.AreNotEqual(currentQueueId, originalQueueId);
        }

        [Test]
        public void Should_be_able_to_publish_and_subscribe()
        {
            var autoResetEvent = new AutoResetEvent(false);
            MyMessage receivedMessage = null;
            bus.Subscribe<MyMessage>("subscriberId", message =>
            {
                Console.WriteLine("Got message '{0}'", message.Text);
                receivedMessage = message;
                autoResetEvent.Set();
            });

            var publishedMessage = new MyMessage { Text = "Hello There Fido!" };
            using (var channel = bus.OpenPublishChannel())
            {
                channel.Publish(publishedMessage);
            }

            // give the task background thread time to process the message.
            autoResetEvent.WaitOne();

            receivedMessage.Text.ShouldEqual(publishedMessage.Text);
        }

        [Test]
        public void Should_load_share_between_multiple_consumers()
        {
            var countdownEvent = new CountdownEvent(2);

            MyMessage receivedMessage1 = null;
            bus.Subscribe<MyMessage>("subscriberId", message =>
            {
                Console.WriteLine("Handler A got '{0}'", message.Text);
                receivedMessage1 = message;
                countdownEvent.Signal();
            });

            MyMessage receivedMessage2 = null;
            bus.Subscribe<MyMessage>("subscriberId", message =>
            {
                Console.WriteLine("Handler B got '{0}'", message.Text);
                receivedMessage2 = message;
                countdownEvent.Signal();
            });

            var publishedMessage1 = new MyMessage { Text = "Hello There From The First!" };
            var publishedMessage2 = new MyMessage { Text = "Hello There From The Second!" };
            using (var channel = bus.OpenPublishChannel())
            {
                channel.Publish(publishedMessage1);
                channel.Publish(publishedMessage2);
            }

            // give the task background thread time to process the message.
            countdownEvent.Wait();

            receivedMessage1.Text.ShouldEqual(publishedMessage1.Text);
            receivedMessage2.Text.ShouldEqual(publishedMessage2.Text);
        }

        [Test]
        public void Should_work_with_topics()
        {
            var countdownEvent = new CountdownEvent(2);

            MyMessage receivedMessage1 = null;
            bus.Subscribe<MyMessage>("barSubscriber", message =>
            {
                Console.WriteLine("*.bar got '{0}'", message.Text);
                receivedMessage1 = message;
                countdownEvent.Signal();
            }, x => x.WithTopic("*.bar"));

            MyMessage receivedMessage2 = null;
            bus.Subscribe<MyMessage>("fooSubscriber", message =>
            {
                Console.WriteLine("foo.* got '{0}'", message.Text);
                receivedMessage2 = message;
                countdownEvent.Signal();
            }, x => x.WithTopic("foo.*"));

            var fooNinja = new MyMessage { Text = "I should go to foo.ninja" };
            var niceBar = new MyMessage { Text = "I should go to nice.bar" };
            using (var channel = bus.OpenPublishChannel())
            {
                channel.Publish(fooNinja, x => x.WithTopic("foo.ninja"));
                channel.Publish(niceBar, x => x.WithTopic("nice.bar"));
            }

            // give the task background thread time to process the message.
            countdownEvent.Wait();

            receivedMessage1.Text.ShouldEqual(niceBar.Text);
            receivedMessage2.Text.ShouldEqual(fooNinja.Text);
        }
    }
}

// ReSharper restore InconsistentNaming