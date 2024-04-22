namespace PointCloudNet;

/// <summary>
/// A comparer that does not return 0 if the two values are equal.
/// </summary>
internal class NoEqualSort<T> : IComparer<T> where T : IComparable
    {
        /// <summary>
        /// The direction
        /// </summary>
        private readonly int direction;

        /// <summary>
        /// Initializes a new instance of the <see cref="NoEqualSort" /> class.
        /// </summary>
        /// <param name="ascendingOrder">if set to <c>true</c> [ascending order].</param>
        internal NoEqualSort(bool ascendingOrder = true)
        {
            direction = ascendingOrder ? 1 : -1;
        }

        public int Compare(T? x, T? y)
        {
            if (x.CompareTo(y) < 0) return direction;
            return -direction;
        }
}