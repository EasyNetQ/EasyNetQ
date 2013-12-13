// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using System.Linq;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class RandomClusterHostSelectionStrategyTests
    {
        private IClusterHostSelectionStrategy<string> defaultClusterHostSelectionStrategy;
        private HashSet<string> hosts;

        [SetUp]
        public void SetUp()
        {
            defaultClusterHostSelectionStrategy = new RandomClusterHostSelectionStrategy<string>
            {
                "0",
                "1",
                "2",
                "3",
            };
            hosts = new HashSet<string>();
        }

        [Test]
        public void Should_end_after_every_item_has_been_returned()
        {
            do
            {
                var item = defaultClusterHostSelectionStrategy.Current();
                hosts.Add(item);
            } while (defaultClusterHostSelectionStrategy.Next());

            Assert.IsTrue(hosts.Contains("0"));
            Assert.IsTrue(hosts.Contains("1"));
            Assert.IsTrue(hosts.Contains("2"));
            Assert.IsTrue(hosts.Contains("3"));
            defaultClusterHostSelectionStrategy.Succeeded.ShouldBeFalse();
        }

        [Test]
        public void Should_end_once_success_is_called()
        {
            var count = 0;
            do
            {
                var item = defaultClusterHostSelectionStrategy.Current();
                hosts.Add(item);
      
                count++;
                if (count == 2) defaultClusterHostSelectionStrategy.Success();

            } while (defaultClusterHostSelectionStrategy.Next());

            defaultClusterHostSelectionStrategy.Succeeded.ShouldBeTrue();
        }

    }
}

// ReSharper restore InconsistentNaming