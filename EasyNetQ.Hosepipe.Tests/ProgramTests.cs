// ReSharper disable InconsistentNaming

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace EasyNetQ.Hosepipe.Tests
{
    [TestFixture]
    public class ProgramTests
    {
        private Program program;

        [SetUp]
        public void SetUp()
        {
            program = new Program(
                new ArgParser(), new MockQueueRetrieval(), new MockMessageWriter());
        }

        [Test]
        public void Should_output_messages_to_directory_with_dump()
        {
            var args = new[]
            {
                "dump",
                "s:localhost",
                "q:EasyNetQ_Default_Error_Queue"
            };

            program.Start(args);
        } 
    }

    public class MockMessageWriter : IMessageWriter
    {
        public void Write(IEnumerable<string> messages, string queueName)
        {
            
        }
    }

    public class MockQueueRetrieval : IQueueRetreival
    {
        public IEnumerable<string> GetMessagesFromQueue(QueueParameters queueParameters)
        {
            return Enumerable.Empty<string>();
        }
    }
}

// ReSharper restore InconsistentNaming