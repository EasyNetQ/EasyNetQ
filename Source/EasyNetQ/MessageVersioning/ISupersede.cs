using EasyNetQ.DI;

namespace EasyNetQ.MessageVersioning
{
    /// <summary>
    /// Marker interface to indicate that a message supersedes a previous version.
    /// </summary>
    /// <remarks>
    /// Requires that <see cref="VersionedMessageSerializationStrategy"/> and <see cref="VersionedExchangeDeclareStrategy"/> are
    /// registered in the <see cref="IServiceRegister"/> to take advantage of message version support.
    /// </remarks>
    /// <typeparam name="T">The type of the message being superseded.</typeparam>
    /// <example>
    /// In the following code, MessageV2 extends and supersedes MessageV1. When MessageV2 is published, it will also be routed to
    /// any MessageV1 subscribers.
    /// <code>
    /// <![CDATA[
    /// public class MessageV1
    /// {
    ///     public string SomeProperty { get; set; }
    /// }
    ///
    /// public class MessageV2 : MessageV1, ISupersede<MessageV1>
    /// {
    ///     public DateTime SomeOtherProperty { get; set; }
    /// }
    /// ]]>
    /// </code>
    /// </example>
    public interface ISupersede<T> where T : class { }
}
