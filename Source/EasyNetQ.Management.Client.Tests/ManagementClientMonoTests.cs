using System;
using NUnit.Framework;

namespace EasyNetQ.Management.Client.Tests
{
    [TestFixture]
    public class ManagementClientMonoTests
    {
		private const string hostUrl = "http://localhost";
        private const string username = "guest";
        private const string password = "guest";
        private const int port = 15672;

        [Test]
        public void Should_get_overview_on_mono()
        {
            var managementClient2 = new ManagementClient(hostUrl, username, password, port, true);

            var overview = managementClient2.GetOverview();

            Console.Out.WriteLine("overview.ManagementVersion = {0}", overview.ManagementVersion);
            foreach (var exchangeType in overview.ExchangeTypes)
            {
                Console.Out.WriteLine("exchangeType.Name = {0}", exchangeType.Name);
            }
            foreach (var listener in overview.Listeners)
            {
                Console.Out.WriteLine("listener.IpAddress = {0}", listener.IpAddress);
            }

            Console.Out.WriteLine("overview.Messages = {0}", overview.QueueTotals != null ? overview.QueueTotals.Messages : 0);

            foreach (var context in overview.Contexts)
            {
                Console.Out.WriteLine("context.Description = {0}", context.Description);
            }
        }
    }
}
