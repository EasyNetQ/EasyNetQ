// ReSharper disable InconsistentNaming

using NUnit.Framework;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class ClusterTests
    {
        private const string clusterHost1 = "ubuntu";
        private const string clusterHost2 = "ubuntu";
        private const string clusterPort1 = "5672"; // rabbit@ubuntu
        private const string clusterPort2 = "5673"; // rabbit_1@ubuntu
        private string connectionString;
        private IBus bus;

        [SetUp]
        public void SetUp()
        {
            const string hostFormat = "{0}:{1}";
            var host1 = string.Format(hostFormat, clusterHost1, clusterPort1);
            var host2 = string.Format(hostFormat, clusterHost2, clusterPort2);
            var hosts = string.Format("{0},{1}", host1, host2);
            connectionString = string.Format("host={0}", hosts);

            // bus = RabbitHutch.CreateBus(connectionString);
        }

        [TearDown]
        public void TearDown()
        {
            // bus.Dispose();
        }

        [Test]
        public void Should_create_the_correct_connection_string()
        {
            connectionString.ShouldEqual("host=ubuntu:5672,ubuntu:5673");
        }
    }
}

// ReSharper restore InconsistentNaming