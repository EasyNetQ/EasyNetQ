using System;
using System.Text;

namespace EasyNetQ.Producer
{
    internal static class MessagePropertiesExtensions
    {
        public const string ConfirmationIdHeader = "EasyNetQ.Confirmation.Id";

        public static MessageProperties SetConfirmationId(this MessageProperties properties, ulong confirmationId)
        {
            properties.Headers[ConfirmationIdHeader] = confirmationId.ToString();
            return properties;
        }

        public static bool TryGetConfirmationId(this MessageProperties properties, out ulong confirmationId)
        {
            confirmationId = 0;
            return properties.Headers.TryGetValue(ConfirmationIdHeader, out var value) &&
                   ulong.TryParse(Encoding.UTF8.GetString(value as byte[] ?? Array.Empty<byte>()), out confirmationId);
        }
    }
}
