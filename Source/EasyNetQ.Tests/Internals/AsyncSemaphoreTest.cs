using EasyNetQ.Internals;
using NUnit.Framework;

namespace EasyNetQ.Tests.Internals
{
    [TestFixture]
    public class AsyncSemaphoreTest
    {
        [Test]
        public void TestWaitRelease()
        {
            var semaphore = new AsyncSemaphore(1);
            Assert.AreEqual(1, semaphore.Available);
            semaphore.Wait();
            Assert.AreEqual(0, semaphore.Available);
            semaphore.Release();
            Assert.AreEqual(1, semaphore.Available);
        }
    }
}