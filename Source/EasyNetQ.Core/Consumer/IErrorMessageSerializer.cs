namespace EasyNetQ.Consumer
{
    public interface IErrorMessageSerializer
    {
        string Serialize(byte[] messageBody);

        byte[] Deserialize(string messageBody);
    }
}
