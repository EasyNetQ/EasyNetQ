using System.Diagnostics.CodeAnalysis;
using EasyNetQ.Consumer;
using EasyNetQ.Producer;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace EasyNetQ.Tests.Mocking;

[SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP004:Don't ignore created IDisposable")]
public class MockBuilder : IDisposable
{
    private readonly IServiceProvider serviceProvider;
    private readonly IBus bus;

    private readonly IBasicProperties basicProperties = new RabbitMQ.Client.BasicProperties();
    private readonly Stack<IChannel> channelPool = new();
    private readonly List<IChannel> channels = new();
    private readonly IConnection connection = Substitute.For<IConnection>();
    private readonly IConnectionFactory connectionFactory = Substitute.For<IConnectionFactory>();
    private readonly List<AsyncDefaultBasicConsumer> consumers = new();

    public MockBuilder() : this(_ => { })
    {
    }

    public MockBuilder(Action<IServiceCollection> registerServices) : this("host=localhost", registerServices)
    {
    }

    public MockBuilder(string connectionString) : this(connectionString, _ => { })
    {
    }

    public MockBuilder(string connectionString, Action<IServiceCollection> registerServices)
    {
        for (var i = 0; i < 10; i++)
            channelPool.Push(Substitute.For<IChannel, IRecoverable>());

        connectionFactory.CreateConnectionAsync(Arg.Any<IList<AmqpTcpEndpoint>>()).Returns(Task.FromResult(connection));

        connection.IsOpen.Returns(true);
        connection.Endpoint.Returns(new AmqpTcpEndpoint("localhost"));
        connection.CreateChannelAsync().Returns(async _ =>
        {
            var channel = channelPool.Pop();
            channels.Add(channel);
            new RabbitMQ.Client.BasicProperties().Returns(basicProperties);
            channel.IsOpen.Returns(true);
            channel.BasicConsumeAsync(null, false, null, true, false, null, null)
                .Returns(async consumeInvocation =>
                {
                    var queueName = (string)consumeInvocation[0];
                    var consumerTag = (string)consumeInvocation[2];
                    var consumer = (AsyncDefaultBasicConsumer)consumeInvocation[6];

                    ConsumerQueueNames.Add(queueName);
                    await consumer.HandleBasicConsumeOkAsync(consumerTag);
                    consumers.Add(consumer);
                    return string.Empty;
                });
            channel.QueueDeclareAsync(null, true, false, false, null)
               .Returns(async queueDeclareInvocation =>
               {
                   var queueName = (string)queueDeclareInvocation[0];
                   return await Task.FromResult(new QueueDeclareOk(queueName, 0, 0));
               });
            channel.WaitForConfirmsAsync(default).ReturnsForAnyArgs(true);

            await Task.Yield();
            return channel;
        });

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(connectionFactory);

        serviceCollection.AddEasyNetQ(connectionString);
        registerServices(serviceCollection);
        serviceProvider = serviceCollection.BuildServiceProvider();
        bus = serviceProvider.GetRequiredService<IBus>();
    }

    public IBus Bus => bus;

    public IConnectionFactory ConnectionFactory => connectionFactory;

    public IConnection Connection => connection;

    public List<IChannel> Channels => channels;

    public List<AsyncDefaultBasicConsumer> Consumers => consumers;

    public IChannel NextModel => channelPool.Peek();

    public IPubSub PubSub => serviceProvider.GetRequiredService<IPubSub>();

    public IRpc Rpc => serviceProvider.GetRequiredService<IRpc>();

    public ISendReceive SendReceive => serviceProvider.GetRequiredService<ISendReceive>();

    public IScheduler Scheduler => serviceProvider.GetRequiredService<IScheduler>();

    public IEventBus EventBus => serviceProvider.GetRequiredService<IEventBus>();

    public IConventions Conventions => serviceProvider.GetRequiredService<IConventions>();

    public ITypeNameSerializer TypeNameSerializer => serviceProvider.GetRequiredService<ITypeNameSerializer>();

    public ISerializer Serializer => serviceProvider.GetRequiredService<ISerializer>();

    public IProducerConnection ProducerConnection => serviceProvider.GetRequiredService<IProducerConnection>();
    public IConsumerConnection ConsumerConnection => serviceProvider.GetRequiredService<IConsumerConnection>();

    public IConsumeErrorStrategy ConsumeErrorStrategy => serviceProvider.GetRequiredService<IConsumeErrorStrategy>();

    public List<string> ConsumerQueueNames { get; } = new();

    public virtual void Dispose() => (serviceProvider as IDisposable)?.Dispose();
}
