namespace EasyNetQ.Internals;

internal static class NumberHelpers
{
    private static readonly ulong[] UlongDigitsCountLookup;

    static NumberHelpers()
    {
        UlongDigitsCountLookup = new ulong[20];
        UlongDigitsCountLookup[0] = 1;
        for (var i = 1; i < UlongDigitsCountLookup.Length; ++i)
            UlongDigitsCountLookup[i] = UlongDigitsCountLookup[i - 1] * 10;
    }

    public static int ULongBytesCount(ulong value)
    {
        if (value == 0) return 1;

        var count = 0;
        for (; count < UlongDigitsCountLookup.Length; count++)
            if (UlongDigitsCountLookup[count] > value)
                break;
        return count;
    }

    public static byte[] FormatULongToBytes(ulong value)
    {
        var bytes = new byte[ULongBytesCount(value)];
        for (var i = bytes.Length - 1; i >= 0; --i)
        {
            bytes[i] = (byte)('0' + value % 10);
            value /= 10;
        }
        return bytes;
    }

    public static bool TryParseULongFromBytes(byte[] bytes, out ulong value)
    {
        value = 0;
        if (bytes.Length == 0) return false;

        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < bytes.Length; ++i)
        {
            if (bytes[i] < '0' || bytes[i] > '9')
                return false;

            value = value * 10 + (ulong)(bytes[i] - '0');
        }

        return true;
    }
}
