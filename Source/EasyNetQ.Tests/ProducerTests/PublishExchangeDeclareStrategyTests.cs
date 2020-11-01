// ReSharper disable InconsistentNaming

using System;
using System.Threading.Tasks;
using EasyNetQ.Internals;
using EasyNetQ.MessageVersioning;
using EasyNetQ.Producer;
using EasyNetQ.Topology;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace EasyNetQ.Tests.ProducerTests
{
    public class ExchangeDeclareStrategyTests
    {
        private const string exchangeName = "the_exchange";

        [Fact]
        public void Should_declare_exchange_again_if_first_attempt_failed()
        {
            var exchangeDeclareCount = 0;

            var advancedBus = Substitute.For<IAdvancedBus>();
            IExchange exchange = new Exchange(exchangeName);

            advancedBus.ExchangeDeclareAsync(exchangeName, Arg.Any<Action<IExchangeDeclareConfiguration>>()).Returns(
                x => Task.FromException(new Exception()),
                x =>
                {
                    exchangeDeclareCount++;
                    return Task.FromResult(exchange);
                });

            var exchangeDeclareStrategy = new VersionedExchangeDeclareStrategy(Substitute.For<IConventions>(), advancedBus);
            try
            {
                exchangeDeclareStrategy.DeclareExchange(exchangeName, ExchangeType.Topic);
            }
            catch (Exception)
            {
            }

            var declaredExchange = exchangeDeclareStrategy.DeclareExchange(exchangeName, ExchangeType.Topic);
            advancedBus.Received(2).ExchangeDeclareAsync(exchangeName, Arg.Any<Action<IExchangeDeclareConfiguration>>());
            declaredExchange.Should().BeSameAs(exchange);
            exchangeDeclareCount.Should().Be(1);
        }

        [Fact]
        public void Should_declare_exchange_the_first_time_declare_is_called()
        {
            var exchangeDeclareCount = 0;
            var advancedBus = Substitute.For<IAdvancedBus>();
            IExchange exchange = new Exchange(exchangeName);
            advancedBus.ExchangeDeclareAsync(exchangeName, Arg.Any<Action<IExchangeDeclareConfiguration>>())
                .Returns(x =>
                {
                    exchangeDeclareCount++;
                    return Task.FromResult(exchange);
                });

            var publishExchangeDeclareStrategy = new DefaultExchangeDeclareStrategy(Substitute.For<IConventions>(), advancedBus);

            var declaredExchange = publishExchangeDeclareStrategy.DeclareExchange(exchangeName, ExchangeType.Topic);

            advancedBus.Received().ExchangeDeclareAsync(exchangeName, Arg.Any<Action<IExchangeDeclareConfiguration>>());
            declaredExchange.Should().BeSameAs(exchange);
            exchangeDeclareCount.Should().Be(1);
        }

        [Fact]
        public void Should_not_declare_exchange_the_second_time_declare_is_called()
        {
            var exchangeDeclareCount = 0;
            var advancedBus = Substitute.For<IAdvancedBus>();
            IExchange exchange = new Exchange(exchangeName);
            advancedBus.ExchangeDeclareAsync(exchangeName, Arg.Any<Action<IExchangeDeclareConfiguration>>()).Returns(x =>
            {
                exchangeDeclareCount++;
                return Task.FromResult(exchange);
            });

            var exchangeDeclareStrategy = new DefaultExchangeDeclareStrategy(Substitute.For<IConventions>(), advancedBus);

            var _ = exchangeDeclareStrategy.DeclareExchange(exchangeName, ExchangeType.Topic);
            var declaredExchange = exchangeDeclareStrategy.DeclareExchange(exchangeName, ExchangeType.Topic);

            advancedBus.Received().ExchangeDeclareAsync(exchangeName, Arg.Any<Action<IExchangeDeclareConfiguration>>());
            declaredExchange.Should().BeSameAs(exchange);
            exchangeDeclareCount.Should().Be(1);
        }
    }
}

// ReSharper restore InconsistentNaming
