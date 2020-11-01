// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EasyNetQ.MultipleExchange;
using EasyNetQ.Producer;
using EasyNetQ.Topology;
using NSubstitute;
using Xunit;

namespace EasyNetQ.Tests.MultipleExchangeTest
{
    public class AdvancedPolymorphismPublishExchangeDeclareStrategyTests
    {
        [Fact(Skip = "Needs to be updated to XUnit")]
        public void When_declaring_exchanges_for_message_type_that_has_no_interface_one_exchange_created()
        {
            var advancedBus = Substitute.For<IAdvancedBus>();

            advancedBus.ExchangeDeclareAsync(
                    Arg.Any<string>(),
                    Arg.Any<Action<IExchangeDeclareConfiguration>>()
                )
                .Returns(Task.FromResult(Substitute.For<IExchange>()));

            advancedBus.BindAsync(Arg.Any<Exchange>(), Arg.Any<Queue>(), Arg.Any<string>(), Arg.Any<IDictionary<string, object>>())
                .Returns(Task.FromResult(Substitute.For<IBinding>()));

            var conventions = Substitute.For<IConventions>();
            conventions.ExchangeNamingConvention.Returns(t => t.Name);

            var exchangeStrategy = new MultipleExchangeDeclareStrategy(conventions, advancedBus);

            exchangeStrategy.DeclareExchange(typeof(MyMessage), ExchangeType.Topic);

/*
            //ensure that only one exchange is declared
            advancedBus.ExchangeDeclareAsync(
                Arg<string>.Is.Anything,
                Arg<string>.Is.Anything,
                Arg<bool>.Is.Anything,
                Arg<bool>.Is.Anything,
                Arg<bool>.Is.Anything,
                Arg<bool>.Is.Anything,
                Arg<string>.Is.Anything,
                Arg<bool>.Is.Anything).ReturnsForAnyArgs(), opt => opt.Repeat.Times(1));

            //ensure that the exchange is not bound to anything
            advancedBus.AssertWasNotCalled(b => b.BindAsync(Arg<IExchange>.Is.Anything, Arg<IExchange>.Is.Anything, Arg<string>.Is.Anything));
*/
        }

        [Fact(Skip = "Needs to be updated to XUnit")]
        public void When_declaring_exchanges_for_message_with_two_interfaces_exchange_per_interface_created_and_bound_to_concrete_version()
        {
            /*var advancedBus = Substitute.For<IAdvancedBus>();

            advancedBus.ExchangeDeclareAsync(
                Arg<string>.Is.Anything,
                Arg<string>.Is.Anything,
                Arg<bool>.Is.Anything,
                Arg<bool>.Is.Anything,
                Arg<bool>.Is.Anything,
                Arg<bool>.Is.Anything,
                Arg<string>.Is.Anything,
                Arg<bool>.Is.Anything))
                                .WhenCalled(m => {
                                    var returnValue = MockRepository.GenerateStub<IExchange>();

                                    switch (m.Arguments[0].ToString())
                                    {
                                        case "EasyNetQ.Tests.MultipleExchangeTest.MessageWithTwoInterfaces:EasyNetQ.Tests":
                                            returnValue.Stub(e => e.Name).Return("MessageWithTwoInterfaces");
                                            break;
                                        case "EasyNetQ.Tests.MultipleExchangeTest.IMessageInterfaceOne:EasyNetQ.Tests":
                                            returnValue.Stub(e => e.Name).Return("IMessageInterfaceOne");
                                            break;
                                        case "EasyNetQ.Tests.MultipleExchangeTest.IMessageInterfaceTwo:EasyNetQ.Tests":
                                            returnValue.Stub(e => e.Name).Return("IMessageInterfaceTwo");
                                            break;
                                        default:
                                            break;
                                    }

                                    m.ReturnValue = Task.FromResult<IExchange>(returnValue);
                                })
                .Return(null);

            advancedBus.Stub(b => b.BindAsync(Arg<IExchange>.Is.Anything, Arg<IExchange>.Is.Anything, Arg<string>.Is.Anything))
                .Return(Task.FromResult<IBinding>(MockRepository.GenerateStub<IBinding>()));

            advancedBus.Stub(c => c.Container.Resolve<IConventions>())
                .Return(new Conventions(new TypeNameSerializer()));

            var publishExchangeStrategy = new MultipleExchangePublishExchangeDeclareStrategy();

            publishExchangeStrategy.DeclareExchangeAsync(advancedBus, typeof(MessageWithTwoInterfaces), ExchangeType.Topic).Wait();

            //ensure that only one exchange is declared for concrete type
            advancedBus.AssertWasCalled(m => m.ExchangeDeclareAsync(
                Arg<string>.Is.Equal("EasyNetQ.Tests.MultipleExchangeTest.MessageWithTwoInterfaces:EasyNetQ.Tests"),
                Arg<string>.Is.Anything,
                Arg<bool>.Is.Anything,
                Arg<bool>.Is.Anything,
                Arg<bool>.Is.Anything,
                Arg<bool>.Is.Anything,
                Arg<string>.Is.Anything,
                Arg<bool>.Is.Anything), opt => opt.Repeat.Times(1));

            //ensure that only one exchange is declared for IMessageInterfaceOne
            advancedBus.AssertWasCalled(m => m.ExchangeDeclareAsync(
                Arg<string>.Is.Equal("EasyNetQ.Tests.MultipleExchangeTest.IMessageInterfaceOne:EasyNetQ.Tests"),
                Arg<string>.Is.Anything,
                Arg<bool>.Is.Anything,
                Arg<bool>.Is.Anything,
                Arg<bool>.Is.Anything,
                Arg<bool>.Is.Anything,
                Arg<string>.Is.Anything,
                Arg<bool>.Is.Anything), opt => opt.Repeat.Times(1));

            //ensure that only one exchange is declared for IMessageInterfaceTwo
            advancedBus.AssertWasCalled(m => m.ExchangeDeclareAsync(
                Arg<string>.Is.Equal("EasyNetQ.Tests.MultipleExchangeTest.IMessageInterfaceTwo:EasyNetQ.Tests"),
                Arg<string>.Is.Anything,
                Arg<bool>.Is.Anything,
                Arg<bool>.Is.Anything,
                Arg<bool>.Is.Anything,
                Arg<bool>.Is.Anything,
                Arg<string>.Is.Anything,
                Arg<bool>.Is.Anything), opt => opt.Repeat.Times(1));

            //ensure the IMessageInterfaceOne exchange is bound to the concrete type version
            advancedBus.AssertWasCalled(b => b.BindAsync(
                Arg<IExchange>.Matches(m => m.Name == "MessageWithTwoInterfaces"),
                Arg<IExchange>.Matches(m => m.Name == "IMessageInterfaceOne"),
                Arg<string>.Is.Anything), opt => opt.Repeat.Times(1));

            //ensure the IMessageInterfaceTwo exchange is bound to the concrete type version
            advancedBus.AssertWasCalled(b => b.BindAsync(
                Arg<IExchange>.Matches(m => m.Name == "MessageWithTwoInterfaces"),
                Arg<IExchange>.Matches(m => m.Name == "IMessageInterfaceTwo"),
                Arg<string>.Is.Anything), opt => opt.Repeat.Times(1));*/
        }
    }
}
