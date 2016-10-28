// ReSharper disable InconsistentNaming

using System;
using System.Threading;
using EasyNetQ.ConnectionString;
using EasyNetQ.Producer;
using NUnit.Framework;
using RabbitMQ.Client;
using NSubstitute;

namespace EasyNetQ.Tests.ClientCommandDispatcherTests
{
    [TestFixture]
    public class When_an_action_is_invoked
    {
        private IClientCommandDispatcher dispatcher;
        private IPersistentChannel channel;
        private bool actionWasInvoked;
        private string actionThreadName;

        [SetUp]
        public void SetUp()
        {
            actionWasInvoked = false;
            actionThreadName = "Not set";

            var parser = new ConnectionStringParser();
            var configuration = parser.Parse("host=localhost");
            var connection = Substitute.For<IPersistentConnection>();
            var channelFactory = Substitute.For<IPersistentChannelFactory>();
            channel = Substitute.For<IPersistentChannel>();

            Action<IModel> action = x =>
                {
                    actionWasInvoked = true;
                    actionThreadName = Thread.CurrentThread.Name;
                };

            channelFactory.CreatePersistentChannel(connection).Returns(channel);
            channel.When(x => x.InvokeChannelAction(Arg.Any<Action<IModel>>()))
                   .Do(x => ((Action<IModel>)x[0])(null));

            dispatcher = new ClientCommandDispatcher(configuration, connection, channelFactory);

            dispatcher.InvokeAsync(action).Wait();
        }

        [TearDown]
        public void TearDown()
        {
            dispatcher.Dispose();
        }

        [Test]
        public void Should_create_a_persistent_channel()
        {
            channel.ShouldNotBeNull();
        }

        [Test]
        public void Should_invoke_the_action()
        {
            actionWasInvoked.ShouldBeTrue();
        }

        [Test]
        public void Should_invoke_the_action_on_the_dispatcher_thread()
        {
            actionThreadName.ShouldEqual("Client Command Dispatcher Thread");
        }
    }
}

// ReSharper restore InconsistentNaming