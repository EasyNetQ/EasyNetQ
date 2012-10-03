namespace EasyNetQ
{
    /// <summary>
    /// Provides a strategy for selecting a host from a list of nodes in a cluster
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IClusterHostSelectionStrategy<T>
    {
        /// <summary>
        /// Add a cluster node
        /// </summary>
        /// <param name="item"></param>
        void Add(T item);

        /// <summary>
        /// Get the currently selected node
        /// </summary>
        /// <returns></returns>
        T Current();

        /// <summary>
        /// Move to the next node
        /// </summary>
        /// <returns></returns>
        bool Next();

        /// <summary>
        /// Mark the current node as successfully connected
        /// </summary>
        void Success();

        /// <summary>
        /// Did the current node successfully connect?
        /// </summary>
        bool Succeeded { get; }

        /// <summary>
        /// The current node has disconnected and we want to run the strategy again
        /// </summary>
        void Reset();
    }
}