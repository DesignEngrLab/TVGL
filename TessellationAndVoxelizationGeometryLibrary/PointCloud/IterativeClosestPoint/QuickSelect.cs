using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVGL.PointMatcherNet
{
    public class QuickSelect
    {
        /// <summary>
        /// Partitions the data between [left,right] so that the nth element is is in sorted order
        /// The sort is ordered by getValue(i)
        /// </summary>
        public static void Select(int[] indices, int left, int right, int n, Func<int, double> getValue)
        {
            if (left == right)
            {
                return;
            }

            while (true)
            {
                int pivotIndex = left;
                pivotIndex = partition(indices, left, right, pivotIndex, getValue);
                /*checkPartition(indices, left, right, pivotIndex, getValue);*/
                if (pivotIndex == n)
                {
                    return;
                }
                else if (n < pivotIndex)
                {
                    right = pivotIndex - 1;
                }
                else
                {
                    left = pivotIndex + 1;
                }
            }
        }
        /*
        private static void checkPartition(int[] indices, int left, int right, int n, Func<int, float> getValue)
        {
            for (int i = left; i < n; i++)
            {
                if (getValue(indices[i]) > getValue(indices[n]))
                    throw new InvalidOperationException();
            }

            for (int i = n+1; i < right; i++)
            {
                if (getValue(indices[i]) < getValue(indices[n]))
                    throw new InvalidOperationException();
            }
        }*/

        private static int partition(int[] indices, int left, int right, int n, Func<int, float> getValue)
        {
            float pivotValue = getValue(indices[n]);
            Swap(indices, n, right); // Move pivot to end
            int storeIndex = left;
            for (int i = left; i < right; i++)
            {
                if (getValue(indices[i]) < pivotValue)
                {
                    Swap(indices, storeIndex, i);
                    storeIndex++;
                }
            }

            Swap(indices, right, storeIndex); // Move pivot to its final place
            return storeIndex;
        }

        private static void Swap(int[] indices, int i, int j)
        {
            int t = indices[i];
            indices[i] = indices[j];
            indices[j] = t;
        }
    }
}
