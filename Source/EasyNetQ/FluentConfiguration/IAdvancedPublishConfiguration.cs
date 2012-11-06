using System;

namespace EasyNetQ.FluentConfiguration
{
    public interface IAdvancedPublishConfiguration
    {
        /// <summary>
        /// An action to run when the published message is successfully delivered.
        /// </summary>
        /// <param name="successCallback"></param>
        /// <returns></returns>
        IAdvancedPublishConfiguration OnSuccess(Action successCallback);

        /// <summary>
        /// An action to run when the published message fails.
        /// </summary>
        /// <param name="failureCallback"></param>
        /// <returns></returns>
        IAdvancedPublishConfiguration OnFailure(Action failureCallback);
    }

    public class AdvancedPublishConfiguration : IAdvancedPublishConfiguration
    {
        public Action SuccessCallback { get; private set; }
        public Action FailureCallback { get; private set; }

        public IAdvancedPublishConfiguration OnSuccess(Action successCallback)
        {
            this.SuccessCallback = successCallback;
            return this;
        }

        public IAdvancedPublishConfiguration OnFailure(Action failureCallback)
        {
            this.FailureCallback = failureCallback;
            return this;
        }
    }
}