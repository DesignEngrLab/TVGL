// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="StatisticCollection.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TVGL
{
    /// <summary>
    /// Class StatisticsExtensions.
    /// </summary>
    public static class StatisticsExtensions
    {
        #region Calc Median
        /// <summary>
        /// Gets the median of the collection using a clever linear algorithm. The list is not actually sorted
        /// which would require an O(nlog(n)) operation.
        /// </summary>
        /// <param name="numbers">The numbers.</param>
        /// <returns>System.Double.</returns>
        /// <value>The median.</value>
        public static double Median(this IEnumerable<double> numbers)
        {
            if (!numbers.Any()) return double.NaN; // throw new ArgumentNullException(nameof(numbers));
            var numberList = new List<double>(numbers); //we need a list and this list will be mutated, so a copy is made.
            var index = (numberList.Count - 1) / 2;
            var loValue = nthOrderStatistic(numberList, index, 0, numberList.Count - 1);
            double median;
            if (int.IsOddInteger(numberList.Count)) median = loValue;
            else
            {
                var hiValue = nthOrderStatistic(numberList, index + 1, 0, numberList.Count - 1);
                median = (loValue + hiValue) / 2;
            }
            return median;
        }

        /// <summary>
        /// Returns Nth smallest element from the list. Here n starts from 0 so that n=0 returns minimum,
        /// n=1 returns 2nd smallest element etc.
        /// Reference: Introduction to Algorithms 3rd Edition, Corman et al, pp 216
        /// </summary>
        /// <param name="numbers">The numbers.</param>
        /// <param name="nthPosition">The nTH position to find in the list of numbers.</param>
        /// <returns>System.Double.</returns>
        public static double NthOrderStatistic(this IEnumerable<double> numbers, int nthPosition)
        {
            var numberList = new List<double>(numbers); //we need a list and this list will be mutated, so a copy is made.
            if (numberList.Count <= nthPosition) return double.PositiveInfinity;
            return nthOrderStatistic(numberList, nthPosition, 0, numberList.Count - 1);
        }
        /// <summary>
        /// NTHs the order statistic.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <param name="n">The n.</param>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        /// <returns>System.Double.</returns>
        private static double nthOrderStatistic(IList<double> list, int n, int start, int end)
        {
            while (true)
            {
                var pivotIndex = partition(list, start, end);
                if (pivotIndex == n)
                    return list[pivotIndex];

                if (n < pivotIndex)
                    end = pivotIndex - 1;
                else
                    start = pivotIndex + 1;
            }
        }

        /// <summary>
        /// Partitions the specified list.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        /// <returns>System.Int32.</returns>
        /// <font color="red">Badly formed XML comment.</font>
        private static int partition(IList<double> list, int start, int end)
        {
            var pivot = list[end];
            var lastLow = start - 1;
            for (var i = start; i < end; i++)
            {
                if (list[i].CompareTo(pivot) <= 0)
                    swap(list, i, ++lastLow);
            }
            swap(list, end, ++lastLow);
            return lastLow;
        }
        /// <summary>
        /// Swaps the specified list.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <param name="i">The i.</param>
        /// <param name="j">The j.</param>
        private static void swap(IList<double> list, int i, int j)
        {
            if (i == j)   //This check is not required but Partition function may make many calls so its for perf reason
                return;
            var temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
        #endregion


        /// <summary>
        /// Gets the mean of the collection.
        /// </summary>
        /// <param name="numbers">The numbers.</param>
        /// <returns>System.Double.</returns>
        /// <value>The mean.</value>
        public static double Mean(this IEnumerable<double> numbers)
        {
            int count = 0;
            double total = 0.0;
            foreach (var number in numbers)
            {
                total += number;
                count++;
            }
            return total / count;
        }
        /// <summary>
        /// Gets the variance from the expected value, the mean.
        /// </summary>
        /// <param name="numbers">The numbers.</param>
        /// <param name="mean">The mean.</param>
        /// <returns>System.Double.</returns>
        /// <value>The variance from mean.</value>
        public static double VarianceFromMean(this IEnumerable<double> numbers, double mean = double.NaN)
        {
            var numbersList = numbers as IList<double> ?? numbers.ToList();
            if (double.IsNaN(mean)) mean = Mean(numbersList);
            var varianceFromMean = 0.0;
            int count = 0;
            foreach (var x in numbersList)
            {
                var d = x - mean;
                varianceFromMean += d * d;
                count++;
            }
            varianceFromMean /= count;
            return varianceFromMean;
        }

        /// <summary>
        /// Gets the variance from the expected value, the median.
        /// </summary>
        /// <param name="numbers">The numbers.</param>
        /// <param name="median">The median.</param>
        /// <returns>System.Double.</returns>
        /// <value>The variance from median.</value>
        public static double VarianceFromMedian(this IEnumerable<double> numbers, double median = double.NaN)
        {
            var numbersList = numbers as IList<double> ?? numbers.ToList();
            if (double.IsNaN(median)) median = Median(numbersList);
            var varianceFromMean = 0.0;
            int count = 0;
            foreach (var x in numbersList)
            {
                var d = x - median;
                varianceFromMean += d * d;
                count++;
            }
            varianceFromMean /= count;
            return varianceFromMean;
        }


        /// <summary>
        /// Gets the normalized root mean square error.
        /// </summary>
        /// <param name="numbers">The numbers.</param>
        /// <param name="targetValue">The target value from which to take the difference for the error.
        /// If not provided, then mean of the numbers is used for the targetValue.</param>
        /// <returns>System.Double.</returns>
        public static double NormalizedRootMeanSquareError(this IEnumerable<double> numbers, double targetValue = double.NaN)
        {
            var numberList = numbers as IList<double> ?? numbers.ToList();
            if (double.IsNaN(targetValue)) targetValue = Mean(numberList);
            var error = 0.0;
            var xMin = double.PositiveInfinity;
            var xMax = double.NegativeInfinity;
            foreach (var x in numberList)
            {
                if (x < xMin) xMin = x;
                if (x > xMax) xMax = x;
                var delta = x - targetValue;
                error += delta * delta;
            }
            error = Math.Sqrt(error / numberList.Count);
            return error / (xMax - xMin);
        }

        /// <summary>
        /// Gets the  root mean square error.
        /// </summary>
        /// <param name="numbers">The numbers.</param>
        /// <param name="targetValue">The target value from which to take the difference for the error.
        /// If not provided, then mean of the numbers is used for the targetValue.</param>
        /// <returns>System.Double.</returns>
        public static double RootMeanSquareError(this IEnumerable<double> numbers, double targetValue = double.NaN)
        {
            var numberList = numbers as IList<double> ?? numbers.ToList();
            if (double.IsNaN(targetValue)) targetValue = Mean(numberList);
            var error = 0.0;
            foreach (var x in numberList)
            {
                var delta = x - targetValue;
                error += delta * delta;
            }
            return Math.Sqrt(error / numberList.Count);
        }


        /// <summary>
        /// Calculates the Area of a standard normal distribution from negative infinity to z.
        /// </summary>
        /// <param name="z">The z.</param>
        /// <returns>A double.</returns>
        public static double AreaOfStandardNormalDistributionFromNegInfToZ(double z)
        {
            // input = z-value (-inf to +inf)
            // output = p under Standard Normal curve from -inf to z
            // e.g., if z = 0.0, function returns 0.5000
            // ACM Algorithm #209
            //also based on article: https://learn.microsoft.com/en-us/archive/msdn-magazine/2015/november/test-run-the-t-test-using-csharp
            double y; // 209 scratch variable
            double p; // result. called 'z' in 209
            double w; // 209 scratch variable
            if (z == 0.0)
                p = 0.0;
            else
            {
                y = Math.Abs(z) / 2;
                if (y >= 3.0)
                {
                    p = 1.0;
                }
                else if (y < 1.0)
                {
                    w = y * y;
                    p = ((((((((0.000124818987 * w
                    - 0.001075204047) * w + 0.005198775019) * w
                    - 0.019198292004) * w + 0.059054035642) * w
                    - 0.151968751364) * w + 0.319152932694) * w
                    - 0.531923007300) * w + 0.797884560593) * y * 2.0;
                }
                else
                {
                    y = y - 2.0;
                    p = (((((((((((((-0.000045255659 * y
                    + 0.000152529290) * y - 0.000019538132) * y
                    - 0.000676904986) * y + 0.001390604284) * y
                    - 0.000794620820) * y - 0.002034254874) * y
                    + 0.006549791214) * y - 0.010557625006) * y
                    + 0.011630447319) * y - 0.009279453341) * y
                    + 0.005353579108) * y - 0.002141268741) * y
                    + 0.000535310849) * y + 0.999936657524;
                }
            }
            if (z > 0.0)
                return (p + 1.0) / 2;
            else
                return (1.0 - p) / 2;
        }

        /// <summary>
        /// Calculates the area the under a t-distribution from negative infinity to t.
        /// </summary>
        /// <param name="t">The t.</param>
        /// <param name="df">The df, or degrees-of-freedom.</param>
        /// <returns>A double.</returns>
        public static double AreaUnderTDistribution(double t, double df)
        {
            // for large integer df or double df
            // adapted from ACM algorithm 395
            // returns 2-tail p-value
            double n = df; // to sync with ACM parameter name
            double a, b, y;
            t = t * t;
            y = t / n;
            b = y + 1.0;
            if (y > 1.0E-6) y = Math.Log(b);
            a = n - 0.5;
            b = 48.0 * a * a;
            y = a * y;
            y = (((((-0.4 * y - 3.3) * y - 24.0) * y - 85.5) /
            (0.8 * y * y + 100.0 + b) + y + 3.0) / b + 1.0) *
            Math.Sqrt(y);
            return 2.0 * AreaOfStandardNormalDistributionFromNegInfToZ(-y); // ACM algorithm 209
        }

        public static double CalcPTest(double meanX, double meanY, double varX, double varY, int n1, int n2)
        {
            double top = (meanX - meanY);
            double bot = Math.Sqrt((varX / n1) + (varY / n2));
            double t = top / bot;
            //In words, the t statistic is the difference between the two sample means, divided by the
            //square root of the sum of the variances divided by their associated sample sizes.
            //Next, the degrees of freedom is calculated:
            double num = ((varX / n1) + (varY / n2)) * ((varX / n1) + (varY / n2));
            double denomLeft = ((varX / n1) * (varX / n1)) / (n1 - 1);
            double denomRight = ((varY / n2) * (varY / n2)) / (n2 - 1);
            double denom = denomLeft + denomRight;
            double df = num / denom;
            //The calculation of the degrees of freedom for the Welch t-test is somewhat tricky and
            //the equation isn’t at all obvious. Fortunately, you’ll never have to modify this calculation.
            //Method TTest concludes by computing the p-value and displaying all the calculated values:
            var p = AreaUnderTDistribution(t, df); // Cumulative two-tail density
            return p;
        }
    }
}
