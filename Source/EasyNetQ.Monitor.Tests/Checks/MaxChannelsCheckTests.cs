// ReSharper disable InconsistentNaming

using EasyNetQ.Management.Client.Model;
using EasyNetQ.Monitor.Checks;
using NUnit.Framework;
using Rhino.Mocks;

namespace EasyNetQ.Monitor.Tests.Checks
{
    [TestFixture]
    public class MaxChannelsCheckTests : CheckTestBase
    {
        private ICheck maxChannelsCheck;

        protected override void DoSetUp()
        {
            maxChannelsCheck = new MaxChannelsCheck(100);
        }

        [Test]
        public void Should_alert_when_channels_exceed_maximum()
        {
            ManagementClient.Stub(x => x.GetOverview()).Return(new Overview
            {
                object_totals = new ObjectTotals
                {
                    channels = 100
                }
            });

            var result = maxChannelsCheck.RunCheck(ManagementClient);

            result.Alert.ShouldBeTrue();
            result.Message.ShouldEqual("broker 'http://the.broker.com' channels have exceeded alert level 100, now 100");
        }

        [Test]
        public void Should_not_alert_when_channels_are_under_maximum()
        {
            ManagementClient.Stub(x => x.GetOverview()).Return(new Overview
            {
                object_totals = new ObjectTotals
                {
                    channels = 99
                }
            });

            var result = maxChannelsCheck.RunCheck(ManagementClient);

            result.Alert.ShouldBeFalse();
        }
    }
}

// ReSharper restore InconsistentNaming