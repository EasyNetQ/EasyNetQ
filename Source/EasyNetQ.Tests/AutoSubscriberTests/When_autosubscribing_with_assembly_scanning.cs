using System;
using System.Collections.Generic;
using EasyNetQ.AutoSubscribe;
using EasyNetQ.Tests.Mocking;
using NUnit.Framework;
using Rhino.Mocks;

namespace EasyNetQ.Tests.AutoSubscriberTests
{
    [TestFixture]
    public class When_autosubscribing_with_assembly_scanning
    {
        private MockBuilder mockBuilder;

        private const string expectedQueueName1 =
            "EasyNetQ.Tests.AutoSubscriberTests.When_autosubscribing+MessageA:EasyNetQ.Tests_my_app:d7617d39b90b6b695b90c630539a12e2";

        private const string expectedQueueName2 =
            "EasyNetQ.Tests.AutoSubscriberTests.When_autosubscribing+MessageB:EasyNetQ.Tests_MyExplicitId";

        private const string expectedQueueName3 =
            "EasyNetQ.Tests.AutoSubscriberTests.When_autosubscribing+MessageC:EasyNetQ.Tests_my_app:8b7980aa5e42959b4202e32ee442fc52";

        [SetUp]
        public void SetUp()
        {
            mockBuilder = new MockBuilder();
//            mockBuilder = new MockBuilder(x => x.Register<IEasyNetQLogger, ConsoleLogger>());

            var autoSubscriber = new AutoSubscriber(mockBuilder.Bus, "my_app");

            autoSubscriber.Subscribe(GetType().Assembly);
        }

        [Test]
        public void Should_have_declared_the_queues()
        {
            Action<string> assertQueueDeclared = queueName =>
                mockBuilder.Channels[0].AssertWasCalled(x => x.QueueDeclare(
                    Arg<string>.Is.Equal(queueName),
                    Arg<bool>.Is.Equal(true),
                    Arg<bool>.Is.Equal(false),
                    Arg<bool>.Is.Equal(false),
                    Arg<IDictionary<string, object>>.Is.Anything
                    ));

            assertQueueDeclared(expectedQueueName1);
            assertQueueDeclared(expectedQueueName2);
            assertQueueDeclared(expectedQueueName3);
        }

        [Test]
        public void Should_have_bound_to_queues()
        {
            Action<int, string, string> assertConsumerStarted = (channelIndex, queueName, topicName) =>
                mockBuilder.Channels[0].AssertWasCalled(x =>
                    x.QueueBind(
                        Arg<string>.Is.Equal(queueName),
                        Arg<string>.Is.Anything,
                        Arg<string>.Is.Equal(topicName), new Dictionary<string, object>())
                    );

            assertConsumerStarted(1, expectedQueueName1, "#");
            assertConsumerStarted(2, expectedQueueName2, "#");
            assertConsumerStarted(3, expectedQueueName3, "Important");
        }

        [Test]
        public void Should_have_started_consuming_from_the_correct_queues()
        {
            mockBuilder.ConsumerQueueNames.Contains(expectedQueueName1).ShouldBeTrue();
            mockBuilder.ConsumerQueueNames.Contains(expectedQueueName2).ShouldBeTrue();
            mockBuilder.ConsumerQueueNames.Contains(expectedQueueName3).ShouldBeTrue();
        }
    }
}