using EasyNetQ.Interception;

namespace EasyNetQ.Tests.Interception;

public class BuildInInterceptorsTests
{
    [Fact]
    public void ShouldCompressAndDecompress()
    {
        var interceptor = new GZipInterceptor();
        var body = "haha"u8.ToArray();
        var outgoingMessage = new PublishMessage(new MessageProperties(), body);
        var message = interceptor.OnPublish(outgoingMessage);
        var incomingMessage = new ConsumeMessage(new MessageReceivedInfo("", 0, false, "exchange", "routingKey", "queue"), message.Properties, message.Body);
        Assert.Equal(body, interceptor.OnConsume(incomingMessage).Body.ToArray());
    }

    [Fact]
    public void ShouldEncryptAndDecrypt()
    {
        var interceptor = new TripleDESInterceptor(Convert.FromBase64String("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"), Convert.FromBase64String("aaaaaaaaaaa="));
        var body = "haha"u8.ToArray();
        var outgoingMessage = new PublishMessage(new MessageProperties(), body);
        var message = interceptor.OnPublish(outgoingMessage);
        var incomingMessage = new ConsumeMessage(new MessageReceivedInfo("", 0, false, "exchange", "routingKey", "queue"), message.Properties, message.Body);
        Assert.Equal(body, interceptor.OnConsume(incomingMessage).Body.ToArray());
    }

    [Fact]
    public void ShouldCallAddedInterceptorsOnProduce()
    {
        var sourceMessage = new PublishMessage(new MessageProperties(), Array.Empty<byte>());
        var firstMessage = new PublishMessage(new MessageProperties(), Array.Empty<byte>());
        var secondMessage = new PublishMessage(new MessageProperties(), Array.Empty<byte>());

        var first = Substitute.For<IPublishConsumeInterceptor>();
        var second = Substitute.For<IPublishConsumeInterceptor>();
        first.OnPublish(sourceMessage).Returns(firstMessage);
        second.OnPublish(firstMessage).Returns(secondMessage);
        var composite = new CompositePublishConsumerInterceptor(new[] { first, second });
        Assert.Equal(secondMessage, composite.OnPublish(sourceMessage));
    }

    [Fact]
    public void ShouldCallAddedInterceptorsOnConsume()
    {
        var sourceMessage = new ConsumeMessage(new MessageReceivedInfo("", 0, false, "exchange", "routingKey", "queue"), new MessageProperties(),
            Array.Empty<byte>());
        var firstMessage = new ConsumeMessage(new MessageReceivedInfo("", 0, false, "exchange", "routingKey", "queue"), new MessageProperties(),
            Array.Empty<byte>());
        var secondMessage = new ConsumeMessage(new MessageReceivedInfo("", 0, false, "exchange", "routingKey", "queue"), new MessageProperties(),
            Array.Empty<byte>());

        var first = Substitute.For<IPublishConsumeInterceptor>();
        var second = Substitute.For<IPublishConsumeInterceptor>();
        first.OnConsume(secondMessage).Returns(firstMessage);
        second.OnConsume(sourceMessage).Returns(secondMessage);
        var composite = new CompositePublishConsumerInterceptor(new[] { first, second });
        Assert.Equal(firstMessage, composite.OnConsume(sourceMessage));
    }
}
