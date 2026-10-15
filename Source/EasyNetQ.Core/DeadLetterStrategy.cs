namespace EasyNetQ;

/// <summary>
/// Represents dead letter strategies of a queue
/// </summary>
public static class DeadLetterStrategy
{
    /// <summary>
    /// Default one for quorum queues.
    /// Messages are removed from the original queue immediately after publishing to the DLX target queue.
    /// This ensures that there is no chance of excessive message buildup that could exhaust broker resources, but messages can be lost if the target queue isn't available to accept messages.
    /// </summary>
    public const string AtMostOnce = "at-most-once";

    /// <summary>
    /// Messages are re-published with publisher confirms turned on internally.
    /// </summary>
    public const string AtLeastOnce = "at-least-once";
}
