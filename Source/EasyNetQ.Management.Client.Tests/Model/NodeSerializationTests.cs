// ReSharper disable InconsistentNaming

using System.Collections.Generic;
using EasyNetQ.Management.Client.Model;
using NUnit.Framework;

namespace EasyNetQ.Management.Client.Tests.Model
{
    [TestFixture]
    public class NodeSerializationTests
    {
        private List<Node> nodes;

        [SetUp]
        public void SetUp()
        {
            nodes = ResourceLoader.LoadObjectFromJson<List<Node>>("Nodes.json");
        }

        [Test]
        public void Should_load_one_node()
        {
            nodes.Count.ShouldEqual(1);
        }

        [Test]
        public void Should_have_node_properties()
        {
            var node = nodes[0];

            node.Name.ShouldEqual("rabbit@THOMAS");
            node.Uptime.ShouldEqual(98495039);
        }
    }
}

// ReSharper restore InconsistentNaming