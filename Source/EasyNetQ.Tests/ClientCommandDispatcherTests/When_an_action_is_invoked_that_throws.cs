// ReSharper disable InconsistentNaming

using System;
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
        private IClientCommandDispatcher dispatcher;
        private IPersistentChannel channel;

        public When_an_action_is_invoked_that_throws()
        {
            var parser = new ConnectionStringParser();
            var configuration = parser.Parse("host=localhost");
            var connection = Substitute.For<IPersistentConnection>();
            var channelFactory = Substitute.For<IPersistentChannelFactory>();
            channel = Substitute.For<IPersistentChannel>();

            channelFactory.CreatePersistentChannel(connection).Returns(channel);
            channel.WhenForAnyArgs(x => x.InvokeChannelAction(null))
                   .Do(x => ((Action<IModel>)x[0])(null));

            dispatcher = new ClientCommandDispatcher(configuration, connection, channelFactory);
        }

        public void Dispose()
        {
            dispatcher.Dispose();
        }

        [Fact]
        public void Should_raise_the_exception_on_the_calling_thread()
        {
            var exception = new CrazyTestOnlyException();
            
            var task = dispatcher.InvokeAsync(x =>
            {
                throw exception;
            });

            try
            {
                task.Wait();
            }
            catch (AggregateException aggregateException)
            {
                aggregateException.InnerException.Should().BeSameAs(exception);
            }
        }

        [Fact]
        public void Should_call_action_when_previous_throwed_an_exception()
        {
            Action<IModel> errorAction = x => { throw new Exception(); };
            var goodActionWasInvoked = false;
            Action<IModel> goodAction = x => { goodActionWasInvoked = true; };

            try
            {
                dispatcher.InvokeAsync(errorAction).Wait();
            }
            catch
            {
                // ignore exception
            }

            dispatcher.InvokeAsync(goodAction).Wait();
            goodActionWasInvoked.Should().BeTrue();
        }

        private class CrazyTestOnlyException : Exception { }
    }
}

// ReSharper restore InconsistentNaming