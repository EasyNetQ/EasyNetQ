using RabbitMQ.Client;

namespace EasyNetQ.Persistent;

public readonly struct ActionBasedPersistentChannelAction : IPersistentChannelAction<bool>
{
    private readonly Action<IModel> action;

    public ActionBasedPersistentChannelAction(Action<IModel> action) => this.action = action;

    public bool Invoke(IModel model)
    {
        action(model);
        return true;
    }
}
