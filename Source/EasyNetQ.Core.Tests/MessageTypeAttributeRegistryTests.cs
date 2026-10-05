namespace EasyNetQ.Core.Tests;

public class MessageTypeAttributeRegistryTests
{
    [MessageType("tests.recheck.v1", Aliases = ["Old.Recheck"])]
    public sealed record Recheck(string Slug);

    [Fact]
    public void Should_use_the_attribute_wire_name_whoever_registers_the_type_first()
    {
        var registry = new MessageTypeRegistry(new DefaultTypeNameSerializer());

        // e.g. a test assembly's generated module that only saw the type at a call site
        registry.GetOrAdd<Recheck>().WireName.Should().Be("tests.recheck.v1");

        // then the declaring assembly's module registers the attribute values: no conflict
        var act = () => registry.Register<Recheck>("tests.recheck.v1", ["Old.Recheck"]);
        act.Should().NotThrow();
        registry.TryGetByWireName("Old.Recheck", out var byAlias).Should().BeTrue();
        byAlias.Type.Should().Be(typeof(Recheck));
    }

    [Fact]
    public void Should_use_the_attribute_wire_name_on_the_runtime_path()
    {
        var registry = new MessageTypeRegistry(new DefaultTypeNameSerializer());

        registry.GetOrAdd(typeof(Recheck)).WireName.Should().Be("tests.recheck.v1");
        registry.TryGetByWireName("Old.Recheck", out _).Should().BeTrue();
    }
}
