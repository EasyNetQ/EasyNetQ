using System.Text;
using EasyNetQ.Pipeline;
using EasyNetQ.Pipeline.Middleware;

namespace EasyNetQ.Tests.MessageVersioningTests;

public class ResolveMessageTypeStepVersioningTests
{
    private readonly DefaultTypeNameSerializer typeNames = new();
    private readonly MessageTypeRegistry registry;
    private readonly HandlerTable table;

    public ResolveMessageTypeStepVersioningTests()
    {
        registry = new MessageTypeRegistry(typeNames);
        table = new HandlerTable(registry);
        table.Add<MyMessage>(static (_, _) => new ValueTask<AckDecision>(AckDecision.Ack));
    }

    [Fact]
    public async Task Should_fall_back_to_the_first_loadable_alternative_of_an_unknown_newer_version()
    {
        var context = Context("Acme.MyMessageV3, Acme.NotDeployedHere", typeof(MyMessageV2), typeof(MyMessage));

        await new ResolveMessageTypeStep().InvokeAsync(context, static _ => default);

        context.MessageType!.Type.Should().Be<MyMessageV2>();
        table.Resolve(context.MessageType).Descriptor.Type.Should().Be<MyMessage>("the V1 handler serves V2");
    }

    [Fact]
    public async Task Should_keep_the_declared_type_when_it_is_loadable()
    {
        var context = Context(typeNames.Serialize(typeof(MyMessageV2)), typeof(MyMessage));

        await new ResolveMessageTypeStep().InvokeAsync(context, static _ => default);

        context.MessageType!.Type.Should().Be<MyMessageV2>();
    }

    [Fact]
    public async Task Should_throw_when_neither_the_type_nor_an_alternative_resolves()
    {
        var context = Context("Acme.MyMessageV3, Acme.NotDeployedHere");
        context.Properties = context.Properties.SetHeader("Alternative-Message-Types", Encoding.UTF8.GetBytes("Acme.MyMessageV2, Acme.NotDeployedHere"));

        var act = async () => await new ResolveMessageTypeStep().InvokeAsync(context, static _ => default);

        await act.Should().ThrowAsync<UnknownMessageTypeException>();
    }

    private ConsumeContext Context(string type, params Type[] alternatives)
    {
        var consumer = TestContexts.Consumer();
        consumer.Handlers = table;
        var header = Encoding.UTF8.GetBytes(string.Join(";", alternatives.Select(typeNames.Serialize)));
        return new ConsumeContext(consumer)
        {
            Properties = new MessageProperties { Type = type }.SetHeader("Alternative-Message-Types", header)
        };
    }
}
