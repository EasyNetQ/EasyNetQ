// ReSharper disable InconsistentNaming

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using RabbitMQ.Client.Framing.v0_9_1;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class MessagePropertiesTests
    {
        [SetUp]
        public void SetUp()
        {
        }

        [Test]
        public void Should_copy_from_Rabbit_client_properties()
        {
            const string replyTo = "reply to";

            var properties = new MessageProperties();
            var originalProperties = new BasicProperties {ReplyTo = replyTo};

            properties.CopyFrom(originalProperties);

            properties.ReplyTo.ShouldEqual(replyTo);
        }

        [Test]
        public void Should_copy_to_rabbit_client_properties()
        {
            const string replyTo = "reply to";

            var properties = new MessageProperties { ReplyTo = replyTo };
            var destinationProperties = new BasicProperties();

            properties.CopyTo(destinationProperties);

            destinationProperties.ReplyTo.ShouldEqual(replyTo);
            destinationProperties.IsReplyToPresent().ShouldBeTrue();
            destinationProperties.IsMessageIdPresent().ShouldBeFalse();
        }

        [Test]
        public void Should_be_able_to_write_debug_properties()
        {
            const string expectedDebugProperties = 
                "ContentType=content_type, ContentEncoding=content_encoding, " + 
                "Headers=[key1=value1, key2=value2], DeliveryMode=10, Priority=3, CorrelationId=NULL, " + 
                "ReplyTo=reply_to, Expiration=expiration, MessageId=message_id, Timestamp=123456, Type=type, " + 
                "UserId=userid, AppId=app_id, ClusterId=cluster_id";

            var stringBuilder = new StringBuilder();
            var headers = new Hashtable
                {
                    {"key1", "value1"},
                    {"key2", "value2"}
                };

            var properties = new MessageProperties
                {
                    AppId = "app_id",
                    ClusterId = "cluster_id",
                    ContentEncoding = "content_encoding",
                    ContentType = "content_type",
                    //CorrelationId = "correlation_id",
                    DeliveryMode = 10,
                    Expiration = "expiration",
                    Headers = headers,
                    MessageId = "message_id",
                    Priority = 3,
                    ReplyTo = "reply_to",
                    Timestamp = 123456,
                    Type = "type",
                    UserId = "userid",
                };

            properties.AppendPropertyDebugStringTo(stringBuilder);

            stringBuilder.ToString().ShouldEqual(expectedDebugProperties);
        }
    }
}

// ReSharper restore InconsistentNaming