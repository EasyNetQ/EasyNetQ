// ReSharper disable InconsistentNaming

using System;
using System.Text;
using EasyNetQ.SystemMessages;
using Xunit;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using EasyNetQ.Tests;

namespace EasyNetQ.Hosepipe.Tests
{
    public class ErrorMessageRepublishSpike
    {
        readonly ISerializer serializer = new JsonSerializer();

        [Fact]
        public void Should_deserialise_error_message_correctly()
        {
            var error = serializer.BytesToMessage<Error>(Encoding.UTF8.GetBytes(errorMessage));

            error.RoutingKey.ShouldEqual("originalRoutingKey");
            error.Message.ShouldEqual("{ Text:\"Hello World\"}");
        }

        [Fact]
        public void Should_fail_to_deseralize_some_other_random_message()
        {
            const string randomMessage = "{\"Text\":\"Hello World\"}";
            var error = serializer.BytesToMessage<Error>(Encoding.UTF8.GetBytes(randomMessage));
            error.Message.ShouldBeNull();
        }

        [Fact][Explicit("Requires a localhost instance of RabbitMQ to run")]
        public void Should_be_able_to_republish_message()
        {
            var error = serializer.BytesToMessage<Error>(Encoding.UTF8.GetBytes(errorMessage));

            var connectionFactory = new ConnectionFactory
            {
                HostName = "localhost",
                UserName = "guest",
                Password = "guest"
            };

            using (var connection = connectionFactory.CreateConnection())
            using (var model = connection.CreateModel())
            {
                try
                {
                    model.ExchangeDeclarePassive(error.Exchange);

                    var properties = model.CreateBasicProperties();
                    error.BasicProperties.CopyTo(properties);

                    var body = Encoding.UTF8.GetBytes(error.Message);

                    model.BasicPublish(error.Exchange, error.RoutingKey, properties, body);
                }
                catch (OperationInterruptedException)
                {
                    Console.WriteLine("The exchange, '{0}', described in the error message does not exist on '{1}', '{2}'",
                        error.Exchange, connectionFactory.HostName, connectionFactory.VirtualHost);
                }
            }
        }

        private const string errorMessage = 
@"{
    ""RoutingKey"":""originalRoutingKey"",
    ""Exchange"":""orginalExchange"",
    ""Exception"":""System.Exception: I just threw!"",
    ""Message"":""{ Text:\""Hello World\""}"",
    ""DateTime"":""\/Date(1312196313848+0100)\/"",
    ""BasicProperties"":{
        ""ContentTypePresent"":false,
        ""ContentEncodingPresent"":false,
        ""HeadersPresent"":false,
        ""DeliveryModePresent"":false,
        ""PriorityPresent"":false,
        ""CorrelationIdPresent"":true,
        ""ReplyToPresent"":false,
        ""ExpirationPresent"":false,
        ""MessageIdPresent"":false,
        ""TimestampPresent"":false,
        ""TypePresent"":false,
        ""UserIdPresent"":false,
        ""AppIdPresent"":true,
        ""ClusterIdPresent"":false,
        ""ContentType"":null,
        ""ContentEncoding"":null,
        ""Headers"":{},
        ""DeliveryMode"":0,
        ""Priority"":0,
        ""CorrelationId"":""123"",
        ""ReplyTo"":null,
        ""Expiration"":null,
        ""MessageId"":null,
        ""Timestamp"":0,
        ""Type"":null,
        ""UserId"":null,
        ""AppId"":""456"",
        ""ClusterId"":null}}";
    }
}

// ReSharper restore InconsistentNaming