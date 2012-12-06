// ReSharper disable InconsistentNaming

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
            var count = 0;
            foreach (Broker broker in section.Brokers)
            {
                count++;
                broker.ManagementUrl.ShouldEqual("http://localhost");
                broker.Username.ShouldEqual("guest");
                broker.Password.ShouldEqual("guest");
            }
            count.ShouldEqual(1);
        }
    }
}

// ReSharper restore InconsistentNaming