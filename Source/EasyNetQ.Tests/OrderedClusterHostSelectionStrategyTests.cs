// ReSharper disable InconsistentNaming

using System.IO;
using NUnit.Framework;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class OrderedClusterHostSelectionStrategyTests
    {
        private IClusterHostSelectionStrategy<string> defaultClusterHostSelectionStrategy;
        private StringWriter writer;    

        [SetUp]
        public void SetUp()
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

        [Test]
        public void Should_end_after_every_item_has_been_returned()
        {
            do
            {
                var item = defaultClusterHostSelectionStrategy.Current();
                writer.Write(item);
            } while (defaultClusterHostSelectionStrategy.Next());

            writer.ToString().ShouldEqual("0123");
            defaultClusterHostSelectionStrategy.Succeeded.ShouldBeFalse();
        }

        [Test]
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

            writer.ToString().ShouldEqual("01");
            defaultClusterHostSelectionStrategy.Succeeded.ShouldBeTrue();
        }

        [Test]
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

            writer.ToString().ShouldEqual("012_301_230_123_012_301_230_123_012_301_");
        }
    }
}

// ReSharper restore InconsistentNaming