namespace EasyNetQ.Management.Client.Model
{
    /// <summary>
    /// The criteria for retrieving messages from a queue
    /// </summary>
    public class GetMessagesCriteria
    {
        public long Count { get; private set; }
        public bool Requeue { get; private set; }
        public string Encoding { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="count">
        /// controls the number of messages to get. You may get fewer messages than this 
        /// if the queue cannot immediately provide them.
        /// </param>
        /// <param name="requeue">
        /// determines whether the messages will be removed from the queue. If requeue 
        /// is true they will be requeued - but their position in the queue may change 
        /// and their redelivered flag will be set.
        /// </param>
        public GetMessagesCriteria(long count, bool requeue)
        {
            Requeue = requeue;
            Count = count;
            Encoding = "auto";
        }
    }
}