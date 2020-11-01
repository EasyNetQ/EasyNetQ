// ReSharper disable InconsistentNaming

using System;
using System.Linq;
using EasyNetQ.ConnectionString;
using FluentAssertions;
using Xunit;

namespace EasyNetQ.Tests
{
    public class ConnectionStringTests
    {
        private const string connectionStringValue =
            "host=192.168.1.1:1001,my.little.host:1002;virtualHost=Copa;username=Copa;" +
            "password=abc_xyz;port=12345;requestedHeartbeat=3";
        private ConnectionConfiguration connectionString;

        private ConnectionConfiguration defaults;

        public ConnectionStringTests()
        {
            connectionString = new ConnectionStringParser().Parse(connectionStringValue);
            defaults = new ConnectionStringParser().Parse("host=localhost");
        }

        [Fact]
        public void Should_parse_host()
        {
            connectionString.Hosts.First().Host.Should().Be("192.168.1.1");
        }

        [Fact]
        public void Should_parse_host_port()
        {
            connectionString.Hosts.First().Port.Should().Be(1001);
        }

        [Fact]
        public void Should_parse_second_host()
        {
            connectionString.Hosts.Last().Host.Should().Be("my.little.host");
        }

        [Fact]
        public void Should_parse_seond_port()
        {
            connectionString.Hosts.Last().Port.Should().Be((ushort)1002);
        }

        [Fact]
        public void Should_parse_virtualHost()
        {
            connectionString.VirtualHost.Should().Be("Copa");
        }

        [Fact]
        public void Should_parse_username()
        {
            connectionString.UserName.Should().Be("Copa");
        }

        [Fact]
        public void Should_parse_password()
        {
            connectionString.Password.Should().Be("abc_xyz");
        }

        [Fact]
        public void Should_throw_on_malformed_string()
        {
            Assert.Throws<EasyNetQException>(() =>
            {
                new ConnectionStringParser().Parse("not a well formed name value pair;");
            });
        }

        [Fact]
        public void Should_fail_if_host_is_not_present()
        {
            Assert.Throws<EasyNetQException>(() =>
            {
                new ConnectionStringParser().Parse(
                "virtualHost=Copa;username=Copa;password=abc_xyz;port=12345;requestedHeartbeat=3");
            });
        }

        [Fact]
        public void Should_parse_port()
        {
            connectionString.Port.Should().Be(12345);
        }

        [Fact]
        public void Should_parse_heartbeat()
        {
            connectionString.RequestedHeartbeat.Should().Be(TimeSpan.FromSeconds(3));
        }

        [Fact]
        public void Should_parse_host_only()
        {
            defaults.Hosts.First().Host.Should().Be("localhost");
        }

        [Fact]
        public void Should_set_default_port()
        {
            defaults.Port.Should().Be(5672);
        }

        [Fact]
        public void Should_set_default_virtual_host()
        {
            defaults.VirtualHost.Should().Be("/");
        }

        [Fact]
        public void Should_set_default_username()
        {
            defaults.UserName.Should().Be("guest");
        }

        [Fact]
        public void Should_set_default_password()
        {
            defaults.Password.Should().Be("guest");
        }

        [Fact]
        public void Should_set_default_requestHeartbeat()
        {
            defaults.RequestedHeartbeat.Should().Be(TimeSpan.FromSeconds(10));
        }

        [Fact]
        public void Should_not_have_case_sensitive_keys()
        {
            const string connectionStringAlternateCasing =
                "Host=192.168.1.1:1001,my.little.host:1002;VirtualHost=Copa;UserName=Copa;" +
                "Password=abc_xyz;Port=12345;RequestedHeartbeat=3";

            var parsed = new ConnectionStringParser().Parse(connectionStringAlternateCasing);
            parsed.Hosts.First().Host.Should().Be("192.168.1.1");
            parsed.Hosts.First().Port.Should().Be(1001);
            parsed.VirtualHost.Should().Be("Copa");
            parsed.UserName.Should().Be("Copa");
            parsed.Password.Should().Be("abc_xyz");
            parsed.Port.Should().Be(12345);
            parsed.RequestedHeartbeat.Should().Be(TimeSpan.FromSeconds(3));
        }
    }
}

// ReSharper restore InconsistentNaming
