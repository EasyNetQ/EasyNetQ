// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace EasyNetQ.Hosepipe.Tests
{
    [TestFixture]
    public class ProgramTests
    {
        private Program program;
        private MockMessageWriter messageWriter;
        private MockQueueRetrieval queueRetrieval;

        [SetUp]
        public void SetUp()
        {
            messageWriter = new MockMessageWriter();
            queueRetrieval = new MockQueueRetrieval();

            program = new Program(
                new ArgParser(), queueRetrieval, messageWriter);
        }

        private const string expectedDumpOutput =
@"0 Messages from queue 'EasyNetQ_Default_Error_Queue'
output to directory 'C:\Source\Mike.AmqpSpike\EasyNetQ.Hosepipe.Tests\bin\Debug'
";

        [Test]
        public void Should_output_messages_to_directory_with_dump()
        {
            var args = new[]
            {
                "dump",
                "s:localhost",
                "q:EasyNetQ_Default_Error_Queue"
            };

            var writer = new StringWriter();
            Console.SetOut(writer);

            program.Start(args);

            writer.GetStringBuilder().ToString().ShouldEqual(expectedDumpOutput);

            messageWriter.Parameters.QueueName.ShouldEqual("EasyNetQ_Default_Error_Queue");
            messageWriter.Parameters.HostName.ShouldEqual("localhost");
        } 
    }

    public class MockMessageWriter : IMessageWriter
    {
        public QueueParameters Parameters { get; set; }

        public void Write(IEnumerable<string> messages, QueueParameters queueParameters)
        {
            Parameters = queueParameters;
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