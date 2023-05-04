// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="IPriorityQueue.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System.Collections.Generic;

namespace Priority_Queue
{
    /// <summary>
    /// The IPriorityQueue interface.  This is mainly here for purists, and in case I decide to add more implementations later.
    /// For speed purposes, it is actually recommended that you *don't* access the priority queue through this interface, since the JIT can
    /// (theoretically?) optimize method calls from concrete-types slightly better.
    /// </summary>
    /// <typeparam name="TItem">The type of the t item.</typeparam>
    /// <typeparam name="TPriority">The type of the t priority.</typeparam>
    internal interface IPriorityQueue<TItem, in TPriority> : IEnumerable<TItem>
    {
        /// <summary>
        /// Enqueue a node to the priority queue.  Lower values are placed in front. Ties are broken by first-in-first-out.
        /// See implementation for how duplicates are handled.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="priority">The priority.</param>
        void Enqueue(TItem node, TPriority priority);

        /// <summary>
        /// Removes the head of the queue (node with minimum priority; ties are broken by order of insertion), and returns it.
        /// </summary>
        /// <returns>TItem.</returns>
        TItem Dequeue();

        /// <summary>
        /// Removes every node from the queue.
        /// </summary>
        void Clear();

        /// <summary>
        /// Returns whether the given node is in the queue.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns><c>true</c> if [contains] [the specified node]; otherwise, <c>false</c>.</returns>
        bool Contains(TItem node);

        /// <summary>
        /// Removes a node from the queue.  The node does not need to be the head of the queue.
        /// </summary>
        /// <param name="node">The node.</param>
        void Remove(TItem node);

        /// <summary>
        /// Call this method to change the priority of a node.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="priority">The priority.</param>
        void UpdatePriority(TItem node, TPriority priority);

        /// <summary>
        /// Returns the head of the queue, without removing it (use Dequeue() for that).
        /// </summary>
        /// <value>The first.</value>
        TItem First { get; }

        /// <summary>
        /// Returns the number of nodes in the queue.
        /// </summary>
        /// <value>The count.</value>
        int Count { get; }
    }
}
