using System;
using System.Text;
using EasyNetQ.Interception;
using NUnit.Framework;
using Rhino.Mocks;

namespace EasyNetQ.Tests.Interception
{
    [TestFixture]
    public class BuildInInterceptorsTests : UnitTestBase
    {
        [Test]
        public void ShouldCompressAndDecompress()
        {
            var gZipInterceptor = new GZipInterceptor();
            var body = Encoding.UTF8.GetBytes("haha");
            var rawMessage = new RawMessage(new MessageProperties(), body);
            Assert.AreEqual(body, gZipInterceptor.OnConsume(gZipInterceptor.OnProduce(rawMessage)).Body);
        }


        [Test]
        public void ShouldEncryptAndDecrypt()
        {
            var tripleDESInterceptor = new TripleDESInterceptor(Convert.FromBase64String("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"), Convert.FromBase64String("aaaaaaaaaaa="));
            var body = Encoding.UTF8.GetBytes("haha");
            var rawMessage = new RawMessage(new MessageProperties(), body);
            Assert.AreEqual(body, tripleDESInterceptor.OnConsume(tripleDESInterceptor.OnProduce(rawMessage)).Body);
        }

        [Test]
        public void ShouldCallAddedInterceptorsOnProduce()
        {
            var sourceMessage = new RawMessage(new MessageProperties(), new byte[0]);
            var firstMessage = new RawMessage(new MessageProperties(), new byte[0]);
            var secondMessage = new RawMessage(new MessageProperties(), new byte[0]);
            
            var first = NewMock<IProduceConsumeInterceptor>();
            var second = NewMock<IProduceConsumeInterceptor>();
            first.Expect(x => x.OnProduce(sourceMessage)).Return(firstMessage);
            second.Expect(x => x.OnProduce(firstMessage)).Return(secondMessage);

            var compositeInterceptor = new CompositeInterceptor();
            compositeInterceptor.Add(first);
            compositeInterceptor.Add(second);
            Assert.AreEqual(secondMessage, compositeInterceptor.OnProduce(sourceMessage));
        }

        [Test]
        public void ShouldCallAddedInterceptorsOnConsume()
        {
            var sourceMessage = new RawMessage(new MessageProperties(), new byte[0]);
            var firstMessage = new RawMessage(new MessageProperties(), new byte[0]);
            var secondMessage = new RawMessage(new MessageProperties(), new byte[0]);
            
            
            var first = NewMock<IProduceConsumeInterceptor>();
            
            var second = NewMock<IProduceConsumeInterceptor>();
            first.Expect(x => x.OnConsume(secondMessage)).Return(firstMessage);
            second.Expect(x => x.OnConsume(sourceMessage)).Return(secondMessage);

            var compositeInterceptor = new CompositeInterceptor();
            compositeInterceptor.Add(first);
            compositeInterceptor.Add(second);
            Assert.AreEqual(firstMessage, compositeInterceptor.OnConsume(sourceMessage));
        }
    }
}