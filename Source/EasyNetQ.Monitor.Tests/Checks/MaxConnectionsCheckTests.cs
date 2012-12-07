// ReSharper disable InconsistentNaming

using EasyNetQ.Management.Client.Model;
using EasyNetQ.Monitor.Checks;
using NUnit.Framework;
using Rhino.Mocks;
using log4net;

namespace EasyNetQ.Monitor.Tests.Checks
{
    [TestFixture]
    public class MaxConnectionsCheckTests : CheckTestBase
    {
        private ICheck maxConnectionsCheck;

        protected override void DoSetUp()
        {
            var log = MockRepository.GenerateStub<ILog>();

            maxConnectionsCheck = new MaxConnectionsCheck(100, log);
        }

        [Test]
        public void Should_alert_when_connections_are_over_configured_limit()
        {
            ManagementClient.Stub(x => x.GetOverview()).Return(new Overview
            {
                ObjectTotals = new ObjectTotals
                {
                    Connections = 100
                }
            });
            var result = maxConnectionsCheck.RunCheck(ManagementClient);
            result.Alert.ShouldBeTrue();
            result.Message.ShouldEqual(
                "broker http://the.broker.com connections have exceeded alert level 100. Now 100");
        }

        [Test]
        public void Should_not_alert_when_connections_are_under_configured_limit()
        {
            ManagementClient.Stub(x => x.GetOverview()).Return(new Overview
            {
                ObjectTotals = new ObjectTotals
                {
                    Connections = 99
                }
            });
            var result = maxConnectionsCheck.RunCheck(ManagementClient);
            result.Alert.ShouldBeFalse();
        }
    }
}

// ReSharper restore InconsistentNaming