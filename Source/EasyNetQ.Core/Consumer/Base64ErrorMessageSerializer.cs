using System;

namespace EasyNetQ.Consumer
{
    public class Base64ErrorMessageSerializer : IErrorMessageSerializer
    {
        public string Serialize(byte[] messageBody)
        {
            return Convert.ToBase64String(messageBody);
        }

        public byte[] Deserialize(string messageBody)
        {
            return Convert.FromBase64String(messageBody);
        }
    }
}
