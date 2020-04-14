using System.Collections.Generic;

namespace OldTVGL
{
    internal class SortByLength : IComparer<Edge>
    {
        private readonly int sortDirection = 1;
        internal SortByLength(bool ascending)
        {
            if (ascending) sortDirection = -1;
        }
        public int Compare(Edge x, Edge y)
        {
            if (x.Equals(y)) return 0;
            if (x.Length < y.Length) return sortDirection;
            return -1 * sortDirection;
        }
    }
    internal class SortByLengthNoEqual : IComparer<Edge>
    {
        private readonly int sortDirection = 1;
        internal SortByLengthNoEqual(bool ascending)
        {
            if (ascending) sortDirection = -1;
        }
        public int Compare(Edge x, Edge y)
        {
            if (x.Equals(y)) return -1;
                //return x.IndexInList < y.IndexInList ? -1 : 1;
            if (x.Length < y.Length) return sortDirection;
            return -1 * sortDirection;
        }
    }
}