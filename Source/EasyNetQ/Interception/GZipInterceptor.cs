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
        public ProducedMessage OnProduce(ProducedMessage message)
        {
            var properties = message.Properties;
            var body = message.Body;
            using var output = new MemoryStream();
            using (var compressingStream = new GZipStream(output, CompressionMode.Compress))
                compressingStream.Write(body, 0, body.Length);
            return new ProducedMessage(properties, output.ToArray());
        }

        /// <inheritdoc />
        public ConsumedMessage OnConsume(ConsumedMessage message)
        {
            var receivedInfo = message.ReceivedInfo;
            var properties = message.Properties;
            var body = message.Body;
            using var output = new MemoryStream();
            using (var compressedStream = new MemoryStream(body))
            using (var decompressingStream = new GZipStream(compressedStream, CompressionMode.Decompress))
                decompressingStream.CopyTo(output);
            return new ConsumedMessage(receivedInfo, properties, output.ToArray());
        }
    }
}
