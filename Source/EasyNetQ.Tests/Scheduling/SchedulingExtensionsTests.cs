// ReSharper disable InconsistentNaming

using EasyNetQ.DI;
using EasyNetQ.Scheduling;
using Xunit;
using NSubstitute;

namespace EasyNetQ.Tests.Scheduling
{
    public class InterceptionExtensionsTests
    {
        [Fact]
        public void When_using_EnableDelayedExchangeScheduler_extension_method_required_services_are_registered()
        {
            var serviceRegister = Substitute.For<IServiceRegister>();
            serviceRegister.EnableDelayedExchangeScheduler();
            serviceRegister.Received().Register<IScheduler, DelayedExchangeScheduler>();
        }

        [Fact]
        public void When_using_EnableDeadLetterExchangeAndMessageTtlScheduler_extension_method_required_services_are_registered()
        {
            var serviceRegister = Substitute.For<IServiceRegister>();
            serviceRegister.EnableDeadLetterExchangeAndMessageTtlScheduler();
            serviceRegister.Received().Register<IScheduler, DeadLetterExchangeAndMessageTtlScheduler>();
        }
    }
}