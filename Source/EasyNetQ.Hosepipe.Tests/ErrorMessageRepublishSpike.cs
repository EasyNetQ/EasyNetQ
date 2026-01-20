// ReSharper disable InconsistentNaming

using EasyNetQ.SystemMessages;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System.Text;

namespace EasyNetQ.Hosepipe.Tests;

public class ErrorMessageRepublishSpike
{
    private readonly ITestOutputHelper testOutputHelper;
    private readonly ISerializer serializer = new Serialization.NewtonsoftJson.NewtonsoftJsonSerializer();

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
    [Traits.Explicit("Requires a localhost instance of RabbitMQ to run")]
    public async Task Should_be_able_to_republish_message()
    {
        var error = (Error)serializer.BytesToMessage(typeof(Error), Encoding.UTF8.GetBytes(errorMessage));

        var connectionFactory = new ConnectionFactory
        {
            HostName = "localhost",
            UserName = "guest",
            Password = "guest"
        };

        using var connection = await connectionFactory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();
        try
        {
            await channel.ExchangeDeclarePassiveAsync(error.Exchange);

            var properties = new BasicProperties();
            error.BasicProperties.CopyTo(properties);

            var body = Encoding.UTF8.GetBytes(error.Message);

            await channel.BasicPublishAsync(error.Exchange, error.RoutingKey, false, properties, body);
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
    ""Queue"":""originalQueue"",
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

// ReSharper restore InconsistentNaming
