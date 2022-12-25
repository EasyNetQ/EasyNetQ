using RabbitMQ.Client;

namespace EasyNetQ.Persistent;

public readonly struct FuncBasedPersistentChannelAction<TResult> : IPersistentChannelAction<TResult>
{
    private readonly Func<IModel, TResult> func;

    public FuncBasedPersistentChannelAction(Func<IModel, TResult> func) => this.func = func;

    public TResult Invoke(IModel model) => func(model);
}
