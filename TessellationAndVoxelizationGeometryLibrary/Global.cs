using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace TVGL
{
    /// <summary>
    /// Class Constants.
    /// </summary>
    internal static class Global
    {
        /// <summary>
        /// Finds the index.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items">The items.</param>
        /// <param name="predicate">The predicate.</param>
        /// <returns>System.Int32.</returns>
        internal static int FindIndex<T>(this IEnumerable<T> items, Predicate<T> predicate)
        {
            var itemsList = items as IList<T> ?? items.ToList();
            var numItems = itemsList.Count;
            if (numItems == 0) return -1;
            var index = 0;
            foreach (var item in itemsList)
            {
                if (predicate(item)) return index;
                index++;
            }
            return -1;
        }

        /// <summary>
        /// The Pseudoangle function is used to sort points in a counter-clockwise order.
        /// It is intended to be much faster than the atan2 function. 
        /// https://stackoverflow.com/questions/16542042/fastest-way-to-sort-vectors-by-angle-without-actually-computing-that-angle
        /// It is not as accurate as atan2, but it is monotonic and preserves the ordering starting
        /// with 0 at the positive x-axis and increasing counter-clockwise to 2 (at 180 degrees)
        /// and then increasing to 4 (at 360 degrees). It appears to be more than 10X faster than atan2.
        /// | Method      | Mean      | Error     | StdDev    |
        /// |------------ |----------:|----------:|----------:|
        /// | ATan2       | 3.3143 ns | 0.0660 ns | 0.1503 ns |
        /// | PseudoAngle | 0.2663 ns | 0.0120 ns | 0.0100 ns | (using BenchmarkDotNet)
        /// </summary>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static double Pseudoangle(double dx, double dy)
        {
            var p = dx / (Math.Abs(dx) + Math.Abs(dy)); // -1 .. 1 increasing with x
            if (dy < 0) return 3 + p;  //  2 .. 4 increasing with x
            return 1 - p;  //  0 .. 2 decreasing with x
        }

        internal static void SwapItemsInList<T>(int i, int j, IList<T> points)
        {
            var temp = points[i];
            points[i] = points[j];
            points[j] = temp;
        }
        /// <summary>
        /// Switches a and b polygon relationship.
        /// </summary>
        /// <param name="relationship">The relationship.</param>
        /// <returns>PolyRelInternal.</returns>
        internal static PolyRelInternal SwitchAAndBPolygonRelationship(this PolyRelInternal relationship)
        {
            if ((relationship & PolyRelInternal.Intersection) == PolyRelInternal.AInsideB)
            {
                relationship |= PolyRelInternal.BInsideA;
                relationship &= ~PolyRelInternal.AInsideB;
            }
            else if ((relationship & PolyRelInternal.Intersection) == PolyRelInternal.BInsideA)
            {
                relationship |= PolyRelInternal.AInsideB;
                relationship &= ~PolyRelInternal.BInsideA;
            }
            return relationship;
        }

        /// <summary>
        /// Enumerates the thruple.
        /// </summary>
        /// <param name="thruple">The thruple.</param>
        /// <returns>A list of TS.</returns>
        internal static IEnumerable<T> EnumerateThruple<T>(this (T, T, T) thruple)
        {
            yield return thruple.Item1;
            yield return thruple.Item2;
            yield return thruple.Item3;
        }
    }
}