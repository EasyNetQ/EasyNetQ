using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace EasyNetQ;

internal static class ActivityExtensions
{
    /// <summary>
    /// Adds OpenTelemetry tags and event according to <paramref name="ex"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RecordException(this Activity self, Exception ex)
    {
        var activityTagsCollection = new ActivityTagsCollection {
                    { "exception.type", ex.GetType().FullName},
                    { "exception.stacktrace", ex.ToString()},
                };
        if (!string.IsNullOrWhiteSpace(ex.Message))
        {
            activityTagsCollection.Add("exception.message", ex.Message);
        }
        self.AddEvent(new ActivityEvent("exception", default, activityTagsCollection));
    }
}
