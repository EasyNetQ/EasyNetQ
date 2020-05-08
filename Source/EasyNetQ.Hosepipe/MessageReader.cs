using System;
using System.Collections.Generic;
using System.IO;

namespace EasyNetQ.Hosepipe
{
    public class MessageReader : IMessageReader
    {
        public IEnumerable<HosepipeMessage> ReadMessages(QueueParameters parameters)
        {
            return ReadMessages(parameters, null);
        }

        public IEnumerable<HosepipeMessage> ReadMessages(QueueParameters parameters, string messageName)
        {
            if (!Directory.Exists(parameters.MessagesOutputDirectory))
            {
                Console.WriteLine("Directory '{0}' does not exist", parameters.MessagesOutputDirectory);
                yield break;
            }

            var bodyPattern = (messageName ?? "*") + ".*.message.txt";

            foreach (var file in Directory.GetFiles(parameters.MessagesOutputDirectory, bodyPattern))
            {
                const string messageTag = ".message.";
                var directoryName = Path.GetDirectoryName(file);
                var fileName = Path.GetFileName(file);
                var propertiesFileName = Path.Combine(directoryName, fileName.Replace(messageTag, ".properties."));
                var infoFileName = Path.Combine(directoryName, fileName.Replace(messageTag, ".info."));

                var body = File.ReadAllText(file);

                var propertiesJson = File.ReadAllText(propertiesFileName);
                var properties = Newtonsoft.Json.JsonConvert.DeserializeObject<MessageProperties>(propertiesJson);

                var infoJson = File.ReadAllText(infoFileName);
                var info = Newtonsoft.Json.JsonConvert.DeserializeObject<MessageReceivedInfo>(infoJson);

                yield return new HosepipeMessage(body, properties, info);
            }
        }
    }
}
