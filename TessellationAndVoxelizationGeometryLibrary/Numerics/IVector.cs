namespace TVGL
{
    /// <summary>
    /// An interface for a structure with nD position.
    /// </summary>
    public interface IVector
    {
        double this[int i] { get; }
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
        //double Z { get; init; }
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

        static DefaultPoint Null => new DefaultPoint { Coordinates = null };

    }
}