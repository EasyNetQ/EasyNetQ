// ReSharper disable InconsistentNaming

using System.Linq;
using EasyNetQ.MessageVersioning;
using FluentAssertions;
using Xunit;

namespace EasyNetQ.Tests.MessageVersioningTests
{
    public class MessageVersionStackTests
    {
        [Fact]
        public void Unversioned_messages_create_a_stack_containing_the_message_type()
        {
            var stack = new MessageVersionStack( typeof( MyMessage ) );

            Assert.Equal(typeof(MyMessage), stack.Single());
        }

        [Fact]
        public void Versioned_messages_create_a_stack_containing_the_message_type_and_all_superseded_types_oldest_first()
        {
            var stack = new MessageVersionStack( typeof( MyMessageV2 ));

            Assert.Equal(typeof(MyMessage), stack.ElementAt( 0 ));
            Assert.Equal(typeof(MyMessageV2), stack.ElementAt( 1 ));
        }

        [Fact]
        public void Versioned_message_stack_works_for_more_than_two_versions_and_types_are_ordered_oldest_first()
        {
            var stack = new MessageVersionStack( typeof( MyMessageV3 ));

            Assert.Equal(typeof(MyMessage), stack.ElementAt( 0 ));
            Assert.Equal(typeof(MyMessageV2), stack.ElementAt( 1 ));
            Assert.Equal(typeof(MyMessageV3), stack.ElementAt( 2 ));
        }

        [Fact]
        public void Versioned_message_stack_works_with_arbitrary_type_names()
        {
            var stack = new MessageVersionStack( typeof( ComplexMessage ));

            Assert.Equal(typeof(SimpleMessage), stack.ElementAt( 0 ));
            Assert.Equal(typeof(AdvancedMessage), stack.ElementAt( 1 ));
            Assert.Equal(typeof(ComplexMessage), stack.ElementAt( 2 ));
        }

        [Fact]
        public void If_given_just_an_object_the_stack_can_handle_it_without_exceptions()
        {
            var stack = new MessageVersionStack( typeof( object ));
        
            Assert.Equal(typeof(object), stack.ElementAt( 0 ));
        }

        [Fact]
        public void Pop_returns_the_top_of_the_stack()
        {
            var stack = new MessageVersionStack( typeof( MyMessageV2 ) );
            var top = stack.ElementAt( 0 );
            Assert.Equal( stack.Pop(),  top );
        }

        [Fact]
        public void IsEmpty_returns_false_for_non_empty_stack()
        {
            var stack = new MessageVersionStack( typeof( MyMessage ) );
            stack.Should().NotBeEmpty();
        }

        [Fact]
        public void IsEmpty_returns_true_for_empty_stack()
        {
            var stack = new MessageVersionStack( typeof( MyMessage ) );
            stack.Pop();

            stack.Should().BeEmpty();
        }

        [Fact]
        public void When_message_type_is_not_subclass_of_superseded_type_exception_is_thrown()
        {
            Assert.Throws<EasyNetQException>( () => new MessageVersionStack( typeof( MyOtherMessage ) ) );
        }
    }

    public class MyOtherMessage : MyMessage, ISupersede<MyMessageV2>
    {
        public int AnotherNumber { get; set; }
    }

    public class SimpleMessage
    {
        public string Message { get; set; }
    }

    public class AdvancedMessage : SimpleMessage, ISupersede<SimpleMessage>
    {
        public string VeryAdvanced { get; set; }
    }

    public class ComplexMessage : AdvancedMessage, ISupersede<AdvancedMessage>
    {
        public string SoComplex { get; set; }
    }    
}