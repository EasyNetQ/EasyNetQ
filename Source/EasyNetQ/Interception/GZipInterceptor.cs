using System.Buffers;
using System.IO.Compression;

namespace EasyNetQ.Interception;

/// <summary>
///     An interceptor which compresses and decompressed messages
/// </summary>
public class GZipInterceptor : IPublishConsumeInterceptor
{
    /// <inheritdoc />
    public PublishMessage OnPublish(in PublishMessage message)
    {
        var body = ArrayPool<byte>.Shared.Rent(message.Body.Length); // most likely rented array is larger than message.Body

        try
        {
            message.Body.CopyTo(body);
            using var output = new MemoryStream();
            using (var compressingStream = new GZipStream(output, CompressionMode.Compress))
                compressingStream.Write(body, 0, message.Body.Length);
            return new PublishMessage(message.Properties, output.ToArray()); // TODO: think of a better memory management for interceptors
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(body);
        }
    }

    /// <inheritdoc />
    public ConsumeMessage OnConsume(in ConsumeMessage message)
    {
        var body = ArrayPool<byte>.Shared.Rent(message.Body.Length); // most likely rented array is larger than message.Body

        try
        {
            message.Body.CopyTo(body);

            using var output = new MemoryStream();
            using (var compressedStream = new MemoryStream(body, 0, message.Body.Length))
            using (var decompressingStream = new GZipStream(compressedStream, CompressionMode.Decompress))
                decompressingStream.CopyTo(output);
            return new ConsumeMessage(message.ReceivedInfo, message.Properties, output.ToArray()); // TODO: think of better memory management for interceptors
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(body);
        }
    }
}
