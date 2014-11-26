// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.ConnectionString;
using EasyNetQ.Loggers;
using EasyNetQ.Producer;
using NUnit.Framework;

namespace EasyNetQ.Tests.Integration
{
    [TestFixture]
    [Explicit("Requires a RabbitMQ instance on localhost")]
    public class ClientCommandDispatcherTests
    {
        private IClientCommandDispatcher dispatcher;
        private IPersistentConnection connection;

        [SetUp]
        public void SetUp()
        {
            var eventBus = new EventBus();
            var logger = new ConsoleLogger();
            var parser = new ConnectionStringParser();
            var configuration = parser.Parse("host=localhost");
            var hostSelectionStrategy = new RandomClusterHostSelectionStrategy<ConnectionFactoryInfo>();
            var connectionFactory = new ConnectionFactoryWrapper(configuration, hostSelectionStrategy);
            connection = new PersistentConnection(connectionFactory, logger, eventBus);
            var persistentChannelFactory = new PersistentChannelFactory(logger, configuration, eventBus);

            dispatcher = new ClientCommandDispatcher(connection, persistentChannelFactory);
        }

        [TearDown]
        public void TearDown()
        {
            dispatcher.Dispose();
            connection.Dispose();
        }

        [Test]
        public void Should_dispatch_simple_channel_action()
        {
            var task = dispatcher.Invoke(x =>
                {
                    x.ExchangeDeclare("MyExchange", "direct");
                    Console.Out.WriteLine("declare executed");
                });
            task.Wait();
            Console.Out.WriteLine("Task complete");
        }

        [Test]
        public void Should_bubble_exception()
        {
            var task = dispatcher.Invoke(x =>
            {
                x.ExchangeDeclare("MyExchange", "topic");
                Console.Out.WriteLine("declare executed");
            });
            task.Wait();
            Console.Out.WriteLine("Task complete");
        }

        [Test]
        public void Should_be_able_to_get_result_back()
        {
            var task = dispatcher.Invoke(x => x.QueueDeclare("MyQueue", true, false, false, null));
            task.Wait();
            var queueDeclareOk = task.Result;
            Console.Out.WriteLine(queueDeclareOk.QueueName);
        }

        [Test]
        public void Should_be_able_to_do_lots_of_operations_from_different_threads()
        {
            Helpers.ClearAllQueues();

            var tasks = new List<Task>();
            var body = Encoding.UTF8.GetBytes("Hello World!");
            for (var i = 0; i < 10; i++)
            {
                tasks.Add(Task.Factory.StartNew(() =>
                    {
                        for (var j = 0; j < 100000; j++)
                        {
                            dispatcher.Invoke(
                                x =>
                                x.BasicPublish("", "MyQueue", false, false, x.CreateBasicProperties(), body)
                                ).Wait();
                        }
                    }, TaskCreationOptions.LongRunning));
            }

            var running = true;
            var killerTask = Task.Factory.StartNew(() =>
                {
                    while (running)
                    {
                        Thread.Sleep(1000);
                        Helpers.CloseConnection();
                    }
                }, TaskCreationOptions.LongRunning);

            Task.WaitAll(tasks.ToArray());
            Console.Out.WriteLine("Workers complete");
            running = false;
            killerTask.Wait();
            Console.Out.WriteLine("Killer complete");
        }
    }
}

// ReSharper restore InconsistentNaming