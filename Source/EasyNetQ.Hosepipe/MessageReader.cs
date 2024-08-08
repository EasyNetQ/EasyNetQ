using Newtonsoft.Json;

namespace EasyNetQ.Hosepipe;

public class MessageReader : IMessageReader
{
    public IAsyncEnumerable<HosepipeMessage> ReadMessagesAsync(QueueParameters parameters)
    {
        return ReadMessagesAsync(parameters, null);
    }

    public async IAsyncEnumerable<HosepipeMessage> ReadMessagesAsync(QueueParameters parameters, string messageName)
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

            var body = await File.ReadAllTextAsync(file);

            var propertiesJson = await File.ReadAllTextAsync(propertiesFileName);
            var properties = JsonConvert.DeserializeObject<MessageProperties>(propertiesJson);

            var infoJson = await File.ReadAllTextAsync(infoFileName);
            var info = JsonConvert.DeserializeObject<MessageReceivedInfo>(infoJson);

            yield return new HosepipeMessage(body, properties, info);
        }
    }
}
