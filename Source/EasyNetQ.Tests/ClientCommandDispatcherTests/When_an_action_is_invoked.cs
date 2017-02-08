// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EasyNetQ.ConnectionString;
using EasyNetQ.Producer;
using Xunit;
using RabbitMQ.Client;
using NSubstitute;

namespace EasyNetQ.Tests.ClientCommandDispatcherTests
{
    public class When_an_action_is_invoked : IDisposable
    {
        private IClientCommandDispatcher dispatcher;
        private IPersistentChannel channel;
        private bool actionWasInvoked;
        private string actionThreadName;

        public When_an_action_is_invoked()
        {
            actionWasInvoked = false;

            var parser = new ConnectionStringParser();
            var configuration = parser.Parse("host=localhost");
            var connection = Substitute.For<IPersistentConnection>();
            var channelFactory = Substitute.For<IPersistentChannelFactory>();
            channel = Substitute.For<IPersistentChannel>();

            Action<IModel> action = x =>
                {
                    actionWasInvoked = true;
                };

            channelFactory.CreatePersistentChannel(connection).Returns(channel);
            channel.When(x => x.InvokeChannelAction(Arg.Any<Action<IModel>>()))
                   .Do(x => ((Action<IModel>)x[0])(null));

            dispatcher = new ClientCommandDispatcher(configuration, connection, channelFactory);

            dispatcher.InvokeAsync(action).Wait();
        }

        public void Dispose()
        {
            dispatcher.Dispose();
        }

        [Fact]
        public void Should_create_a_persistent_channel()
        {
            channel.ShouldNotBeNull();
        }

        [Fact]
        public void Should_invoke_the_action()
        {
            actionWasInvoked.ShouldBeTrue();
        }

        [Fact]
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