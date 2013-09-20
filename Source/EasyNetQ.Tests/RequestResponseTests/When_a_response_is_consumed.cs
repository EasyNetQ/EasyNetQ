// ReSharper disable InconsistentNaming

using NUnit.Framework;
using Rhino.Mocks;

namespace EasyNetQ.Tests.RequestResponseTests
{
    [TestFixture]
    public class When_a_response_is_consumed : RequestResponseTestBase
    {
        private bool responseHandlerWasCalled;

        protected override void AdditionalSetup()
        {
            responseHandlerWasCalled = false;

            MakeRequest(x =>
                {
                    responseHandlerWasCalled = true;
                });

            ReturnResponse();
        }

        [Test]
        public void Should_invoke_the_response_handler()
        {
            responseHandlerWasCalled.ShouldBeTrue();
        }

        [Test]
        public void Should_dispose_of_the_model_after_single_use()
        {
            mockBuilder.Channels[2].AssertWasCalled(x => x.Dispose());
        }
    }
}

// ReSharper restore InconsistentNaming