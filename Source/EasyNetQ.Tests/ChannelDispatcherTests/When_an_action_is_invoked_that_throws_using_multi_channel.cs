// ReSharper disable InconsistentNaming

using System;
using System.Threading.Tasks;
using EasyNetQ.ChannelDispatcher;
using EasyNetQ.Consumer;
using EasyNetQ.Persistent;
using EasyNetQ.Producer;
using FluentAssertions;
using NSubstitute;
using RabbitMQ.Client;
using Xunit;

namespace EasyNetQ.Tests.ChannelDispatcherTests
{
    public class When_an_action_is_invoked_that_throws_using_multi_channel : IDisposable
    {
        private readonly IChannelDispatcher dispatcher;

        public When_an_action_is_invoked_that_throws_using_multi_channel()
        {
            var channelFactory = Substitute.For<IPersistentChannelFactory>();
            var producerConnection = Substitute.For<IProducerConnection>();
            var consumerConnection = Substitute.For<IConsumerConnection>();
            var channel = Substitute.For<IPersistentChannel>();

            channelFactory.CreatePersistentChannel(producerConnection, new PersistentChannelOptions()).Returns(channel);
            channel.InvokeChannelActionAsync<int>(null)
                .ReturnsForAnyArgs(x => ((Func<IModel, int>)x[0]).Invoke(null));

            dispatcher = new MultiChannelDispatcher(1, producerConnection, consumerConnection, channelFactory);
        }

        public void Dispose()
        {
            dispatcher.Dispose();
        }

        [Fact]
        public async Task Should_raise_the_exception_on_the_calling_thread()
        {
            await Assert.ThrowsAsync<CrazyTestOnlyException>(
                () => dispatcher.InvokeAsync<int>(_ => throw new CrazyTestOnlyException(), ChannelDispatchOptions.ProducerTopology)
            );
        }

        [Fact]
        public async Task Should_call_action_when_previous_throwed_an_exception()
        {
            await Assert.ThrowsAsync<Exception>(
                () => dispatcher.InvokeAsync<int>(_ => throw new Exception(), ChannelDispatchOptions.ProducerTopology)
            );

            var result = await dispatcher.InvokeAsync(_ => 42, ChannelDispatchOptions.ProducerTopology);
            result.Should().Be(42);
        }

        private class CrazyTestOnlyException : Exception { }
    }
}

// ReSharper restore InconsistentNaming
