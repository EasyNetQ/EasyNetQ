// ReSharper disable InconsistentNaming

using EasyNetQ.Management.Client;
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
        private IAlertSink alertSink;

        [SetUp]
        public void SetUp()
        {
            check = MockRepository.GenerateStub<ICheck>();
            check
                .Stub(x => x.RunCheck(Arg<IManagementClient>.Is.Anything))
                .Return(new CheckResult(true, "boo!"))
                .Repeat.Any();

            brokers = new[]
            {
                new Broker{ ManagementUrl = "http://broker1", Username = "user1", Password = "password1"}, 
            };

            checks = new[]
            {
                check,
            };

            managementClient = MockRepository.GenerateStub<IManagementClient>();
            managementClientFactory = MockRepository.GenerateStub<IManagementClientFactory>();

            managementClientFactory.Stub(x => x.CreateManagementClient(Arg<Broker>.Is.Anything)).Return(managementClient)
                .Repeat.Any();

            alertSink = MockRepository.GenerateStub<IAlertSink>();

            monitorRun = new MonitorRun(brokers, checks, managementClientFactory, new[] { alertSink });
        }

        [Test]
        public void Should_run_checks_with_all_clients_for_all_brokers()
        {
            monitorRun.Run();

            managementClientFactory.AssertWasCalled(x => x.CreateManagementClient(brokers[0]));

            check.AssertWasCalled(x => x.RunCheck(managementClient));

            alertSink.AssertWasCalled(x => x.Alert("boo!"));
        }
    }
}

// ReSharper restore InconsistentNaming