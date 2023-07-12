// <copyright file="BinaryTreeNavigator.cs" company="Eric Regina">
// Copyright (c) Eric Regina. All rights reserved.
// </copyright>

namespace TVGL.KDTree
{
    using System;
    using System.Runtime.CompilerServices;
    using TVGL.ConvexHull;

    /// <summary>
    /// Allows one to navigate a binary tree stored in an <see cref="Array"/> using familiar
    /// tree navigation concepts.
    /// </summary>
    /// <typeparam name="TPoint">The type of the individual points.</typeparam>
    /// <typeparam name="TNode">The type of the individual nodes.</typeparam>
    internal readonly struct BinaryTreeNavigator<TPoint, TNode> where TPoint : IPoint
    {
        public static BinaryTreeNavigator<TPoint, TNode> Empty 
            = new BinaryTreeNavigator<TPoint, TNode>(Array.Empty<TPoint>(), Array.Empty<TNode>(), -1);

        /// <summary>
        /// A reference to the pointArray in which the binary tree is stored in.
        /// </summary>
        private readonly TPoint[] pointArray;

        private readonly TNode[] nodeArray;

        /// <summary>
        /// The index in the pointArray that the current node resides in.
        /// </summary>
        internal readonly int Index;

        /// <summary>
        /// The left child of the current node.
        /// </summary>
        internal BinaryTreeNavigator<TPoint, TNode> Left
            =>
                LeftChildIndex(Index) < pointArray.Length - 1
                    ? new BinaryTreeNavigator<TPoint, TNode>(pointArray, nodeArray, LeftChildIndex(Index))
                    : Empty;

        /// <summary>
        /// The right child of the current node.
        /// </summary>
        internal BinaryTreeNavigator<TPoint, TNode> Right
               =>
                   RightChildIndex(Index) < pointArray.Length - 1
                       ? new BinaryTreeNavigator<TPoint, TNode>(pointArray, nodeArray, RightChildIndex(Index))
                       : Empty;

        /// <summary>
        /// The parent of the current node.
        /// </summary>
        internal BinaryTreeNavigator<TPoint, TNode> Parent => Index == 0 
            ? Empty 
            : new BinaryTreeNavigator<TPoint, TNode>(pointArray, nodeArray, ParentIndex(Index));

        /// <summary>
        /// The current <typeparamref name="TPoint"/>.
        /// </summary>
        internal TPoint Point => pointArray[Index];

        /// <summary>
        /// The current <typeparamref name="TNode"/>
        /// </summary>
        internal TNode Node => nodeArray==null? default(TNode): nodeArray[Index];

        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryTreeNavigator{TPoint, TNode}"/> class.
        /// </summary>
        /// <param name="pointArray">The point array backing the binary tree.</param>
        /// <param name="nodeArray">The node array corresponding to the point array.</param>
        /// <param name="index">The index of the node of interest in the pointArray. If not given, the node navigator start at the 0 index (the root of the tree).</param>
        internal BinaryTreeNavigator(TPoint[] pointArray, TNode[] nodeArray, int index = 0)
        {
            Index = index;
            this.pointArray = pointArray;
            this.nodeArray = nodeArray;
        }

        /// <summary>
        /// Computes the index of the right child of the current node-index.
        /// </summary>
        /// <param name="index">The index of the current node.</param>
        /// <returns>The index of the right child.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int RightChildIndex(int index)
        {
            return (2 * index) + 2;
        }

        /// <summary>
        /// Computes the index of the left child of the current node-index.
        /// </summary>
        /// <param name="index">The index of the current node.</param>
        /// <returns>The index of the left child.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int LeftChildIndex(int index)
        {
            return (2 * index) + 1;
        }

        /// <summary>
        /// Computes the index of the parent of the current node-index.
        /// </summary>
        /// <param name="index">The index of the current node.</param>
        /// <returns>The index of the parent node.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int ParentIndex(int index)
        {
            return (index - 1) / 2;
        }
    }
}
