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


}