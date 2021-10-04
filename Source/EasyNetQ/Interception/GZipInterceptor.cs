using System.Buffers;
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
            var body = ArrayPool<byte>.Shared.Rent(message.Body.Length); // most likely rented array is larger than message.Body

            try
            {
                message.Body.CopyTo(body);
                using var output = new MemoryStream();
                using (var compressingStream = new GZipStream(output, CompressionMode.Compress))
                    compressingStream.Write(body, 0, message.Body.Length);
                return new ProducedMessage(message.Properties, output.ToArray()); // TODO: think of a better memory management for interceptors
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(body);
            }
        }

        /// <inheritdoc />
        public ConsumedMessage OnConsume(in ConsumedMessage message)
        {
            var body = ArrayPool<byte>.Shared.Rent(message.Body.Length); // most likely rented array is larger than message.Body

            try
            {
                message.Body.CopyTo(body);

                using var output = new MemoryStream();
                using (var compressedStream = new MemoryStream(body, 0, message.Body.Length))
                using (var decompressingStream = new GZipStream(compressedStream, CompressionMode.Decompress))
                    decompressingStream.CopyTo(output);
                return new ConsumedMessage(message.ReceivedInfo, message.Properties, output.ToArray()); // TODO: think of better memory management for interceptors
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(body);
            }
        }
    }
}
