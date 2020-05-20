// ReSharper disable InconsistentNaming

using System;
using System.Threading.Tasks;
using EasyNetQ.ConnectionString;
using EasyNetQ.Producer;
using FluentAssertions;
using NSubstitute;
using RabbitMQ.Client;
using Xunit;

namespace EasyNetQ.Tests.ClientCommandDispatcherTests
{
    public class When_an_action_is_invoked_that_throws : IDisposable
    {
        private readonly IClientCommandDispatcher dispatcher;

        public When_an_action_is_invoked_that_throws()
        {
            var parser = new ConnectionStringParser();
            var configuration = parser.Parse("host=localhost");
            var connection = Substitute.For<IPersistentConnection>();
            var channelFactory = Substitute.For<IPersistentChannelFactory>();
            var channel = Substitute.For<IPersistentChannel>();

            channelFactory.CreatePersistentChannel(connection).Returns(channel);
            channel.InvokeChannelActionAsync<int>(null, default)
                .ReturnsForAnyArgs(x => ((Func<IModel, int>)x[0]).Invoke(null));

            dispatcher = new DefaultClientCommandDispatcher(configuration, connection, channelFactory);
        }

        public void Dispose()
        {
            dispatcher.Dispose();
        }

        [Fact]
        public async Task Should_raise_the_exception_on_the_calling_thread()
        {
            await Assert.ThrowsAsync<CrazyTestOnlyException>(
                () => dispatcher.InvokeAsync<int>(x => throw new CrazyTestOnlyException(), default)
            ).ConfigureAwait(false);
        }

        [Fact]
        public async Task Should_call_action_when_previous_throwed_an_exception()
        {
            await Assert.ThrowsAsync<Exception>(
                () => dispatcher.InvokeAsync<int>(x => throw new Exception(), default)
            ).ConfigureAwait(false);

            var result = await dispatcher.InvokeAsync(x => 42, default).ConfigureAwait(false);
            result.Should().Be(42);
        }

        private class CrazyTestOnlyException : Exception { }
    }
}

// ReSharper restore InconsistentNaming
