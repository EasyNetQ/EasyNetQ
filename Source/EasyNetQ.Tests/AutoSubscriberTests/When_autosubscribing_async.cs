using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EasyNetQ.AutoSubscribe;
using EasyNetQ.Tests.Mocking;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace EasyNetQ.Tests.AutoSubscriberTests
{
    public class When_autosubscribing_async : IDisposable
    {
        readonly MockBuilder mockBuilder;

        const string expectedQueueName1 =
            "EasyNetQ.Tests.AutoSubscriberTests.When_autosubscribing_async+MessageA, EasyNetQ.Tests_my_app:9a0467719db423b16b7e5c35d25b877c";

        const string expectedQueueName2 =
            "EasyNetQ.Tests.AutoSubscriberTests.When_autosubscribing_async+MessageB, EasyNetQ.Tests_MyExplicitId";

        const string expectedQueueName3 =
            "EasyNetQ.Tests.AutoSubscriberTests.When_autosubscribing_async+MessageC, EasyNetQ.Tests_my_app:63691066ce7a3fb38d685ce30873d12e";

        public When_autosubscribing_async()
        {
            //mockBuilder = new MockBuilder();
            mockBuilder = new MockBuilder();

            var autoSubscriber = new AutoSubscriber(bus: mockBuilder.Bus, subscriptionIdPrefix: "my_app");
            autoSubscriber.SubscribeAsync(typeof(MyAsyncConsumer));
        }

        public void Dispose()
        {
            mockBuilder.Bus.Dispose();
        }

        [Fact]
        public void Should_have_declared_the_queues()
        {
            void VerifyQueueDeclared(string queueName) => 
                mockBuilder.Channels[0].Received().QueueDeclare(
                    queue: Arg.Is(value: queueName), 
                    durable: Arg.Is(true), 
                    exclusive: Arg.Is(false), 
                    autoDelete: Arg.Is(false), 
                    arguments: Arg.Any<IDictionary<string, object>>()
                );

            VerifyQueueDeclared(queueName: expectedQueueName1);
            VerifyQueueDeclared(queueName: expectedQueueName2);
            VerifyQueueDeclared(queueName: expectedQueueName3);
        }

        [Fact]
        public void Should_have_bound_to_queues()
        {
            // ReSharper disable once CollectionNeverUpdated.Local
            var parameters = new Dictionary<string, object>(){ };

            void ConsumerStarted(int channelIndex, string queueName, string topicName) => 
                mockBuilder.Channels[0].Received().QueueBind(
                    queue: Arg.Is(value: queueName), 
                    exchange: Arg.Any<string>(), 
                    routingKey: Arg.Is(value: topicName), 
                    arguments: Arg.Is<IDictionary<string, object>>(x => x.SequenceEqual(parameters))
                );

            ConsumerStarted(channelIndex: 1, queueName: expectedQueueName1, topicName: "#");
            ConsumerStarted(channelIndex: 2, queueName: expectedQueueName2, topicName: "#");
            ConsumerStarted(channelIndex: 3, queueName: expectedQueueName3, topicName: "Important");
        }

        [Fact]
        public void Should_have_started_consuming_from_the_correct_queues()
        {
            mockBuilder.ConsumerQueueNames.Contains(expectedQueueName1).Should().BeTrue();
            mockBuilder.ConsumerQueueNames.Contains(expectedQueueName2).Should().BeTrue();
            mockBuilder.ConsumerQueueNames.Contains(expectedQueueName3).Should().BeTrue();
        }

        //Discovered by reflection over test assembly, do not remove.
        class MyAsyncConsumer : IConsumeAsync<MessageA>, IConsumeAsync<MessageB>, IConsumeAsync<MessageC>
        {
            public Task ConsumeAsync(MessageA message)
            {
                throw new NotImplementedException();
            }

            [AutoSubscriberConsumer(SubscriptionId = "MyExplicitId")]
            public Task ConsumeAsync(MessageB message)
            {
                throw new NotImplementedException();
            }

            [ForTopic("Important")]
            public Task ConsumeAsync(MessageC message)
            {
                throw new NotImplementedException();
            }
        }

        class MessageA
        {
            public string Text { get; set; }
        }

        class MessageB
        {
            public string Text { get; set; }
        }

        class MessageC
        {
            public string Text { get; set; }
        }

    }
}