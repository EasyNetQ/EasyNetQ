using EasyNetQ.ConnectionString;
using EasyNetQ.Consumer;
using EasyNetQ.Producer;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace EasyNetQ.Tests.Mocking;

public class MockBuilder : IDisposable
{
    private readonly IServiceProvider serviceProvider;
    private readonly IBus bus;

    private readonly IBasicProperties basicProperties = new BasicProperties();
    private readonly Stack<IModel> channelPool = new();
    private readonly List<IModel> channels = new();
    private readonly IConnection connection = Substitute.For<IAutorecoveringConnection>();
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
            channelPool.Push(Substitute.For<IModel, IRecoverable>());

        connectionFactory.CreateConnection(Arg.Any<IList<AmqpTcpEndpoint>>()).Returns(connection);
        connection.IsOpen.Returns(true);
        connection.Endpoint.Returns(new AmqpTcpEndpoint("localhost"));

        connection.CreateModel().Returns(_ =>
        {
            var channel = channelPool.Pop();
            channels.Add(channel);
            channel.CreateBasicProperties().Returns(basicProperties);
            channel.IsOpen.Returns(true);
            channel.BasicConsume(null, false, null, true, false, null, null)
                .ReturnsForAnyArgs(consumeInvocation =>
                {
                    var queueName = (string)consumeInvocation[0];
                    var consumerTag = (string)consumeInvocation[2];
                    var consumer = (AsyncDefaultBasicConsumer)consumeInvocation[6];

                    ConsumerQueueNames.Add(queueName);
                    consumer.HandleBasicConsumeOk(consumerTag)
                        .GetAwaiter()
                        .GetResult();
                    consumers.Add(consumer);
                    return string.Empty;
                });
            channel.QueueDeclare(null, true, false, false, null)
                .ReturnsForAnyArgs(queueDeclareInvocation =>
                {
                    var queueName = (string)queueDeclareInvocation[0];
                    return new QueueDeclareOk(queueName, 0, 0);
                });
            channel.WaitForConfirms(default).ReturnsForAnyArgs(true);

            return channel;
        });

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(connectionFactory);

        RabbitHutch.RegisterBus(
            serviceCollection,
            x => x.GetRequiredService<IConnectionStringParser>().Parse(connectionString),
            registerServices
        );

        registerServices(serviceCollection);
        serviceProvider = serviceCollection.BuildServiceProvider();
        bus = serviceProvider.GetRequiredService<IBus>();
    }

    public IBus Bus => bus;

    public IConnectionFactory ConnectionFactory => connectionFactory;

    public IConnection Connection => connection;

    public List<IModel> Channels => channels;

    public List<AsyncDefaultBasicConsumer> Consumers => consumers;

    public IModel NextModel => channelPool.Peek();

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

    public void Dispose() => (serviceProvider as IDisposable)?.Dispose();
}
