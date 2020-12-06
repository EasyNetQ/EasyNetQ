// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace EasyNetQ.Hosepipe.Tests
{
    public class FileMessageWriterTests
    {
        private readonly string tempDirectory = Path.Combine(Path.GetTempPath(), @"MessageOutput");

        [Fact][Traits.Explicit("Writes files to the file system")]
        public void WriteSomeFiles()
        {
            var directory = new DirectoryInfo(tempDirectory);

            if (!directory.Exists)
            {
                directory.Create();
            }
            else
            {
                foreach (var file in directory.EnumerateFiles())
                {
                    file.Delete();
                }
            }

            var properties = new MessageProperties();
            var info = Helper.CreateMessageReceivedInfo();
            var writer = new FileMessageWriter();
            var messages = new List<HosepipeMessage>
            {
                new HosepipeMessage("This is message one", properties, info),
                new HosepipeMessage("This is message two", properties, info),
                new HosepipeMessage("This is message three", properties, info)
            };

            var parameters = new QueueParameters
            {
                QueueName = "Queue EasyNetQ_Tests_TestAsyncRequestMessage:EasyNetQ_Tests_Messages",
                MessagesOutputDirectory = tempDirectory
            };

            writer.Write(messages, parameters);

            foreach (var file in directory.EnumerateFiles())
            {
                Console.Out.WriteLine("{0}", file.Name);
            }
        }

        [Fact]
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
