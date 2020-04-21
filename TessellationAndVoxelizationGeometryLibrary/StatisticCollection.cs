using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

namespace TVGL
{
    public class StatisticCollection : ICollection<double>
    {
        private readonly List<double> list = new List<double>();
        public int Count => list.Count;

        public bool IsReadOnly => false;


        #region Calc Median
        /// <summary>
        /// Partitions the given list around a pivot element such that all elements on left of pivot are <= pivot
        /// and the ones at thr right are > pivot. This method can be used for sorting, N-order statistics such as
        /// as median finding algorithms.
        /// Pivot is selected ranodmly if random number generator is supplied else its selected as last element in the list.
        /// Reference: Introduction to Algorithms 3rd Edition, Corman et al, pp 171
        /// </summary>
        private int Partition(int start, int end, Random rnd = null)
        {
            if (rnd != null)
                Swap(end, rnd.Next(start, end + 1));

            var pivot = list[end];
            var lastLow = start - 1;
            for (var i = start; i < end; i++)
            {
                if (list[i].CompareTo(pivot) <= 0)
                    Swap(i, ++lastLow);
            }
            Swap(end, ++lastLow);
            return lastLow;
        }

        /// <summary>
        /// Returns Nth smallest element from the list. Here n starts from 0 so that n=0 returns minimum, n=1 returns 2nd smallest element etc.
        /// Note: specified list would be mutated in the process.
        /// Reference: Introduction to Algorithms 3rd Edition, Corman et al, pp 216
        /// </summary>
        public double NthOrderStatistic(int n, Random rnd = null)
        {
            return NthOrderStatistic(n, 0, list.Count - 1, rnd);
        }
        private double NthOrderStatistic(int n, int start, int end, Random rnd)
        {
            while (true)
            {
                var pivotIndex = Partition(start, end, rnd);
                if (pivotIndex == n)
                    return list[pivotIndex];

                if (n < pivotIndex)
                    end = pivotIndex - 1;
                else
                    start = pivotIndex + 1;
            }
        }

        private void Swap(int i, int j)
        {
            if (i == j)   //This check is not required but Partition function may make many calls so its for perf reason
                return;
            var temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
        #endregion
        #region Properies
        /// <summary>
        /// Gets the median of the collection.
        /// </summary>
        /// <value>The median.</value>
        public double Median
        {
            get
            {
                if (double.IsNaN(median))
                {
                    var index = (list.Count - 1) / 2;
                    var loValue = NthOrderStatistic(index);
                    if (list.Count % 2 != 0) median = loValue;
                    else
                    {
                        var hiValue = NthOrderStatistic(index + 1);
                        median = (loValue + hiValue) / 2;
                    }
                }
                return median;
            }
        }
        double median = double.NaN;

        /// <summary>
        /// Gets the mean of the collection.
        /// </summary>
        /// <value>The mean.</value>
        public double Mean
        {
            get
            {
                if (double.IsNaN(mean))
                {
                    mean = list.Sum(x => x);
                    mean /= Count;
                }
                return mean;
            }
        }
        double mean = double.NaN;

        /// <summary>
        /// Gets the variance from the expected value, the mean.
        /// </summary>
        /// <value>The variance from mean.</value>
        public double VarianceFromMean
        {
            get
            {
                if (double.IsNaN(varianceFromMean))
                {
                     varianceFromMean = 0;
                    foreach (var x in list)
                    {
                        var d = x - Mean;
                        varianceFromMean += d * d;
                    }
                    varianceFromMean /= Count;
                }
                return varianceFromMean;
            }
        }
        double varianceFromMean = double.NaN;

        /// <summary>
        /// Gets the variance from the expected value, the median.
        /// </summary>
        /// <value>The variance from median.</value>
        public double VarianceFromMedian
        {
            get
            {
                if (double.IsNaN(varianceFromMedian))
                {
                     varianceFromMedian = 0;
                    var thisMedian = Median; //need to calc Median first since order of list is altered
                    foreach (var x in list)
                    {
                        var d = x - thisMedian;
                        varianceFromMedian += d * d;
                    }
                    varianceFromMean /= Count;
                }
                return varianceFromMedian;
            }
        }
        double varianceFromMedian = double.NaN;
        #endregion

        public void Add(double item)
        {
            list.Add(item);
            resetProperties();
        }

        public void Clear()
        {
            list.Clear();
        }

        public bool Contains(double item)
        {
            return list.Contains(item);
        }

        public void CopyTo(double[] array, int arrayIndex)
        {
            list.CopyTo(array, arrayIndex);
        }

        public bool Remove(double item)
        {
            if(list.Remove(item))
            {
                resetProperties();
                return true;
            }
            return false;
        }

        private void resetProperties()
        {
            mean = double.NaN;
            median = double.NaN;
            varianceFromMean = double.NaN;
            varianceFromMedian = double.NaN;
        }

        public IEnumerator<double> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return list.GetEnumerator();
        }
    }
}
