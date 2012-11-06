using System;
using System.Collections.Generic;

namespace EasyNetQ.FluentConfiguration
{
    /// <summary>
    /// Allow fluent configuration for Publish
    /// </summary>
    /// <typeparam name="T">The message type to publish</typeparam>
    public interface IPublishConfiguration<T>
    {
        /// <summary>
        /// Add a topic for message publish
        /// </summary>
        /// <param name="topic">The topic to add</param>
        /// <returns></returns>
        IPublishConfiguration<T> WithTopic(string topic);

        /// <summary>
        /// An action to run when the published message is successfully delivered.
        /// </summary>
        /// <param name="successCallback"></param>
        /// <returns></returns>
        IPublishConfiguration<T> OnSuccess(Action successCallback);

        /// <summary>
        /// An action to run when the published message fails.
        /// </summary>
        /// <param name="failureCallback"></param>
        /// <returns></returns>
        IPublishConfiguration<T> OnFailure(Action failureCallback);
    }

    public class PublishConfiguration<T> : IPublishConfiguration<T>
    {
        public IList<string> Topics { get; private set; }
        public Action SuccessCallback { get; private set; }
        public Action FailureCallback { get; private set; }

        public PublishConfiguration()
        {
            Topics = new List<string>();
        }

        public IPublishConfiguration<T> WithTopic(string topic)
        {
            Topics.Add(topic);
            return this;
        }

        public IPublishConfiguration<T> OnSuccess(Action successCallback)
        {
            SuccessCallback = successCallback;
            return this;
        }

        public IPublishConfiguration<T> OnFailure(Action failureCallback)
        {
            FailureCallback = failureCallback;
            return this;
        }
    }
}