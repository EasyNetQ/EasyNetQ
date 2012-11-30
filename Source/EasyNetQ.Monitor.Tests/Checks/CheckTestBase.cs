using EasyNetQ.Management.Client;
using NUnit.Framework;
using Rhino.Mocks;

namespace EasyNetQ.Monitor.Tests.Checks
{
    public abstract class CheckTestBase
    {
        protected IManagementClient ManagementClient;
        protected const string BrokerUrl = "http://the.broker.com";

        [SetUp]
        public void SetUp()
        {
            ManagementClient = MockRepository.GenerateStub<IManagementClient>();
            ManagementClient.Stub(x => x.HostUrl).Return(BrokerUrl);
            DoSetUp();
        }

        protected abstract void DoSetUp();
    }
}