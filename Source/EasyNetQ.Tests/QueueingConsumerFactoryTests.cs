// ReSharper disable InconsistentNaming

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Loggers;
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing.v0_9_1;
using Rhino.Mocks;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class QueueingConsumerFactoryTests
    {
        private QueueingConsumerFactory queueingConsumerFactory;
        private IConsumerErrorStrategy consumerErrorStrategy;
        private AutoResetEvent autoResetEvent;
        private const string consumerTag = "abc";
        const ulong deliveryTag = 123;
        

        [SetUp]
        public void SetUp()
        {
            consumerErrorStrategy = MockRepository.GenerateStub<IConsumerErrorStrategy>();
            queueingConsumerFactory = new QueueingConsumerFactory(new ConsoleLogger(), consumerErrorStrategy);
            autoResetEvent = new AutoResetEvent(false);
            queueingConsumerFactory.SynchronisationAction = () => autoResetEvent.Set();
        }

        [Test]
        public void Should_ack_on_successful_message_handler()
        {
            var model = MockRepository.GenerateStub<IModel>();

            var args = CreateBasicDeliverEventArgs(consumerTag, deliveryTag, model, SuccessMessageCallback);

            queueingConsumerFactory.HandleMessageDelivery(args);
            autoResetEvent.WaitOne(1000);

            model.AssertWasCalled(x => x.BasicAck(deliveryTag, false));
        }

        [Test]
        public void Should_ack_on_successful_message_handler_and_ignore_postExcaptionAckStrategy()
        {
            consumerErrorStrategy.Stub(x => x.PostExceptionAckStrategy()).Return(PostExceptionAckStrategy.ShouldNackWithRequeue);
            var model = MockRepository.GenerateStub<IModel>();

            var args = CreateBasicDeliverEventArgs(consumerTag, deliveryTag, model, SuccessMessageCallback);

            queueingConsumerFactory.HandleMessageDelivery(args);
            autoResetEvent.WaitOne(1000);

            model.AssertWasCalled(x => x.BasicAck(deliveryTag, false));
        }

        [Test]
        public void Should_ack_on_error_when_error_strategy_request()
        {
            consumerErrorStrategy.Stub(x => x.PostExceptionAckStrategy()).Return(PostExceptionAckStrategy.ShouldAck);

            var model = MockRepository.GenerateStub<IModel>();

            var args = CreateBasicDeliverEventArgs(consumerTag, deliveryTag, model, ExceptionMessageCallback);

            queueingConsumerFactory.HandleMessageDelivery(args);
            autoResetEvent.WaitOne(1000);

            model.AssertWasCalled(x => x.BasicAck(deliveryTag, false));
            model.AssertWasNotCalled(x => x.BasicNack(Arg<ulong>.Is.Anything, Arg<bool>.Is.Anything, Arg<bool>.Is.Anything));
        }

        [Test]
        public void Should_noAck_with_requeue_on_error_when_error_strategy_requests()
        {
            consumerErrorStrategy.Stub(x => x.PostExceptionAckStrategy()).Return(PostExceptionAckStrategy.ShouldNackWithRequeue);

            var model = MockRepository.GenerateStub<IModel>();

            var args = CreateBasicDeliverEventArgs(consumerTag, deliveryTag, model, ExceptionMessageCallback);

            queueingConsumerFactory.HandleMessageDelivery(args);
            autoResetEvent.WaitOne(1000);

            model.AssertWasCalled(x => x.BasicNack(deliveryTag, false, true));
            model.AssertWasNotCalled(x => x.BasicAck(Arg<ulong>.Is.Anything, Arg<bool>.Is.Anything));
        }

        [Test]
        public void Should_noAck_with_no_requeue_when_error_strategy_requets()
        {
            consumerErrorStrategy.Stub(x => x.PostExceptionAckStrategy()).Return(PostExceptionAckStrategy.ShouldNackWithoutRequeue);

            var model = MockRepository.GenerateStub<IModel>();

            var args = CreateBasicDeliverEventArgs(consumerTag, deliveryTag, model, ExceptionMessageCallback);

            queueingConsumerFactory.HandleMessageDelivery(args);
            autoResetEvent.WaitOne(1000);

            model.AssertWasCalled(x => x.BasicNack(deliveryTag, false, false));
            model.AssertWasNotCalled(x => x.BasicAck(Arg<ulong>.Is.Anything, Arg<bool>.Is.Anything));
        }

        [Test]
        public void Should_not_ack_or_nack_when_error_strategy_requests()
        {
            consumerErrorStrategy.Stub(x => x.PostExceptionAckStrategy()).Return(PostExceptionAckStrategy.DoNothing);

            var model = MockRepository.GenerateStub<IModel>();

            var args = CreateBasicDeliverEventArgs(consumerTag, deliveryTag, model, ExceptionMessageCallback);

            queueingConsumerFactory.HandleMessageDelivery(args);
            autoResetEvent.WaitOne(1000);

            model.AssertWasNotCalled(x => x.BasicAck(Arg<ulong>.Is.Anything, Arg<bool>.Is.Anything));
            model.AssertWasNotCalled(x => x.BasicNack(Arg<ulong>.Is.Anything, Arg<bool>.Is.Anything, Arg<bool>.Is.Anything));
        }

        [Test]
        public void Should_be_able_to_recreate_consumer_with_existing_consumerTag()
        {
            var model = MockRepository.GenerateStub<IModel>();
            queueingConsumerFactory.CreateConsumer(new SubscriptionAction(consumerTag, null, false), model, false, null);

            bool succeeded;
            try
            {
                queueingConsumerFactory.CreateConsumer(new SubscriptionAction(consumerTag, null, false), model, false,
                                                       null);
                succeeded = true;
            }
            catch (Exception)
            {
                succeeded = false;
            }

            Assert.IsTrue(succeeded);
        }

        private BasicDeliverEventArgs CreateBasicDeliverEventArgs(string consumerTag, ulong deliveryTag, IModel model, MessageCallback callback)
        {
            var consumer = queueingConsumerFactory.CreateConsumer(
                new SubscriptionAction(consumerTag, null, false), model, false, callback);

            consumer.HandleBasicConsumeOk(consumer.ConsumerTag);

            var args = new BasicDeliverEventArgs
            {
                ConsumerTag = consumer.ConsumerTag,
                RoutingKey = "abc",
                BasicProperties = new BasicProperties
                {
                    CorrelationId = "xyz"
                },
                DeliveryTag = deliveryTag,
                Body = Encoding.UTF8.GetBytes("Hello World"),
                Exchange = "the exchange",
                Redelivered = false
            };
            return args;
        }

        Task SuccessMessageCallback(
            string consumerTag, 
            ulong deliveryTag, 
            bool redelivered, 
            string exchange, 
            string routingKey, 
            IBasicProperties properties, 
            byte[] body)
        {
            return Task.Factory.StartNew(() => { });
        }

        Task ExceptionMessageCallback(
            string consumerTag, 
            ulong deliveryTag, 
            bool redelivered, 
            string exchange, 
            string routingKey, 
            IBasicProperties properties, 
            byte[] body)
        {
            return Task.Factory.StartNew(() =>
            {
                throw new Exception("Something awful happened!!");
            });
        }
    }
}

// ReSharper restore InconsistentNaming