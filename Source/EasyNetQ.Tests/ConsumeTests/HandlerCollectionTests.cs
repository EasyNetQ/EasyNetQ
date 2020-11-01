// ReSharper disable InconsistentNaming

using EasyNetQ.Consumer;
using FluentAssertions;
using Xunit;

namespace EasyNetQ.Tests.ConsumeTests
{
    public class HandlerCollectionTests
    {
        private readonly IHandlerCollection handlerCollection;

        private bool myMessageHandlerExecuted = false;
        private bool animalHandlerExecuted = false;

        public HandlerCollectionTests()
        {
            handlerCollection = new HandlerCollection();

            handlerCollection.Add<MyMessage>((message, info) =>
                {
                    myMessageHandlerExecuted = true;
                });
            handlerCollection.Add<IAnimal>((message, info) =>
                {
                    animalHandlerExecuted = true;
                });
        }

        [Fact]
        public void Should_return_matching_handler()
        {
            var handler = handlerCollection.GetHandler<MyMessage>();

            handler(new Message<MyMessage>(new MyMessage()), null, default);
            myMessageHandlerExecuted.Should().BeTrue();
        }

        [Fact]
        public void Should_return_supertype_handler()
        {
            var handler = handlerCollection.GetHandler<Dog>();

            handler(new Message<Dog>(new Dog()), null, default);
            animalHandlerExecuted.Should().BeTrue();
        }

        [Fact]
        public void Should_throw_if_handler_is_not_found()
        {
            Assert.Throws<EasyNetQException>(() =>
            {
                handlerCollection.GetHandler<MyOtherMessage>();
            });
        }

        [Fact]
        public void Should_return_matching_handler_by_type()
        {
            var handler = handlerCollection.GetHandler(typeof(MyMessage));

            handler(new Message<MyMessage>(new MyMessage()), null, default);
            myMessageHandlerExecuted.Should().BeTrue();
        }

        [Fact]
        public void Should_return_supertype_handler_by_type()
        {
            var handler = handlerCollection.GetHandler(typeof(Dog));

            handler(new Message<Dog>(new Dog()), null, default);
            animalHandlerExecuted.Should().BeTrue();
        }

        [Fact]
        public void Should_return_a_null_logger_if_ThrowOnNoMatchingHandler_is_false()
        {
            handlerCollection.ThrowOnNoMatchingHandler = false;
            var handler = handlerCollection.GetHandler<MyOtherMessage>();

            handler(new Message<MyOtherMessage>(new MyOtherMessage()), null, default);
            myMessageHandlerExecuted.Should().BeFalse();
            animalHandlerExecuted.Should().BeFalse();
        }

        [Fact]
        public void Should_not_be_able_to_register_multiple_handlers_for_the_same_type()
        {
            Assert.Throws<EasyNetQException>(() =>
            {
                handlerCollection.Add<MyMessage>((message, info) => { });
            });
        }
    }
}

// ReSharper restore InconsistentNaming
