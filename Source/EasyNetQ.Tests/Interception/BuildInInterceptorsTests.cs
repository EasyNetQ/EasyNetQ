using EasyNetQ.Interception;
using NSubstitute;
using System;
using System.Text;
using Xunit;

namespace EasyNetQ.Tests.Interception
{
    public class BuildInInterceptorsTests
    {
        [Fact]
        public void ShouldCompressAndDecompress()
        {
            var interceptor = new GZipInterceptor();
            var body = Encoding.UTF8.GetBytes("haha");
            var outgoingMessage = new ProducedMessage(new MessageProperties(), body);
            var message = interceptor.OnProduce(outgoingMessage);
            var incomingMessage = new ConsumedMessage(null, message.Properties, message.Body);
            Assert.Equal(body, interceptor.OnConsume(incomingMessage).Body);
        }

        [Fact]
        public void ShouldEncryptAndDecrypt()
        {
            var interceptor = new TripleDESInterceptor(Convert.FromBase64String("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"), Convert.FromBase64String("aaaaaaaaaaa="));
            var body = Encoding.UTF8.GetBytes("haha");
            var outgoingMessage = new ProducedMessage(new MessageProperties(), body);
            var message = interceptor.OnProduce(outgoingMessage);
            var incomingMessage = new ConsumedMessage(null, message.Properties, message.Body);
            Assert.Equal(body, interceptor.OnConsume(incomingMessage).Body);
        }

        [Fact]
        public void ShouldCallAddedInterceptorsOnProduce()
        {
            var sourceMessage = new ProducedMessage(new MessageProperties(), new byte[0]);
            var firstMessage = new ProducedMessage(new MessageProperties(), new byte[0]);
            var secondMessage = new ProducedMessage(new MessageProperties(), new byte[0]);

            var first = Substitute.For<IProduceConsumeInterceptor>();
            var second = Substitute.For<IProduceConsumeInterceptor>();
            first.OnProduce(sourceMessage).Returns(firstMessage);
            second.OnProduce(firstMessage).Returns(secondMessage);

            var compositeInterceptor = new CompositeInterceptor();
            compositeInterceptor.Add(first);
            compositeInterceptor.Add(second);
            Assert.Equal(secondMessage, compositeInterceptor.OnProduce(sourceMessage));
        }

        [Fact]
        public void ShouldCallAddedInterceptorsOnConsume()
        {
            var sourceMessage = new ConsumedMessage(null, new MessageProperties(), new byte[0]);
            var firstMessage = new ConsumedMessage(null, new MessageProperties(), new byte[0]);
            var secondMessage = new ConsumedMessage(null, new MessageProperties(), new byte[0]);

            var first = Substitute.For<IProduceConsumeInterceptor>();
            var second = Substitute.For<IProduceConsumeInterceptor>();
            first.OnConsume(secondMessage).Returns(firstMessage);
            second.OnConsume(sourceMessage).Returns(secondMessage);

            var compositeInterceptor = new CompositeInterceptor();
            compositeInterceptor.Add(first);
            compositeInterceptor.Add(second);
            Assert.Equal(firstMessage, compositeInterceptor.OnConsume(sourceMessage));
        }
    }
}
