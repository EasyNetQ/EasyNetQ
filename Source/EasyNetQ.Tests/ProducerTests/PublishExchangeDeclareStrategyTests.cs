using System.Threading.Tasks;
using EasyNetQ.MessageVersioning;
using EasyNetQ.Topology;

namespace EasyNetQ.Tests.ProducerTests;

public class ExchangeDeclareStrategyTests
{
    private const string exchangeName = "the_exchange";

    [Fact]
    public async Task Should_declare_exchange_again_if_first_attempt_failed()
    {
        var exchangeDeclareCount = 0;

        var advancedBus = Substitute.For<IAdvancedBus>();
        var exchange = new Exchange(exchangeName);

        advancedBus.ExchangeDeclareAsync(exchangeName).Returns(
            _ => Task.FromException(new Exception()),
            _ =>
            {
                exchangeDeclareCount++;
                return Task.FromResult(exchange);
            });

        using var exchangeDeclareStrategy = new VersionedExchangeDeclareStrategy(Substitute.For<IConventions>(), advancedBus);
        try
        {
            await exchangeDeclareStrategy.DeclareExchangeAsync(exchangeName, ExchangeType.Topic);
        }
        catch (Exception)
        {
        }

        var declaredExchange = await exchangeDeclareStrategy.DeclareExchangeAsync(exchangeName, ExchangeType.Topic);
        await advancedBus.Received(2).ExchangeDeclareAsync(exchangeName);
        declaredExchange.Should().BeEquivalentTo(exchange);
        exchangeDeclareCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_declare_exchange_the_first_time_declare_is_called()
    {
        var exchangeDeclareCount = 0;
        var advancedBus = Substitute.For<IAdvancedBus>();
        var exchange = new Exchange(exchangeName);
        advancedBus.ExchangeDeclareAsync(exchangeName)
            .Returns(_ =>
            {
                exchangeDeclareCount++;
                return Task.FromResult(exchange);
            });

        using var publishExchangeDeclareStrategy = new DefaultExchangeDeclareStrategy(Substitute.For<IConventions>(), advancedBus);

        var declaredExchange = await publishExchangeDeclareStrategy.DeclareExchangeAsync(exchangeName, ExchangeType.Topic);

        await advancedBus.Received().ExchangeDeclareAsync(exchangeName);
        declaredExchange.Should().BeEquivalentTo(exchange);
        exchangeDeclareCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_not_declare_exchange_the_second_time_declare_is_called()
    {
        var exchangeDeclareCount = 0;
        var advancedBus = Substitute.For<IAdvancedBus>();
        var exchange = new Exchange(exchangeName);
        advancedBus.ExchangeDeclareAsync(exchangeName).Returns(_ =>
        {
            exchangeDeclareCount++;
            return Task.FromResult(exchange);
        });

        using var exchangeDeclareStrategy = new DefaultExchangeDeclareStrategy(Substitute.For<IConventions>(), advancedBus);

        await exchangeDeclareStrategy.DeclareExchangeAsync(exchangeName, ExchangeType.Topic);
        var declaredExchange = await exchangeDeclareStrategy.DeclareExchangeAsync(exchangeName, ExchangeType.Topic);

        await advancedBus.Received().ExchangeDeclareAsync(exchangeName);
        declaredExchange.Should().BeEquivalentTo(exchange);
        exchangeDeclareCount.Should().Be(1);
    }
}
