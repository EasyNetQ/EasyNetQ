// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EasyNetQ.Events;
using EasyNetQ.Producer;
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Rhino.Mocks;

namespace EasyNetQ.Tests.ProducerTests
{
    [TestFixture]
    public class PublisherBaseTests
    {
        private IPublisher publisher;
        IModel channel;

        [SetUp]
        public void SetUp()
        {
            channel = MockRepository.GenerateStub<IModel>();

            publisher = new PublisherBase();
        }

        [Test]
        public void Should_complete_task_immediately_without_waiting_for_ack()
        {
            var taskWasExecuted = false;
            var task = publisher.Publish(channel, model => taskWasExecuted = true);
            task.Wait();
            taskWasExecuted.ShouldBeTrue();
        }
    }

    [TestFixture]
    public class PublisherFactoryTests_when_publisher_confirms_enabled
    {
        private IPublisher publisher;

        [Test]
        public void Should_return_instance_of_publisher_confirms()
        {
            var eventBus = MockRepository.GenerateStub<IEventBus>();
            var logger = MockRepository.GenerateStub<IEasyNetQLogger>();

            var connectionConfiguration = new ConnectionConfiguration
                {
                    PublisherConfirms = true
                };

            publisher = PublisherFactory.CreatePublisher(connectionConfiguration, logger, eventBus);

            Assert.IsAssignableFrom<PublisherConfirms>(publisher);
        }
    }

    [TestFixture]
    public class PublisherFactoryTests_when_publisher_confirms_disabled
    {
        private IPublisher publisher;

        [Test]
        public void Should_return_instance_of_publisher_base()
        {
            var eventBus = MockRepository.GenerateStub<IEventBus>();
            var logger = MockRepository.GenerateStub<IEasyNetQLogger>();

            var connectionConfiguration = new ConnectionConfiguration
            {
                PublisherConfirms = false
            };

            publisher = PublisherFactory.CreatePublisher(connectionConfiguration, logger, eventBus);

            Assert.IsAssignableFrom<PublisherBase>(publisher);
        }
    }

    [TestFixture]
    public class PublisherConfirmsTests
    {
        private IPublisher publisherConfirms;
        IModel channel;
        private IEventBus eventBus;

        [SetUp]
        public void SetUp()
        {
            channel = MockRepository.GenerateStub<IModel>();
            eventBus = MockRepository.GenerateStub<IEventBus>();

            var connectionConfiguration = new ConnectionConfiguration
                {
                    Timeout = 1
                };

            var logger = MockRepository.GenerateStub<IEasyNetQLogger>();

            publisherConfirms = new PublisherConfirms(connectionConfiguration, logger, eventBus);
        }

        [Test]
        public void Should_register_for_publisher_confirms()
        {
            var task = publisherConfirms.Publish(channel, model => { });

            channel.Raise(x => x.BasicAcks += null, null, new BasicAckEventArgs());
            task.Wait();

            channel.AssertWasCalled(x => x.ConfirmSelect());
        }

        [Test]
        public void Should_complete_successfully_when_acked()
        {
            var actionWasExecuted = false;

            var task = publisherConfirms.Publish(channel, model => actionWasExecuted = true);

            channel.Raise(x => x.BasicAcks += null, null, new BasicAckEventArgs());
            task.Wait();
            actionWasExecuted.ShouldBeTrue();
        }

        [Test]
        [ExpectedException(typeof(PublishNackedException))]
        public void Should_throw_exception_when_nacked()
        {
            var task = publisherConfirms.Publish(channel, model => {});

            channel.Raise(x => x.BasicNacks += null, null, new BasicNackEventArgs());

            try
            {
                task.Wait();
            }
            catch (AggregateException aggregateException)
            {
                throw aggregateException.InnerException;
            }
        }

        [Test]
        [ExpectedException(typeof(TimeoutException))]
        public void Should_timeout_if_neither_ack_or_nack_is_raised()
        {
            var task = publisherConfirms.Publish(channel, model => { });
            try
            {
                task.Wait();
            }
            catch (AggregateException aggregateException)
            {
                throw aggregateException.InnerException;
            }
        }

        [Test]
        public void Should_match_ack_with_sequence_number()
        {
            ulong sequenceNumber = 0;
            channel.Stub(x => x.NextPublishSeqNo).WhenCalled(x => x.ReturnValue = sequenceNumber++).Return(0);

            var tasks = new List<Task>();

            for (ulong i = 0; i < 10; i++)
            {
                tasks.Add(publisherConfirms.Publish(channel, model => { }));
                channel.Raise(x => x.BasicAcks += null, null, new BasicAckEventArgs
                    {
                        DeliveryTag = i
                    });
            }

            Task.WaitAll(tasks.ToArray());
        }
    }

    [TestFixture]
    public class PublisherConfirmsTests_when_channel_reconnects
    {
        private IPublisher publisherConfirms;
        private IEventBus eventBus;

        [SetUp]
        public void SetUp()
        {
            eventBus = new EventBus();

            var connectionConfiguration = new ConnectionConfiguration
            {
                PublisherConfirms = true,
                Timeout = 1000
            };

            var logger = MockRepository.GenerateStub<IEasyNetQLogger>();

            publisherConfirms = PublisherFactory.CreatePublisher(connectionConfiguration, logger, eventBus);
        }

        [Test]
        public void Should_complete_task_when_new_model_acks()
        {
            var channel1 = MockRepository.GenerateStub<IModel>();
            var channel2 = MockRepository.GenerateStub<IModel>();

            var modelsUsedInPublish = new List<IModel>();

            // do the publish, this should be retried against the new model after it reconnects.
            var task = publisherConfirms.Publish(channel1, modelsUsedInPublish.Add);

            // new channel connects
            eventBus.Publish(new PublishChannelCreatedEvent(channel2));

            // new channel ACKs (sequence number is 0)
            channel2.Raise(x => x.BasicAcks += null, null, new BasicAckEventArgs());

            // wait for task to complete
            task.Wait();

            // should have published on both channels:
            modelsUsedInPublish.Count.ShouldEqual(2);
            modelsUsedInPublish[0].ShouldBeTheSameAs(channel1);
            modelsUsedInPublish[1].ShouldBeTheSameAs(channel2);
        }
    }
}

// ReSharper restore InconsistentNaming