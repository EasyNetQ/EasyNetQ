// ReSharper disable InconsistentNaming

using EasyNetQ.AutoSubscribe;
using EasyNetQ.Tests.Mocking;
using FluentAssertions;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Xunit;

namespace EasyNetQ.Tests.AutoSubscriberTests
{
    public class When_autosubscribing_with_explicit_implementation : IDisposable
    {
        private MockBuilder mockBuilder;

        private const string expectedQueueName1 =
            "EasyNetQ.Tests.AutoSubscriberTests.When_autosubscribing_with_explicit_implementation+MessageA, EasyNetQ.Tests_my_app:fe528c6fdb14f1b5a2216b78ab508ca9";

        private const string expectedQueueName2 =
            "EasyNetQ.Tests.AutoSubscriberTests.When_autosubscribing_with_explicit_implementation+MessageB, EasyNetQ.Tests_MyExplicitId";

        private const string expectedQueueName3 =
            "EasyNetQ.Tests.AutoSubscriberTests.When_autosubscribing_with_explicit_implementation+MessageC, EasyNetQ.Tests_my_app:34db9400fe90bb9dc2cf2743a21dadbf";

        public When_autosubscribing_with_explicit_implementation()
        {
            //mockBuilder = new MockBuilder();
            mockBuilder = new MockBuilder();

            var autoSubscriber = new AutoSubscriber(mockBuilder.Bus, "my_app");
            autoSubscriber.Subscribe(new[] { typeof(MyConsumer), typeof(MyGenericAbstractConsumer<>) });
        }

        public void Dispose()
        {
            mockBuilder.Bus.Dispose();
        }

        [Fact]
        public void Should_have_declared_the_queues()
        {
            Action<string> assertQueueDeclared = queueName =>
                mockBuilder.Channels[0].Received().QueueDeclare(
                    Arg.Is(queueName),
                    Arg.Is(true),
                    Arg.Is(false),
                    Arg.Is(false),
                    Arg.Is((IDictionary<string, object>)null)
                );

            assertQueueDeclared(expectedQueueName1);
            assertQueueDeclared(expectedQueueName2);
            assertQueueDeclared(expectedQueueName3);
        }

        [Fact]
        public void Should_have_bound_to_queues()
        {
            Action<int, string, string> assertConsumerStarted =
                (channelIndex, queueName, topicName) => mockBuilder.Channels[0].Received().QueueBind(
                    Arg.Is(queueName),
                    Arg.Any<string>(),
                    Arg.Is(topicName),
                    Arg.Is((IDictionary<string, object>)null)
                );

            assertConsumerStarted(1, expectedQueueName1, "#");
            assertConsumerStarted(2, expectedQueueName2, "#");
            assertConsumerStarted(3, expectedQueueName3, "Important");
        }

        [Fact]
        public void Should_have_started_consuming_from_the_correct_queues()
        {
            mockBuilder.ConsumerQueueNames.Contains(expectedQueueName1).Should().BeTrue();
            mockBuilder.ConsumerQueueNames.Contains(expectedQueueName2).Should().BeTrue();
            mockBuilder.ConsumerQueueNames.Contains(expectedQueueName3).Should().BeTrue();
        }

        // Discovered by reflection over test assembly, do not remove.
        private class MyConsumer : IConsume<MessageA>, IConsume<MessageB>, IConsume<MessageC>
        {
            void IConsume<MessageA>.Consume(MessageA message, CancellationToken cancellationToken)
            {
            }

            [AutoSubscriberConsumer(SubscriptionId = "MyExplicitId")]
            void IConsume<MessageB>.Consume(MessageB message, CancellationToken cancellationToken)
            {
            }

            [ForTopic("Important")]
            void IConsume<MessageC>.Consume(MessageC message, CancellationToken cancellationToken)
            {
            }
        }

        //Discovered by reflection over test assembly, do not remove.
        private abstract class MyGenericAbstractConsumer<TMessage> : IConsume<TMessage>
          where TMessage : class
        {
            public virtual void Consume(TMessage message, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }

        private class MessageA
        {
            public string Text { get; set; }
        }

        private class MessageB
        {
            public string Text { get; set; }
        }

        private class MessageC
        {
            public string Text { get; set; }
        }
    }
}

// ReSharper restore InconsistentNaming
