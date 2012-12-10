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
            overview.ManagementVersion.ShouldEqual("2.8.6");
            overview.StatisticsLevel.ShouldEqual("fine");
        }

        [Test]
        public void Should_congtain_exchange_types()
        {
            overview.ExchangeTypes[0].Name.ShouldEqual("topic");
            overview.ExchangeTypes[0].Description.ShouldEqual("AMQP topic exchange, as per the AMQP specification");
            overview.ExchangeTypes[0].Enabled.ShouldBeTrue();
        }
    }
}

// ReSharper restore InconsistentNaming