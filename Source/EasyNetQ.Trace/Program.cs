using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EasyNetQ.Trace
{
    class Program
    {
        private const string traceExchange = "amq.rabbitmq.trace";
        private const string publishRoutingKey = "publish.#";
        private const string deliverRoutingKey = "deliver.#";
        private static readonly CancellationTokenSource tokenSource = 
            new CancellationTokenSource();
        private static readonly BlockingCollection<BasicDeliverEventArgs> deliveryQueue = 
            new BlockingCollection<BasicDeliverEventArgs>(1);

        static void Main(string[] args)
        {
            Console.WriteLine("Trace is running. Ctrl-C to exit");
            var autoResetEvent = new AutoResetEvent(false);
            Console.CancelKeyPress += (sender, eventArgs) =>
                {
                    eventArgs.Cancel = true;
                    tokenSource.Cancel();
                    autoResetEvent.Set();
                    deliveryQueue.Dispose();
                };

            var connectionString = args.Length == 0
                                       ? "amqp://localhost/"
                                       : args[0];

            HandleDelivery();
            using (ConnectAndSubscribe(connectionString))
            {
                autoResetEvent.WaitOne();
            }

            Console.WriteLine("Shutdown");
        }

        static void HandleDelivery()
        {
            var deliveryThread = new Thread(() =>
                {
                    try
                    {
                        foreach (var deliverEventArgs in deliveryQueue.GetConsumingEnumerable(tokenSource.Token))
                        {
                            HandleDelivery(deliverEventArgs);
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        // deliveryQueue has been disposed so do nothing
                    }
                }) {Name = "EasyNetQ.Trace - delivery thread."};
            deliveryThread.Start();
        }

        static IDisposable ConnectAndSubscribe(string connectionString)
        {
            var connectionFactory = new ConnectionFactory
                {
                    Uri = connectionString,
                    ClientProperties = new Dictionary<string, string>
                        {
                            { "Client", "EasyNetQ.Trace" },
                            { "Host", Environment.MachineName }
                        },
                    RequestedHeartbeat = 10
                };

            var connection = connectionFactory.CreateConnection();
            var disposable = new Disposable{ ToBeDisposed = connection };
            connection.ConnectionShutdown += (connection1, reason) =>
                {
                    if(!tokenSource.IsCancellationRequested)
                        disposable.ToBeDisposed = ConnectAndSubscribe(connectionString);
                };

            Subscribe(connection, traceExchange, publishRoutingKey);
            Subscribe(connection, traceExchange, deliverRoutingKey);

            return disposable;
        }

        static void Subscribe(IConnection connection, string exchangeName, string routingKey)
        {
            var thread = new Thread(() =>
                {
                    var channel = connection.CreateModel();
                    var queueDeclareOk = channel.QueueDeclare();
                    channel.QueueBind(queueDeclareOk.QueueName, exchangeName, routingKey);
                    var subscription = new RabbitMQ.Client.MessagePatterns.Subscription(channel, queueDeclareOk.QueueName);

                    try
                    {
                        while (!tokenSource.IsCancellationRequested)
                        {
                            var deliveryArgs = subscription.Next();
                            if (deliveryArgs != null)
                            {
                                deliveryQueue.Add(deliveryArgs, tokenSource.Token);
                            }
                        }
                    }
                    // deliveryQueue has been disposed, so do nothing
                    catch (OperationCanceledException)
                    {}
                    catch (ObjectDisposedException)
                    {}
                    Console.Out.WriteLine("Subscription to exchange {0}, routingKey {1} closed", exchangeName, routingKey);
                }) {Name = string.Format("EasyNetQ.Trace - subscription {0} {1}", exchangeName, routingKey)};
            thread.Start();
        }

        static void HandleDelivery(BasicDeliverEventArgs basicDeliverEventArgs)
        {
            if (basicDeliverEventArgs == null) return;

            Func<string, object> getHeader = key => basicDeliverEventArgs.BasicProperties.Headers.Contains(key)
                ? basicDeliverEventArgs.BasicProperties.Headers[key]
                : null;

            Func<byte[], string> decode = bytes => Encoding.UTF8.GetString(bytes);

            Console.Out.WriteLine("");
            Console.Out.WriteLine("RoutingKey:      {0}", basicDeliverEventArgs.RoutingKey);
            Console.Out.WriteLine("Exchange:        {0}", decode((byte[])getHeader("exchange_name")));
            var body = decode(basicDeliverEventArgs.Body);
            Console.Out.WriteLine(body);
            Console.Out.WriteLine("");
        }
    }

    public class Disposable : IDisposable
    {
        public IDisposable ToBeDisposed { get; set; }

        public void Dispose()
        {
            if (ToBeDisposed != null)
            {
                ToBeDisposed.Dispose();
            }
        }
    }
}
