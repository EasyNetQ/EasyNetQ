using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace EasyNetQ.Hosepipe
{
    public class FileMessageWriter : IMessageWriter
    {
        readonly static Regex invalidCharRegex = new Regex(@"[\\\/:\*\?\""\<\>|]");

        public void Write(IEnumerable<string> messages, QueueParameters parameters)
        {
            var count = 0;
            foreach (string message in messages)
            {
                var fileName = SanitiseQueueName(parameters.QueueName) + "." + count.ToString() + ".message.txt";
                var path = Path.Combine(parameters.MessageFilePath, fileName);
                if(File.Exists(path))
                {
                    Console.WriteLine("Overwriting existing messsage file: {0}", path);
                }
                try
                {
                    File.WriteAllText(path, message);
                }
                catch (DirectoryNotFoundException)
                {
                    throw new EasyNetQHosepipeException(
                        string.Format("Directory '{0}' does not exist", parameters.MessageFilePath));
                }
                count++;
            }
        }

        public static string SanitiseQueueName(string queueName)
        {
            return invalidCharRegex.Replace(queueName, "_");
        }
    }
}