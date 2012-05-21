// ReSharper disable InconsistentNaming

using NUnit.Framework;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class ConnectionStringTests
    {
        const string connectionStringValue = 
            "first=First Value;host=192.168.1.1;virtualHost=Copa;username=Copa;password=abc_xyz;port=12345";
        private ConnectionString connectionString;

        [SetUp]
        public void SetUp()
        {
            connectionString = new ConnectionString(connectionStringValue);
        }

        [Test]
        public void Should_parse_connection_string_into_name_value_pairs()
        {
            connectionString.GetValue("first").ShouldEqual("First Value");
            connectionString.GetValue("host").ShouldEqual("192.168.1.1");
            connectionString.GetValue("virtualHost").ShouldEqual("Copa");
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
            new ConnectionString("not a well formed name value pair;");
        }

        [Test]
        public void Should_parse_port()
        {
            connectionString.Port.ShouldEqual("12345");
        }
    }
}

// ReSharper restore InconsistentNaming