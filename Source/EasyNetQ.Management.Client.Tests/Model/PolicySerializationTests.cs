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
            _policy = ResourceLoader.LoadObjectFromJson<Policy[]>("Policies_ha.json", ManagementClient.Settings);
        }

        [Test]
        public void Should_read_exactly_ha_properly()
        {
            _policy.Count().ShouldEqual(2);
            var exactlyPolicies = _policy.Where(p => p.Name == "ha-duplicate");
            Assert.AreEqual(1, exactlyPolicies.Count());
            var policy = exactlyPolicies.First();
            Assert.AreEqual(HaMode.Exactly, policy.Definition.HaMode);
            Assert.AreEqual(HaMode.Exactly, policy.Definition.HaParams.AssociatedHaMode);
            Assert.AreEqual(2, policy.Definition.HaParams.ExactlyCount);
            Assert.AreEqual("^dup.*", policy.Pattern);
            Assert.AreEqual(HaSyncMode.Manual, policy.Definition.HaSyncMode);
            Assert.AreEqual(1, policy.Priority);
        }

        [Test]
        public void Should_read_nodes_ha_properly()
        {
            _policy.Count().ShouldEqual(2);
            var mirrorTestPolicies = _policy.Where(p => p.Name == "mirror_test");
            Assert.AreEqual(1, mirrorTestPolicies.Count());
            var policy = mirrorTestPolicies.First();
            Assert.AreEqual(HaMode.Nodes, policy.Definition.HaMode);
            Assert.AreEqual(HaMode.Nodes, policy.Definition.HaParams.AssociatedHaMode);
            Assert.AreEqual(new[] { "rabbit@rab5", "rabbit@rab6" }, policy.Definition.HaParams.Nodes);
            Assert.AreEqual("mirror", policy.Pattern);
            Assert.AreEqual(HaSyncMode.Automatic, policy.Definition.HaSyncMode);
            Assert.AreEqual(0, policy.Priority);
        }
    }
}