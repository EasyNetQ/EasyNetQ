using System.Diagnostics;

namespace EasyNetQ.Tests;

public class W3cTraceContextTests
{
    [Theory]
    [MemberData(nameof(TryParseTraceparent_Input))]
    public void TryParseTraceparent(
        string input,
        bool expectedResult,
        ActivityTraceId expectedTraceId,
        ActivitySpanId expectedSpanId,
        ActivityTraceFlags expectedFlags
    )
    {
        var result = W3cTraceContext.TryParseTraceparent(input, out var traceId, out var spanId, out var traceFlags);
        Assert.Equal(expectedResult, result);
        Assert.Equal(expectedTraceId, traceId);
        Assert.Equal(expectedSpanId, spanId);
        Assert.Equal(expectedFlags, traceFlags);
    }

    public readonly static IEnumerable<object[]> TryParseTraceparent_Input = new[]
    {
        new object[]
        {
            "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01",
            true,
            ActivityTraceId.CreateFromString("0af7651916cd43dd8448eb211c80319c"),
            ActivitySpanId.CreateFromString("b7ad6b7169203331"),
            ActivityTraceFlags.Recorded,
        },
        new object[]
        {
            "00-0af7651916cd43dd8448eb211c80319c-2b5b2e68225d41d0-00",
            true,
            ActivityTraceId.CreateFromString("0af7651916cd43dd8448eb211c80319c"),
            ActivitySpanId.CreateFromString("2b5b2e68225d41d0"),
            ActivityTraceFlags.None,
        },
        new object[]
        {
            "11-0af7651916cd43dd8448eb211c80319c-2b5b2e68225d41d0-00",
            false,
            default(ActivityTraceId),
            default(ActivitySpanId),
            default(ActivityTraceFlags),
        },
        new object[]
        {
            "11-0af7651916cd43dd8448eb211c80319c-2b5b2e68225d41d0-00111",
            false,
            default(ActivityTraceId),
            default(ActivitySpanId),
            default(ActivityTraceFlags),
        },
        new object[]
        {
            "00-0af7651916cd43dd8448eb211c80319c-Zb5b2e68225d1dZ-00",
            false,
            default(ActivityTraceId),
            default(ActivitySpanId),
            default(ActivityTraceFlags),
        },
    };
}
