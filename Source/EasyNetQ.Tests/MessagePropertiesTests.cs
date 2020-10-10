// ReSharper disable InconsistentNaming
using FluentAssertions;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace EasyNetQ.Tests
{
    public class MessagePropertiesTests
    {
        [Fact]
        public void Should_copy_from_Rabbit_client_properties()
        {
            const string replyTo = "reply to";

            var properties = new MessageProperties();
            var originalProperties = new BasicProperties { ReplyTo = replyTo };

            properties.CopyFrom(originalProperties);

            properties.ReplyTo.Should().Be(replyTo);
        }

        [Fact]
        public void Should_copy_to_rabbit_client_properties()
        {
            const string replyTo = "reply to";

            var properties = new MessageProperties { ReplyTo = replyTo };
            var destinationProperties = new BasicProperties();

            properties.CopyTo(destinationProperties);

            destinationProperties.ReplyTo.Should().Be(replyTo);
            destinationProperties.IsReplyToPresent().Should().BeTrue();
            destinationProperties.IsMessageIdPresent().Should().BeFalse();
        }

        [Fact]
        public void Should_clone()
        {
            const string replyTo = "reply to";

            var properties = new MessageProperties
            {
                ReplyTo = replyTo,
                Headers = new Dictionary<string, object>
                          {
                              { "AString", "ThisIsAString" },
                              { "AnInt", 123 }
                          }
                };

            var destinationProperties = (MessageProperties)properties.Clone();

            destinationProperties.ReplyTo.Should().Be(replyTo);
            destinationProperties.ReplyToPresent.Should().BeTrue();
            destinationProperties.MessageIdPresent.Should().BeFalse();
            destinationProperties.Headers.Should().BeEquivalentTo(properties.Headers);
        }

        [Fact]
        public void Should_be_able_to_write_debug_properties()
        {
            const string expectedDebugProperties =
                "ContentType=content_type, ContentEncoding=content_encoding, " +
                "Headers=[key1=value1, key2=value2], DeliveryMode=10, Priority=3, CorrelationId=NULL, " +
                "ReplyTo=reply_to, Expiration=expiration, MessageId=message_id, Timestamp=123456, Type=type, " +
                "UserId=userid, AppId=app_id, ClusterId=cluster_id";

            var headers = new Dictionary<string, object>
                {
                    { "key1", "value1" },
                    { "key2", "value2" }
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

            properties.ToString().Should().Be(expectedDebugProperties);
        }

        [Fact]
        public void Should_throw_if_any_string_property_exceeds_255_chars()
        {
            var longInput = new string('*', 256);

            var properties = new MessageProperties();
            var stringFields = properties.GetType().GetProperties().Where(x => x.PropertyType == typeof(string));
            foreach (var propertyInfo in stringFields)
            {
                var threw = false;
                try
                {
                    propertyInfo.SetValue(properties, longInput, new object[0]);
                }
                catch (TargetInvocationException exception)
                {
                    if (exception.InnerException is EasyNetQException)
                    {
                        // Console.Out.WriteLine(exception.InnerException.Message);
                        threw = true;
                    }
                    else
                    {
                        throw;
                    }
                }
                if (!threw)
                {
                    Assert.True(false, "Over length property set didn't fail");
                }
            }
        }
    }
}

// ReSharper restore InconsistentNaming
