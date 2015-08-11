// ReSharper disable InconsistentNaming

using System.Linq;
using EasyNetQ.ConnectionString;
using NUnit.Framework;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class ConnectionConfigurationTests
    {
        [Test]
        public void The_validate_method_should_apply_AMQPconnection_idempotently()
        {
            ConnectionConfiguration connectionConfiguration = new ConnectionStringParser().Parse("amqp://amqphost:1234/");
            connectionConfiguration.Validate(); // Simulates additional call to .Validate(); made by some RabbitHutch.CreateBus(...) overloads, in addition to call within ConnectionStringParser.Parse().  

            connectionConfiguration.Hosts.Count().ShouldEqual(1);
            connectionConfiguration.Hosts.Single().Host.ShouldEqual("amqphost");
            connectionConfiguration.Hosts.Single().Port.ShouldEqual(1234);
        }

        [Test]
        public void Should_apply_ssloption_to_host()
        {
            ConnectionConfiguration connectionConfiguration = new ConnectionStringParser().Parse("host=host1");
            connectionConfiguration.Hosts.Count(h => h.Ssl.Enabled).ShouldEqual(0);

            connectionConfiguration.ApplySslOptions("servername=host1");

            connectionConfiguration.Hosts.Single().Ssl.Enabled.ShouldEqual(true);
        }

        [Test]
        public void Should_apply_full_ssloptions()
        {
            ConnectionConfiguration connectionConfiguration = new ConnectionStringParser().Parse("host=host1");
            connectionConfiguration.Hosts.Count(h => h.Ssl.Enabled).ShouldEqual(0);

            connectionConfiguration.ApplySslOptions("servername=host1;certpath=mycert.p12;certpassphrase=secret");

            connectionConfiguration.Hosts.Single().Ssl.Enabled.ShouldEqual(true);
            connectionConfiguration.Hosts.Single().Ssl.CertPath.ShouldEqual("mycert.p12");
            connectionConfiguration.Hosts.Single().Ssl.CertPassphrase.ShouldEqual("secret");

        }

        [Test]
        public void Should_apply_ssloptions_to_multiple_hosts()
        {
            ConnectionConfiguration connectionConfiguration = new ConnectionStringParser().Parse("host=host1,host2");
            connectionConfiguration.Hosts.Count().ShouldEqual(2);
            connectionConfiguration.Hosts.Count(h => h.Ssl.Enabled).ShouldEqual(0);

            connectionConfiguration.ApplySslOptions("servername=host1,servername=host2");

            connectionConfiguration.Hosts.Count(h => h.Ssl.Enabled).ShouldEqual(2);
        }

        [Test]
        public void Should_default_to_ssl_port_when_ssl_enabled()
        {
            ConnectionConfiguration connectionConfiguration = new ConnectionStringParser().Parse("host=host1");

            connectionConfiguration.ApplySslOptions("servername=host1");

            connectionConfiguration.Hosts.Single().Port.ShouldEqual(5671);
        }

        [Test]
        public void Should_not_impose_default_ssl_port_when_ssl_enabled_but_port_explicitly_specified()
        {
            ConnectionConfiguration connectionConfiguration = new ConnectionStringParser().Parse("host=host1:1234");

            connectionConfiguration.ApplySslOptions("servername=host1");

            connectionConfiguration.Hosts.Single().Port.ShouldEqual(1234);
        }

        [Test]
        public void Should_not_default_to_ssl_port_when_ssl_disabled()
        {
            ConnectionConfiguration connectionConfiguration = new ConnectionStringParser().Parse("host=host1");

            connectionConfiguration.ApplySslOptions("servername=host1;enabled=false");

            connectionConfiguration.Hosts.Single().Port.ShouldEqual(5672);
        }

        [Test]
        public void Should_throw_on_no_matching_host()
        {
            ConnectionConfiguration connectionConfiguration = new ConnectionStringParser().Parse("host=host1");

            Assert.That(() => connectionConfiguration.ApplySslOptions("servername=nonmatchinghost"), Throws.InstanceOf<EasyNetQException>().With.Message.Contains("nonmatchinghost"));
        }

        [Test]
        public void Should_throw_on_multiple_matching_hosts()
        {
            ConnectionConfiguration connectionConfiguration = new ConnectionStringParser().Parse("host=host1,host1");

            Assert.That(() => connectionConfiguration.ApplySslOptions("servername=host1"), Throws.InstanceOf<EasyNetQException>().With.Message.Contains("Multiple").And.Message.Contains("host1"));
        }

        [Test]
        public void Should_throw_on_configure_ssloptions_for_same_host_twice()
        {
            ConnectionConfiguration connectionConfiguration = new ConnectionStringParser().Parse("host=host1");

            Assert.That(() => connectionConfiguration.ApplySslOptions("servername=host1,servername=host1"), Throws.InstanceOf<EasyNetQException>().With.Message.Contains("Conflicting"));
        }

        [Test]
        public void Should_throw_on_configure_ssloptions_both_on_configurationconnection_and_on_hosts()
        {
            ConnectionConfiguration connectionConfiguration = new ConnectionStringParser().Parse("host=host1");
            connectionConfiguration.Ssl.Enabled = true;

            Assert.That(() => connectionConfiguration.ApplySslOptions("servername=host1"), Throws.InstanceOf<EasyNetQException>().With.Message.Contains("ambiguous"));
        }
    }
}

// ReSharper restore InconsistentNaming