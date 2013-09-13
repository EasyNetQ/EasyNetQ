// ReSharper disable InconsistentNaming

using System.Threading;
using NUnit.Framework;
using Rhino.Mocks;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class ConsumerDispatcherFactoryTests
    {
        private IConsumerDispatcherFactory dispatcherFactory;
        private IEasyNetQLogger logger;

        [SetUp]
        public void SetUp()
        {
            logger = MockRepository.GenerateStub<IEasyNetQLogger>();
            dispatcherFactory = new ConsumerDispatcherFactory(logger);
        }

        [TearDown]
        public void TearDown()
        {
            dispatcherFactory.Dispose();
        }

        [Test]
        public void Should_only_create_a_single_IConsumerDispatcher_instance()
        {
            var dispatcher1 = dispatcherFactory.GetConsumerDispatcher();
            var dispatcher2 = dispatcherFactory.GetConsumerDispatcher();

            dispatcher1.ShouldBeTheSameAs(dispatcher2);
        }

        [Test]
        public void Should_dispose_dispatcher_when_factory_is_disposed()
        {
            var dispatcher = dispatcherFactory.GetConsumerDispatcher();
            dispatcherFactory.Dispose();
            ((ConsumerDispatcher)dispatcher).IsDisposed.ShouldBeTrue();
        }

        [Test]
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

            threadName.ShouldEqual("EasyNetQ consumer dispatch thread");
        }
    }
}

// ReSharper restore InconsistentNaming