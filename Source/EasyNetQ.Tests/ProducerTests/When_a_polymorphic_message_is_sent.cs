// ReSharper disable InconsistentNaming
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EasyNetQ.Tests.Mocking;
using NSubstitute;
using RabbitMQ.Client;
using Xunit;

namespace EasyNetQ.Tests.ProducerTests
{
    public class When_a_polymorphic_message_is_sent : IDisposable
    {
        private readonly MockBuilder mockBuilder;
        private const string interfaceTypeName = "EasyNetQ.Tests.ProducerTests.IMyMessageInterface, EasyNetQ.Tests";
        private const string implementationTypeName = "EasyNetQ.Tests.ProducerTests.MyImplementation, EasyNetQ.Tests";

        public When_a_polymorphic_message_is_sent()
        {
            mockBuilder = new MockBuilder();

            var message = new MyImplementation
                {
                    Text = "Hello Polymorphs!",
                    NotInInterface = "Hi"
                };

            mockBuilder.PubSub.Publish<IMyMessageInterface>(message);
        }

        public void Dispose()
        {
            mockBuilder.Bus.Dispose();
        }

        [Fact]
        public void Should_name_exchange_after_interface()
        {
            mockBuilder.Channels[0].Received().ExchangeDeclare(
                Arg.Is(interfaceTypeName),
                Arg.Is("topic"),
                Arg.Is(true),
                Arg.Is(false),
                Arg.Any<IDictionary<string, object>>()
            );
        }

        [Fact]
        public void Should_publish_to_correct_exchange()
        {
            mockBuilder.Channels[1].Received().BasicPublish(
                    Arg.Is(interfaceTypeName),
                    Arg.Is(""),
                    Arg.Is(false),
                    Arg.Is<IBasicProperties>(x => x.Type == implementationTypeName),
                    Arg.Is<ReadOnlyMemory<byte>>(
                        x => x.ToArray().SequenceEqual(
                            Encoding.UTF8.GetBytes("{\"Text\":\"Hello Polymorphs!\",\"NotInInterface\":\"Hi\"}")
                        )
                    )
                );
        }
    }

    public interface IMyMessageInterface
    {
        string Text { get; set; }
    }

    public class MyImplementation : IMyMessageInterface
    {
        public string Text { get; set; }
        public string NotInInterface { get; set; }
    }
}

// ReSharper restore InconsistentNaming
