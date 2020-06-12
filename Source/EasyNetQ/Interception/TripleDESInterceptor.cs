using System.Security.Cryptography;

namespace EasyNetQ.Interception
{
    /// <summary>
    ///     An interceptor which encrypts and decrypts messages
    /// </summary>
    public class TripleDESInterceptor : IProduceConsumeInterceptor
    {
        private readonly byte[] iv;
        private readonly byte[] key;

        public TripleDESInterceptor(byte[] key, byte[] iv)
        {
            this.iv = iv;
            this.key = key;
        }

        /// <inheritdoc />
        public ProducedMessage OnProduce(ProducedMessage message)
        {
            var properties = message.Properties;
            var body = message.Body;
            using var tripleDes = TripleDES.Create();
            using var tripleDesEncryptor = tripleDes.CreateEncryptor(key, iv);
            var encryptedBody = tripleDesEncryptor.TransformFinalBlock(body, 0, body.Length);
            return new ProducedMessage(properties, encryptedBody);
        }

        /// <inheritdoc />
        public ConsumedMessage OnConsume(ConsumedMessage message)
        {
            using var tripleDes = TripleDES.Create();
            using var tripleDesDecryptor = tripleDes.CreateDecryptor(key, iv);

            var receivedInfo = message.ReceivedInfo;
            var properties = message.Properties;
            var body = message.Body;
            return new ConsumedMessage(
                receivedInfo, properties, tripleDesDecryptor.TransformFinalBlock(body, 0, body.Length)
            );
        }
    }
}
