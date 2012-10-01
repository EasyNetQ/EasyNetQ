// ReSharper disable InconsistentNaming

using System.Linq;
using EasyNetQ.ConnectionString;
using NUnit.Framework;

namespace EasyNetQ.Tests.ConnectionString
{
    [TestFixture]
    public class ConnectionStringParserTests
    {
        private IConnectionStringParser connectionStringParser;
        private const string connectionString =
            "virtualHost=Copa;username=Copa;host=192.168.1.1;password=abc_xyz;port=12345;requestedHeartbeat=3";

        [SetUp]
        public void SetUp()
        {
            connectionStringParser = new ConnectionStringParser();
        }

        [Test]
        public void Should_correctly_parse_connection_string()
        {
            var connectionConfiguration = connectionStringParser.Parse(connectionString);

            connectionConfiguration.Hosts.First().Host.ShouldEqual("192.168.1.1");
            connectionConfiguration.VirtualHost.ShouldEqual("Copa");
            connectionConfiguration.UserName.ShouldEqual("Copa");
            connectionConfiguration.Password.ShouldEqual("abc_xyz");
            connectionConfiguration.Port.ShouldEqual(12345);
            connectionConfiguration.RequestedHeartbeat.ShouldEqual(3);
        }
    }
}

// ReSharper restore InconsistentNaming