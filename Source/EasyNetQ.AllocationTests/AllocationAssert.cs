using System.Runtime.CompilerServices;

namespace EasyNetQ.AllocationTests;

/// <summary>
///     Deterministic per-iteration allocation measurement. Unlike BenchmarkDotNet timings this is stable on
///     shared CI runners, so the ceilings asserted by the tests act as a ratchet: they may only go down.
/// </summary>
public static class AllocationAssert
{
    private const int WarmupIterations = 100;
    private const int MeasuredIterations = 1_000;

    public static long BytesPerIteration(Func<ValueTask> action)
    {
        return Measure(() => Complete(action()));
    }

    public static long BytesPerIteration<T>(Func<ValueTask<T>> action)
    {
        return Measure(() => Complete(action()));
    }

    public static long BytesPerIteration(Func<Task> action)
    {
        return Measure(() => Complete(new ValueTask(action())));
    }

    public static long BytesPerIteration(Action action)
    {
        return Measure(action);
    }

    public static void ShouldNotExceed(long actualBytes, long ceilingBytes, [CallerMemberName] string scenario = "")
    {
        TestContext.Current.TestOutputHelper?.WriteLine($"{scenario}: {actualBytes} B/iteration (ceiling {ceilingBytes} B)");
        Assert.True(
            actualBytes <= ceilingBytes,
            $"{scenario} allocated {actualBytes} B per iteration, ceiling is {ceilingBytes} B. " +
            "If this is an intentional regression, raise the ceiling explicitly; otherwise find the new allocation."
        );
    }

    private static long Measure(Action iteration)
    {
        for (var i = 0; i < WarmupIterations; i++)
            iteration();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < MeasuredIterations; i++)
            iteration();
        var after = GC.GetAllocatedBytesForCurrentThread();

        return (after - before) / MeasuredIterations;
    }

    private static void Complete(ValueTask task)
    {
        if (!task.IsCompleted)
            throw new InvalidOperationException("Measured action must complete synchronously; asynchronous continuations would run on another thread and escape measurement.");
        task.GetAwaiter().GetResult();
    }

    private static void Complete<T>(ValueTask<T> task)
    {
        if (!task.IsCompleted)
            throw new InvalidOperationException("Measured action must complete synchronously; asynchronous continuations would run on another thread and escape measurement.");
        task.GetAwaiter().GetResult();
    }
}
