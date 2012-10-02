// ReSharper disable InconsistentNaming

using System;
using System.IO;
using NUnit.Framework;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class TryNextCollectionTests
    {
        private TryNextCollection<int> tryNextCollection;
        private StringWriter writer;    

        [SetUp]
        public void SetUp()
        {
            tryNextCollection = new TryNextCollection<int>
            {
                0,
                1,
                2,
                3,
            };

            writer = new StringWriter();
        }

        [Test]
        public void Should_end_after_every_item_has_been_returned()
        {
            do
            {
                var item = tryNextCollection.Current();
                writer.Write(item);
            } while (tryNextCollection.Next());

            writer.ToString().ShouldEqual("0123");
            tryNextCollection.Succeeded.ShouldBeFalse();
        }

        [Test]
        public void Should_end_once_success_is_called()
        {
            var count = 0;
            do
            {
                var item = tryNextCollection.Current();
                writer.Write(item);

                count++;
                if (count == 2) tryNextCollection.Success();

            } while (tryNextCollection.Next());

            writer.ToString().ShouldEqual("01");
            tryNextCollection.Succeeded.ShouldBeTrue();
        }

        [Test]
        public void Should_restart_from_next_item_and_then_try_all()
        {
            for (var i = 0; i < 10; i++)
            {
                var count = 0;
                tryNextCollection.Reset();
                do
                {
                    var item = tryNextCollection.Current();
                    writer.Write(item);

                    count++;
                    if (count == 3) tryNextCollection.Success();

                } while (tryNextCollection.Next());
            }

            writer.ToString().ShouldEqual("012301230123012301230123012301");
        }
    }
}

// ReSharper restore InconsistentNaming