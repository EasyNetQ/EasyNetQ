using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace EasyNetQ.Hosepipe;

public class FileMessageWriter : IMessageWriter
{
    private static readonly Regex InvalidCharRegex = new(@"[\\\/:\*\?\""\<\>|]", RegexOptions.Compiled);

    public async Task WriteAsync(
        IAsyncEnumerable<HosepipeMessage> messages,
        QueueParameters parameters,
        CancellationToken cancellationToken = default)
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

            await File.WriteAllTextAsync(bodyPath, message.Body, cancellationToken);
            await File.WriteAllTextAsync(propertiesPath, JsonConvert.SerializeObject(message.Properties), cancellationToken);
            await File.WriteAllTextAsync(infoPath, JsonConvert.SerializeObject(message.Info), cancellationToken);

            count++;
        }
    }

    public static string SanitiseQueueName(string queueName)
    {
        return InvalidCharRegex.Replace(queueName, "_");
    }
}
