using RabbitMQ.Client;

namespace EasyNetQ.Persistent;

public readonly struct FuncBasedPersistentChannelAction<TResult> : IPersistentChannelAction<TResult>
{
    private readonly Func<IChannel, Task<TResult>> func;

    public FuncBasedPersistentChannelAction(Func<IChannel, Task<TResult>> func) => this.func = func;

    public Task<TResult> InvokeAsync(IChannel channel, CancellationToken cancellationToken) => func(channel);
}
