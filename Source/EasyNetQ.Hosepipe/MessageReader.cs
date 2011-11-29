using System;
using System.Collections.Generic;
using System.IO;

namespace EasyNetQ.Hosepipe
{
    public class MessageReader : IMessageReader 
    {
        public IEnumerable<string> ReadMessages(QueueParameters parameters)
        {
            return ReadMessages(parameters, null);
        }

        public IEnumerable<string> ReadMessages(QueueParameters parameters, string messageName)
        {
            if (!Directory.Exists(parameters.MessageFilePath))
            {
                Console.WriteLine("Directory '{0}' does not exist", parameters.MessageFilePath);
                yield break;
            }

            var pattern = (messageName ?? "*") + ".*.message.txt";

            foreach (var file in Directory.GetFiles(parameters.MessageFilePath, pattern))
            {
                yield return File.ReadAllText(file);
            }
        }
    }
}