﻿// ReSharper disable InconsistentNaming

using System;
using EasyNetQ.MessageVersioning;
using EasyNetQ.Producer;
using EasyNetQ.Topology;
using NUnit.Framework;
using Rhino.Mocks;

namespace EasyNetQ.Tests.ProducerTests
{
    [TestFixture]
    public class PublishExchangeDeclareStrategyTests
    {
        private const string exchangeName = "the_exchange";

        [Test]
        public void Should_declare_exchange_the_first_time_declare_is_called()
        {
            var exchangeDeclareCount = 0;

            var publishExchangeDeclareStrategy = new PublishExchangeDeclareStrategy();
            var advancedBus = MockRepository.GenerateStub<IAdvancedBus>();
            IExchange exchange = new Exchange(exchangeName);
            advancedBus
                .Stub(x => x.ExchangeDeclare(exchangeName, "topic"))
                .Return(exchange)
                .WhenCalled(x => exchangeDeclareCount++);

            var declaredExchange = publishExchangeDeclareStrategy.DeclareExchange(advancedBus, exchangeName, ExchangeType.Topic);

            advancedBus.AssertWasCalled(x => x.ExchangeDeclare(exchangeName, "topic"));
            declaredExchange.ShouldBeTheSameAs(exchange);
            exchangeDeclareCount.ShouldEqual(1);
        }

        [Test]
        public void Should_not_declare_exchange_the_second_time_declare_is_called()
        {
            var exchangeDeclareCount = 0;

            var publishExchangeDeclareStrategy = new Producer.PublishExchangeDeclareStrategy();
            var advancedBus = MockRepository.GenerateStub<IAdvancedBus>();
            IExchange exchange = new Exchange(exchangeName);
            advancedBus
                .Stub(x => x.ExchangeDeclare(exchangeName, "topic"))
                .Return(exchange)
                .WhenCalled(x => exchangeDeclareCount++);
            var _ = publishExchangeDeclareStrategy.DeclareExchange(advancedBus, exchangeName, ExchangeType.Topic);
            var declaredExchange = publishExchangeDeclareStrategy.DeclareExchange(advancedBus, exchangeName, ExchangeType.Topic);

            advancedBus.AssertWasCalled(x => x.ExchangeDeclare(exchangeName, "topic"));
            declaredExchange.ShouldBeTheSameAs(exchange);
            exchangeDeclareCount.ShouldEqual(1);
        }

        [Test]
        public void Should_declare_exchange_again_if_first_attempt_failed()
        {
            var exchangeDeclareCount = 0;
           
            var advancedBus = MockRepository.GenerateStrictMock<IAdvancedBus>();
            IExchange exchange = new Exchange(exchangeName);

            advancedBus
                .Expect(x => x.ExchangeDeclare(exchangeName, "topic"))
                .Throw(new Exception())
                .WhenCalled(x => exchangeDeclareCount++);

            advancedBus
                .Expect(x => x.ExchangeDeclare(exchangeName, "topic"))
                .Return(exchange)
                .WhenCalled(x => exchangeDeclareCount++);

            var publishExchangeDeclareStrategy = new VersionedPublishExchangeDeclareStrategy();
            try
            {
                publishExchangeDeclareStrategy.DeclareExchange(advancedBus, exchangeName, ExchangeType.Topic);
            }
            catch (Exception)
            {
            }
            var declaredExchange = publishExchangeDeclareStrategy.DeclareExchange(advancedBus, exchangeName, ExchangeType.Topic);
            advancedBus.AssertWasCalled(x => x.ExchangeDeclare(exchangeName, "topic"));
            advancedBus.AssertWasCalled(x => x.ExchangeDeclare(exchangeName, "topic"));
            declaredExchange.ShouldBeTheSameAs(exchange);
            exchangeDeclareCount.ShouldEqual(1);
        }
    }
}

// ReSharper restore InconsistentNaming