using HubSerializedMessage = Microsoft.AspNetCore.SignalR.SerializedMessage;
using System.Buffers;
using System.Buffers.Binary;
using System.Text;
using Microsoft.AspNetCore.SignalR;

namespace EasyNetQ.AspNetCore.SignalR.Internal;

internal enum BackplaneMessageKind : byte
{
    Invocation = 1,
    GroupCommand = 2,
    Ack = 3,
    Completion = 4,
}

internal enum InvocationTarget : byte
{
    All = 0,
    Connection = 1,
    Group = 2,
    User = 3,
}

internal enum GroupAction : byte
{
    Add = 1,
    Remove = 2,
}

internal sealed record BackplaneInvocation(
    InvocationTarget Target,
    string? TargetName,
    IReadOnlyList<string>? ExcludedConnectionIds,
    IReadOnlyList<HubSerializedMessage> Messages,
    string? InvocationId = null,
    string? ReturnServer = null
);

internal sealed record BackplaneGroupCommand(int Id, string OriginServer, GroupAction Action, string ConnectionId, string GroupName);

internal sealed record BackplaneCompletion(string ProtocolName, ReadOnlyMemory<byte> Completion);

/// <summary>
///     Binary envelope of backplane traffic. Hub payloads stay as SignalR serialized them (one copy per supported
///     protocol), so no reflection and no JSON is involved here. Version byte first, so the format can evolve.
/// </summary>
internal static class BackplaneProtocol
{
    private const byte Version = 1;

    public static byte[] WriteInvocation(BackplaneInvocation invocation)
    {
        var writer = new Writer();
        writer.Header(BackplaneMessageKind.Invocation);
        writer.Byte((byte)invocation.Target);
        writer.NullableString(invocation.TargetName);
        writer.StringList(invocation.ExcludedConnectionIds);
        writer.NullableString(invocation.InvocationId);
        writer.NullableString(invocation.ReturnServer);
        writer.VarInt(invocation.Messages.Count);
        foreach (var message in invocation.Messages)
        {
            writer.String(message.ProtocolName);
            writer.Bytes(message.Serialized.Span);
        }
        return writer.ToArray();
    }

    public static byte[] WriteGroupCommand(BackplaneGroupCommand command)
    {
        var writer = new Writer();
        writer.Header(BackplaneMessageKind.GroupCommand);
        writer.Int32(command.Id);
        writer.String(command.OriginServer);
        writer.Byte((byte)command.Action);
        writer.String(command.ConnectionId);
        writer.String(command.GroupName);
        return writer.ToArray();
    }

    public static byte[] WriteAck(int id)
    {
        var writer = new Writer();
        writer.Header(BackplaneMessageKind.Ack);
        writer.Int32(id);
        return writer.ToArray();
    }

    public static byte[] WriteCompletion(BackplaneCompletion completion)
    {
        var writer = new Writer();
        writer.Header(BackplaneMessageKind.Completion);
        writer.String(completion.ProtocolName);
        writer.Bytes(completion.Completion.Span);
        return writer.ToArray();
    }

    public static BackplaneMessageKind ReadKind(ReadOnlySpan<byte> body)
    {
        if (body.Length < 2)
            throw new InvalidDataException("Backplane message too short");
        if (body[0] != Version)
            throw new InvalidDataException($"Unsupported backplane message version {body[0]}");
        return (BackplaneMessageKind)body[1];
    }

    public static BackplaneInvocation ReadInvocation(ReadOnlyMemory<byte> body)
    {
        var reader = new Reader(body, BackplaneMessageKind.Invocation);
        var target = (InvocationTarget)reader.Byte();
        var targetName = reader.NullableString();
        var excluded = reader.StringList();
        var invocationId = reader.NullableString();
        var returnServer = reader.NullableString();
        var count = reader.VarInt();
        var messages = new HubSerializedMessage[count];
        for (var i = 0; i < count; i++)
        {
            var protocol = reader.String();
            messages[i] = new HubSerializedMessage(protocol, reader.Bytes());
        }
        return new BackplaneInvocation(target, targetName, excluded, messages, invocationId, returnServer);
    }

