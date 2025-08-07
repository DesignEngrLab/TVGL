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
    public static class Global
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
        public static double Pseudoangle(double dx, double dy)
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


        /// <summary>
        /// Finds the index where the value should be inserted into the collection to maintain
        /// increasing order.
        /// </summary>
        /// <param name="array">the sorted array of doubles</param>
        /// <param name="queryValue">the value to insert</param>
        /// <param name="inclusiveLowIndex">the inclusive starting low index</param>
        /// <param name="inclusiveHighIndex">the inclusive starting low index (usually one less than the count)</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IncreasingDoublesBinarySearch(this IList<double> array, double queryValue,
            int inclusiveLowIndex, int inclusiveHighIndex)
        {
            // This binary search is modified/simplified from Array.BinarySearch
            // (https://referencesource.microsoft.com/mscorlib/a.html#b92d187c91d4c9a9)
            // here we are simply trying to order the doubles in increasing order
            while (inclusiveLowIndex <= inclusiveHighIndex)
            {
                // try the point in the middle of the range. note the >> 1 is a bit shift to quickly divide by 2
                int i = inclusiveLowIndex + ((inclusiveHighIndex - inclusiveLowIndex) >> 1);
                var valueAtIndex = array[i];
                if (queryValue == valueAtIndex) return i; //equal values could be in any order
                if (queryValue > valueAtIndex) inclusiveLowIndex = i + 1;
                else inclusiveHighIndex = i - 1;
            }
            return inclusiveLowIndex;
        }

        /// <summary>
        /// Finds the index where the value should be inserted into the collection to maintain
        /// increasing order.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int IncreasingDoublesBinarySearch(this IList<double> array, double value)
        => IncreasingDoublesBinarySearch(array, value, 0, array.Count - 1);

        public static ILogger Logger
        {
            set { logger = value; }
            get
            {
                if (logger == null) SetLogger(LogLevel.Trace);
                return logger;
            }
        }
        private static ILogger logger;

        public static void SetLogger(LogLevel minimumLevelToReport)
        {
            using ILoggerFactory factory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddConsole()
                    .SetMinimumLevel(minimumLevelToReport);
            });
            logger = factory.CreateLogger("TVGL");
        }

        public static IPresenter3D Presenter3D
        {
            set { presenter3D = value; }
            get
            {
                if (presenter3D == null)
                    presenter3D = new EmptyPresenter3D();
                return presenter3D;
            }
        }
        private static IPresenter3D presenter3D;
        public static IPresenter2D Presenter2D
        {
            set { presenter2D = value; }
            get
            {
                if (presenter2D == null)
                    presenter2D = new EmptyPresenter2D();
                return presenter2D;
            }
        }
        private static IPresenter2D presenter2D;

    }
}