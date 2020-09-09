using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL
{
    public static class Extensions
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
            var numberList =new List<double>(numbers); //we need a list and this list will be mutated, so a copy is made.
            var index = (numberList.Count - 1) / 2;
            var loValue = nthOrderStatistic(numberList, index, 0, numberList.Count - 1);
            double median;
            if (numberList.Count % 2 != 0) median = loValue;
            else
            {
                var hiValue = nthOrderStatistic(numberList, index + 1, 0, numberList.Count - 1);
                median = (loValue + hiValue) / 2;
            }
            return median;
        }

        /// <summary>
        /// Returns Nth smallest element from the list. Here n starts from 0 so that n=0 returns minimum, n=1 returns 2nd smallest element etc.
        /// Note: specified list would be mutated in the process.
        /// Reference: Introduction to Algorithms 3rd Edition, Corman et al, pp 216
        /// </summary>
        /// <param name="numbers">The numbers.</param>
        /// <param name="nthPosition">The nTH position to find in the list of numbers.</param>
        /// <returns>System.Double.</returns>
        public static double NthOrderStatistic(this IEnumerable<double> numbers, int nthPosition)
        {
            var numberList = new List<double>(numbers); //we need a list and this list will be mutated, so a copy is made.
            return nthOrderStatistic(numberList, nthPosition, 0, numberList.Count - 1);
        }
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
        /// Partitions the given list around a pivot element such that all elements on left of pivot are <= pivot
        /// and the ones at thr right are > pivot. This method can be used for sorting, N-order statistics such as
        /// as median finding algorithms.
        /// Reference: Introduction to Algorithms 3rd Edition, Corman et al, pp 171
        /// </summary>
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
        /// <value>The variance from mean.</value>
        public static double VarianceFromMean(this IEnumerable<double> numbers, double mean = double.NaN)
        {
            if (double.IsNaN(mean)) mean = Mean(numbers);
            var varianceFromMean = 0.0;
            int count = 0;
            foreach (var x in numbers)
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
            if (double.IsNaN(median)) median = Median(numbers);
            var varianceFromMean = 0.0;
            int count = 0;
            foreach (var x in numbers)
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
        public static double NormalizedRootMeanSquareError(this IEnumerable<double> numbers, double targetValue=double.NaN)
        {
            var numberList = numbers as IList<double> ?? numbers.ToList();
            if (double.IsNaN(targetValue)) targetValue = Mean(numberList);
            var error = 0.0;
            foreach (var x in numberList)
            {
                var delta = x - targetValue;
                error += delta * delta;
            }
            error = Math.Sqrt(error);
            return error / numberList.Max();
        }
    }
}
