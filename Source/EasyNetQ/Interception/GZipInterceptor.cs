using System.IO;
using System.IO.Compression;

namespace EasyNetQ.Interception
{
    /// <summary>
    ///     An interceptor which compresses and decompressed messages
    /// </summary>
    public class GZipInterceptor : IProduceConsumeInterceptor
    {
        /// <inheritdoc />
        public ProducedMessage OnProduce(in ProducedMessage message)
        {
            var properties = message.Properties;
            var body = message.Body.ToArray(); // TODO Do not copy here
            using var output = new MemoryStream();
            using (var compressingStream = new GZipStream(output, CompressionMode.Compress))
                compressingStream.Write(body, 0, body.Length);
            return new ProducedMessage(properties, output.ToArray());
        }

        /// <inheritdoc />
        public ConsumedMessage OnConsume(in ConsumedMessage message)
        {
            var receivedInfo = message.ReceivedInfo;
            var properties = message.Properties;
            var body = message.Body;
            using var output = new MemoryStream();
            using (var compressedStream = new MemoryStream(body.ToArray())) // TODO Do not copy here
            using (var decompressingStream = new GZipStream(compressedStream, CompressionMode.Decompress))
                decompressingStream.CopyTo(output);
            return new ConsumedMessage(receivedInfo, properties, output.ToArray());
        }
    }
}
