using System.Security.Cryptography;

namespace EasyNetQ.Interception
{
    public class TripleDESInterceptor : IProduceConsumeInterceptor
    {
        private readonly byte[] iv;
        private readonly byte[] key;

        public TripleDESInterceptor(byte[] key, byte[] iv)
        {
            this.iv = iv;
            this.key = key;
        }

        public RawMessage OnProduce(RawMessage rawMessage)
        {
            var properties = rawMessage.Properties;
            var body = rawMessage.Body;
            using (var tripleDes = TripleDES.Create())
            {
                using (var tripleDesEncryptor = tripleDes.CreateEncryptor(key, iv))
                {
                    var encryptedBody = tripleDesEncryptor.TransformFinalBlock(body, 0, body.Length);
                    return new RawMessage(properties, encryptedBody);
                }
            }
        }

        public RawMessage OnConsume(RawMessage rawMessage)
        {
            var properties = rawMessage.Properties;
            var body = rawMessage.Body;
            using (var tripleDes = TripleDES.Create())
            {
                using (var tripleDesDecryptor = tripleDes.CreateDecryptor(key, iv))
                {
                    var decryptedBody = tripleDesDecryptor.TransformFinalBlock(body, 0, body.Length);
                    return new RawMessage(properties, decryptedBody);
                }
            }
        }
    }
}