using System.Runtime.Intrinsics;
using System.Numerics;
namespace PolygonSharp
{
    public class Vertex2D

    {
        #region Properties

        /// <summary>
        ///     Gets the loop ID that this node belongs to.
        /// </summary>
        /// <value>The loop identifier.</value>
        public int LoopID { get; set; }

        /// <summary>
        ///     Gets or sets the x.
        /// </summary>
        /// <value>The x.</value>
        public float X => Constants.longToRealScale * Coordinates.GetUpper().ToScalar();

        /// <summary>
        ///     Gets or sets the y.
        /// </summary>
        /// <value>The y.</value>
        public float Y => Constants.longToRealScale * Coordinates.GetLower().ToScalar();

        /// <summary>
        ///     Gets the line that starts at this node.
        /// </summary>
        /// <value>The start line.</value>
        public PolygonEdge StartLine { get; internal set; }

        /// <summary>
        ///     Gets the line that ends at this node.
        /// </summary>
        /// <value>The end line.</value>
        public PolygonEdge EndLine { get; internal set; }

        /// <summary>
        ///     Gets the base class, Point of this node.
        /// </summary>
        /// <value>The point.</value>
        public Vector128<long> Coordinates { get; set; }

        public int IndexInList { get; internal set; }

        #endregion Properties


        #region Constructor

        /// <summary>
        ///     Initializes a new instance of the <see cref="Node" /> class.
        /// </summary>
        /// <param name="currentPoint">The current point.</param>
        /// <param name="loopID">The loop identifier.</param>
        public Vertex2D(Vector2 currentPoint, int referenceID, int loopID)
        {
            LoopID = loopID;
            Coordinates = Vector128.Create((long)(Constants.realToLongScale * currentPoint.X),
                (long)(Constants.realToLongScale * currentPoint.Y));
            IndexInList = referenceID;
        }

        public Vertex2D Copy()
        {
            return new Vertex2D
            {
                Coordinates = this.Coordinates,
                IndexInList = this.IndexInList,
                LoopID = this.LoopID,
            };
        }

        // the following private argument-less constructor is only used in the copy function
        public Vertex2D()
        {
        }
        public Vertex2D(double x, double y)
        {
            Coordinates = Vector128.Create((long)(realToLongScale * x), (long)(realToLongScale * y));
        }


        public override string ToString()
        {
            return "{" + X + "," + Y + "}";
        }

        internal void Transform(Matrix3x2 matrix)
        {
            Coordinates = Coordinates.Transform(matrix);
        }
        #endregion Constructor
    }
}