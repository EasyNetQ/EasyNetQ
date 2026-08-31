using System.Collections.Concurrent;

namespace EasyNetQ.Pipeline;

/// <summary>
///     A small lock-free pool of reusable contexts: a single fast slot plus a bounded queue
/// </summary>
public sealed class ContextPool<TContext> where TContext : LayerContext
{
    private readonly Func<TContext> factory;
    private readonly int maxRetained;
    private readonly ConcurrentQueue<TContext> items = new();
    private TContext? fastItem;
    private int count;

    /// <summary>
    ///     Creates a pool
    /// </summary>
    /// <param name="factory">Creates a new context when the pool is empty</param>
    /// <param name="maxRetained">Maximum number of idle contexts kept (defaults to twice the processor count)</param>
    public ContextPool(Func<TContext> factory, int? maxRetained = null)
    {
        this.factory = factory;
        this.maxRetained = maxRetained ?? Environment.ProcessorCount * 2;
    }

    /// <summary>
    ///     Takes a context from the pool or creates one
    /// </summary>
    public TContext Rent()
    {
        var item = fastItem;
        if (item is not null && Interlocked.CompareExchange(ref fastItem, null, item) == item)
            return item;

        if (items.TryDequeue(out item))
        {
            Interlocked.Decrement(ref count);
            return item;
        }

        return factory();
    }

    /// <summary>
    ///     Resets <paramref name="context" /> and puts it back, unless it was detached or the pool is full
    /// </summary>
    public void Return(TContext context)
    {
        if (context.IsDetached) return;

        context.Reset();

        if (fastItem is null && Interlocked.CompareExchange(ref fastItem, context, null) is null)
            return;

        if (Interlocked.Increment(ref count) <= maxRetained)
            items.Enqueue(context);
        else
            Interlocked.Decrement(ref count);
    }
}
