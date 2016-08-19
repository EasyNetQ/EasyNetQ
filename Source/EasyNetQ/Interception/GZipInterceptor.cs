using System.IO;
using System.IO.Compression;

namespace EasyNetQ.Interception
{
    public class GZipInterceptor : IProduceConsumeInterceptor
    {
        public RawMessage OnProduce(RawMessage rawMessage)
        {
            var properties = rawMessage.Properties;
            var body = rawMessage.Body;
            using (var output = new MemoryStream())
            {
                using (var compressingStream = new GZipStream(output, CompressionMode.Compress))
                    compressingStream.Write(body, 0, body.Length);
                return new RawMessage(properties, output.ToArray());
            }
        }

        public RawMessage OnConsume(RawMessage rawMessage)
        {
            var properties = rawMessage.Properties;
            var body = rawMessage.Body;
            using (var output = new MemoryStream())
            {
                using (var compressedStream = new MemoryStream(body))
                {
                    using (var decompressingStream = new GZipStream(compressedStream, CompressionMode.Decompress))
                        decompressingStream.CopyTo(output);
                }
                return new RawMessage(properties, output.ToArray());
            }
        }
    }
}