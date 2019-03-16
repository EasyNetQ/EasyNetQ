// ReSharper disable InconsistentNaming

using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace EasyNetQ.Tests
{
    public class RandomClusterHostSelectionStrategyTests
    {
        private IClusterHostSelectionStrategy<string> clusterHostSelectionStrategy;
        private HashSet<string> hosts;

        public RandomClusterHostSelectionStrategyTests()
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

            hosts.Should().Contain(new[] {"0", "1", "2", "3"});
            clusterHostSelectionStrategy.Succeeded.Should().BeFalse();
        }

        [Fact]
        public void Should_forget_success_after_reset()
        {
            do
            {
                clusterHostSelectionStrategy.Current();
                clusterHostSelectionStrategy.Success();
            } while (clusterHostSelectionStrategy.Next());

            clusterHostSelectionStrategy.Succeeded.Should().BeTrue();
            clusterHostSelectionStrategy.Reset();
            clusterHostSelectionStrategy.Succeeded.Should().BeFalse();
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

            clusterHostSelectionStrategy.Succeeded.Should().BeTrue();
        }

    }
}

// ReSharper restore InconsistentNaming