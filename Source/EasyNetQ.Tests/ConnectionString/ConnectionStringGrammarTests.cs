// ReSharper disable InconsistentNaming

using System.Linq;
using EasyNetQ.ConnectionString;
using NUnit.Framework;
using Sprache;

namespace EasyNetQ.Tests.ConnectionString
{
    [TestFixture]
    public class ConnectionStringGrammarTests
    {
        [SetUp]
        public void SetUp()
        {
        }

        [Test]
        public void Should_parse_host()
        {
            var host = ConnectionStringGrammar.Host.Parse("my.host.com:1234");

            host.Host.ShouldEqual("my.host.com");
            host.Port.ShouldEqual(1234);
        }

        [Test]
        public void Should_parse_host_without_port()
        {
            var host = ConnectionStringGrammar.Host.Parse("my.host.com");

            host.Host.ShouldEqual("my.host.com");
            host.Port.ShouldEqual(0);
        }

        [Test]
        public void Should_parse_list_of_hosts()
        {
            var hosts = ConnectionStringGrammar.Hosts.Parse("host.one:1001,host.two:1002,host.three:1003");

            hosts.Count().ShouldEqual(3);
            hosts.ElementAt(0).Host.ShouldEqual("host.one");
            hosts.ElementAt(0).Port.ShouldEqual(1001);
            hosts.ElementAt(1).Host.ShouldEqual("host.two");
            hosts.ElementAt(1).Port.ShouldEqual(1002);
            hosts.ElementAt(2).Host.ShouldEqual("host.three");
            hosts.ElementAt(2).Port.ShouldEqual(1003);
        }

        [Test]
        public void Should_parse_amqp()
        {
            var hosts = ConnectionStringGrammar.AMQP.Parse("amqp://localhost/");
            
            hosts.Port.ShouldEqual(-1);
            hosts.Host.ShouldEqual("localhost");
        } 
        
        [Test]
        public void Should_try_to_parse_amqp()
        {
            Assert.That(() => ConnectionStringGrammar.AMQP.Parse("asd"),
                        Throws.InstanceOf<ParseException>()
                        .With.Message.Contains("asd"));
        }

        [Test]
        public void Should_throw_when_parsing_empty()
        {
            Assert.That(() => ConnectionStringGrammar.ConnectionStringBuilder.Parse(""), Throws.InstanceOf<ParseException>());
        }
    }


}

// ReSharper restore InconsistentNaming