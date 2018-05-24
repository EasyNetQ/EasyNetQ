// ReSharper disable InconsistentNaming

using System;
using System.Threading;
using Xunit;

namespace EasyNetQ.Tests.Integration
{
    [Explicit("Requires a local RabbitMQ instance to work")]
    public class PolymorphicPubSub : IDisposable
    {
        private IBus bus;

        public PolymorphicPubSub()
        {
            bus = RabbitHutch.CreateBus("host=localhost");
        }

        public void Dispose()
        {
            bus.Dispose();
        }

        [Fact]
        public void Should_publish_some_animals()
        {
            var cat = new Cat
            {
                Name = "Gobbolino",
                Meow = "Purr"
            };

            var dog = new Dog
            {
                Name = "Rover",
                Bark = "Woof"
            };

            bus.Publish<IAnimal>(cat);
            bus.Publish<IAnimal>(dog);
        }

        [Fact]
        public void Should_consume_the_correct_message_type()
        {
            bus.Subscribe<IAnimal>("polymorphic_test", @interface =>
                {
                    var cat = @interface as Cat;
                    var dog = @interface as Dog;

                    if (cat != null)
                    {
                        Console.Out.WriteLine("Name = {0}", cat.Name);
                        Console.Out.WriteLine("Meow = {0}", cat.Meow);
                    }
                    else if (dog != null)
                    {
                        Console.Out.WriteLine("Name = {0}", dog.Name);
                        Console.Out.WriteLine("Bark = {0}", dog.Bark);
                    }
                    else
                    {
                        Console.Out.WriteLine("message was not a dog or a cat");
                    }
                });

            Thread.Sleep(500);
        }
    }
}

// ReSharper restore InconsistentNaming