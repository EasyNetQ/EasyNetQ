using System.Collections.Generic;
using System.IO;

namespace EasyNetQ.Hosepipe
{
    public class FileMessageWriter : IMessageWriter
    {
        private readonly string outputDirectoryPath;

        public FileMessageWriter(string outputDirectoryPath)
        {
            this.outputDirectoryPath = outputDirectoryPath;
        }

        public void Write(IEnumerable<string> messages, string queueName)
        {
            var count = 0;
            foreach (string message in messages)
            {
                var fileName = queueName + "." + count.ToString() + ".message.txt";
                var path = Path.Combine(outputDirectoryPath, fileName);
                File.WriteAllText(path, message);
                count++;
            }
        }
    }
}