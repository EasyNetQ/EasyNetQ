// ReSharper disable InconsistentNaming

using System;
using System.Threading;
using Xunit;

namespace EasyNetQ.Tests.Integration
{
    [Explicit("Requires a local RabbitMQ instance to work")]
    public class PolymorphicRpc : IDisposable
    {
        private IBus bus;

        public PolymorphicRpc()
        {
            bus = RabbitHutch.CreateBus("host=localhost");
        }

        public void Dispose()
        {
            bus.Dispose();
        }

        [Fact]
        public void Should_request_some_animals()
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

            bus.RequestAsync<IAnimal, IAnimal>(cat);
            bus.RequestAsync<IAnimal, IAnimal>(dog);
        }

        [Fact]
        public void Should_request_respond_with_correct_message_types()
        {
            bus.Respond<IAnimal, IAnimal>(@interface =>
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

                return @interface;
            });

            Thread.Sleep(500);

            IAnimal request = new Cat
            {
                Name = "Gobbolino",
                Meow = "Purr"
            };

            IAnimal response = bus.Request<IAnimal, IAnimal>(request);

            Assert.Equal(request.Name, response.Name);
            Assert.Same(request.GetType(), response.GetType());
        }
    }
}

// ReSharper restore InconsistentNaming