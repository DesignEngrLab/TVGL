using System.Collections.Generic;

namespace TVGL
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
}