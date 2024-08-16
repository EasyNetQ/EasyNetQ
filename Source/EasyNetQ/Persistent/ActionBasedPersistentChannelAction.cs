using RabbitMQ.Client;

namespace EasyNetQ.Persistent;

public readonly struct ActionBasedPersistentChannelAction : IPersistentChannelAction<bool>
{
    private readonly Action<IChannel> action;

    public ActionBasedPersistentChannelAction(Action<IChannel> action) => this.action = action;

    public Task<BasicGetResult?> Invoke(IChannel channel)
    {
        action(channel);
        return true;
    }
}
