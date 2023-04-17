// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="IFixedSizePriorityQueue.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************

namespace Priority_Queue
{
    /// <summary>
    /// A helper-interface only needed to make writing unit tests a bit easier (hence the 'internal' access modifier)
    /// </summary>
    /// <typeparam name="TItem">The type of the t item.</typeparam>
    /// <typeparam name="TPriority">The type of the t priority.</typeparam>
    internal interface IFixedSizePriorityQueue<TItem, in TPriority> : IPriorityQueue<TItem, TPriority>
    {
        /// <summary>
        /// Resize the queue so it can accept more nodes.  All currently enqueued nodes are remain.
        /// Attempting to decrease the queue size to a size too small to hold the existing nodes results in undefined behavior
        /// </summary>
        /// <param name="maxNodes">The maximum nodes.</param>
        void Resize(int maxNodes);

        /// <summary>
        /// Returns the maximum number of items that can be enqueued at once in this queue.  Once you hit this number (ie. once Count == MaxSize),
        /// attempting to enqueue another item will cause undefined behavior.
        /// </summary>
        /// <value>The maximum size.</value>
        int MaxSize { get; }

        /// <summary>
        /// By default, nodes that have been previously added to one queue cannot be added to another queue.
        /// If you need to do this, please call originalQueue.ResetNode(node) before attempting to add it in the new queue
        /// </summary>
        /// <param name="node">The node.</param>
        void ResetNode(TItem node);
    }
}
