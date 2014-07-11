// ReSharper disable InconsistentNaming

using System.Threading.Tasks;
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
        private const string exchangeName = "the_exchange";
        private IExchange exchange;
        private Task<IExchange> exchangeTask;
        private int exchangeDeclareCount;

        
        [SetUp]
        public void SetUp()
        {
            exchangeDeclareCount = 0;

            publishExchangeDeclareStrategy = new PublishExchangeDeclareStrategy();
            advancedBus = MockRepository.GenerateStub<IAdvancedBus>();
            exchange = new Exchange(exchangeName);
            exchangeTask = TaskHelpers.FromResult(exchange);
            advancedBus
                .Stub(x => x.ExchangeDeclareAsync(exchangeName, "topic"))
                .Return(exchangeTask)
                .WhenCalled(x => exchangeDeclareCount++);
        }

        [Test]
        public void Should_declare_exchange_the_first_time_declare_is_called()
        {
            var declaredExchange = publishExchangeDeclareStrategy.DeclareExchange(advancedBus, exchangeName, ExchangeType.Topic);

            advancedBus.AssertWasCalled(x => x.ExchangeDeclareAsync(exchangeName, "topic"));
            declaredExchange.ShouldBeTheSameAs(exchange);
            exchangeDeclareCount.ShouldEqual(1);
        }

        [Test]
        public void Should_not_declare_exchange_the_second_time_declare_is_called()
        {
            var _ = publishExchangeDeclareStrategy.DeclareExchange(advancedBus, exchangeName, ExchangeType.Topic);
            var declaredExchange = publishExchangeDeclareStrategy.DeclareExchange(advancedBus, exchangeName, ExchangeType.Topic);

            advancedBus.AssertWasCalled(x => x.ExchangeDeclareAsync(exchangeName, "topic"));
            declaredExchange.ShouldBeTheSameAs(exchange);
            exchangeDeclareCount.ShouldEqual(1);
        }
    }
}

// ReSharper restore InconsistentNaming