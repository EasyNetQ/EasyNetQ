namespace EasyNetQ.AutoSubscribe;

public interface IConsumeAsync<in T> where T : class
{
    Task ConsumeAsync(T message, CancellationToken cancellationToken = default);
}
