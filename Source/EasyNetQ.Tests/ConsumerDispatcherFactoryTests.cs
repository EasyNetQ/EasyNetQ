// ReSharper disable InconsistentNaming
using System;
using System.Threading;
using EasyNetQ.ConnectionString;
using EasyNetQ.Consumer;
using FluentAssertions;
using Xunit;
using NSubstitute;

namespace EasyNetQ.Tests
{
    public class ConsumerDispatcherFactoryTests : IDisposable
    {
        private IConsumerDispatcherFactory dispatcherFactory;

        public ConsumerDispatcherFactoryTests()
        {
            var parser = new ConnectionStringParser();
            var configuration = parser.Parse("host=localhost");
            dispatcherFactory = new ConsumerDispatcherFactory(configuration);
        }

        public void Dispose()
        {
            dispatcherFactory.Dispose();
        }

        [Fact]
        public void Should_only_create_a_single_IConsumerDispatcher_instance()
        {
            var dispatcher1 = dispatcherFactory.GetConsumerDispatcher();
            var dispatcher2 = dispatcherFactory.GetConsumerDispatcher();

            dispatcher1.Should().BeSameAs(dispatcher2);
        }

        [Fact]
        public void Should_dispose_dispatcher_when_factory_is_disposed()
        {
            var dispatcher = dispatcherFactory.GetConsumerDispatcher();
            dispatcherFactory.Dispose();
            ((ConsumerDispatcher)dispatcher).IsDisposed.Should().BeTrue();
        }

        [Fact]
        public void Should_run_actions_on_the_consumer_thread()
        {
            var dispatcher = dispatcherFactory.GetConsumerDispatcher();
            var autoResetEvent = new AutoResetEvent(false);
            var threadName = Thread.CurrentThread.Name;

            dispatcher.QueueAction(() =>
                {
                    threadName = Thread.CurrentThread.Name;
                    autoResetEvent.Set();
                });

            autoResetEvent.WaitOne(100);

            threadName.Should().Be("EasyNetQ consumer dispatch thread");
        }

        [Fact]
        public void Should_clear_queue_on_disconnect()
        {
            var dispatcher = dispatcherFactory.GetConsumerDispatcher();
            var autoResetEvent1 = new AutoResetEvent(false);
            var autoResetEvent2 = new AutoResetEvent(false);
            var actionExecuted = false;

            // queue first action, we're going to block on this one
            dispatcher.QueueAction(() => autoResetEvent1.WaitOne(100));

            // queue second action, this should be cleared when 
            // the dispatcher factory's OnDisconnected method is called
            // and never run.
            dispatcher.QueueAction(() =>
                {
                    actionExecuted = true;
                });

            // disconnect
            dispatcherFactory.OnDisconnected();

            // release the block on the first event
            autoResetEvent1.Set();

            // now queue up a new action and wait for it to complete
            dispatcher.QueueAction(() => autoResetEvent2.Set());
            autoResetEvent2.WaitOne(100);

            // check that the second action was never run
            actionExecuted.Should().BeFalse();
        }
    }
}

// ReSharper restore InconsistentNaming