// ReSharper disable InconsistentNaming

using System;
using EasyNetQ.MessageVersioning;
using EasyNetQ.Producer;
using EasyNetQ.Topology;
using FluentAssertions;
using Xunit;
using NSubstitute;

namespace EasyNetQ.Tests.ProducerTests
{
    public class ExchangeDeclareStrategyTests
    {
        private const string exchangeName = "the_exchange";

        [Fact]
        public void Should_declare_exchange_the_first_time_declare_is_called()
        {
            var exchangeDeclareCount = 0;
            var advancedBus = Substitute.For<IAdvancedBus>();
            IExchange exchange = new Exchange(exchangeName);
            advancedBus.ExchangeDeclare(exchangeName, "topic")
                .Returns(x =>
                {
                    exchangeDeclareCount++;
                    return exchange;
                });

            var exchangeDeclareStrategy = new DefaultExchangeDeclareStrategy(Substitute.For<IConventions>(), advancedBus);
           
            var declaredExchange = exchangeDeclareStrategy.DeclareExchange(exchangeName, ExchangeType.Topic);

            advancedBus.Received().ExchangeDeclare(exchangeName, "topic");
            declaredExchange.Should().BeSameAs(exchange);
            exchangeDeclareCount.Should().Be(1);
        }

        [Fact]
        public void Should_not_declare_exchange_the_second_time_declare_is_called()
        {
            var exchangeDeclareCount = 0;
            var advancedBus = Substitute.For<IAdvancedBus>();
            IExchange exchange = new Exchange(exchangeName);
            advancedBus.ExchangeDeclare(exchangeName, "topic") .Returns(x =>
            {
                exchangeDeclareCount++;
                return exchange;
            });

            var exchangeDeclareStrategy = new DefaultExchangeDeclareStrategy(Substitute.For<IConventions>(), advancedBus);
  
            var _ = exchangeDeclareStrategy.DeclareExchange(exchangeName, ExchangeType.Topic);
            var declaredExchange = exchangeDeclareStrategy.DeclareExchange(exchangeName, ExchangeType.Topic);

            advancedBus.Received().ExchangeDeclare(exchangeName, "topic");
            declaredExchange.Should().BeSameAs(exchange);
            exchangeDeclareCount.Should().Be(1);
        }

        [Fact]
        public void Should_declare_exchange_again_if_first_attempt_failed()
        {
            var exchangeDeclareCount = 0;
           
            var advancedBus = Substitute.For<IAdvancedBus>();
            IExchange exchange = new Exchange(exchangeName);

            advancedBus.ExchangeDeclare(exchangeName, "topic").Returns(
                x => throw new Exception(),
                x =>
                {
                    exchangeDeclareCount++;
                    return exchange;
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
            advancedBus.Received(2).ExchangeDeclare(exchangeName, "topic");
            declaredExchange.Should().BeSameAs(exchange);
            exchangeDeclareCount.Should().Be(1);
        }
    }
}

// ReSharper restore InconsistentNaming
