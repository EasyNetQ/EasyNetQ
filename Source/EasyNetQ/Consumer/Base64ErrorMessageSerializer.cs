using System;

namespace EasyNetQ.Consumer
{
    public class Base64ErrorMessageSerializer : IErrorMessageSerializer
    {
        /// <inheritdoc />
        public string Serialize(byte[] messageBody) => Convert.ToBase64String(messageBody);

        /// <inheritdoc />
        public byte[] Deserialize(string messageBody) => Convert.FromBase64String(messageBody);
    }
}
