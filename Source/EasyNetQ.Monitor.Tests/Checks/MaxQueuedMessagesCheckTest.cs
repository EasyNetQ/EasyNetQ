// ReSharper disable InconsistentNaming

using EasyNetQ.Management.Client.Model;
using EasyNetQ.Monitor.Checks;
using NUnit.Framework;
using Rhino.Mocks;

namespace EasyNetQ.Monitor.Tests.Checks
{
    [TestFixture]
    public class MaxQueuedMessagesCheckTest : CheckTestBase
    {
        private ICheck maxQueuedMessagesCheck;

        protected override void DoSetUp()
        {
            maxQueuedMessagesCheck = new MaxQueuedMessagesCheck(100);
        }

        [Test]
        public void Should_alert_when_total_queued_messages_exceeds_maximum()
        {
            ManagementClient.Stub(x => x.GetOverview()).Return(new Overview
            {
                queue_totals = new QueueTotals
                {
                    messages = 100
                }
            });

            var result = maxQueuedMessagesCheck.RunCheck(ManagementClient);

            result.Alert.ShouldBeTrue();
            result.Message.ShouldEqual("broker 'http://the.broker.com' queued messages exceed alert level 100, now 100");
        }

        [Test]
        public void Should_not_alert_when_total_queued_messages_is_below_alert_level()
        {
            ManagementClient.Stub(x => x.GetOverview()).Return(new Overview
            {
                queue_totals = new QueueTotals
                {
                    messages = 99
                }
            });

            var result = maxQueuedMessagesCheck.RunCheck(ManagementClient);

            result.Alert.ShouldBeFalse();
        }
    }
}

// ReSharper restore InconsistentNaming