﻿// ReSharper disable InconsistentNaming

using System.Collections.Generic;
using NUnit.Framework;

namespace EasyNetQ.Tests
{
    public class RandomClusterHostSelectionStrategyTests
    {
        private IClusterHostSelectionStrategy<string> clusterHostSelectionStrategy;
        private HashSet<string> hosts;

        [SetUp]
        public void SetUp()
        {
            clusterHostSelectionStrategy = new RandomClusterHostSelectionStrategy<string>
            {
                "0",
                "1",
                "2",
                "3",
            };
            hosts = new HashSet<string>();
        }

        [Fact]
        public void Should_end_after_every_item_has_been_returned()
        {
            do
            {
                var item = clusterHostSelectionStrategy.Current();
                hosts.Add(item);
            } while (clusterHostSelectionStrategy.Next());

            Assert.True(hosts.Contains("0"));
            Assert.True(hosts.Contains("1"));
            Assert.True(hosts.Contains("2"));
            Assert.True(hosts.Contains("3"));
            clusterHostSelectionStrategy.Succeeded.ShouldBeFalse();
        }

        [Fact]
        public void Should_forget_success_after_reset()
        {
            do
            {
                clusterHostSelectionStrategy.Current();
                clusterHostSelectionStrategy.Success();
            } while (clusterHostSelectionStrategy.Next());
            clusterHostSelectionStrategy.Succeeded.ShouldBeTrue();
            clusterHostSelectionStrategy.Reset();
            clusterHostSelectionStrategy.Succeeded.ShouldBeFalse();
        }

        [Fact]
        public void Should_end_once_success_is_called()
        {
            var count = 0;
            do
            {
                var item = clusterHostSelectionStrategy.Current();
                hosts.Add(item);
      
                count++;
                if (count == 2) clusterHostSelectionStrategy.Success();

            } while (clusterHostSelectionStrategy.Next());

            clusterHostSelectionStrategy.Succeeded.ShouldBeTrue();
        }

    }
}

// ReSharper restore InconsistentNaming