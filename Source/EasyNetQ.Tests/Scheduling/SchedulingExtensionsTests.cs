// ReSharper disable InconsistentNaming

using EasyNetQ.Scheduling;
using EasyNetQ.Tests.Interception;
using NUnit.Framework;
using Rhino.Mocks;

namespace EasyNetQ.Tests.Scheduling
{
    [TestFixture]
    public class InterceptionExtensionsTests : UnitTestBase
    {
        [Test]
        public void When_using_EnableDelayedExchangeScheduler_extension_method_required_services_are_registered()
        {
            var serviceRegister = NewMock<IServiceRegister>();
            serviceRegister.Expect(x => x.Register<IScheduler, DelayedExchangeScheduler>()).TentativeReturn();
            serviceRegister.EnableDelayedExchangeScheduler();
        }

        [Test]
        public void When_using_EnableDeadLetterExchangeAndMessageTtlScheduler_extension_method_required_services_are_registered()
        {
            var serviceRegister = NewMock<IServiceRegister>();
            serviceRegister.Expect(x => x.Register<IScheduler, DeadLetterExchangeAndMessageTtlScheduler>()).TentativeReturn();
            serviceRegister.EnableDeadLetterExchangeAndMessageTtlScheduler();
        }

        [Test]
        public void When_using_EnableExternalSchedulerV2_extension_method_required_services_are_registered()
        {
            var serviceRegister = NewMock<IServiceRegister>();
            serviceRegister.Expect(x => x.Register<IScheduler, ExternalSchedulerV2>()).TentativeReturn();
            serviceRegister.EnableExternalSchedulerV2();
        }
    }
}