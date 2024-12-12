// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="Comparators.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;

namespace TVGL
{
    /// <summary>
    /// Class SortByIndexInList.
    /// Implements the <see cref="System.Collections.Generic.IComparer{TVGL.TessellationBaseClass}" />
    /// </summary>
    /// <seealso cref="System.Collections.Generic.IComparer{TVGL.TessellationBaseClass}" />
    internal class SortByIndexInList : IComparer<TessellationBaseClass>
    {
        /// <summary>
        /// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
        /// </summary>
        /// <param name="x">The first object to compare.</param>
        /// <param name="y">The second object to compare.</param>
        /// <returns>A signed integer that indicates the relative values of <paramref name="x" /> and <paramref name="y" />, as shown in the following table.
        /// <list type="table"><listheader><term> Value</term><description> Meaning</description></listheader><item><term> Less than zero</term><description><paramref name="x" /> is less than <paramref name="y" />.</description></item><item><term> Zero</term><description><paramref name="x" /> equals <paramref name="y" />.</description></item><item><term> Greater than zero</term><description><paramref name="x" /> is greater than <paramref name="y" />.</description></item></list></returns>
        public int Compare(TessellationBaseClass x, TessellationBaseClass y)
        {
            if (x.Equals(y)) return 0;
            if (x.IndexInList < y.IndexInList) return -1;
            else return 1;
        }
    }

    /// <summary>
    /// Class ReverseSort.
    /// Implements the <see cref="System.Collections.Generic.IComparer{System.Double}" />
    /// </summary>
    /// <seealso cref="System.Collections.Generic.IComparer{System.Double}" />
    internal class ReverseSort : IComparer<double>
    {
        /// <summary>
        /// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
        /// </summary>
        /// <param name="x">The first object to compare.</param>
        /// <param name="y">The second object to compare.</param>
        /// <returns>A signed integer that indicates the relative values of <paramref name="x" /> and <paramref name="y" />, as shown in the following table.
        /// <list type="table"><listheader><term> Value</term><description> Meaning</description></listheader><item><term> Less than zero</term><description><paramref name="x" /> is less than <paramref name="y" />.</description></item><item><term> Zero</term><description><paramref name="x" /> equals <paramref name="y" />.</description></item><item><term> Greater than zero</term><description><paramref name="x" /> is greater than <paramref name="y" />.</description></item></list></returns>
        public int Compare(double x, double y)
        {
            if (x == y) return 0;
            if (x < y) return 1;
            return -1;
        }
    }

    /// <summary>
    /// Class ForwardSort.
    /// Implements the <see cref="System.Collections.Generic.IComparer{System.Double}" />
    /// </summary>
    /// <seealso cref="System.Collections.Generic.IComparer{System.Double}" />
    internal class ForwardSort : IComparer<double>
    {
        /// <summary>
        /// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
        /// </summary>
        /// <param name="x">The first object to compare.</param>
        /// <param name="y">The second object to compare.</param>
        /// <returns>A signed integer that indicates the relative values of <paramref name="x" /> and <paramref name="y" />, as shown in the following table.
        /// <list type="table"><listheader><term> Value</term><description> Meaning</description></listheader><item><term> Less than zero</term><description><paramref name="x" /> is less than <paramref name="y" />.</description></item><item><term> Zero</term><description><paramref name="x" /> equals <paramref name="y" />.</description></item><item><term> Greater than zero</term><description><paramref name="x" /> is greater than <paramref name="y" />.</description></item></list></returns>
        public int Compare(double x, double y)
        {
            if (x == y) return 0;
            if (x < y) return -1;
            return 1;
        }
    }

    /// <summary>
    /// Class AbsoluteValueSort.
    /// Implements the <see cref="System.Collections.Generic.IComparer{System.Double}" />
    /// </summary>
    /// <seealso cref="System.Collections.Generic.IComparer{System.Double}" />
    internal class AbsoluteValueSort : IComparer<double>
    {
        /// <summary>
        /// Compares the specified x.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns>System.Int32.</returns>
        public int Compare(double x, double y)
        {
            if (Math.Abs(x) == Math.Abs(y)) return 0;
            if (Math.Abs(x) < Math.Abs(y)) return -1;
            return 1;
        }
    }

