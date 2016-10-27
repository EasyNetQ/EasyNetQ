// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EasyNetQ.ConnectionString;
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

            var parser = new ConnectionStringParser();
            var configuration = parser.Parse("host=localhost");
            var connection = MockRepository.GenerateStub<IPersistentConnection>();
            var channelFactory = MockRepository.GenerateStub<IPersistentChannelFactory>();
            channel = MockRepository.GenerateStub<IPersistentChannel>();

            Action<IModel> action = x =>
                {
                    actionWasInvoked = true;
                };

            channelFactory.Stub(x => x.CreatePersistentChannel(connection)).Return(channel);
            channel.Stub(x => x.InvokeChannelAction(null)).IgnoreArguments().WhenCalled(
                x => ((Action<IModel>)x.Arguments[0])(null));

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
        public void Should_call_actions_concurrently()
        {
            const int numberOfCalls = 1000;
            var actionsWereInvoked = new Dictionary<int, bool>();
            var actions = (from key in Enumerable.Range(0, numberOfCalls)
                let action = new Action<IModel>(x => actionsWereInvoked[key] = true)
                select new {Key = key, Action = action}).ToArray();

            Task.WhenAll(actions.Select(x => dispatcher.InvokeAsync(x.Action))).Wait();

            foreach (var action in actions)
            {
                var failMessage = string.Format("Action with key {0} was not invoked", action.Key);
                var wasInvoked = actionsWereInvoked.ContainsKey(action.Key) && actionsWereInvoked[action.Key];
                Assert.True(wasInvoked, failMessage);
            }
        }
    }
}

// ReSharper restore InconsistentNaming