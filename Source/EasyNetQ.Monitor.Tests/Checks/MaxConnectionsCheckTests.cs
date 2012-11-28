// ReSharper disable InconsistentNaming

using System;
using EasyNetQ.Management.Client;
using EasyNetQ.Management.Client.Model;
using EasyNetQ.Monitor.Checks;
using NUnit.Framework;
using Rhino.Mocks;

namespace EasyNetQ.Monitor.Tests.Checks
{
    [TestFixture]
    public class MaxConnectionsCheckTests
    {
        private ICheck maxConnectionsCheck;
        private IManagementClient managementClient;
        private MonitorConfigurationSection configuration;
        private Broker broker;
        private const string brokerUrl = "http://the.broker.com";

        [SetUp]
        public void SetUp()
        {
            maxConnectionsCheck = new MaxConnectionsCheck();
            managementClient = MockRepository.GenerateStub<IManagementClient>();
            configuration = new MonitorConfigurationSection
            {
                CheckSettings = new CheckSettings
                {
                    AlertConnectionCount = 100
                }
            };

            broker = new Broker
            {
                ManagementUrl = brokerUrl
            };
        }

        [Test]
        public void Should_alert_when_connections_are_over_configured_limit()
        {
            managementClient.Stub(x => x.GetOverview()).Return(new Overview
            {
                object_totals = new ObjectTotals
                {
                    connections = 101
                }
            });
            var result = maxConnectionsCheck.RunCheck(managementClient, configuration, broker);
            result.Alert.ShouldBeTrue();
            result.Message.ShouldEqual(
                "broker http://the.broker.com connections have exceeded alert level 100. Now 101");
        }

        [Test]
        public void Should_not_alert_when_connections_are_under_configured_limit()
        {
            managementClient.Stub(x => x.GetOverview()).Return(new Overview
            {
                object_totals = new ObjectTotals
                {
                    connections = 99
                }
            });
            var result = maxConnectionsCheck.RunCheck(managementClient, configuration, broker);
            result.Alert.ShouldBeFalse();
        }
    }
}

// ReSharper restore InconsistentNaming