// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EasyNetQ.Internals;
using EasyNetQ.MessageVersioning;
using EasyNetQ.Producer;
using EasyNetQ.Topology;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace EasyNetQ.Tests.MessageVersioningTests
{
    public class VersionedPublishExchangeDeclareStrategyTests
    {
        private class ExchangeStub : IExchange
        {
            public ExchangeStub BoundTo { get; set; }
            public string Name { get; set; }
            public string Type { get; }
            public bool IsDurable { get; }
            public bool IsAutoDelete { get; }
            public IDictionary<string, object> Arguments { get; }
        }

        [Fact]
        public void Should_declare_exchange_again_if_first_attempt_failed()
        {
            var exchangeDeclareCount = 0;
            var exchangeName = "exchangeName";

            var advancedBus = Substitute.For<IAdvancedBus>();
            IExchange exchange = new Exchange(exchangeName);

            advancedBus.ExchangeDeclareAsync(exchangeName, Arg.Any<Action<IExchangeDeclareConfiguration>>()).Returns(
                x => TaskHelpers.FromException(new Exception()),
                x =>
                {
                    exchangeDeclareCount++;
                    return Task.FromResult(exchange);
                });

            var conventions = Substitute.For<IConventions>();
            conventions.ExchangeNamingConvention = t => t.Name;

            var publishExchangeDeclareStrategy = new VersionedPublishExchangeDeclareStrategy(conventions, advancedBus);
            try
            {
                publishExchangeDeclareStrategy.DeclareExchange(exchangeName, ExchangeType.Topic);
            }
            catch (Exception)
            {
            }

            var declaredExchange = publishExchangeDeclareStrategy.DeclareExchange(exchangeName, ExchangeType.Topic);
            advancedBus.Received(2).ExchangeDeclareAsync(exchangeName, Arg.Any<Action<IExchangeDeclareConfiguration>>());
            declaredExchange.Should().BeSameAs(exchange);
            exchangeDeclareCount.Should().Be(1);
        }


        // Unversioned message - exchange declared
        // Versioned message - superceded exchange declared, then superceding, then bind
        [Fact]
        public void When_declaring_exchanges_for_unversioned_message_one_exchange_created()
        {
            var exchanges = new List<ExchangeStub>();
            var advancedBus = Substitute.For<IAdvancedBus>();
            advancedBus.ExchangeDeclareAsync(null, null)
                .ReturnsForAnyArgs(mi =>
                {
                    var exchange = new ExchangeStub { Name = (string)mi[0] };
                    exchanges.Add(exchange);
                    return Task.FromResult<IExchange>(exchange);
                });

            advancedBus.BindAsync(Arg.Any<IExchange>(), Arg.Any<IExchange>(), Arg.Is("#"))
                .Returns(mi =>
                {
                    var source = (ExchangeStub) mi[0];
                    var destination = (ExchangeStub) mi[1];
                    source.BoundTo = destination;
                    return Task.FromResult(Substitute.For<IBinding>());
                });

            var conventions = Substitute.For<IConventions>();
            conventions.ExchangeNamingConvention = t => t.Name;

            var publishExchangeStrategy = new VersionedPublishExchangeDeclareStrategy(conventions, advancedBus);

            publishExchangeStrategy.DeclareExchange(typeof(MyMessage), ExchangeType.Topic);

            Assert.True(exchanges.Count == 1); //, "Single exchange should have been created" );
            Assert.Equal("MyMessage", exchanges[0].Name); //, "Exchange should have used naming convection to name the exchange" );
            Assert.Null(exchanges[0].BoundTo); // "Unversioned message should not create any exchange to exchange bindings" );
        }

        [Fact]
        public void When_declaring_exchanges_for_versioned_message_exchange_per_version_created_and_bound_to_superceding_version()
        {
            var exchanges = new List<ExchangeStub>();

            var advancedBus = Substitute.For<IAdvancedBus>();
            advancedBus.ExchangeDeclareAsync(null, null)
                .ReturnsForAnyArgs(mi =>
                {
                    var exchange = new ExchangeStub { Name = (string)mi[0] };
                    exchanges.Add(exchange);
                    return Task.FromResult<IExchange>(exchange);
                });

            advancedBus.BindAsync(Arg.Any<IExchange>(), Arg.Any<IExchange>(), Arg.Is("#"))
                .Returns(mi =>
                {
                    var source = (ExchangeStub) mi[0];
                    var destination = (ExchangeStub) mi[1];
                    source.BoundTo = destination;
                    return Task.FromResult(Substitute.For<IBinding>());
                });

            var conventions = Substitute.For<IConventions>();
            conventions.ExchangeNamingConvention = t => t.Name;

            var publishExchangeStrategy = new VersionedPublishExchangeDeclareStrategy(conventions, advancedBus);

            publishExchangeStrategy.DeclareExchange(typeof(MyMessageV2), ExchangeType.Topic);

            Assert.Equal(2, exchanges.Count); //, "Two exchanges should have been created" );
            Assert.Equal("MyMessage", exchanges[0].Name); //, "Superseded message exchange should been created first" );
            Assert.Equal("MyMessageV2", exchanges[1].Name); //, "Superseding message exchange should been created second" );
            Assert.Equal(exchanges[0], exchanges[1].BoundTo); //, "Superseding message exchange should route message to superseded exchange" );
            Assert.Null(exchanges[0].BoundTo); //, "Superseded message exchange should route messages anywhere" );
        }
    }
}
