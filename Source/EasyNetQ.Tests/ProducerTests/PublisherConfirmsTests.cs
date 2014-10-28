// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EasyNetQ.Events;
using EasyNetQ.Producer;
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing;
using Rhino.Mocks;

namespace EasyNetQ.Tests.ProducerTests
{
    [TestFixture]
    public class PublisherBasicTests
    {
        private IPublisher publisher;
        private IEventBus eventBus;
        IModel channel;
        
        [SetUp]
        public void SetUp()
        {
            channel = MockRepository.GenerateStub<IModel>();
            eventBus = MockRepository.GenerateStub<IEventBus>();

            publisher = new PublisherBasic(eventBus);
        }

        [Test]
        public void Should_register_for_message_returns()
        {
             publisher.Publish(channel, model => { }).Wait();

            channel.AssertWasCalled(x => x.BasicReturn += Arg<BasicReturnEventHandler>.Is.Anything);
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
    public class PublisherBasicTests_when_message_returned
    {
        private IPublisher publisher;
        private IEventBus eventBus;
        private IModel channelMock;

        [SetUp]
        public void SetUp()
        {
            channelMock = MockRepository.GenerateStub<IModel>();
            eventBus = MockRepository.GenerateStub<IEventBus>();
            
            publisher = new PublisherBasic(eventBus);
        }

        [Test]
        public void Should_raise_message_returned_event_when_message_returned()
        {
            const string exchange = "the exchange";
            const string replyText = "reply text";
            const string routingKey = "routing key";
            var body = new byte[0];
            var properties = new BasicProperties();
            
            publisher.Publish(channelMock, model => { }).Wait();
            
            channelMock.Raise(x => x.BasicReturn += null, null, new BasicReturnEventArgs
                {
                    Body = body,
                    Exchange = exchange,
                    ReplyText = replyText,
                    RoutingKey = routingKey,
                    BasicProperties = properties
                });

            var arg = eventBus.GetArgumentsForCallsMadeOn(x => x.Publish(Arg<ReturnedMessageEvent>.Is.Anything))[0][0];

            var messageEvent = arg as ReturnedMessageEvent;
            Assert.NotNull(messageEvent);
            Assert.AreSame(body, messageEvent.Body);
            Assert.NotNull(messageEvent.Properties);
            Assert.AreEqual(exchange, messageEvent.Info.Exchange);
            Assert.AreEqual(replyText, messageEvent.Info.ReturnReason);
            Assert.AreEqual(routingKey, messageEvent.Info.RoutingKey);
        }

    }

    [TestFixture]
    public class PublisherBasicTests_when_channel_reconnects
    {
        private IPublisher publisher;
        private IEventBus eventBus;

        [SetUp]
        public void SetUp()
        {
            eventBus = new EventBus();

            publisher = new PublisherBasic(eventBus);
        }

        [Test]
        public void Should_remove_event_handler_from_old_channel()
        {
            var channel1 = MockRepository.GenerateStub<IModel>();
            var channel2 = MockRepository.GenerateStub<IModel>();

            publisher.Publish(channel1, model => { }).Wait();
            eventBus.Publish(new PublishChannelCreatedEvent(channel2));
            publisher.Publish(channel2, model => { }).Wait();

            channel1.AssertWasCalled(x => x.BasicReturn -= Arg<BasicReturnEventHandler>.Is.Anything);
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
        public void Should_return_instance_of_publisher_basic()
        {
            var eventBus = MockRepository.GenerateStub<IEventBus>();
            var logger = MockRepository.GenerateStub<IEasyNetQLogger>();

            var connectionConfiguration = new ConnectionConfiguration
            {
                PublisherConfirms = false
            };

            publisher = PublisherFactory.CreatePublisher(connectionConfiguration, logger, eventBus);

            Assert.IsAssignableFrom<PublisherBasic>(publisher);
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
        public void Should_register_for_message_returns()
        {
            var task = publisherConfirms.Publish(channel, model => { });

            channel.Raise(x => x.BasicAcks += null, null, new BasicAckEventArgs());
            task.Wait();

            channel.AssertWasCalled(x => x.BasicReturn += Arg<BasicReturnEventHandler>.Is.Anything);
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
    public class PublisherConfirmsTests_when_message_returned
    {
        private IPublisher publisherConfirms;
        private IEventBus eventBus;
        private IModel channel;

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
        public void Should_raise_message_returned_event_when_message_returned()
        {
            const string exchange = "the exchange";
            const string replyText = "reply text";
            const string routingKey = "routing key";
            var body = new byte[0];
            var properties = new BasicProperties();

            var task = publisherConfirms.Publish(channel, model => { });

            channel.Raise(x => x.BasicAcks += null, null, new BasicAckEventArgs());
            channel.Raise(x => x.BasicReturn += null, null, new BasicReturnEventArgs
            {
                Body = body,
                Exchange = exchange,
                ReplyText = replyText,
                RoutingKey = routingKey,
                BasicProperties = properties
            });

            task.Wait();

            var arg = eventBus.GetArgumentsForCallsMadeOn(x => x.Publish(Arg<ReturnedMessageEvent>.Is.Anything))[0][0];

            var messageEvent = arg as ReturnedMessageEvent;
            Assert.NotNull(messageEvent);
            Assert.AreSame(body, messageEvent.Body);
            Assert.NotNull(messageEvent.Properties);
            Assert.AreEqual(exchange, messageEvent.Info.Exchange);
            Assert.AreEqual(replyText, messageEvent.Info.ReturnReason);
            Assert.AreEqual(routingKey, messageEvent.Info.RoutingKey);
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
    
        [Test]
        public void Should_remove_event_handler_from_old_channel()
        {
            var channel1 = MockRepository.GenerateStub<IModel>();
            var channel2 = MockRepository.GenerateStub<IModel>();

            // do the publish, this should be retried against the new model after it reconnects.
            var task = publisherConfirms.Publish(channel1, model => { });

            // new channel connects
            eventBus.Publish(new PublishChannelCreatedEvent(channel2));

            // new channel ACKs (sequence number is 0)
            channel2.Raise(x => x.BasicAcks += null, null, new BasicAckEventArgs());

            // wait for task to complete
            task.Wait();

            channel1.AssertWasCalled(x => x.BasicAcks -= Arg<BasicAckEventHandler>.Is.Anything);
            channel1.AssertWasCalled(x => x.BasicNacks -= Arg<BasicNackEventHandler>.Is.Anything);
            channel1.AssertWasCalled(x => x.BasicReturn -= Arg<BasicReturnEventHandler>.Is.Anything);
        }
    }
}

// ReSharper restore InconsistentNaming