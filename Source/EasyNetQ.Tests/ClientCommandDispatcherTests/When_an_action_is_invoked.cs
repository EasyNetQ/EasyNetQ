// ReSharper disable InconsistentNaming

using System;
using System.Threading;
using EasyNetQ.Producer;
using NUnit.Framework;
using RabbitMQ.Client;
using Rhino.Mocks;

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

            var connection = MockRepository.GenerateStub<IPersistentConnection>();
            var channelFactory = MockRepository.GenerateStub<IPersistentChannelFactory>();
            channel = MockRepository.GenerateStub<IPersistentChannel>();

            Action<IModel> action = x =>
                {
                    actionWasInvoked = true;
                    actionThreadName = Thread.CurrentThread.Name;
                };

            channelFactory.Stub(x => x.CreatePersistentChannel(connection)).Return(channel);
            channel.Stub(x => x.InvokeChannelAction(null)).IgnoreArguments().WhenCalled(
                x => ((Action<IModel>)x.Arguments[0])(null));

            dispatcher = new ClientCommandDispatcher(connection, channelFactory);

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