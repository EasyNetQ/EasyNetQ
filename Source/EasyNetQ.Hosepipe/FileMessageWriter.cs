using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace EasyNetQ.Hosepipe
{
    public class FileMessageWriter : IMessageWriter
    {
        readonly static Regex invalidCharRegex = new Regex(@"[\\\/:\*\?\""\<\>|]");

        public void Write(IEnumerable<HosepipeMessage> messages, QueueParameters parameters)
        {
            var count = 0;
            foreach (var message in messages)
            {
                var uniqueFileName = SanitiseQueueName(parameters.QueueName) + "." + count.ToString();

                var bodyPath = Path.Combine(parameters.MessageFilePath, uniqueFileName + ".message.txt");
                var propertiesPath = Path.Combine(parameters.MessageFilePath, uniqueFileName + ".properties.txt");
                var infoPath = Path.Combine(parameters.MessageFilePath, uniqueFileName + ".info.txt");
                
                if(File.Exists(bodyPath))
                {
                    Console.WriteLine("Overwriting existing messsage file: {0}", bodyPath);
                }
                try
                {
                    File.WriteAllText(bodyPath, message.Body);
                    File.WriteAllText(propertiesPath, Newtonsoft.Json.JsonConvert.SerializeObject(message.Properties));
                    File.WriteAllText(infoPath, Newtonsoft.Json.JsonConvert.SerializeObject(message.Info));
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