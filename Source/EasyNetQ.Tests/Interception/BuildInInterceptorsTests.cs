using System;
using System.Text;
using EasyNetQ.Interception;
using Xunit;
using NSubstitute;

namespace EasyNetQ.Tests.Interception
{
    public class BuildInInterceptorsTests
    {
        [Fact]
        public void ShouldCompressAndDecompress()
        {
            var gZipInterceptor = new GZipInterceptor();
            var body = Encoding.UTF8.GetBytes("haha");
            var rawMessage = new RawMessage(new MessageProperties(), body);
            Assert.Equal(body, gZipInterceptor.OnConsume(gZipInterceptor.OnProduce(rawMessage)).Body);
        }


        [Fact]
        public void ShouldEncryptAndDecrypt()
        {
            var tripleDESInterceptor = new TripleDESInterceptor(Convert.FromBase64String("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"), Convert.FromBase64String("aaaaaaaaaaa="));
            var body = Encoding.UTF8.GetBytes("haha");
            var rawMessage = new RawMessage(new MessageProperties(), body);
            Assert.Equal(body, tripleDESInterceptor.OnConsume(tripleDESInterceptor.OnProduce(rawMessage)).Body);
        }

        [Fact]
        public void ShouldCallAddedInterceptorsOnProduce()
        {
            var sourceMessage = new RawMessage(new MessageProperties(), new byte[0]);
            var firstMessage = new RawMessage(new MessageProperties(), new byte[0]);
            var secondMessage = new RawMessage(new MessageProperties(), new byte[0]);
            
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
            var sourceMessage = new RawMessage(new MessageProperties(), new byte[0]);
            var firstMessage = new RawMessage(new MessageProperties(), new byte[0]);
            var secondMessage = new RawMessage(new MessageProperties(), new byte[0]);
            
            
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