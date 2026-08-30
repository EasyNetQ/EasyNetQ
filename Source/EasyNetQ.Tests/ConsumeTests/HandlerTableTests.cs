namespace EasyNetQ.Tests.ConsumeTests;

public class HandlerTableTests
{
    private readonly MessageTypeRegistry registry = new(new DefaultTypeNameSerializer());
    private readonly HandlerTable table;

    public HandlerTableTests()
    {
        table = new HandlerTable(registry);
        table.Add<MyMessage>(static (_, _) => new ValueTask<AckDecision>(AckDecision.Ack));
    }

    [Fact]
    public void Polymorphic_resolution_must_not_change_the_resolved_descriptor()
    {
        var derivedWireName = registry.GetOrAdd<MyDerivedMessage>().WireName;

        // regression: the polymorphic handler-resolution cache used to poison descriptor resolution, so from the
        // second message on, derived messages were deserialized as the base type
        for (var i = 0; i < 3; i++)
        {
            var descriptor = table.ResolveDescriptor(derivedWireName);
            descriptor.Type.Should().Be<MyDerivedMessage>("message {0} must keep its concrete type", i);

            var entry = table.Resolve(descriptor);
            entry.Descriptor.Type.Should().Be<MyMessage>("the base handler serves derived messages");
        }
    }

    [Fact]
    public void Exact_registrations_resolve_their_own_descriptor_and_entry()
    {
        var wireName = registry.GetOrAdd<MyMessage>().WireName;

        var descriptor = table.ResolveDescriptor(wireName);
        descriptor.Type.Should().Be<MyMessage>();
        table.Resolve(descriptor).Descriptor.Type.Should().Be<MyMessage>();
    }

    [Fact]
    public void Unmatched_types_should_throw_or_noop_depending_on_configuration()
    {
        var descriptor = registry.GetOrAdd<OtherMessage>();

        var act = () => table.Resolve(descriptor);
        act.Should().Throw<EasyNetQException>();

        table.ThrowOnNoMatchingHandler = false;
        table.Resolve(descriptor).Should().NotBeNull();
    }

    private sealed class MyDerivedMessage : MyMessage;

    private sealed class OtherMessage;
}