    /// <summary>
    /// A comparer for optimization that can be used for either
    /// ascending or descending.
    /// </summary>
    public class NoEqualSort : IComparer<double>
    //public class NoEqualSort<T> : IComparer<T> where T :IComparable
    {
        /// <summary>
        /// The direction
        /// </summary>
        private readonly int direction;

        /// <summary>
        /// Initializes a new instance of the <see cref="NoEqualSort" /> class.
        /// </summary>
        /// <param name="ascendingOrder">if set to <c>true</c> [ascending order].</param>
        public NoEqualSort(bool ascendingOrder = true)
        {
            direction = ascendingOrder ? -1 : 1;
        }

        /// <summary>
        /// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
        /// </summary>
        /// <param name="x">The first object to compare.</param>
        /// <param name="y">The second object to compare.</param>
        /// <returns>A signed integer that indicates the relative values of <paramref name="x" /> and <paramref name="y" />, as
        /// shown in the following table.Value Meaning Less than zero<paramref name="x" /> is less than <paramref name="y" />
        /// .Zero<paramref name="x" /> equals <paramref name="y" />.Greater than zero<paramref name="x" /> is greater than
        /// <paramref name="y" />.</returns>
        public int Compare(double x, double y)
        {
            if (x < y) return direction;
            return -direction;
        }
        //public int Compare(T x, T y)
        //{
        //    if (x.CompareTo(y) < 0) return direction;
        //    return -direction;
        //}
    }

    /// <summary>
    /// Class TwoDSortXFirst.
    /// Implements the <see cref="System.Collections.Generic.IComparer{IPoint2D}" />
    /// </summary>
    /// <seealso cref="System.Collections.Generic.IComparer{IPoint2D}" />
    internal class TwoDSortXFirst : IComparer<IVector2D>
    {

        /// <summary>
        /// Compares the specified v1.
        /// </summary>
        /// <param name="v1">The v1.</param>
        /// <param name="v2">The v2.</param>
        /// <returns>System.Int32.</returns>
        public int Compare(IVector2D v1, IVector2D v2)
        {
            if (v1.X.IsPracticallySame(v2.X))
                return (v1.Y < v2.Y) ? -1 : 1;
            return (v1.X < v2.X) ? -1 : 1;
        }
    }

    /// <summary>
    /// Class TwoDSortYFirst.
    /// Implements the <see cref="System.Collections.Generic.IComparer{IPoint2D}" />
    /// </summary>
    /// <seealso cref="System.Collections.Generic.IComparer{IPoint2D}" />
    internal class TwoDSortYFirst : IComparer<IVector2D>
    {

        /// <summary>
        /// Compares the specified v1.
        /// </summary>
        /// <param name="v1">The v1.</param>
        /// <param name="v2">The v2.</param>
        /// <returns>System.Int32.</returns>
        public int Compare(IVector2D v1, IVector2D v2)
        {
            if (v1.Y.IsPracticallySame(v2.Y))
                return (v1.X < v2.X) ? -1 : 1;
            return (v1.Y < v2.Y) ? -1 : 1;
        }
    }

    /// <summary>
    /// Class VertexSortedByDirection.
    /// Implements the <see cref="System.Collections.Generic.IComparer{TVGL.Vertex2D}" />
    /// </summary>
    /// <seealso cref="System.Collections.Generic.IComparer{TVGL.Vertex2D}" />
    internal class VertexSortedByDirection : IComparer<Vertex2D>
    {
        /// <summary>
        /// The sweep direction
        /// </summary>
        private readonly Vector2IP sweepDirection;
        /// <summary>
        /// The along direction
        /// </summary>
        private readonly Vector2IP alongDirection;

        /// <summary>
        /// Initializes a new instance of the <see cref="VertexSortedByDirection"/> class.
        /// </summary>
        /// <param name="sweepDirection">The sweep direction.</param>
        internal VertexSortedByDirection(Vector2IP sweepDirection)
        {
            this.sweepDirection = sweepDirection;
            alongDirection = new Vector2IP(-sweepDirection.Y, sweepDirection.X, sweepDirection.W);

        }
        /// <summary>
        /// Compares the specified v1.
        /// </summary>
        /// <param name="v1">The v1.</param>
        /// <param name="v2">The v2.</param>
        /// <returns>System.Int32.</returns>
        public int Compare(Vertex2D v1, Vertex2D v2)
        {
            var d1 = v1.Coordinates.Dot2D(sweepDirection);
            var d2 = v2.Coordinates.Dot2D(sweepDirection);
            var compare = d1.CompareTo(d2);
            if (compare == 0)
                return v1.Coordinates.Dot2D(alongDirection).CompareTo(v2.Coordinates.Dot2D(alongDirection));

            else return compare;
        }
    }

}