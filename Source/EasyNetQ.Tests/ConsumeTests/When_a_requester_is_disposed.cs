// ReSharper disable InconsistentNaming

using EasyNetQ.Tests.Mocking;
using NUnit.Framework;
using Rhino.Mocks;

namespace EasyNetQ.Tests.ConsumeTests
{
    [TestFixture]
    public class When_a_requester_is_disposed
    {
        private MockBuilder mockBuilder;

        [SetUp]
        public void SetUp()
        {
            mockBuilder = new MockBuilder();

            var cancellation = mockBuilder.Bus.Respond<MyMessage, MyMessage>(x => new MyMessage());
            cancellation.Dispose();
        }

        [Test]
        public void Should_dispose_of_channel()
        {
            mockBuilder.Channels[1].AssertWasCalled(x => x.Dispose());
        }
    }
}

// ReSharper restore InconsistentNaming