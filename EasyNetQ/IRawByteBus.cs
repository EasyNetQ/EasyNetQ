namespace EasyNetQ
{
    public interface IRawByteBus
    {
        void RawPublish(string typeName, byte[] messageBody);
    }
}