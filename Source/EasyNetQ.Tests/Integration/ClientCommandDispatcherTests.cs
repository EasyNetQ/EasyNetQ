﻿// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.ConnectionString;
using EasyNetQ.DI;
using EasyNetQ.Producer;
using Xunit;

namespace EasyNetQ.Tests.Integration
{
    [Explicit("Requires a RabbitMQ instance on localhost")]
    public class ClientCommandDispatcherTests : IDisposable
    {
        public ClientCommandDispatcherTests()
        {
            var eventBus = new EventBus();
            var parser = new ConnectionStringParser();
            var configuration = parser.Parse("host=localhost");
            var connectionFactory = ConnectionFactoryFactory.CreateConnectionFactory(configuration);
            connection = new PersistentConnection(configuration, connectionFactory, eventBus);
            var persistentChannelFactory = new PersistentChannelFactory(configuration, eventBus);
            dispatcher = new ClientCommandDispatcher(configuration, connection, persistentChannelFactory);
        }

        public void Dispose()
        {
            dispatcher.Dispose();
            connection.Dispose();
        }

        private IClientCommandDispatcher dispatcher;
        private IPersistentConnection connection;

        [Fact]
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
                        dispatcher.InvokeAsync(
                            x => x.BasicPublish("", "MyQueue", false, x.CreateBasicProperties(), body),
                            default
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

        [Fact]
        public void Should_be_able_to_get_result_back()
        {
            var task = dispatcher.InvokeAsync(x => x.QueueDeclare("MyQueue", true, false, false, null), default);
            task.Wait();
            var queueDeclareOk = task.Result;
            Console.Out.WriteLine(queueDeclareOk.QueueName);
        }

        [Fact]
        public void Should_bubble_exception()
        {
            var task = dispatcher.InvokeAsync(x =>
            {
                x.ExchangeDeclare("MyExchange", "topic", true, false, new Dictionary<string, object>());
                Console.Out.WriteLine("declare executed");
            }, default);
            task.Wait();
            Console.Out.WriteLine("Task complete");
        }

        [Fact]
        public void Should_dispatch_simple_channel_action()
        {
            var task = dispatcher.InvokeAsync(x =>
            {
                x.ExchangeDeclare("MyExchange", "direct", true, false, new Dictionary<string, object>());
                Console.Out.WriteLine("declare executed");
            }, default);
            task.Wait();
            Console.Out.WriteLine("Task complete");
        }
    }
}

// ReSharper restore InconsistentNaming
