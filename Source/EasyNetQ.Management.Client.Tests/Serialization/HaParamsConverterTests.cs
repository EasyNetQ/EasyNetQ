namespace EasyNetQ.Management.Client.Tests.Serialization
{
    using Client.Model;
    using Client.Serialization;
    using Newtonsoft.Json;
    using NUnit.Framework;

    [TestFixture]
    class HaParamsConverterTests
    {
        [Test]
        public void Should_only_handle_HaParams()
        {
            Assert.IsTrue(new HaParamsConverter().CanConvert(typeof(HaParams)));
            Assert.IsFalse(new HaParamsConverter().CanConvert(typeof(HaMode)));
        }

        [Test]
        public void Should_deserialize_exactly_count()
        {
            var deserializedObject = JsonConvert.DeserializeObject<HaParams>("5", new HaParamsConverter());
            Assert.NotNull(deserializedObject);
            Assert.AreEqual(HaMode.Exactly, deserializedObject.AssociatedHaMode);
            Assert.AreEqual(5L, deserializedObject.ExactlyCount);
        }

        [Test]
        public void Should_deserialize_nodes_list()
        {
            var deserializedObject = JsonConvert.DeserializeObject<HaParams>("[\"a\", \"b\"]", new HaParamsConverter());
            Assert.NotNull(deserializedObject);
            Assert.AreEqual(HaMode.Nodes, deserializedObject.AssociatedHaMode);
            Assert.NotNull(deserializedObject.Nodes);
            Assert.AreEqual(2, deserializedObject.Nodes.Length);
            Assert.Contains("a", deserializedObject.Nodes);
            Assert.Contains("b", deserializedObject.Nodes);
        }

        [Test]
        [ExpectedException(typeof(JsonSerializationException))]
        public void Should_not_be_able_to_deserialize_non_string_list()
        {
            JsonConvert.DeserializeObject<HaParams>("[1,2]", new HaParamsConverter());
        }

        [Test]
        public void Should_be_able_to_serialize_count()
        {
            Assert.AreEqual("2", JsonConvert.SerializeObject(new HaParams{AssociatedHaMode = HaMode.Exactly, ExactlyCount = 2}, new HaParamsConverter()));
        }

        [Test]
        public void Should_be_able_to_serialize_list()
        {
            Assert.AreEqual(JsonConvert.SerializeObject(new[] { "a", "b" }), JsonConvert.SerializeObject(new HaParams { AssociatedHaMode = HaMode.Nodes, Nodes = new[] { "a", "b" } }, new HaParamsConverter()));
        }

        [Test]
        [ExpectedException(typeof(JsonSerializationException))]
        public void Should_not_be_able_to_serialize_null_nodes_list()
        {
            JsonConvert.SerializeObject(new HaParams { AssociatedHaMode = HaMode.Nodes }, new HaParamsConverter());
        }

        [Test]
        [ExpectedException(typeof(JsonSerializationException))]
        public void Should_not_be_able_to_serialize_unset_type()
        {
            JsonConvert.SerializeObject(new HaParams { AssociatedHaMode = HaMode.Unset, Nodes = new[] { "a", "b" } }, new HaParamsConverter());
        }

        [Test]
        [ExpectedException(typeof(JsonSerializationException))]
        public void Should_not_be_able_to_serialize_all_type()
        {
            JsonConvert.SerializeObject(new HaParams { AssociatedHaMode = HaMode.All, Nodes = new[] { "a", "b" } }, new HaParamsConverter());
        }
    }
}
