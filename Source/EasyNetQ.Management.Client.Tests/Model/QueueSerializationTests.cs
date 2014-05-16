// ReSharper disable InconsistentNaming

using System.Collections.Generic;
using EasyNetQ.Management.Client.Model;
using NUnit.Framework;

namespace EasyNetQ.Management.Client.Tests.Model
{
    [TestFixture]
    public class QueueSerializationTests
    {
        private List<Queue> queues;

        [Test]
        public void BackingQueueStatus_With_NextSeqId_Exceeding_IntMaxLength_Can_Be_Deserialized()
        {
            queues = ResourceLoader.LoadObjectFromJson<List<Queue>>("Queues.json");
            var queue = queues[1];

            queue.BackingQueueStatus.NextSeqId.ShouldEqual(((long)int.MaxValue) + 1);
        }
    }
}

// ReSharper restore InconsistentNaming