// ReSharper disable InconsistentNaming

using System;
using System.Text;
using System.Threading;
using EasyNetQ.Loggers;
using EasyNetQ.Tests.Mocking;
using NUnit.Framework;
using RabbitMQ.Client.Framing.v0_9_1;

namespace EasyNetQ.Tests.RequestResponseTests
{
    [TestFixture]
    public abstract class RequestResponseTestBase
    {
        protected MockBuilder mockBuilder;

        [SetUp]
        public void SetUp()
        {
            var conventions = new Conventions
            {
                RpcExchangeNamingConvention = () => "rpc_exchange",
                RpcReturnQueueNamingConvention = () => "rpc_return_queue",
                ConsumerTagConvention = () => "the_consumer_tag"
            };

            mockBuilder = new MockBuilder(x => x
                .Register<IEasyNetQLogger>(_ => new ConsoleLogger())
                .Register<IConventions>(_ => conventions)
                );

            AdditionalSetup();
        }

        protected void MakeRequest(Action<MyOtherMessage> responseHandler = null)
        {
            using (var channel = mockBuilder.Bus.OpenPublishChannel())
            {
                var request = new MyMessage { Text = "Hello World!" };

                channel.Request(request, responseHandler ?? (response => { }));
            }
        }

        protected void ReturnResponse()
        {
            var body = Encoding.UTF8.GetBytes("{ \"Text\": \"Hello World!\" }");
            var properties = new BasicProperties
                {
                    Type = "EasyNetQ_Tests_MyOtherMessage:EasyNetQ_Tests"
                };
            var consumerTag = "the_consumer_tag";

            var autoResetEvent = new AutoResetEvent(false);
            var handlerRunning = (HandlerRunner)mockBuilder.ServiceProvider.Resolve<IHandlerRunner>();
            handlerRunning.OnAckSent = () => autoResetEvent.Set();

            mockBuilder.Consumers[0].HandleBasicDeliver(consumerTag, 0, false, "", "rpc_return_queue", properties, body);

            autoResetEvent.WaitOne(500);
        }

        protected abstract void AdditionalSetup();
    }
}

// ReSharper restore InconsistentNaming