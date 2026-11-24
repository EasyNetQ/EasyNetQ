using EasyNetQ.Pipeline;

namespace EasyNetQ.Tests.Pipeline;

public class PropertyBagTests
{
    private static readonly PropertyKey<string> Name = new("name");
    private static readonly PropertyKey<int> Count = new("count");
    private static readonly PropertyKey<string> AnotherName = new("name");

    [Fact]
    public void Should_roundtrip_values()
    {
        var bag = new PropertyBag();

        bag.Set(Name, "a");
        bag.Set(Count, 3);

        bag.TryGet(Name, out var name).Should().BeTrue();
        name.Should().Be("a");
        bag.TryGet(Count, out var count).Should().BeTrue();
        count.Should().Be(3);
        bag.Count.Should().Be(2);
    }

    [Fact]
    public void Should_replace_existing_value()
    {
        var bag = new PropertyBag();

        bag.Set(Name, "a");
        bag.Set(Name, "b");

        bag.TryGet(Name, out var name).Should().BeTrue();
        name.Should().Be("b");
        bag.Count.Should().Be(1);
    }

    [Fact]
    public void Should_distinguish_keys_by_identity_not_name()
    {
        var bag = new PropertyBag();

        bag.Set(Name, "a");

        bag.TryGet(AnotherName, out _).Should().BeFalse();
        Name.Should().NotBe(AnotherName);
    }

    [Fact]
    public void Should_remove_and_clear()
    {
        var bag = new PropertyBag();
        bag.Set(Name, "a");
        bag.Set(Count, 1);

        bag.Remove(Name).Should().BeTrue();
        bag.Remove(Name).Should().BeFalse();
        bag.TryGet(Name, out _).Should().BeFalse();
        bag.TryGet(Count, out _).Should().BeTrue();

        bag.Clear();
        bag.Count.Should().Be(0);
        bag.TryGet(Count, out _).Should().BeFalse();

        bag.Set(Count, 2);
        bag.TryGet(Count, out var count).Should().BeTrue();
        count.Should().Be(2);
    }

    [Fact]
    public void Should_grow_beyond_initial_capacity()
    {
        var bag = new PropertyBag();
        var keys = Enumerable.Range(0, 20).Select(i => new PropertyKey<int>($"key{i}")).ToArray();

        for (var i = 0; i < keys.Length; i++)
            bag.Set(keys[i], i);

        bag.Count.Should().Be(keys.Length);
        for (var i = 0; i < keys.Length; i++)
        {
            bag.TryGet(keys[i], out var value).Should().BeTrue();
            value.Should().Be(i);
        }
    }
}
