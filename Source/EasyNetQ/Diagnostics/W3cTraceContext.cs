using System.Diagnostics;

namespace EasyNetQ;

/// <summary>
///     We don't want to add a direct dependency to OpenTelemetry packages, so this
///     class contains parts to follow <see href="https://www.w3.org/TR/trace-context/"/>
/// </summary>
internal static class W3cTraceContext
{
    /// <summary>
    ///     <see href="https://www.w3.org/TR/trace-context/#header-name">traceparent</see>
    /// </summary>
    public const string TraceParent = "traceparent";

    /// <summary>
    ///     <see href="https://www.w3.org/TR/trace-context/#tracestate-header">tracestate</see>
    /// </summary>
    public const string TraceState = "tracestate";

    /// <summary>
    ///     Tries to parse w3c traceparent string like: <example>00-b468d5bdaaf241abb60e5b1bd2c87ae4-2b5b2e68225d41d0-01</example>
    /// </summary>
    public static bool TryParseTraceparent(string traceparent, out ActivityTraceId traceId, out ActivitySpanId spanId, out ActivityTraceFlags traceFlags)
    {
        const int VersionPrefixIdLength = 3;
        const int TraceIdLength = 32;
        const int VersionAndTraceIdLength = 36;
        const int SpanIdLength = 16;
        const int VersionAndTraceIdAndSpanIdLength = 53;
        const int TraceparentLengthV0 = 55;

        traceId = default;
        spanId = default;
        traceFlags = default;

        if (string.IsNullOrWhiteSpace(traceparent) || traceparent.Length < TraceparentLengthV0)
            return false;
        try
        {
            if (traceparent[VersionPrefixIdLength - 1] != '-')
                return false;

            var versionMajor = HexCharToByte(traceparent[0]);
            var versionMinor = HexCharToByte(traceparent[1]);

            if (versionMajor == 0xf && versionMinor == 0xf)
                return false;
            // we only works with w3c 00- version
            if (versionMajor != 0 || versionMinor != 0)
                return false;

            if (traceparent[VersionAndTraceIdLength - 1] != '-')
                return false;

            traceId = ActivityTraceId.CreateFromString(traceparent.AsSpan().Slice(VersionPrefixIdLength, TraceIdLength));

            if (traceparent[VersionAndTraceIdAndSpanIdLength - 1] != '-')
                return false;

            spanId = ActivitySpanId.CreateFromString(traceparent.AsSpan().Slice(VersionAndTraceIdLength, SpanIdLength));
            var flags = HexCharToByte(traceparent[VersionAndTraceIdAndSpanIdLength + 1]);

            if ((flags & 1) == 1)
            {
                traceFlags |= ActivityTraceFlags.Recorded;
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static byte HexCharToByte(char c)
    {
        if ((c >= '0') && (c <= '9'))
            return (byte)(c - '0');

        if ((c >= 'a') && (c <= 'f'))
            return (byte)(c - 'a' + 10);

        throw new ArgumentOutOfRangeException(nameof(c), c, $"Invalid hex character '{c}'");
    }
}
