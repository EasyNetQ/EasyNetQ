// ReSharper disable InconsistentNaming

using System.Threading.Tasks;
using EasyNetQ.Tests.Mocking;
using EasyNetQ.Topology;
using NUnit.Framework;
using Rhino.Mocks;

namespace EasyNetQ.Tests.ConsumeTests
{
    [TestFixture]
    public class When_a_consumer_is_cancelled_by_the_broker
    {
        private MockBuilder mockBuilder;

        [SetUp]
        public void SetUp()
        {
            mockBuilder = new MockBuilder();

            var queue = new Queue("my_queue", false);

            mockBuilder.Bus.Advanced.Consume(queue, (bytes, properties, arg3) => Task.Factory.StartNew(() => { }));

            mockBuilder.Consumers[0].HandleBasicCancel("consumer_tag");
        }

        [Test]
        public void Should_dispose_of_the_model()
        {
            mockBuilder.Consumers[0].Model.AssertWasCalled(x => x.Dispose());
        }
    }
}

// ReSharper restore InconsistentNaming