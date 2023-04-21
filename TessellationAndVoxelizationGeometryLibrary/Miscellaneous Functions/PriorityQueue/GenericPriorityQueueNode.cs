
// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="GenericPriorityQueueNode.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
namespace Priority_Queue
{
    /// <summary>
    /// Class GenericPriorityQueueNode.
    /// </summary>
    /// <typeparam name="TPriority">The type of the t priority.</typeparam>
    internal class GenericPriorityQueueNode<TPriority>
    {
        /// <summary>
        /// The Priority to insert this node at.
        /// Cannot be manually edited - see queue.Enqueue() and queue.UpdatePriority() instead
        /// </summary>
        /// <value>The priority.</value>
        internal TPriority Priority { get; set; }

        /// <summary>
        /// Represents the current position in the queue
        /// </summary>
        /// <value>The index of the queue.</value>
        internal int QueueIndex { get; set; }

        /// <summary>
        /// Represents the order the node was inserted in
        /// </summary>
        /// <value>The index of the insertion.</value>
        internal long InsertionIndex { get; set; }


#if DEBUG
        /// <summary>
        /// The queue this node is tied to. Used only for debug builds.
        /// </summary>
        /// <value>The queue.</value>
        internal object Queue { get; internal set; }
#endif
    }
}
