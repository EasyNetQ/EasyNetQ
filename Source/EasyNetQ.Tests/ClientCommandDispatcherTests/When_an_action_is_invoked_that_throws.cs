// ReSharper disable InconsistentNaming

using System;
using EasyNetQ.ConnectionString;
using EasyNetQ.Producer;
using NUnit.Framework;
using RabbitMQ.Client;
using NSubstitute;

namespace EasyNetQ.Tests.ClientCommandDispatcherTests
{
    [TestFixture]
    public class When_an_action_is_invoked_that_throws
    {
        private IClientCommandDispatcher dispatcher;
        private IPersistentChannel channel;

        [SetUp]
        public void SetUp()
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

        [TearDown]
        public void TearDown()
        {
            dispatcher.Dispose();
        }

        [Test]
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
                aggregateException.InnerException.ShouldBeTheSameAs(exception);
            }
        }

        private class CrazyTestOnlyException : Exception { }
    }
}

// ReSharper restore InconsistentNaming