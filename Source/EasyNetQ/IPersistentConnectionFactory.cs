namespace EasyNetQ
{
    public interface IPersistentConnectionFactory
    {
        IPersistentConnection CreateConnection();
    }
}