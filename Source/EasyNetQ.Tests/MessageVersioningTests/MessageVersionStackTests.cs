// ReSharper disable InconsistentNaming

using System.Linq;
using EasyNetQ.MessageVersioning;
using NUnit.Framework;

namespace EasyNetQ.Tests.MessageVersioningTests
{
    [TestFixture]
    public class MessageVersionStackTests
    {
        [Test]
        public void Unversioned_messages_create_a_stack_containing_the_message_type()
        {
            var stack = new MessageVersionStack( typeof( MyMessage ) );

            Assert.That( stack.Single(), Is.EqualTo( typeof( MyMessage ) ) );
        }

        [Test]
        public void Versioned_messages_create_a_stack_containing_the_message_type_and_all_superseded_types_oldest_first()
        {
            var stack = new MessageVersionStack( typeof( MyMessageV2 ) );

            Assert.That( stack.ElementAt( 0 ), Is.EqualTo( typeof( MyMessage ) ) );
            Assert.That( stack.ElementAt( 1 ), Is.EqualTo( typeof( MyMessageV2 ) ) );
        }

        [Test]
        public void Pop_returns_the_top_of_the_stack()
        {
            var stack = new MessageVersionStack( typeof( MyMessageV2 ) );
            var top = stack.ElementAt( 0 );
            Assert.That( stack.Pop(), Is.EqualTo( top ) );
        }

        [Test]
        public void IsEmpty_returns_false_for_non_empty_stack()
        {
            var stack = new MessageVersionStack( typeof( MyMessage ) );

            Assert.That( stack.Count(), Is.GreaterThan( 0 ) );
            Assert.That( stack.IsEmpty(), Is.False );
        }

        [Test]
        public void IsEmpty_returns_true_for_empty_stack()
        {
            var stack = new MessageVersionStack( typeof( MyMessage ) );
            stack.Pop();

            Assert.That( stack.Count(), Is.EqualTo( 0 ) );
            Assert.That( stack.IsEmpty(), Is.True );
        }

        [Test]
        public void When_message_type_is_not_subclass_of_superseded_type_exception_is_thrown()
        {
            Assert.Throws<EasyNetQException>( () => new MessageVersionStack( typeof( MyOtherMessage ) ) );
        }
    }

    public class MyOtherMessage : MyMessage, ISupersede<MyMessageV2>
    {
        public int AnotherNumber { get; set; }
    }
}