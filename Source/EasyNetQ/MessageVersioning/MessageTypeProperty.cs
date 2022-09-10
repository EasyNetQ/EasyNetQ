using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EasyNetQ.MessageVersioning;

public class MessageTypeProperty
{
    private const string AlternativeMessageTypesHeaderKey = "Alternative-Message-Types";
    private const string AlternativeMessageTypeSeparator = ";";
    private readonly List<string> alternativeTypes;
    private readonly string firstAlternativeMessageType;

    private readonly ITypeNameSerializer typeNameSerializer;

    private MessageTypeProperty(ITypeNameSerializer typeNameSerializer, Type messageType)
    {
        this.typeNameSerializer = typeNameSerializer;
        var messageVersions = new MessageVersionStack(messageType);
        // MessageVersionStack has most recent version at the bottom of the stack (hence the reverse)
        // and includes the actual message type (hence the first / removeat)
        alternativeTypes = messageVersions
            .Select(typeNameSerializer.Serialize)
            .Reverse()
            .ToList();
        firstAlternativeMessageType = alternativeTypes[0];
        alternativeTypes.RemoveAt(0);
    }

    private MessageTypeProperty(ITypeNameSerializer typeNameSerializer, string firstAlternativeMessageType, string? alternativeTypesHeader)
    {
        this.typeNameSerializer = typeNameSerializer;
        this.firstAlternativeMessageType = firstAlternativeMessageType;
        alternativeTypes = (alternativeTypesHeader ?? "")
            .Split(new[] { AlternativeMessageTypeSeparator }, StringSplitOptions.RemoveEmptyEntries)
            .ToList();
    }

    public void AppendTo(MessageProperties messageProperties)
    {
        messageProperties.Type = firstAlternativeMessageType;

        if (alternativeTypes.Count > 0)
            messageProperties.Headers[AlternativeMessageTypesHeaderKey] = string.Join(AlternativeMessageTypeSeparator, alternativeTypes);
    }

    public Type GetMessageType()
    {
        if (TryDeserializeType(firstAlternativeMessageType, out var messageType))
            return messageType!;

        foreach (var alternativeType in alternativeTypes)
            if (TryDeserializeType(alternativeType, out messageType))
                return messageType!;

        throw new EasyNetQException(
            "Cannot find declared message type {0} or any of the specified alternative types {1}", this.firstAlternativeMessageType,
            string.Join(AlternativeMessageTypeSeparator, alternativeTypes)
        );
    }

    public static MessageTypeProperty CreateForMessageType(Type messageType, ITypeNameSerializer typeNameSerializer) => new(typeNameSerializer, messageType);

    public static MessageTypeProperty ExtractFromProperties(MessageProperties messageProperties, ITypeNameSerializer typeNameSerializer)
    {
        var messageType = messageProperties.Type;
        if (messageType == null)
            throw new EasyNetQException("Type is empty");

        if (!messageProperties.HeadersPresent || !messageProperties.Headers.ContainsKey(AlternativeMessageTypesHeaderKey))
            return new MessageTypeProperty(typeNameSerializer, messageType, null);

        if (messageProperties.Headers[AlternativeMessageTypesHeaderKey] is not byte[] rawHeader)
            throw new EasyNetQException(
                "{0} header was present but contained no data or was not encoded as a byte[].",
                AlternativeMessageTypesHeaderKey
            );

        var alternativeTypesHeader = Encoding.UTF8.GetString(rawHeader);
        return new MessageTypeProperty(typeNameSerializer, messageType, alternativeTypesHeader);
    }

    private bool TryDeserializeType(string typeString, out Type? messageType)
    {
        try
        {
            messageType = typeNameSerializer.DeSerialize(typeString);
            return true;
        }
        catch
        {
            messageType = null;
            return false;
        }
    }
}
