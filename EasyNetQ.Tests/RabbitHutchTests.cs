// ReSharper disable InconsistentNaming
using NUnit.Framework;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class RabbitHutchTests
    {
        [SetUp]
        public void SetUp() {}

        [Test]
        public void Should_create_the_correct_rabbit_host_with_default_vhost()
        {
            var rabbitHost = RabbitHutch.GetRabbitHost("localhost");

            rabbitHost.HostName.ShouldEqual("localhost");
            rabbitHost.VirtualHost.ShouldEqual("/");
        }

        [Test]
        public void Should_create_correct_rabbit_host()
        {
            var rabbitHost = RabbitHutch.GetRabbitHost("myserver/myvhost");

            rabbitHost.HostName.ShouldEqual("myserver");
            rabbitHost.VirtualHost.ShouldEqual("myvhost");
        }

        [Test]
        [ExpectedException(typeof(EasyNetQException), 
            ExpectedMessage = @"hostname has too many parts, expecting '<server>/<vhost>' but was: 'one/two/three'")]
        public void Should_throw_if_hostname_has_too_many_parts()
        {
            RabbitHutch.GetRabbitHost("one/two/three");
        }
    }
}

// ReSharper restore InconsistentNaming