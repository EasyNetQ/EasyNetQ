// ReSharper disable InconsistentNaming

using NUnit.Framework;
using Rhino.Mocks;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class DefaultServiceProviderTests
    {
        private IServiceProvider serviceProvider;

        private IMyFirst myFirst;
        private SomeDelegate someDelegate;

        [SetUp]
        public void SetUp()
        {
            myFirst = MockRepository.GenerateStub<IMyFirst>();
            someDelegate = () => { };

            var defaultServiceProvider = new DefaultServiceProvider();
            
            defaultServiceProvider.Register(x => myFirst);
            defaultServiceProvider.Register(x => someDelegate);
            defaultServiceProvider.Register<IMySecond>(x => new MySecond(x.Resolve<IMyFirst>()));

            serviceProvider = defaultServiceProvider;
        }

        [Test]
        public void Should_resolve_a_service_interface()
        {
            var resolvedService = serviceProvider.Resolve<IMyFirst>();
            resolvedService.ShouldBeTheSameAs(myFirst);
        }

        [Test]
        public void Should_resolve_a_delegate_service()
        {
            var resolvedService = serviceProvider.Resolve<SomeDelegate>();
            resolvedService.ShouldBeTheSameAs(someDelegate);
        }

        [Test]
        public void Should_resolve_a_service_with_dependencies()
        {
            var resolvedService = serviceProvider.Resolve<IMySecond>();
            resolvedService.First.ShouldBeTheSameAs(myFirst);
        }

        [Test, Explicit("Requires RabbitMQ instance")]
        public void Should_be_able_to_replace_bus_components()
        {
            var logger = MockRepository.GenerateStub<IEasyNetQLogger>();

            using(var bus = RabbitHutch.CreateBus("host=localhost", x => x.Register(_ => logger)))
            {
                // should not blow up
            }
        }
    }

    public interface IMyFirst
    {
        
    }

    public delegate void SomeDelegate();

    public interface IMySecond
    {
        IMyFirst First { get; }
    }

    public class MySecond : IMySecond
    {
        private readonly IMyFirst myFirst;

        public MySecond(IMyFirst myFirst)
        {
            this.myFirst = myFirst;
        }


        public IMyFirst First
        {
            get { return myFirst; }
        }
    }
}

// ReSharper restore InconsistentNaming