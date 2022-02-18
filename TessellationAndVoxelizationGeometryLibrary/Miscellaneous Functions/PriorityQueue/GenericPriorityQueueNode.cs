
// The files in this folder are an abbreviated version of BlueRaja's Optimized Priority Queue.
// https://github.com/BlueRaja/High-Speed-Priority-Queue-for-C-Sharp
// It was found to not only outperform the PriorityQueue introduced in
// .NET 6, but it also has capabilities to Update the priority of an 
// existing state, and Remove a state from the queue.
// The FastPriorityQueue, the FastPriorityQueueNode, the StablePriorityQueue, the
// StablePriorityQueueNode have been removed. The Simple one is used only because
// the reliance on the 'Node classes cannot be produced without having to create
// a dictionary. This is actually what the SimplePriorityQueue does (although
// the name implies that it is simpler.
namespace Priority_Queue
{
    public class GenericPriorityQueueNode<TPriority>
    {
        /// <summary>
        /// The Priority to insert this node at.
        /// Cannot be manually edited - see queue.Enqueue() and queue.UpdatePriority() instead
        /// </summary>
        public TPriority Priority { get; protected internal set; }

        /// <summary>
        /// Represents the current position in the queue
        /// </summary>
        public int QueueIndex { get; internal set; }

        /// <summary>
        /// Represents the order the node was inserted in
        /// </summary>
        public long InsertionIndex { get; internal set; }


#if DEBUG
        /// <summary>
        /// The queue this node is tied to. Used only for debug builds.
        /// </summary>
        public object Queue { get; internal set; }
#endif
    }
}
