using EasyNetQ.Interception;
using NSubstitute;
using System;
using System.Text;
using Xunit;

namespace EasyNetQ.Tests.Interception;

public class BuildInInterceptorsTests
{
    [Fact]
    public void ShouldCompressAndDecompress()
    {
        var interceptor = new GZipInterceptor();
        var body = Encoding.UTF8.GetBytes("haha");
        var outgoingMessage = new ProducedMessage(new MessageProperties(), body);
        var message = interceptor.OnProduce(outgoingMessage);
        var incomingMessage = new ConsumedMessage(new MessageReceivedInfo("", 0, false, "exchange", "routingKey", "queue"), message.Properties, message.Body);
        Assert.Equal(body, interceptor.OnConsume(incomingMessage).Body.ToArray());
    }

    [Fact]
    public void ShouldEncryptAndDecrypt()
    {
        var interceptor = new TripleDESInterceptor(Convert.FromBase64String("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"), Convert.FromBase64String("aaaaaaaaaaa="));
        var body = Encoding.UTF8.GetBytes("haha");
        var outgoingMessage = new ProducedMessage(new MessageProperties(), body);
        var message = interceptor.OnProduce(outgoingMessage);
        var incomingMessage = new ConsumedMessage(new MessageReceivedInfo("", 0, false, "exchange", "routingKey", "queue"), message.Properties, message.Body);
        Assert.Equal(body, interceptor.OnConsume(incomingMessage).Body.ToArray());
    }

    [Fact]
    public void ShouldCallAddedInterceptorsOnProduce()
    {
        var sourceMessage = new ProducedMessage(new MessageProperties(), Array.Empty<byte>());
        var firstMessage = new ProducedMessage(new MessageProperties(), Array.Empty<byte>());
        var secondMessage = new ProducedMessage(new MessageProperties(), Array.Empty<byte>());

        var first = Substitute.For<IProduceConsumeInterceptor>();
        var second = Substitute.For<IProduceConsumeInterceptor>();
        first.OnProduce(sourceMessage).Returns(firstMessage);
        second.OnProduce(firstMessage).Returns(secondMessage);
        var composite = new CompositeProduceConsumerInterceptor(new[] { first, second });
        Assert.Equal(secondMessage, composite.OnProduce(sourceMessage));
    }

    [Fact]
    public void ShouldCallAddedInterceptorsOnConsume()
    {
        var sourceMessage = new ConsumedMessage(new MessageReceivedInfo("", 0, false, "exchange", "routingKey", "queue"), new MessageProperties(), Array.Empty<byte>());
        var firstMessage = new ConsumedMessage(new MessageReceivedInfo("", 0, false, "exchange", "routingKey", "queue"), new MessageProperties(), Array.Empty<byte>());
        var secondMessage = new ConsumedMessage(new MessageReceivedInfo("", 0, false, "exchange", "routingKey", "queue"), new MessageProperties(), Array.Empty<byte>());

        var first = Substitute.For<IProduceConsumeInterceptor>();
        var second = Substitute.For<IProduceConsumeInterceptor>();
        first.OnConsume(secondMessage).Returns(firstMessage);
        second.OnConsume(sourceMessage).Returns(secondMessage);
        var composite = new CompositeProduceConsumerInterceptor(new[] { first, second });
        Assert.Equal(firstMessage, composite.OnConsume(sourceMessage));
    }
}
