using NUnit.Framework;

namespace EasyNetQ.Tests
{
	[TestFixture]
	public class DefaultMessageSerializationStrategyTests
	{
		// Serialise returns expected message body, sets typename and correlation id
		// Serialise does not override correlation id if it is present
		// Deserialise returns the expected message type and message and properties are set
		[Test]
		public void Write_Some_Tests()
		{
			Assert.Fail( "You have failed to write any tests!");
		}
	}

	[TestFixture]
	public class VersionedMessageSerializationStrategyTests
	{
		// Serialise returns expected message body, sets typename and correlation id
		// Serialise does not override correlation id if it is present
		// Deserialise returns the expected message type and message and properties are set
		// If the message supersedes another, correct fallback types are included during serialisation
		// If the message contains fallback types, the first available type will be used to deserialise the message
		[Test]
		public void Write_Some_Tests()
		{
			Assert.Fail( "You have failed to write any tests!");
		}
	}

	[TestFixture]
	public class MessageVersionStackTests
	{
		[Test]
		public void Write_Some_Tests()
		{
			Assert.Fail("You have failed to write any tests!");
		}
	}

	[TestFixture]
	public class MessageTypePropertiesTests
	{
		[Test]
		public void Write_Some_Tests()
		{
			Assert.Fail("You have failed to write any tests!");
		}
	}

	public class VersionedPublishExchangeDeclareStrategyTests
	{
		[Test]
		public void Write_Some_Tests()
		{
			Assert.Fail("You have failed to write any tests!");
		}
	}
}