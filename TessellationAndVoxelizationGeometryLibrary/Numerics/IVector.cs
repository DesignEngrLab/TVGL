using System.Collections;
using System.Collections.Generic;

namespace TVGL
{
    /// <summary>
    /// An interface for a structure with nD position.
    /// </summary>
    public interface IVector : System.Collections.Generic.IReadOnlyList<double>
    {
        bool IsNull();
        static IVector Null { get; }
    }

    public interface IVector2D : IVector
    {
        /// <summary>
        /// Gets the x.
        /// </summary>
        /// <value>The x.</value>
        double X { get; init; }

        /// <summary>
        /// Gets the y.
        /// </summary>
        /// <value>The y.</value>
        double Y { get; init; }
    }
    public interface IVector3D : IVector2D
    {
        /// <summary>
        /// Gets the z.
        /// </summary>
        /// <value>The z.</value>
        double Z { get; init; }
    }

    /// <summary>
    /// "Default" vertex.
    /// </summary>
    public class DefaultPoint : IVector
    {
        public double this[int i]
        {
            get { return Coordinates[i]; }
            set { Coordinates[i] = value; }
        }
        /// <summary>
        /// Coordinates of the vertex.
        /// </summary>
        /// <value>The position.</value>
        public double[] Coordinates { get; set; }

        public bool IsNull()
        {
            return Coordinates == null;
        }

        public IEnumerator<double> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
                yield return Coordinates[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        static DefaultPoint Null => new DefaultPoint { Coordinates = null };

        public int Count => Coordinates.Length;
    }
}