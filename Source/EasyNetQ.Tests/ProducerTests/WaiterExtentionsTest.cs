using System;
using EasyNetQ.Tests.Interception;
using NUnit.Framework;
using Rhino.Mocks;
using EasyNetQ.Producer.Waiters;

namespace EasyNetQ.Tests.ProducerTests
{
    [TestFixture]
    public class WaiterExtentionsTest : UnitTestBase
    {
        [Test]
        public void When_using_EnableReconnectionWithFixedDelay()
        {
            var serviceRegister = NewMock<IServiceRegister>();
            serviceRegister.Expect(x => x.Register(Arg<Func<IServiceProvider, IReconnectionWaiterFactory>>.Is.Anything)).TentativeReturn();
            serviceRegister.EnableReconnectionWithFixedDelay();
        }

        [Test]
        public void When_using_EnableReconnectionWithExponentialBackoffDelay()
        {
            var serviceRegister = NewMock<IServiceRegister>();
            serviceRegister.Expect(x => x.Register(Arg<Func<IServiceProvider, IReconnectionWaiterFactory>>.Is.Anything)).TentativeReturn();
            serviceRegister.EnableReconnectionWithExponentialBackoffDelay();
        }
    }
}