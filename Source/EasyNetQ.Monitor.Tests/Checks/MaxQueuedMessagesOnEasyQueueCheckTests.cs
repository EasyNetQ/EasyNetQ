// ReSharper disable InconsistentNaming

using System;
using EasyNetQ.Management.Client.Model;
using EasyNetQ.Monitor.Checks;
using NUnit.Framework;
using Rhino.Mocks;

namespace EasyNetQ.Monitor.Tests.Checks
{
    [TestFixture]
    public class MaxQueuedMessagesOnEasyQueueCheckTests : CheckTestBase
    {
        private ICheck maxQueuedMessagesOnEasyQueueCheck;

        protected override void DoSetUp()
        {
            maxQueuedMessagesOnEasyQueueCheck = new MaxQueuedMessagesOnEasyQueueCheck(100);
        }

        [Test]
        public void Should_alert_if_any_queue_has_more_than_the_alert_level_messages()
        {
            ManagementClient.Stub(x => x.GetQueues()).Return(new[]
            {
                new Queue
                {
                    Name = "my_queue",
                    Vhost = "my_virtual_host",
                    Messages = 101
                }
            });

            var result = maxQueuedMessagesOnEasyQueueCheck.RunCheck(ManagementClient);

            result.Alert.ShouldBeTrue();
            result.Message.ShouldEqual(
                "Broker 'http://the.broker.com', Queues 'my_queue on my_virtual_host with 101 messages' " + 
                "have exceeded the maximum number of allowed messages 100.");
        }

        [Test]
        public void Should_not_alert_if_no_queues_are_over_the_alert_level()
        {
            ManagementClient.Stub(x => x.GetQueues()).Return(new[]
            {
                new Queue
                {
                    Name = "my_queue",
                    Vhost = "my_virtual_host",
                    Messages = 99
                }
            });

            var result = maxQueuedMessagesOnEasyQueueCheck.RunCheck(ManagementClient);

            result.Alert.ShouldBeFalse();
        }
    }
}

// ReSharper restore InconsistentNaming