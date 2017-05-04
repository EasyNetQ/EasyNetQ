// ReSharper disable InconsistentNaming

using System;
using System.Threading;
using Xunit;
using EasyNetQ.NonGeneric;

namespace EasyNetQ.Tests.Integration
{
    [Explicit("Requires a RabbitMQ instance on localhost to work")]
    public class NonGenericExtensionsIntegrationTests : IDisposable
    {
        private IBus bus;

        public NonGenericExtensionsIntegrationTests()
        {
            bus = RabbitHutch.CreateBus("host=localhost");
        }

        public void Dispose()
        {
            bus.Dispose();
        }

        [Fact]
        public void Should_be_able_to_use_the_non_generic_subscribe_method()
        {
            var are = new AutoResetEvent(false);

            bus.Subscribe(typeof (MyMessage), "non_generic_test", x =>
                {
                    Console.Out.WriteLine("Got Message: {0}", ((MyMessage)x).Text);
                    are.Set();
                });

            bus.Publish(new MyMessage{ Text = "Hi Mrs Non Generic :)"});

            are.WaitOne(1000);
        }
    }
}

// ReSharper restore InconsistentNaming