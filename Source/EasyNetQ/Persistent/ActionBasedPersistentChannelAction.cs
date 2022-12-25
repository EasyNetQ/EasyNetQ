using EasyNetQ.Internals;
using RabbitMQ.Client;

namespace EasyNetQ.Persistent;

public readonly struct ActionBasedPersistentChannelAction : IPersistentChannelAction<NoResult>
{
    private readonly Action<IModel> action;

    public ActionBasedPersistentChannelAction(Action<IModel> action) => this.action = action;

    public NoResult Invoke(IModel model)
    {
        action(model);
        return NoResult.Instance;
    }
}
