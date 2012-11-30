// ReSharper disable InconsistentNaming

using System.IO;
using NUnit.Framework;

namespace EasyNetQ.Monitor.Tests
{
    [TestFixture]
    public class MonitorConfigurationSectionTests
    {
        private MonitorConfigurationSection section;

        [SetUp]
        public void SetUp()
        {
            section = MonitorConfigurationSection.Settings;
        }

        [Test]
        public void Should_get_brokers()
        {
            const string expectedResult = 
@"managementUrl = http://localhost, username = guest, password = guest
";

            var writer = new StringWriter();

            foreach (Broker broker in section.Brokers)
            {
                writer.WriteLine("managementUrl = {0}, username = {1}, password = {2}", 
                    broker.ManagementUrl,
                    broker.Username,
                    broker.Password);
            }

            writer.GetStringBuilder().ToString().ShouldEqual(expectedResult);
            // Console.WriteLine(writer.GetStringBuilder().ToString());
        }
    }
}

// ReSharper restore InconsistentNaming