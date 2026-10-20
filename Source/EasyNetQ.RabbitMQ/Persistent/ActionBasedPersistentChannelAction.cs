using RabbitMQ.Client;

namespace EasyNetQ.Persistent;

public readonly struct ActionBasedPersistentChannelAction : IPersistentChannelAction<bool>
{
    private readonly Func<IChannel, Task> action;

    public ActionBasedPersistentChannelAction(Func<IChannel, Task> action) => this.action = action;

    public async Task<bool> InvokeAsync(IChannel channel, CancellationToken cancellationToken = default)
    {
        await action(channel);
        return true;
    }
}
