// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using EasyNetQ.AdvancedMessagePolymorphism;
using EasyNetQ.Topology;
using NUnit.Framework;
using Rhino.Mocks;
using System.Threading.Tasks;
using System.Linq;

namespace EasyNetQ.Tests.AdvancedMessagePolymorphismTest
{
    [TestFixture]
    public class AdvancedPolymorphismPublishExchangeDeclareStrategyTests
    {
        [Test]
        public void When_declaring_exchanges_for_message_type_that_has_no_interface_one_exchange_created()
        {
            var advancedBus = MockRepository.GenerateStub<IAdvancedBus>();

            advancedBus.Stub(b => b.ExchangeDeclareAsync(
                Arg<string>.Is.Anything, 
                Arg<string>.Is.Anything,
                Arg<bool>.Is.Anything,
                Arg<bool>.Is.Anything,
                Arg<bool>.Is.Anything,
                Arg<bool>.Is.Anything,
                Arg<string>.Is.Anything,
                Arg<bool>.Is.Anything))
                .Return(Task.FromResult<IExchange>(MockRepository.GenerateStub<IExchange>()));

            advancedBus.Stub(b => b.BindAsync(Arg<IExchange>.Is.Anything, Arg<IExchange>.Is.Anything, Arg<string>.Is.Anything))
                .Return(Task.FromResult<IBinding>(MockRepository.GenerateStub<IBinding>()));

            advancedBus.Stub(c => c.Container.Resolve<IConventions>())
                .Return(new Conventions(new TypeNameSerializer()));

            var publishExchangeStrategy = new AdvancedPolymorphismPublishExchangeDeclareStrategy();

            publishExchangeStrategy.DeclareExchangeAsync(advancedBus, typeof(MyMessage), ExchangeType.Topic).Wait();

            //ensure that only one exchange is declared
            advancedBus.AssertWasCalled(m => m.ExchangeDeclareAsync(
                Arg<string>.Is.Anything,
                Arg<string>.Is.Anything,
                Arg<bool>.Is.Anything,
                Arg<bool>.Is.Anything,
                Arg<bool>.Is.Anything,
                Arg<bool>.Is.Anything,
                Arg<string>.Is.Anything,
                Arg<bool>.Is.Anything), opt => opt.Repeat.Times(1));

            //ensure that the exchange is not bound to anything
            advancedBus.AssertWasNotCalled(b => b.BindAsync(Arg<IExchange>.Is.Anything, Arg<IExchange>.Is.Anything, Arg<string>.Is.Anything));
        }

        [Test]
        public void When_declaring_exchanges_for_message_with_two_interfaces_exchange_per_interface_created_and_bound_to_concrete_version()
        {
            var advancedBus = MockRepository.GenerateStub<IAdvancedBus>();

            advancedBus.Stub(b => b.ExchangeDeclareAsync(
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
                                        case "EasyNetQ.Tests.AdvancedMessagePolymorphismTest.MessageWithTwoInterfaces:EasyNetQ.Tests":
                                            returnValue.Stub(e => e.Name).Return("MessageWithTwoInterfaces");
                                            break;
                                        case "EasyNetQ.Tests.AdvancedMessagePolymorphismTest.IMessageInterfaceOne:EasyNetQ.Tests":
                                            returnValue.Stub(e => e.Name).Return("IMessageInterfaceOne");
                                            break;
                                        case "EasyNetQ.Tests.AdvancedMessagePolymorphismTest.IMessageInterfaceTwo:EasyNetQ.Tests":
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

            var publishExchangeStrategy = new AdvancedPolymorphismPublishExchangeDeclareStrategy();

            publishExchangeStrategy.DeclareExchangeAsync(advancedBus, typeof(MessageWithTwoInterfaces), ExchangeType.Topic).Wait();

            //ensure that only one exchange is declared for concrete type
            advancedBus.AssertWasCalled(m => m.ExchangeDeclareAsync(
                Arg<string>.Is.Equal("EasyNetQ.Tests.AdvancedMessagePolymorphismTest.MessageWithTwoInterfaces:EasyNetQ.Tests"),
                Arg<string>.Is.Anything,
                Arg<bool>.Is.Anything,
                Arg<bool>.Is.Anything,
                Arg<bool>.Is.Anything,
                Arg<bool>.Is.Anything,
                Arg<string>.Is.Anything,
                Arg<bool>.Is.Anything), opt => opt.Repeat.Times(1));

            //ensure that only one exchange is declared for IMessageInterfaceOne
            advancedBus.AssertWasCalled(m => m.ExchangeDeclareAsync(
                Arg<string>.Is.Equal("EasyNetQ.Tests.AdvancedMessagePolymorphismTest.IMessageInterfaceOne:EasyNetQ.Tests"),
                Arg<string>.Is.Anything,
                Arg<bool>.Is.Anything,
                Arg<bool>.Is.Anything,
                Arg<bool>.Is.Anything,
                Arg<bool>.Is.Anything,
                Arg<string>.Is.Anything,
                Arg<bool>.Is.Anything), opt => opt.Repeat.Times(1));

            //ensure that only one exchange is declared for IMessageInterfaceTwo
            advancedBus.AssertWasCalled(m => m.ExchangeDeclareAsync(
                Arg<string>.Is.Equal("EasyNetQ.Tests.AdvancedMessagePolymorphismTest.IMessageInterfaceTwo:EasyNetQ.Tests"),
                Arg<string>.Is.Anything,
                Arg<bool>.Is.Anything,
                Arg<bool>.Is.Anything,
                Arg<bool>.Is.Anything,
                Arg<bool>.Is.Anything,
                Arg<string>.Is.Anything,
                Arg<bool>.Is.Anything), opt => opt.Repeat.Times(1));

            //ensure the IMessageInterfaceOne exchange is bound to the concreate type version
            advancedBus.AssertWasCalled(b => b.BindAsync(
                Arg<IExchange>.Matches( m => m.Name == "MessageWithTwoInterfaces"),
                Arg<IExchange>.Matches(m => m.Name == "IMessageInterfaceOne"), 
                Arg<string>.Is.Anything), opt => opt.Repeat.Times(1));

            //ensure the IMessageInterfaceTwo exchange is bound to the concreate type version
            advancedBus.AssertWasCalled(b => b.BindAsync(
                Arg<IExchange>.Matches(m => m.Name == "MessageWithTwoInterfaces"),
                Arg<IExchange>.Matches(m => m.Name == "IMessageInterfaceTwo"),
                Arg<string>.Is.Anything), opt => opt.Repeat.Times(1));
        }
    }
}