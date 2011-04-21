// ReSharper disable InconsistentNaming
using System;
using NUnit.Framework;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class DelegateNameBuilderTests
    {
        [Test]
        public void Should_be_able_to_build_a_name_for_an_anonymous_delegate()
        {
            const string expectedName = 
                "EasyNetQ_Tests_DelegateNameBuilderTests__Should_be_able_to_build_a_name_for_an_anonymous_delegate_b__0";
            Action action = () => { };

            var delegateName = DelegateNameBuilder.CreateNameFrom(action);

            delegateName.ShouldEqual(expectedName);
        }
    }
}

// ReSharper restore InconsistentNaming