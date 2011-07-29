using System;
using System.Collections.Generic;
using System.IO;

namespace EasyNetQ.Hosepipe
{
    public class MessageReader : IMessageReader 
    {
        public IEnumerable<string> ReadMessages(QueueParameters parameters)
        {
            if (!Directory.Exists(parameters.MessageFilePath))
            {
                Console.WriteLine("Directory '{0}' does not exist", parameters.MessageFilePath);
                yield break;
            }

            foreach (var file in Directory.GetFiles(parameters.MessageFilePath, "*.message.txt"))
            {
                yield return File.ReadAllText(file);
            }
        }
    }
}