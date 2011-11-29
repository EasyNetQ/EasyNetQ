namespace EasyNetQ
{
    /// <summary>
    /// A Saga should implement this interface.
    /// </summary>
    public interface ISaga
    {
        /// <summary>
        /// Initialised is called by SagaHost after the Saga is detected.
        /// Use this method to subscribe to messages and create the
        /// Saga process.
        /// </summary>
        /// <param name="bus"></param>
        void Initialize(IBus bus);
    }
}