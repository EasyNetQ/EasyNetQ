// ReSharper disable InconsistentNaming

using EasyNetQ.ConnectionString;
using NUnit.Framework;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class ConnectionStringTests
    {
        const string connectionStringValue =
            "host=192.168.1.1;virtualHost=Copa;username=Copa;password=abc_xyz;port=12345;requestedHeartbeat=3";
        private IConnectionConfiguration connectionString;

        private IConnectionConfiguration defaults;

        [SetUp]
        public void SetUp()
        {
            connectionString = new ConnectionStringParser().Parse(connectionStringValue);
            defaults = new ConnectionStringParser().Parse("host=localhost");
        }

        [Test]
        public void Should_parse_host()
        {
            connectionString.Host.ShouldEqual("192.168.1.1");
        }

        [Test]
        public void Should_parse_virtualHost()
        {
            connectionString.VirtualHost.ShouldEqual("Copa");
        }

        [Test]
        public void Should_parse_username()
        {
            connectionString.UserName.ShouldEqual("Copa");
        }

        [Test]
        public void Should_parse_password()
        {
            connectionString.Password.ShouldEqual("abc_xyz");
        }

        [Test, ExpectedException(typeof(EasyNetQException))]
        public void Should_throw_on_malformed_string()
        {
            new ConnectionStringParser().Parse("not a well formed name value pair;");
        }

        [Test, ExpectedException(typeof(EasyNetQException))]
        public void Should_fail_if_host_is_not_present()
        {
            new ConnectionStringParser().Parse(
                "virtualHost=Copa;username=Copa;password=abc_xyz;port=12345;requestedHeartbeat=3");
        }

        [Test]
        public void Should_parse_port()
        {
            connectionString.Port.ShouldEqual(12345);
        }

        [Test]
        public void Should_parse_heartbeat()
        {
            connectionString.RequestedHeartbeat.ShouldEqual(3);
        }

        [Test]
        public void Should_set_default_port()
        {
            defaults.Port.ShouldEqual(5672);
        }

        [Test]
        public void Should_set_default_virtual_host()
        {
            defaults.VirtualHost.ShouldEqual("/");
        }

        [Test]
        public void Should_set_default_username()
        {
            defaults.UserName.ShouldEqual("guest");

        }

        [Test]
        public void Should_set_default_password()
        {
            defaults.Password.ShouldEqual("guest");
        }

        [Test]
        public void Should_set_default_requestHeartbeat()
        {
            defaults.RequestedHeartbeat.ShouldEqual(0);
        }
    }
}

// ReSharper restore InconsistentNaming