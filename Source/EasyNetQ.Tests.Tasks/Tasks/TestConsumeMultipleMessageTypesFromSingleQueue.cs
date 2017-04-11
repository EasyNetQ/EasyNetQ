using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Consumer;
using EasyNetQ.Tests.Tasks;
using Net.CommandLine;

namespace EasyNetQ.Tests.Performance.Consumer
{


    public class TestConsumeMultipleMessageTypesFromSingleQueue : ICommandLineTask, IDisposable
    {
        IBus bus;

        public Task Run(CancellationToken cancellationToken)
        {
            var logger = new NoDebugLogger();

            bus = RabbitHutch.CreateBus("host=localhost;product=consumer", 
                x => x
                    .Register<IEasyNetQLogger>(_ => logger)
                    .Register<IConventions, SingleQueueNamingConvention>()
                    .Register<IHandlerCollectionFactory, HandlerCollectionPerQueueFactory>()
            );

            bus.SubscribeAsync<MessageA>("multiple", async m => logger.InfoWrite("{0}", m));
            bus.SubscribeAsync<MessageB>("multiple", async m => logger.InfoWrite("{0}", m));

            for (int i = 0; i < 100; i++)
            {
                bus.Publish(new MessageA());
                bus.Publish(new MessageB());
            }

            Console.WriteLine("press enter to exit");
            Console.ReadLine();

            return Task.FromResult(0);
        }

        public void Dispose()
        {
            Console.Out.WriteLine("Shutting down");
            bus.Dispose();
            Console.WriteLine("Shut down complete");
        }
    }

    public class SingleQueueNamingConvention : Conventions
    {
        public SingleQueueNamingConvention(ITypeNameSerializer typeNameSerializer) : base(typeNameSerializer)
        {
            QueueNamingConvention = (type, id) => id;
        }


    }

    public class MessageB
    {
    }

    public class MessageA
    {
    }
}