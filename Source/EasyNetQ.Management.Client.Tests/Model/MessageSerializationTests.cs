// ReSharper disable InconsistentNaming

using EasyNetQ.Management.Client.Model;
using EasyNetQ.Management.Client.Serialization;
using NUnit.Framework;
using Newtonsoft.Json;

namespace EasyNetQ.Management.Client.Tests.Model
{
    [TestFixture]
    public class MessageSerializationTests
    {
        [SetUp]
        public void SetUp()
        {
        }

        [Test]
        public void Should_be_able_to_deserialize_message_with_properties()
        {
            const string json = @"{""payload_bytes"":11,""redelivered"":true,""exchange"":""""," + 
                @"""routing_key"":""management_api_test_queue"",""message_count"":1," +
                @"""properties"":{""delivery_mode"":2,""headers"":{""key"":""value""}},""payload"":""Hello World""," +
                @"""payload_encoding"":""string""}";

            var message = JsonConvert.DeserializeObject<Message>(json, new PropertyConverter());

            message.Properties.Count.ShouldEqual(1);
            message.Payload.ShouldEqual("Hello World");
            message.Properties["delivery_mode"].ShouldEqual("2");
            message.Properties.Headers["key"].ShouldEqual("value");
        }

        [Test]
        public void Should_be_able_to_deserialize_message_without_properties()
        {
            const string json = @"{""payload_bytes"":11,""redelivered"":true,""exchange"":""""," +
                @"""routing_key"":""management_api_test_queue"",""message_count"":1," +
                @"""properties"":[],""payload"":""Hello World""," +
                @"""payload_encoding"":""string""}";

            var message = JsonConvert.DeserializeObject<Message>(json, new PropertyConverter());

            message.Properties.Count.ShouldEqual(0);
        }
    }
}