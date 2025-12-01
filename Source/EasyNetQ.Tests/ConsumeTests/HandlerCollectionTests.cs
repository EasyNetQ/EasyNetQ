using EasyNetQ.Consumer;

namespace EasyNetQ.Tests.ConsumeTests;

public class HandlerCollectionTests
{
    private readonly IHandlerCollection handlerCollection;

    private bool myMessageHandlerExecuted;
    private bool animalHandlerExecuted;

    public HandlerCollectionTests()
    {
        handlerCollection = new HandlerCollection();
        handlerCollection.Add<MyMessage>((_, _) => myMessageHandlerExecuted = true);
        handlerCollection.Add<IAnimal>((_, _) => animalHandlerExecuted = true);
    }

    [Fact]
    public async Task Should_return_matching_handler()
    {
        var handler = handlerCollection.GetHandler(typeof(MyMessage));

        await handler(new Message<MyMessage>(new MyMessage()), default, default);

        myMessageHandlerExecuted.Should().BeTrue();
        animalHandlerExecuted.Should().BeFalse();
    }

    [Fact]
    public async Task Should_return_supertype_handler()
    {
        var handler = handlerCollection.GetHandler(typeof(Dog));

        await handler(new Message<Dog>(new Dog()), default, default);

        animalHandlerExecuted.Should().BeTrue();
        myMessageHandlerExecuted.Should().BeFalse();
    }

    [Fact]
    public void Should_throw_if_handler_is_not_found()
    {
        Assert.Throws<EasyNetQException>(() => handlerCollection.GetHandler(typeof(MyOtherMessage)));
    }

    [Fact]
    public async Task Should_return_matching_handler_by_type()
    {
        var handler = handlerCollection.GetHandler(typeof(MyMessage));

        await handler(new Message<MyMessage>(new MyMessage()), default, default);

        myMessageHandlerExecuted.Should().BeTrue();
        animalHandlerExecuted.Should().BeFalse();
    }

    [Fact]
    public async Task Should_return_supertype_handler_by_type()
    {
        var handler = handlerCollection.GetHandler(typeof(Dog));

        await handler(new Message<Dog>(new Dog()), default, default);

        animalHandlerExecuted.Should().BeTrue();
        myMessageHandlerExecuted.Should().BeFalse();
    }

    [Fact]
    public async Task Should_return_a_null_logger_if_ThrowOnNoMatchingHandler_is_false()
    {
        handlerCollection.ThrowOnNoMatchingHandler = false;

        var handler = handlerCollection.GetHandler(typeof(MyOtherMessage));

        await handler(new Message<MyOtherMessage>(new MyOtherMessage()), default, default);

        myMessageHandlerExecuted.Should().BeFalse();
        animalHandlerExecuted.Should().BeFalse();
    }

    [Fact]
    public void Should_not_be_able_to_register_multiple_handlers_for_the_same_type()
    {
        Assert.Throws<EasyNetQException>(() => handlerCollection.Add<MyMessage>((_, _) => { }));
    }
}
