using RabbitMQ.Client;

namespace EasyNetQ.Persistent;

public readonly struct FuncBasedPersistentChannelAction<TResult> : IPersistentChannelAction<TResult>
{
    private readonly Func<IChannel, TResult> func;

    public FuncBasedPersistentChannelAction(Func<IChannel, TResult> func) => this.func = func;

    public Task<BasicGetResult?> Invoke(IChannel channel) => func(channel);
}
