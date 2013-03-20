namespace EasyNetQ
{
    public interface IConsume<in T> where T : class
    {
        void Consume(T message);
    }
}