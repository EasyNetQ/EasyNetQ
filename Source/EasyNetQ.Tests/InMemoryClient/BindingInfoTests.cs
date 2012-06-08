// ReSharper disable InconsistentNaming

using EasyNetQ.InMemoryClient;
using NUnit.Framework;

namespace EasyNetQ.Tests.InMemoryClient
{
    [TestFixture]
    public class BindingInfoTests
    {
        [SetUp]
        public void SetUp()
        {
        }

        [Test]
        public void Should_exact_match_should_match()
        {
            var bindingInfo = new BindingInfo(new QueueInfo("the queue", true, false, false, null), "abc");

            bindingInfo.RoutingKeyMatches("abc").ShouldBeTrue();
            bindingInfo.RoutingKeyMatches("#").ShouldBeTrue();
            bindingInfo.RoutingKeyMatches("def").ShouldBeFalse();
        }
    }
}

// ReSharper restore InconsistentNaming