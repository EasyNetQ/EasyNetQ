using System;
using System.IO;
using System.Threading;

namespace EasyNetQ.Hosepipe.Tests
{
    [Traits.Explicit(@"Requires a RabbitMQ broker on localhost and access to C:\Temp\MessageOutput")]
    public class ProgramIntegrationTests
    {
        private const string outputPath = @"C:\Temp\MessageOutput";
        private const string queue = "EasyNetQ_Hosepipe_Tests_ProgramIntegrationTests+TestMessage:EasyNetQ_Hosepipe_Tests_hosepipe";

        public void DumpMessages()
        {
            ClearDirectory();

            var args = new[]
                {
                    "dump",
                    "s:localhost",
                    string.Format("q:{0}", queue),
                    string.Format("o:{0}", outputPath)
                };

            Program.Main(args);

            ListDirectory();
        }

        public void InsertMessages()
        {
            var args = new[]
                {
                    "insert",
                    "s:localhost",
                    string.Format("o:{0}", outputPath)
                };

            Program.Main(args);
        }

        public void ListDirectory()
        {
            foreach (var file in Directory.GetFiles(outputPath))
            {
                Console.Out.WriteLine(file);
                Console.Out.WriteLine(File.ReadAllText(file));
                Console.Out.WriteLine("");
            }
        }

        public void ClearDirectory()
        {
            foreach (var file in Directory.GetFiles(outputPath))
            {
                File.Delete(file);
            }
        }

        public void PublishSomeMessages()
        {
            var bus = RabbitHutch.CreateBus("host=localhost");

            for (int i = 0; i < 10; i++)
            {
                bus.PubSub.Publish(new TestMessage { Text = string.Format("\n>>>>>> Message {0}\n", i) });
            }

            bus.Dispose();
        }

        public void ConsumeMessages()
        {
            var bus = RabbitHutch.CreateBus("host=localhost");

            bus.PubSub.Subscribe<TestMessage>("hosepipe", message => Console.WriteLine(message.Text));

            Thread.Sleep(1000);

            bus.Dispose();
        }

        private class TestMessage
        {
            public string Text { get; set; }
        }
    }
}
