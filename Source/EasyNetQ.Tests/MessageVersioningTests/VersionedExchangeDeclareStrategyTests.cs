// ReSharper disable InconsistentNaming

using EasyNetQ.MessageVersioning;
using EasyNetQ.Topology;

namespace EasyNetQ.Tests.MessageVersioningTests;

public class VersionedExchangeDeclareStrategyTests
{
    [Fact]
    public void Should_declare_exchange_again_if_first_attempt_failed()
    {
        var exchangeDeclareCount = 0;
        var exchangeName = "exchangeName";

        var advancedBus = Substitute.For<IAdvancedBus>();
        var exchange = new Exchange(exchangeName);

        advancedBus.ExchangeDeclareAsync(exchangeName, Arg.Any<Action<IExchangeDeclareConfiguration>>()).Returns(
            _ => Task.FromException(new Exception()),
            _ =>
            {
                exchangeDeclareCount++;
                return Task.FromResult(exchange);
            });

        var conventions = Substitute.For<IConventions>();
        conventions.ExchangeNamingConvention.Returns(t => t.Name);

        var exchangeDeclareStrategy = new VersionedExchangeDeclareStrategy(conventions, advancedBus);
        try
        {
            exchangeDeclareStrategy.DeclareExchange(exchangeName, ExchangeType.Topic);
        }
        catch (Exception)
        {
        }

        var declaredExchange = exchangeDeclareStrategy.DeclareExchange(exchangeName, ExchangeType.Topic);
        advancedBus.Received(2).ExchangeDeclareAsync(exchangeName, Arg.Any<Action<IExchangeDeclareConfiguration>>());
        declaredExchange.Should().BeEquivalentTo(exchange);
        exchangeDeclareCount.Should().Be(1);
    }

    // Unversioned message - exchange declared
    // Versioned message - superseded exchange declared, then superseding, then bind
    [Fact]
    public void When_declaring_exchanges_for_unversioned_message_one_exchange_created()
    {
        var exchanges = new List<Exchange>();
        var boundExchanges = new Dictionary<string, string>();
        var advancedBus = Substitute.For<IAdvancedBus>();
        advancedBus.ExchangeDeclareAsync(Arg.Any<string>(), Arg.Any<Action<IExchangeDeclareConfiguration>>())
            .ReturnsForAnyArgs(mi =>
            {
                var exchange = new Exchange((string)mi[0]);
                exchanges.Add(exchange);
                return Task.FromResult(exchange);
            });

        advancedBus.ExchangeBindAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Is("#"), Arg.Any<IDictionary<string, object>>())
            .Returns(mi =>
            {
                var source = (string)mi[0];
                var destination = (string)mi[1];
                boundExchanges.Add(source, destination);
                return Task.CompletedTask;
            });

        var conventions = Substitute.For<IConventions>();
        conventions.ExchangeNamingConvention.Returns(t => t.Name);

        var publishExchangeStrategy = new VersionedExchangeDeclareStrategy(conventions, advancedBus);

        publishExchangeStrategy.DeclareExchange(typeof(MyMessage), ExchangeType.Topic);

        Assert.True(exchanges.Count == 1); //, "Single exchange should have been created" );
        Assert.Equal("MyMessage", exchanges[0].Name); //, "Exchange should have used naming convection to name the exchange" );
        Assert.Empty(boundExchanges); // "Unversioned message should not create any exchange to exchange bindings" );
    }

    [Fact]
    public void When_declaring_exchanges_for_versioned_message_exchange_per_version_created_and_bound_to_superceding_version()
    {
        var exchanges = new List<string>();
        var boundExchanges = new Dictionary<string, string>();
        var advancedBus = Substitute.For<IAdvancedBus>();
        advancedBus.ExchangeDeclareAsync(Arg.Any<string>(), Arg.Any<Action<IExchangeDeclareConfiguration>>())
            .ReturnsForAnyArgs(mi =>
            {
                var exchange = (string)mi[0];
                exchanges.Add(exchange);
                return Task.FromResult(new Exchange(exchange));
            });

        advancedBus.ExchangeBindAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Is("#"), Arg.Any<IDictionary<string, object>>())
            .Returns(mi =>
            {
                var destination = (string)mi[0];
                var source = (string)mi[1];
                boundExchanges.Add(source, destination);
                return Task.CompletedTask;
            });

        var conventions = Substitute.For<IConventions>();
        conventions.ExchangeNamingConvention.Returns(t => t.Name);

        var publishExchangeStrategy = new VersionedExchangeDeclareStrategy(conventions, advancedBus);

        publishExchangeStrategy.DeclareExchange(typeof(MyMessageV2), ExchangeType.Topic);

        Assert.Equal(2, exchanges.Count); //, "Two exchanges should have been created" );
        Assert.Equal("MyMessage", exchanges[0]); //, "Superseded message exchange should been created first" );
        Assert.Equal("MyMessageV2", exchanges[1]); //, "Superseding message exchange should been created second" );

        boundExchanges.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            {exchanges[1], exchanges[0]}
        });
    }
}
