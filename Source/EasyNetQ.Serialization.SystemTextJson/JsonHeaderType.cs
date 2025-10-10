namespace EasyNetQ.Serialization.SystemTextJson;

internal static class JsonHeaderType
{
    public const int Null = 1;

    public const int Bool = 2;

    public const int Byte = 3;
    public const int SByte = 4;

    public const int Int16 = 5;
    public const int Int32 = 6;
    public const int UInt32 = 7;
    public const int Int64 = 8;

    public const int Single = 9;
    public const int Double = 10;

    public const int Decimal = 11;
    public const int AmqpTimestamp = 12;

    public const int String = 13;
    public const int Bytes = 14;
    public const int List = 15;
    public const int Dictionary = 16;
    public const int BinaryTable = 17;
}
