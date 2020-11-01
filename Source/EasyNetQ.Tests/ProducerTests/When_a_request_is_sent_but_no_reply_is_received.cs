// ReSharper disable InconsistentNaming

using System;
using System.Threading.Tasks;
using EasyNetQ.Tests.Mocking;
using Xunit;

namespace EasyNetQ.Tests.ProducerTests
{
    public class When_a_request_is_sent_but_no_reply_is_received : IDisposable
    {
        private readonly MockBuilder mockBuilder;

        public When_a_request_is_sent_but_no_reply_is_received()
        {
            mockBuilder = new MockBuilder("host=localhost;timeout=1");
        }

        public void Dispose()
        {
            mockBuilder.Bus.Dispose();
        }

        [Fact]
        public Task Should_throw_a_cancelled_exception()
        {
            return Assert.ThrowsAsync<TaskCanceledException>(
                () => mockBuilder.Rpc.RequestAsync<TestRequestMessage, TestResponseMessage>(
                    new TestRequestMessage(), c => { }
                )
            );
        }
    }
}

// ReSharper restore InconsistentNaming
