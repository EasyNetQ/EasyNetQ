// ReSharper disable InconsistentNaming

using System.Collections.Generic;
using System.Linq;
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
            bindingInfo.RoutingKeyMatches("def").ShouldBeFalse();
        }

        [Test]
        public void Everything_should_match_hash()
        {
            var bindingInfo = new BindingInfo(new QueueInfo("the queue", true, false, false, null), "#");

            bindingInfo.RoutingKeyMatches("abc").ShouldBeTrue();
            bindingInfo.RoutingKeyMatches("def").ShouldBeTrue();
        }

        [Test]
        public void Dot_separated_keys_should_match_with_wild_cards()
        {
            var bindingInfo = new BindingInfo(new QueueInfo("the queue", true, false, false, null), "a.*.c");

            bindingInfo.RoutingKeyMatches("a.b.c").ShouldBeTrue();
            bindingInfo.RoutingKeyMatches("a.d.c").ShouldBeTrue();
            bindingInfo.RoutingKeyMatches("a.b.d").ShouldBeFalse();
        }

        [Test]
        public void Should_match_wild_card()
        {
            BindingInfo.Match("a.*.c", "a.b.c").ShouldBeTrue();
            BindingInfo.Match("a.*.c", "a.x.c").ShouldBeTrue();
            BindingInfo.Match("a.*.c", "a.b.x").ShouldBeFalse();
            BindingInfo.Match("a.*.c", "x.b.c").ShouldBeFalse();
        }

        [Test]
        public void Should_match_sinlge_values()
        {
            BindingInfo.Match("one", "one").ShouldBeTrue();
            BindingInfo.Match("one", "two").ShouldBeFalse();
        }

        [Test]
        public void Should_match_single_hash()
        {
            BindingInfo.Match("#", "a.b.c").ShouldBeTrue();
        }

        [Test]
        public void Should_match_hash_at_start()
        {
            BindingInfo.Match("#.c", "a.b.c").ShouldBeTrue();
        }

        [Test]
        public void Should_match_hash_at_end()
        {
            BindingInfo.Match("a.#", "a.b.c").ShouldBeTrue();
        }

        [Test]
        public void Should_match_hash_in_middle()
        {
            BindingInfo.Match("a.#.d", "a.b.c.d").ShouldBeTrue();
        }

        [Test]
        public void Should_not_match_hash_if_end_does_not_match()
        {
            BindingInfo.Match("a.#.d", "a.b.c.e").ShouldBeFalse();
        }

        [Test]
        public void Should_match_hash_with_zero_chars()
        {
            BindingInfo.Match("a.#.d", "a.d").ShouldBeTrue();
        }

        [Test]
        public void Should_match_single_hash_with_empty_string()
        {
            BindingInfo.Match("#", "").ShouldBeTrue();
        }

        [Test]
        public void Should_not_match_wrong_numbers_of_parts()
        {
            BindingInfo.Match("a.b.c", "a.b.c.e").ShouldBeFalse();
            BindingInfo.Match("a.b.c.d", "a.b.c").ShouldBeFalse();
            BindingInfo.Match("a.b.#.d", "a.b.c.d.e").ShouldBeFalse();
        }
    }
}

// ReSharper restore InconsistentNaming