namespace ConvexHull.NET
{
	/// <summary>
	/// A comparer for optimization that can be used for either
	/// ascending or descending.
	/// </summary>
	public class NoEqualSort : IComparer<double>
	//public class NoEqualSort<T> : IComparer<T> where T :IComparable
	{
		/// <summary>
		/// The direction
		/// </summary>
		private readonly int direction;

		/// <summary>
		/// Initializes a new instance of the <see cref="NoEqualSort" /> class.
		/// </summary>
		/// <param name="ascendingOrder">if set to <c>true</c> [ascending order].</param>
		public NoEqualSort(bool ascendingOrder = true)
		{
			direction = ascendingOrder ? -1 : 1;
		}

		/// <summary>
		/// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
		/// </summary>
		/// <param name="x">The first object to compare.</param>
		/// <param name="y">The second object to compare.</param>
		/// <returns>A signed integer that indicates the relative values of <paramref name="x" /> and <paramref name="y" />, as
		/// shown in the following table.Value Meaning Less than zero<paramref name="x" /> is less than <paramref name="y" />
		/// .Zero<paramref name="x" /> equals <paramref name="y" />.Greater than zero<paramref name="x" /> is greater than
		/// <paramref name="y" />.</returns>
		public int Compare(double x, double y)
		{
			if (x < y) return direction;
			return -direction;
		}
		//public int Compare(T x, T y)
		//{
		//    if (x.CompareTo(y) < 0) return direction;
		//    return -direction;
		//}
	}
}