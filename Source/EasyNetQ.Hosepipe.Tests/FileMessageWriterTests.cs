// ReSharper disable InconsistentNaming

using System.Collections.Generic;
using NUnit.Framework;

namespace EasyNetQ.Hosepipe.Tests
{
    [TestFixture]
    public class FileMessageWriterTests
    {
        [SetUp]
        public void SetUp() {}

        [Test, Explicit("Writes files to the file system")]
        public void WriteSomeFiles()
        {
            var writer = new FileMessageWriter();
            var messages = new List<string>
            {
                "This is message one",
                "This is message two",
                "This is message three"
            };

            var parameters = new QueueParameters
            {
                QueueName = "Queue EasyNetQ_Tests_TestAsyncRequestMessage:EasyNetQ_Tests_Messages",
                MessageFilePath = @"C:\temp\MessageOutput"
            };

            writer.Write(messages, parameters);
        }

        [Test]
        public void Should_remove_invalid_file_chars()
        {
            const string originalQueueName = @"\A/B:C*D?E""F<G>H|I";
            const string expectedSanitisedQueueName = @"_A_B_C_D_E_F_G_H_I";

            var sanitisedQueueName = FileMessageWriter.SanitiseQueueName(originalQueueName);
            sanitisedQueueName.ShouldEqual(expectedSanitisedQueueName);
        }
    }
}

// ReSharper restore InconsistentNaming