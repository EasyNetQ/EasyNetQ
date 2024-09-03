using Newtonsoft.Json;
using System.Runtime.CompilerServices;

namespace EasyNetQ.Hosepipe;

public class MessageReader : IMessageReader
{
    public IAsyncEnumerable<HosepipeMessage> ReadMessagesAsync(QueueParameters parameters, CancellationToken cancellationToken = default)
    {
        return ReadMessagesAsync(parameters, null, cancellationToken);
    }

    public async IAsyncEnumerable<HosepipeMessage> ReadMessagesAsync(QueueParameters parameters, string messageName, [EnumeratorCancellation] CancellationToken cancellationToken = default)
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
            var propertiesFileName = Path.Combine(directoryName!, fileName.Replace(messageTag, ".properties."));
            var infoFileName = Path.Combine(directoryName!, fileName.Replace(messageTag, ".info."));

            var body = await File.ReadAllTextAsync(file, cancellationToken);

            var propertiesJson = await File.ReadAllTextAsync(propertiesFileName, cancellationToken);
            var properties = JsonConvert.DeserializeObject<MessageProperties>(propertiesJson);

            var infoJson = await File.ReadAllTextAsync(infoFileName, cancellationToken);
            var info = JsonConvert.DeserializeObject<MessageReceivedInfo>(infoJson);

            yield return new HosepipeMessage(body, properties, info);
        }
    }
}
