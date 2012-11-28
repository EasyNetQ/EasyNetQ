// ReSharper disable InconsistentNaming

using EasyNetQ.Management.Client;
using EasyNetQ.Monitor.AlertSinks;
using NUnit.Framework;
using Rhino.Mocks;

namespace EasyNetQ.Monitor.Tests
{
    [TestFixture]
    public class MonitorRunTests
    {
        private MonitorRun monitorRun;

        private Broker[] brokers;
        private ICheck check;
        private ICheck[] checks;
        private IManagementClientFactory managementClientFactory;
        private IManagementClient managementClient;
        private MonitorConfigurationSection configuration;    

        [SetUp]
        public void SetUp()
        {
            check = MockRepository.GenerateStub<ICheck>();
            check
                .Stub(x => x.RunCheck(
                    Arg<IManagementClient>.Is.Anything, 
                    Arg<MonitorConfigurationSection>.Is.Anything,
                    Arg<Broker>.Is.Anything))
                .Return(new CheckResult(true, ""))
                .Repeat.Any();

            brokers = new[]
            {
                new Broker{ ManagementUrl = "http://broker1", Username = "user1", Password = "password1"}, 
                new Broker{ ManagementUrl = "http://broker2", Username = "user2", Password = "password2"}, 
            };

            checks = new[]
            {
                check,
                check,
            };

            managementClient = MockRepository.GenerateStub<IManagementClient>();
            managementClientFactory = MockRepository.GenerateStub<IManagementClientFactory>();

            managementClientFactory.Stub(x => x.CreateManagementClient(Arg<Broker>.Is.Anything)).Return(managementClient)
                .Repeat.Any();

            configuration = MonitorConfigurationSection.Settings;

            monitorRun = new MonitorRun(brokers, checks, managementClientFactory, configuration, new NullAlertSink());
        }

        [Test]
        public void Should_run_checks_with_all_clients_for_all_brokers()
        {
            monitorRun.Run();

            managementClientFactory.AssertWasCalled(x => x.CreateManagementClient(brokers[0]));
            managementClientFactory.AssertWasCalled(x => x.CreateManagementClient(brokers[1]));

            check.AssertWasCalled(x => x.RunCheck(managementClient, configuration, brokers[0]));
            check.AssertWasCalled(x => x.RunCheck(managementClient, configuration, brokers[1]));
        }
    }
}

// ReSharper restore InconsistentNaming