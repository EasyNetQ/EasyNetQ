using System.Text;

namespace EasyNetQ.Consumer;

public class DefaultErrorMessageSerializer : IErrorMessageSerializer
{
    /// <inheritdoc />
    public string Serialize(byte[] messageBody) => Encoding.UTF8.GetString(messageBody);

    /// <inheritdoc />
    public byte[] Deserialize(string messageBody) => Encoding.UTF8.GetBytes(messageBody);
}
