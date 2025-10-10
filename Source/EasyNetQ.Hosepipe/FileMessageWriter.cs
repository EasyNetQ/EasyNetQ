using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace EasyNetQ.Hosepipe;

public class FileMessageWriter : IMessageWriter
{
    private static readonly Regex InvalidCharRegex = new(@"[\\\/:\*\?\""\<\>|]", RegexOptions.Compiled);

    public void Write(IEnumerable<HosepipeMessage> messages, QueueParameters parameters)
    {
        if (!Directory.Exists(parameters.MessagesOutputDirectory))
        {
            Console.WriteLine("Creating messages output directory: {0}", parameters.MessagesOutputDirectory);
            Directory.CreateDirectory(parameters.MessagesOutputDirectory);
        }

        var count = 0;
        foreach (var message in messages)
        {
            var uniqueFileName = SanitiseQueueName(parameters.QueueName) + "." + count;

            var bodyPath = Path.Combine(parameters.MessagesOutputDirectory, uniqueFileName + ".message.txt");
            var propertiesPath = Path.Combine(parameters.MessagesOutputDirectory, uniqueFileName + ".properties.txt");
            var infoPath = Path.Combine(parameters.MessagesOutputDirectory, uniqueFileName + ".info.txt");

            if (File.Exists(bodyPath))
            {
                Console.WriteLine("Overwriting existing message file: {0}", bodyPath);
            }

            File.WriteAllText(bodyPath, message.Body);
            File.WriteAllText(propertiesPath, Newtonsoft.Json.JsonConvert.SerializeObject(message.Properties));
            File.WriteAllText(infoPath, Newtonsoft.Json.JsonConvert.SerializeObject(message.Info));

            count++;
        }
    }

    public static string SanitiseQueueName(string queueName)
    {
        return InvalidCharRegex.Replace(queueName, "_");
    }

    public async Task WriteAsync(IAsyncEnumerable<HosepipeMessage> messages, QueueParameters parameters, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(parameters.MessagesOutputDirectory))
        {
            Console.WriteLine("Creating messages output directory: {0}", parameters.MessagesOutputDirectory);
            Directory.CreateDirectory(parameters.MessagesOutputDirectory);
        }

        var count = 0;
        await foreach (var message in messages)
        {
            var uniqueFileName = SanitiseQueueName(parameters.QueueName) + "." + count;

            var bodyPath = Path.Combine(parameters.MessagesOutputDirectory, uniqueFileName + ".message.txt");
            var propertiesPath = Path.Combine(parameters.MessagesOutputDirectory, uniqueFileName + ".properties.txt");
            var infoPath = Path.Combine(parameters.MessagesOutputDirectory, uniqueFileName + ".info.txt");

            if (File.Exists(bodyPath))
            {
                Console.WriteLine("Overwriting existing message file: {0}", bodyPath);
            }

            await File.WriteAllTextAsync(bodyPath, message.Body);
            await File.WriteAllTextAsync(propertiesPath, Newtonsoft.Json.JsonConvert.SerializeObject(message.Properties));
            await File.WriteAllTextAsync(infoPath, Newtonsoft.Json.JsonConvert.SerializeObject(message.Info));

            count++;
        }
    }
}
