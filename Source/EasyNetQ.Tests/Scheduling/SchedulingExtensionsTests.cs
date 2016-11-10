// ReSharper disable InconsistentNaming

using EasyNetQ.Scheduling;
using EasyNetQ.Tests.Interception;
using NUnit.Framework;
using NSubstitute;

namespace EasyNetQ.Tests.Scheduling
{
    [TestFixture]
    public class InterceptionExtensionsTests
    {
        [Test]
        public void When_using_EnableDelayedExchangeScheduler_extension_method_required_services_are_registered()
        {
            var serviceRegister = Substitute.For<IServiceRegister>();
            serviceRegister.EnableDelayedExchangeScheduler();
            serviceRegister.Received().Register<IScheduler, DelayedExchangeScheduler>();
        }

        [Test]
        public void When_using_EnableDeadLetterExchangeAndMessageTtlScheduler_extension_method_required_services_are_registered()
        {
            var serviceRegister = Substitute.For<IServiceRegister>();
            serviceRegister.EnableDeadLetterExchangeAndMessageTtlScheduler();
            serviceRegister.Received().Register<IScheduler, DeadLetterExchangeAndMessageTtlScheduler>();
        }
    }
}