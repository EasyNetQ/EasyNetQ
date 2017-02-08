using System.Threading;
using System.Threading.Tasks;

namespace EasyNetQ.Internals
{
    /// <summary>
    ///     AsyncSemaphore should be used with a lot of care.
    /// </summary>
    public class AsyncSemaphore
    {
        private readonly SemaphoreSlim semaphore;

        public AsyncSemaphore(int initial)
        {
            semaphore = new SemaphoreSlim(initial);
        }

        public int Available
        {
            get { return semaphore.CurrentCount; }
        }

        public void Wait()
        {
            semaphore.Wait();
        }

        public Task WaitAsync()
        {
            return semaphore.WaitAsync();
        }

        public void Release()
        {
            semaphore.Release();
        }
    }
}