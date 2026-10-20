using EasyNetQ.AspNetCore.SignalR.Internal;
using HubSerializedMessage = Microsoft.AspNetCore.SignalR.SerializedMessage;

namespace EasyNetQ.AspNetCore.SignalR.Tests;

public class When_encoding_backplane_messages
{
    [Fact]
    public void Should_round_trip_an_invocation_with_every_field()
    {
        var invocation = new BackplaneInvocation(
            InvocationTarget.Connection,
            "connection-1",
            ["a", "b"],
            [new HubSerializedMessage("json", new byte[] { 1, 2, 3 }), new HubSerializedMessage("messagepack", new byte[] { 9 })],
            "server:7",
            "server-a"
        );

        var decoded = BackplaneProtocol.ReadInvocation(BackplaneProtocol.WriteInvocation(invocation));

        decoded.Target.Should().Be(InvocationTarget.Connection);
        decoded.TargetName.Should().Be("connection-1");
        decoded.ExcludedConnectionIds.Should().Equal("a", "b");
        decoded.InvocationId.Should().Be("server:7");
        decoded.ReturnServer.Should().Be("server-a");
        decoded.Messages.Select(m => m.ProtocolName).Should().Equal("json", "messagepack");
        decoded.Messages[0].Serialized.ToArray().Should().Equal(1, 2, 3);
    }

    [Fact]
    public void Should_keep_null_and_empty_strings_apart()
    {
        var invocation = new BackplaneInvocation(InvocationTarget.Group, "", null, [new HubSerializedMessage("json", Array.Empty<byte>())]);

        var decoded = BackplaneProtocol.ReadInvocation(BackplaneProtocol.WriteInvocation(invocation));

        decoded.TargetName.Should().Be("");
        decoded.InvocationId.Should().BeNull();
        decoded.ExcludedConnectionIds.Should().BeNull();
    }

    [Fact]
    public void Should_round_trip_unicode_names()
    {
        var command = new BackplaneGroupCommand(42, "sérvér", GroupAction.Remove, "conn-ü", "グループ");

        BackplaneProtocol.ReadGroupCommand(BackplaneProtocol.WriteGroupCommand(command)).Should().Be(command);
    }

    [Fact]
    public void Should_round_trip_acks_and_completions()
    {
        BackplaneProtocol.ReadAck(BackplaneProtocol.WriteAck(int.MaxValue)).Should().Be(int.MaxValue);

        var completion = BackplaneProtocol.ReadCompletion(
            BackplaneProtocol.WriteCompletion(new BackplaneCompletion("json", new byte[] { 7, 8 }))
        );
        completion.ProtocolName.Should().Be("json");
        completion.Completion.ToArray().Should().Equal(7, 8);
    }

    [Fact]
    public void Should_encode_large_payloads()
    {
        var payload = Enumerable.Range(0, 200_000).Select(i => (byte)i).ToArray();
        var invocation = new BackplaneInvocation(InvocationTarget.All, null, null, [new HubSerializedMessage("json", payload)]);

        BackplaneProtocol.ReadInvocation(BackplaneProtocol.WriteInvocation(invocation)).Messages[0].Serialized.ToArray().Should().Equal(payload);
    }

    [Fact]
    public void Should_reject_truncated_messages()
    {
        var bytes = BackplaneProtocol.WriteGroupCommand(new BackplaneGroupCommand(1, "server", GroupAction.Add, "conn", "group"));

        var read = () => BackplaneProtocol.ReadGroupCommand(bytes.AsMemory(0, bytes.Length - 3));

        read.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void Should_reject_an_unknown_version()
    {
        var bytes = BackplaneProtocol.WriteAck(1);
        bytes[0] = 99;

        var read = () => BackplaneProtocol.ReadAck(bytes);

        read.Should().Throw<InvalidDataException>().WithMessage("*version 99*");
    }

    [Fact]
    public void Should_reject_a_different_kind()
    {
        var read = () => BackplaneProtocol.ReadInvocation(BackplaneProtocol.WriteAck(1));

        read.Should().Throw<InvalidDataException>();
    }
}

public class When_naming_backplane_topology
{
    [Fact]
    public void Should_prefix_routing_keys_by_kind()
    {
        BackplaneNames.Group("#").Should().Be("group.#");
        BackplaneNames.User("*").Should().Be("user.*");
        BackplaneNames.Connection("abc").Should().Be("conn.abc");
    }

    [Fact]
    public void Should_hash_keys_over_the_amqp_limit_deterministically()
    {
        var longName = new string('x', 300);

        var key = BackplaneNames.Group(longName);

        key.Should().StartWith("group:").And.HaveLength("group:".Length + 64);
        BackplaneNames.Group(longName).Should().Be(key);
        BackplaneNames.Group(longName + "y").Should().NotBe(key);
    }

    [Fact]
    public void Should_keep_names_within_255_bytes()
    {
        var names = new BackplaneNames(new string('p', 200), new string('h', 200), "server");

        System.Text.Encoding.UTF8.GetByteCount(names.Exchange).Should().BeLessThanOrEqualTo(255);
        System.Text.Encoding.UTF8.GetByteCount(names.Queue).Should().BeLessThanOrEqualTo(255);
    }
}
