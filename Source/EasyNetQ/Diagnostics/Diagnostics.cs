using System.Diagnostics;

namespace EasyNetQ;

internal static class Diagnostics
{
    public static readonly ActivitySource ActivitySource = new(
        "EasyNetQ",
        typeof(Diagnostics).GetType().Assembly.GetName().Version?.ToString() ?? "1.0"
    );
}
