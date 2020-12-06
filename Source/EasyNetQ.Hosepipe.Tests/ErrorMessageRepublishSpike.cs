// ReSharper disable InconsistentNaming

using EasyNetQ.SystemMessages;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace EasyNetQ.Hosepipe.Tests
{
    public class ErrorMessageRepublishSpike
    {
        private readonly ITestOutputHelper testOutputHelper;
        private readonly ISerializer serializer = new JsonSerializer();

        public ErrorMessageRepublishSpike(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void Should_deserialize_error_message_correctly()
        {
            var error = (Error)serializer.BytesToMessage(typeof(Error), Encoding.UTF8.GetBytes(errorMessage));

            error.RoutingKey.ShouldEqual("originalRoutingKey");
            error.Message.ShouldEqual("{ Text:\"Hello World\"}");
        }

        [Fact]
        public void Should_fail_to_deserialize_some_other_random_message()
        {
            const string randomMessage = "{\"Text\":\"Hello World\"}";
            var error = (Error)serializer.BytesToMessage(typeof(Error), Encoding.UTF8.GetBytes(randomMessage));
            error.Message.ShouldBeNull();
        }

        [Fact][Traits.Explicit("Requires a localhost instance of RabbitMQ to run")]
        public void Should_be_able_to_republish_message()
        {
            var error = (Error)serializer.BytesToMessage(typeof(Error), Encoding.UTF8.GetBytes(errorMessage));

            var connectionFactory = new ConnectionFactory
            {
                HostName = "localhost",
                UserName = "guest",
                Password = "guest"
            };

            using var connection = connectionFactory.CreateConnection();
            using var model = connection.CreateModel();
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
                testOutputHelper.WriteLine("The exchange, '{0}', described in the error message does not exist on '{1}', '{2}'", error.Exchange, connectionFactory.HostName, connectionFactory.VirtualHost);
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
