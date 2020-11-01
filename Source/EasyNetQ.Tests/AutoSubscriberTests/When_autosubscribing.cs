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
    public class When_autosubscribing : IDisposable
    {
        private MockBuilder mockBuilder;

        private const string expectedQueueName1 =
            "EasyNetQ.Tests.AutoSubscriberTests.When_autosubscribing+MessageA, EasyNetQ.Tests_my_app:d7617d39b90b6b695b90c630539a12e2";

        private const string expectedQueueName2 =
            "EasyNetQ.Tests.AutoSubscriberTests.When_autosubscribing+MessageB, EasyNetQ.Tests_MyExplicitId";

        private const string expectedQueueName3 =
            "EasyNetQ.Tests.AutoSubscriberTests.When_autosubscribing+MessageC, EasyNetQ.Tests_my_app:8b7980aa5e42959b4202e32ee442fc52";

        public When_autosubscribing()
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
                (channelIndex, queueName, topicName) =>
                    mockBuilder.Channels[0].Received().QueueBind(
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
            public void Consume(MessageA message, CancellationToken cancellationToken)
            {
            }

            [AutoSubscriberConsumer(SubscriptionId = "MyExplicitId")]
            public void Consume(MessageB message, CancellationToken cancellationToken)
            {
            }

            [ForTopic("Important")]
            public void Consume(MessageC message, CancellationToken cancellationToken)
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
        }

        private class MessageB
        {
        }

        private class MessageC
        {
        }
    }
}

// ReSharper restore InconsistentNaming
