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
using System;
using System.Collections.Generic;
using System.Text;

namespace Priority_Queue
{
    /// <summary>
    /// A helper-interface only needed to make writing unit tests a bit easier (hence the 'internal' access modifier)
    /// </summary>
    internal interface IFixedSizePriorityQueue<TItem, in TPriority> : IPriorityQueue<TItem, TPriority>
    {
        /// <summary>
        /// Resize the queue so it can accept more nodes.  All currently enqueued nodes are remain.
        /// Attempting to decrease the queue size to a size too small to hold the existing nodes results in undefined behavior
        /// </summary>
        void Resize(int maxNodes);

        /// <summary>
        /// Returns the maximum number of items that can be enqueued at once in this queue.  Once you hit this number (ie. once Count == MaxSize),
        /// attempting to enqueue another item will cause undefined behavior.
        /// </summary>
        int MaxSize { get; }

        /// <summary>
        /// By default, nodes that have been previously added to one queue cannot be added to another queue.
        /// If you need to do this, please call originalQueue.ResetNode(node) before attempting to add it in the new queue
        /// </summary>
        void ResetNode(TItem node);
    }
}
