// ReSharper disable InconsistentNaming

using EasyNetQ.Producer;
using EasyNetQ.Topology;
using NUnit.Framework;
using Rhino.Mocks;

namespace EasyNetQ.Tests.ProducerTests
{
    [TestFixture]
    public class PublishExchangeDeclareStrategyTests
    {
        private IPublishExchangeDeclareStrategy publishExchangeDeclareStrategy;
        private IAdvancedBus advancedBus;
        const string exchangeName = "the_exchange";
        private readonly IExchange exchange = new Exchange(exchangeName);
        private int exchangeDeclareCount;

        [SetUp]
        public void SetUp()
        {
            exchangeDeclareCount = 0;

            publishExchangeDeclareStrategy = new PublishExchangeDeclareStrategy();
            advancedBus = MockRepository.GenerateStub<IAdvancedBus>();

            advancedBus
                .Stub(x => x.ExchangeDeclare(exchangeName, "topic"))
                .Return(exchange)
                .WhenCalled(x => exchangeDeclareCount++);
        }

        [Test]
        public void Should_declare_exchange_the_first_time_declare_is_called()
        {
            var declaredExchange = publishExchangeDeclareStrategy.DeclareExchange(advancedBus, exchangeName, ExchangeType.Topic);

            advancedBus.AssertWasCalled(x => x.ExchangeDeclare(exchangeName, "topic"));
            declaredExchange.ShouldBeTheSameAs(exchange);
            exchangeDeclareCount.ShouldEqual(1);
        }

        [Test]
        public void Should_not_declare_exchange_the_second_time_declare_is_called()
        {
            var _ = publishExchangeDeclareStrategy.DeclareExchange(advancedBus, exchangeName, ExchangeType.Topic);
            var declaredExchange = publishExchangeDeclareStrategy.DeclareExchange(advancedBus, exchangeName, ExchangeType.Topic);

            advancedBus.AssertWasCalled(x => x.ExchangeDeclare(exchangeName, "topic"));
            declaredExchange.ShouldBeTheSameAs(exchange);
            exchangeDeclareCount.ShouldEqual(1);
        }
    }
}

// ReSharper restore InconsistentNaming