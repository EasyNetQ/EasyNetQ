using NUnit.Framework;
using Rhino.Mocks;

namespace EasyNetQ.Tests.Interception
{
    public class UnitTestBase
    {
        private static MockRepository mockRepository;

        [SetUp]
        public void SetUp()
        {
            mockRepository = new MockRepository();
            mockRepository.ReplayAll();
        }


        [TearDown]
        public void TearDown()
        {
            mockRepository.VerifyAll();
        }

        protected static T NewMock<T>() where T : class
        {
            var mock = mockRepository.StrictMock<T>();
            mock.Replay();
            return mock;
        }
    }
}