    public static BackplaneGroupCommand ReadGroupCommand(ReadOnlyMemory<byte> body)
    {
        var reader = new Reader(body, BackplaneMessageKind.GroupCommand);
        return new BackplaneGroupCommand(reader.Int32(), reader.String(), (GroupAction)reader.Byte(), reader.String(), reader.String());
    }

    public static int ReadAck(ReadOnlyMemory<byte> body) => new Reader(body, BackplaneMessageKind.Ack).Int32();

    public static BackplaneCompletion ReadCompletion(ReadOnlyMemory<byte> body)
    {
        var reader = new Reader(body, BackplaneMessageKind.Completion);
        return new BackplaneCompletion(reader.String(), reader.Bytes());
    }

    private sealed class Writer
    {
        private readonly ArrayBufferWriter<byte> buffer = new(256);

        public void Header(BackplaneMessageKind kind)
        {
            Byte(Version);
            Byte((byte)kind);
        }

        public void Byte(byte value)
        {
            buffer.GetSpan(1)[0] = value;
            buffer.Advance(1);
        }

        public void Int32(int value)
        {
            BinaryPrimitives.WriteInt32LittleEndian(buffer.GetSpan(4), value);
            buffer.Advance(4);
        }

        public void VarInt(int value)
        {
            var remaining = (uint)value;
            while (remaining >= 0x80)
            {
                Byte((byte)(remaining | 0x80));
                remaining >>= 7;
            }
            Byte((byte)remaining);
        }

        public void Bytes(ReadOnlySpan<byte> value)
        {
            VarInt(value.Length);
            value.CopyTo(buffer.GetSpan(value.Length));
            buffer.Advance(value.Length);
        }

        public void String(string value)
        {
            var length = Encoding.UTF8.GetByteCount(value);
            VarInt(length);
            Encoding.UTF8.GetBytes(value, buffer.GetSpan(length));
            buffer.Advance(length);
        }

        // 0 = null, otherwise length + 1
        public void NullableString(string? value)
        {
            if (value is null)
            {
                VarInt(0);
                return;
            }
            var length = Encoding.UTF8.GetByteCount(value);
            VarInt(length + 1);
            Encoding.UTF8.GetBytes(value, buffer.GetSpan(length));
            buffer.Advance(length);
        }

        public void StringList(IReadOnlyList<string>? values)
        {
            VarInt(values?.Count ?? 0);
            if (values is null) return;
            foreach (var value in values)
                String(value);
        }

        public byte[] ToArray() => buffer.WrittenSpan.ToArray();
    }

    private struct Reader
    {
        private readonly ReadOnlyMemory<byte> body;
        private int position;

        public Reader(ReadOnlyMemory<byte> body, BackplaneMessageKind expected)
        {
            var kind = ReadKind(body.Span);
            if (kind != expected)
                throw new InvalidDataException($"Expected a {expected} backplane message, got {kind}");
            this.body = body;
            position = 2;
        }

        private ReadOnlySpan<byte> Take(int count)
        {
            if (count < 0 || position + count > body.Length)
                throw new InvalidDataException("Backplane message truncated");
            var span = body.Span.Slice(position, count);
            position += count;
            return span;
        }

        public byte Byte() => Take(1)[0];

        public int Int32() => BinaryPrimitives.ReadInt32LittleEndian(Take(4));

        public int VarInt()
        {
            uint result = 0;
            for (var shift = 0; shift < 35; shift += 7)
            {
                var b = Byte();
                result |= (uint)(b & 0x7F) << shift;
                if ((b & 0x80) == 0)
                    return checked((int)result);
            }
            throw new InvalidDataException("Malformed length in backplane message");
        }

        public ReadOnlyMemory<byte> Bytes()
        {
            var length = VarInt();
            Take(length);
            return body.Slice(position - length, length);
        }

        public string String() => Encoding.UTF8.GetString(Take(VarInt()));

        public string? NullableString()
        {
            var length = VarInt();
            return length == 0 ? null : Encoding.UTF8.GetString(Take(length - 1));
        }

        public IReadOnlyList<string>? StringList()
        {
            var count = VarInt();
            if (count == 0) return null;
            var values = new string[count];
            for (var i = 0; i < count; i++)
                values[i] = String();
            return values;
        }
    }
}
