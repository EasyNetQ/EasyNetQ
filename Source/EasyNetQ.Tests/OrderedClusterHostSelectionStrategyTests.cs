// ReSharper disable InconsistentNaming

using System.IO;
using FluentAssertions;
using Xunit;

namespace EasyNetQ.Tests
{
    public class OrderedClusterHostSelectionStrategyTests
    {
        private IClusterHostSelectionStrategy<string> defaultClusterHostSelectionStrategy;
        private StringWriter writer;    

        public OrderedClusterHostSelectionStrategyTests()
        {
            defaultClusterHostSelectionStrategy = new OrderedClusterHostSelectionStrategy<string>
            {
                "0",
                "1",
                "2",
                "3",
            };

            writer = new StringWriter();
        }

        [Fact]
        public void Should_end_after_every_item_has_been_returned()
        {
            do
            {
                var item = defaultClusterHostSelectionStrategy.Current();
                writer.Write(item);
            } while (defaultClusterHostSelectionStrategy.Next());

            writer.ToString().Should().Be("0123");
            defaultClusterHostSelectionStrategy.Succeeded.Should().BeFalse();
        }

        [Fact]
        public void Should_end_once_success_is_called()
        {
            var count = 0;
            do
            {
                var item = defaultClusterHostSelectionStrategy.Current();
                writer.Write(item);

                count++;
                if (count == 2) defaultClusterHostSelectionStrategy.Success();

            } while (defaultClusterHostSelectionStrategy.Next());

            writer.ToString().Should().Be("01");
            defaultClusterHostSelectionStrategy.Succeeded.Should().BeTrue();
        }

        [Fact]
        public void Should_restart_from_next_item_and_then_try_all()
        {
            for (var i = 0; i < 10; i++)
            {
                var count = 0;
                defaultClusterHostSelectionStrategy.Reset();
                do
                {
                    var item = defaultClusterHostSelectionStrategy.Current();
                    writer.Write(item);

                    count++;
                    if (count == 3) defaultClusterHostSelectionStrategy.Success();

                } while (defaultClusterHostSelectionStrategy.Next());
                writer.Write("_");
            }

            writer.ToString().Should().Be("012_301_230_123_012_301_230_123_012_301_");
        }
    }
}

// ReSharper restore InconsistentNaming