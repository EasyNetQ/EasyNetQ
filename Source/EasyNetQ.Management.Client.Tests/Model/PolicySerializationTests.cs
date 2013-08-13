namespace EasyNetQ.Management.Client.Tests.Model
{
    using System.Linq;
    using Client.Model;
    using NUnit.Framework;

    [TestFixture]
    public class PolicySerializationTests
    {
        private Policy[] _policy;

        [SetUp]
        public void SetUp()
        {
            _policy = ResourceLoader.LoadObjectFromJson<Policy[]>("Policies_ha_exactly.json", ManagementClient.Settings);
        }

        [Test]
        public void Should_read_exactly_ha_properly()
        {
            _policy.Count().ShouldEqual(1);
            var firstPolicy = _policy.First();
            firstPolicy.Definition.ShouldNotBeNull();
            firstPolicy.Definition.HaMode.ShouldEqual(HaMode.Exactly);
            firstPolicy.Definition.HaParams.ExactlyCount.ShouldEqual(2);
        }
    }
}