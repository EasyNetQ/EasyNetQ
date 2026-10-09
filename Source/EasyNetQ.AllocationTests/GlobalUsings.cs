global using Xunit;

// Allocation measurements use GC.GetAllocatedBytesForCurrentThread and must not share a thread with other tests
[assembly: CollectionBehavior(DisableTestParallelization = true)]
