using System;
using System.Threading.Tasks;

namespace EasyNetQ.Internals
{
    public static class AwaitableDisposableExtensions
    {
        public static AwaitableDisposable<T> ToAwaitableDisposable<T>(this Task<T> source) where T : IDisposable
        {
            Preconditions.CheckNotNull(source, "source");

            return new AwaitableDisposable<T>(source);
        }
    }
}