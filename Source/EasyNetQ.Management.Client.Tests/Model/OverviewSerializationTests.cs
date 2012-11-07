// ReSharper disable InconsistentNaming

using EasyNetQ.Management.Client.Model;
using NUnit.Framework;
using Newtonsoft.Json;

namespace EasyNetQ.Management.Client.Tests.Model
{
    [TestFixture]
    public class OverviewSerializationTests
    {
        private Overview overview;

        [SetUp]
        public void SetUp()
        {
            overview = ResourceLoader.LoadObjectFromJson<Overview>("Overview.json");
        }

        [Test]
        public void Should_contain_management_version()
        {
            overview.management_version.ShouldEqual("2.8.6");
            overview.statistics_level.ShouldEqual("fine");
        }

        [Test]
        public void Should_congtain_exchange_types()
        {
            overview.exchange_types[0].name.ShouldEqual("topic");
            overview.exchange_types[0].description.ShouldEqual("AMQP topic exchange, as per the AMQP specification");
            overview.exchange_types[0].enabled.ShouldBeTrue();
        }
    }
}

// ReSharper restore InconsistentNaming