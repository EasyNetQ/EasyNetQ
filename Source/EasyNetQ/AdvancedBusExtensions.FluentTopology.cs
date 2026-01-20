using EasyNetQ.Topology;

namespace EasyNetQ;

/// <summary>
///     Various extensions for <see cref="IAdvancedBus"/>
/// </summary>
public static partial class AdvancedBusExtensions
{
    /// <summary>
    /// Declare a queue. If the queue already exists this method does nothing
    /// </summary>
    /// <param name="advancedBus">The bus instance</param>
    /// <param name="queue">The name of the queue</param>
    /// <param name="cancellationToken">The cancellation token</param>
    public static Task<Queue> QueueDeclareAsync(
  this IAdvancedBus advancedBus,
  string queue,
  CancellationToken cancellationToken = default)
    {
        return advancedBus.QueueDeclareAsync(queue, options => { }, cancellationToken);
    }
    /// <summary>
    /// Declare a queue. If the queue already exists this method does nothing
    /// </summary>
    /// <param name="advancedBus">The bus instance</param>
    /// <param name="durable"></param>
    /// <param name="exclusive"></param>
    /// <param name="autoDelete"></param>
    /// <param name="arguments"></param>
    /// <param name="cancellationToken">The cancellation token</param>
    public static Task<Queue> QueueDeclareAsync(
  this IAdvancedBus advancedBus,
  string queue,
  bool durable = true,
  bool exclusive = false,
  bool autoDelete = false,
  IDictionary<string, object> arguments = null,
  CancellationToken cancellationToken = default)
    {
        return advancedBus.QueueDeclareAsync(
          queue,
          options =>
          {
              options.AsDurable(durable).AsExclusive(exclusive).AsAutoDelete(autoDelete);
              if (arguments != null)
              {
                  foreach (var item in arguments)
                  {
                      options.WithArgument(item.Key, item.Value);
                  }
              }

          },
          cancellationToken);
    }
    /// <summary>
    /// Declare an exchange
    /// </summary>
    /// <param name="bus">The bus instance</param>
    /// <param name="exchange">The exchange name</param>
    /// <param name="configure">The configuration of exchange</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The exchange</returns>
    public static Task<Exchange> ExchangeDeclareAsync(
        this IAdvancedBus bus,
        string exchange,
        Action<IExchangeDeclareConfiguration> configure,
        CancellationToken cancellationToken = default
    )
    {
        var exchangeDeclareConfiguration = new ExchangeDeclareConfiguration();
        configure(exchangeDeclareConfiguration);

        return bus.ExchangeDeclareAsync(
            exchange: exchange,
            type: exchangeDeclareConfiguration.Type,
            durable: exchangeDeclareConfiguration.IsDurable,
            autoDelete: exchangeDeclareConfiguration.IsAutoDelete,
            arguments: exchangeDeclareConfiguration.Arguments,
            cancellationToken: cancellationToken
        );
    }
}
