using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

using CommandLine;
using CommandLine.Text;

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

        private static Options options;

        private static CSVFile csvFile;

        static void Main(string[] args)
        {
            Console.CancelKeyPress += (sender, eventArgs) =>
                {
                    eventArgs.Cancel = true;
                    tokenSource.Cancel();
                };

            ParserResult<Options> parseResults = Parser.Default.ParseArguments<Options>(args);
            options = parseResults.MapResult(
                parsed => parsed,
                notParsed =>
                {
                    return default(Options);
                });

            if (options == default(Options))
            {
                return;
            }

            if (options.csvoutput != null)
            {
                //Create CSV file and write header row.
                csvFile = new CSVFile(options.csvoutput);

                var columnlist = new List<string>
                        {
                            "Message#",
                            "Date Time",
                            "Routing Key",
                            "Exchange",
                            "Body"
                        };

                csvFile.WriteRow(columnlist);

                var connectionString = options.AMQP;

                Console.WriteLine("Trace is running. Ctrl-C to exit");

                HandleDelivery();
                try
                {

                    using (ConnectAndSubscribe(connectionString))
                    {
                        tokenSource.Token.WaitHandle.WaitOne();
                    }

                    Console.WriteLine("Shutdown");
                }
                catch (Exception e)
                {
                    Console.Out.WriteLine(e.Message);
                }
            }
        }

        static void HandleDelivery()
        {
            int msgCount = 0;
            new Thread(() =>
                {
                    try
                    {
                        foreach (var deliverEventArgs in deliveryQueue.GetConsumingEnumerable(tokenSource.Token))
                        {
                            HandleDelivery(deliverEventArgs, msgCount++);
                        }
                    }
                    // deliveryQueue has been disposed so do nothing
                    catch (OperationCanceledException)
                    { }
                    catch (ObjectDisposedException)
                    { }
                })
            {
                Name = "EasyNetQ.Trace - delivery."
            }.Start();


        }

        static IDisposable ConnectAndSubscribe(string connectionString)
        {
            var connectionFactory = new ConnectionFactory
            {
                Uri = new Uri(connectionString),
                ClientProperties = new Dictionary<string, object>
                        {
                            { "Client", "EasyNetQ.Trace" },
                            { "Host", Environment.MachineName }
                        },
                RequestedHeartbeat = 10
            };

            var connection = connectionFactory.CreateConnection();
            var disposable = new Disposable { ToBeDisposed = connection };
            connection.ConnectionShutdown += (connection1, reason) =>
                {
                    if (!tokenSource.IsCancellationRequested)
                    {
                        Console.Out.WriteLine("\nConnection closed.\nReason {0}\nNow reconnecting", reason.ToString());
                        disposable.ToBeDisposed = ConnectAndSubscribe(connectionString);
                    }
                };

            Subscribe(connection, traceExchange, publishRoutingKey);
            Subscribe(connection, traceExchange, deliverRoutingKey);

            return disposable;
        }

        static void Subscribe(IConnection connection, string exchangeName, string routingKey)
        {
            new Thread(() =>
                {
                    var channel = connection.CreateModel();
                    var queueDeclareOk = channel.QueueDeclare();
                    channel.QueueBind(queueDeclareOk.QueueName, exchangeName, routingKey);
                    var subscription = new RabbitMQ.Client.MessagePatterns.Subscription(channel, queueDeclareOk.QueueName);

                    try
                    {
                        while (!tokenSource.IsCancellationRequested && channel.IsOpen)
                        {
                            var deliveryArgs = subscription.Next();
                            if (!(deliveryArgs == null || tokenSource.IsCancellationRequested))
                            {
                                deliveryQueue.Add(deliveryArgs, tokenSource.Token);
                            }
                        }
                    }
                    // deliveryQueue has been disposed, so do nothing
                    catch (OperationCanceledException)
                    { }
                    catch (ObjectDisposedException)
                    { }
                    Console.Out.WriteLine("Subscription to exchange {0}, routingKey {1} closed", exchangeName, routingKey);
                })
            {
                Name = string.Format("EasyNetQ.Trace - subscription {0} {1}", exchangeName, routingKey)
            }.Start();
        }

        static void HandleDelivery(BasicDeliverEventArgs basicDeliverEventArgs, int msgCount)
        {
            if (basicDeliverEventArgs == null) return;

            Func<string, object> getHeader = key => basicDeliverEventArgs.BasicProperties.Headers.ContainsKey(key)
                ? basicDeliverEventArgs.BasicProperties.Headers[key]
                : null;

            Func<byte[], string> decode = bytes => Encoding.UTF8.GetString(bytes);

            if (!options.quiet)
            {
                //Standard output
                Console.Out.WriteLine("");
                Console.Out.WriteLine("RoutingKey:      {0}", basicDeliverEventArgs.RoutingKey);
                Console.Out.WriteLine("Exchange:        {0}", decode((byte[])getHeader("exchange_name")));
                var body = decode(basicDeliverEventArgs.Body);
                Console.Out.WriteLine(body);
                Console.Out.WriteLine("");
            }

            if (options.csvoutput != null)
            {
                //CSV Output
                //Message#,Date Time,Routing Key,Exchange,Body
                var columnlist = new List<string>
                    {
                        msgCount.ToString(CultureInfo.InvariantCulture),
                        DateTime.Now.ToString(CultureInfo.InvariantCulture),
                        basicDeliverEventArgs.RoutingKey,
                        decode((byte[]) getHeader("exchange_name")),
                        decode(basicDeliverEventArgs.Body)
                    };

                csvFile.WriteRow(columnlist);

            }
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


    /// <summary>
    ///  Define command line options
    /// </summary>
    class Options
    {
        [Option('a', "amqp-connection-string", Required = false, Default = "amqp://localhost/", HelpText = "AMQP Connection string.")]
        public string AMQP { get; set; }

        [Option('q', "quiet", Default = false, HelpText = "Switch off verbose console output")]
        public bool quiet { get; set; }

        [Option('o', "output-csv", Required = false, HelpText = "CSV File name for output")]
        public string csvoutput { get; set; }
    }
}
