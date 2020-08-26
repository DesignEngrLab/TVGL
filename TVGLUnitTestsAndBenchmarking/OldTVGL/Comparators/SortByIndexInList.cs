using System.Collections.Generic;

namespace OldTVGL
{
    internal class SortByIndexInList : IComparer<TessellationBaseClass>
    {
        public int Compare(TessellationBaseClass x, TessellationBaseClass y)
        {
            if (x.Equals(y)) return 0;
            if (x.IndexInList < y.IndexInList) return -1;
            else return 1;
        }
    }
}