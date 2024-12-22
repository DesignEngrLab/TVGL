// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-14-2023
// ***********************************************************************
// <copyright file="BoundingBox.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL
{
    /// <summary>
    /// Class BoundingBox with Generic (T) is for times when you have points you want to save
    /// on the boundary of the bounding box. these points are of type T, which is constrained
    /// to be an IVector3D - currently instantiated by Vertex and Vector3
    /// Implements the <see cref="TVGL.BoundingBox" />
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="TVGL.BoundingBox" />
    public class BoundingBox<T> : BoundingBox where T : IVector3D
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BoundingBox{T}" /> class.
        /// </summary>
        /// <param name="dimensions">The dimensions.</param>
        /// <param name="vectors">The directions.</param>
        /// <param name="pointsOnFaces">The points on faces.</param>
        public BoundingBox(Vector3[] vectors, IList<IEnumerable<T>> pointsOnFaces)
    : this(vectors, pointsOnFaces[0].First(), pointsOnFaces[2].First(),
         pointsOnFaces[4].First())
        {
            PointsOnFaces = pointsOnFaces.Select(pof => pof.ToArray()).ToArray();
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="BoundingBox{T}"/> class.
        /// </summary>
        /// <param name="dimensions">The dimensions.</param>
        /// <param name="vectors">The directions.</param>
        /// <param name="minPointOnDirection0">The minimum point on direction0.</param>
        /// <param name="minPointOnDirection1">The minimum point on direction1.</param>
        /// <param name="minPointOnDirection2">The minimum point on direction2.</param>
        public BoundingBox(Vector3[] vectors, T minPointOnDirection0,
        T minPointOnDirection1, T minPointOnDirection2)
        : base(vectors,
             MiscFunctions.PointCommonToThreePlanes(vectors[0].Normalize(),
                 minPointOnDirection0.Dot(vectors[0].Normalize()), vectors[1].Normalize(),
                 minPointOnDirection1.Dot(vectors[1].Normalize()), vectors[2].Normalize(),
                 minPointOnDirection2.Dot(vectors[2].Normalize())))
        { }

        public BoundingBox(Vector3[] vectors, Vector3 translation)
        : base(vectors, translation) { }

        public BoundingBox(double minX, double minY, double minZ, double maxX, double maxY, double maxZ, List<T>[] pointsOnBox)
            : base(minX, minY, minZ, maxX, maxY, maxZ)
        {
            PointsOnFaces = pointsOnBox.Select(pof => pof.ToArray()).ToArray();
        }

        /// <summary>
        /// The PointsOnFaces is an array of 6 lists which are vertices of the tessellated solid
        /// that are on the faces of the bounding box. They are in the order of direction1-low,
        /// direction1-high, direction2-low, direction2-high, direction3-low, direction3-high.
        /// </summary>
        /// <value>The points on faces.</value>
        [JsonIgnore]
        public T[][] PointsOnFaces { get; private set; }


        /// <summary>
        /// Copies the bounding box to a new one.
        /// </summary>
        /// <returns>BoundingBox.</returns>
        public override BoundingBox Copy()
        {
            var copiedBB = new BoundingBox<T>(this.Vectors.ToArray(), this.TranslationFromOrigin);
            if (PointsOnFaces != null)
                copiedBB.PointsOnFaces = new T[][]
                {
                    this.PointsOnFaces[0].ToArray(),
                    this.PointsOnFaces[1].ToArray(),
                    this.PointsOnFaces[2].ToArray(),
                    this.PointsOnFaces[3].ToArray(),
                    this.PointsOnFaces[4].ToArray(),
                    this.PointsOnFaces[5].ToArray()
                };
            return copiedBB;
        }
    }

    /// <summary>
    /// The BoundingBox is a simple class for representing an arbitrarily oriented box
    /// or 3D prismatic rectangle. It is defined by 1) the Matrix4x4 transform which includes the
    /// rotation and translation from the global frame, 2) the dimension of the box at this coordinate
    /// frame (the box is only in the positive octant of this local frame), 3) (optional) any points
    /// of the enclosing solid that touch the box.
    /// </summary>
    public class BoundingBox
    {
        #region Constructors
        public BoundingBox(Vector3[] vectors, Vector3 translation)
        {
            Vectors = vectors;
            TranslationFromOrigin = translation;
            IsAxisAligned = vectors[0].IsAligned(Vector3.UnitX, dotAligned) &&
                    vectors[1].IsAligned(Vector3.UnitY, dotAligned) &&
                    vectors[2].IsAligned(Vector3.UnitZ, dotAligned);
        }
        private const double dotAligned = 0.999; // about 2.5 degrees


        /// <summary>
        /// Create a bounding box from a minimum and maximum point, which are the two extreme corners of the box.
        /// The box is aligned with the global frame.
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        public BoundingBox(Vector3 min, Vector3 max) : this(min.X, min.Y, min.Z, max.X, max.Y, max.Z) { }

        /// <summary>
        /// Create a bounding box from a minimum and maximum point, which are the two extreme corners of the box.
        /// The box is aligned with the global frame.
        /// </summary>
        /// <param name="minX"></param>
        /// <param name="minY"></param>
        /// <param name="minZ"></param>
        /// <param name="maxX"></param>
        /// <param name="maxY"></param>
        /// <param name="maxZ"></param>
        public BoundingBox(double minX, double minY, double minZ, double maxX, double maxY, double maxZ)
        {
            Vectors = [(maxX - minX) * Vector3.UnitX, (maxY - minY) * Vector3.UnitY, (maxZ - minZ) * Vector3.UnitZ];
            TranslationFromOrigin = new Vector3(minX, minY, minZ);
            IsAxisAligned = true;
        }
        #endregion Constructors

        #region Constructor Defined Fields
        public Vector3[] Vectors { get; }
        public Vector3 TranslationFromOrigin { get; }
        public bool IsAxisAligned { get; }
        #endregion


        #region Properties
        /// <summary>
        /// Gets the bounds. The 0-th Vector3 is the minX, minY, and minZ, the 1st is the maxX, maxY, and maxZ
        /// </summary>
        /// <value>The bounds.</value>
        public Vector3[] Bounds
        {
            get
            {
                return _bounds ??= GetBoxBounds(Vectors, TranslationFromOrigin);
            }
        }
        private Vector3[] _bounds;

        public static Vector3[] GetBoxBounds(Vector3[] vectors, Vector3 origin)
        {
            var minX = origin.X;
            var maxX = origin.X;
            var minY = origin.Y;
            var maxY = origin.Y;
            var minZ = origin.Z;
            var maxZ = origin.Z;
            for (int i = 0; i < 3; i++)
            {
                if (vectors[i].X < 0) minX += vectors[i].X;
                else maxX += vectors[i].X;
                if (vectors[i].Y < 0) minY += vectors[i].Y;
                else maxY += vectors[i].Y;
                if (vectors[i].Z < 0) minZ += vectors[i].Z;
                else maxZ += vectors[i].Z;
            }
            return [new Vector3(minX, minY, minZ), new Vector3(maxX, maxY, maxZ)];
        }



        /// <summary>
        /// Gets the transformation from the global frame to this box. This is only a rotates and translates.
        /// "From Origin" means from an axis algined box in the first octant with a corner at the origin
        /// </summary>
        /// <value>The transform.</value>
        public Matrix4x4 TransformFromOrigin
        {
            get
            {
                if (_transformFromOrigin.IsNull())
                    _transformFromOrigin = new Matrix4x4(Directions[0], Directions[1], Directions[2], TranslationFromOrigin);
                return _transformFromOrigin;
            }
        }
        private Matrix4x4 _transformFromOrigin = Matrix4x4.Null;

        /// <summary>
        /// Gets the transformation from this box to the global frame. This is only a rotate and translate.
        /// </summary>
        /// <value>The transform.</value>
        public Matrix4x4 TransformToOrigin
        {
            get
            {
                if (_transformToOrign.IsNull())
                    _transformToOrign = new Matrix4x4(Directions[0].X, Directions[1].X, Directions[2].X,
                        Directions[0].Y, Directions[1].Y, Directions[2].Y,
                        Directions[0].Z, Directions[1].Z, Directions[2].Z,
                        -TranslationFromOrigin.Dot(Directions[0]),
                        -TranslationFromOrigin.Dot(Directions[1]),
                        -TranslationFromOrigin.Dot(Directions[2]));
                return _transformToOrign;
            }
        }
        private Matrix4x4 _transformToOrign = Matrix4x4.Null;

        /// <summary>
        /// Gets the transformation from the global frame to this box. This is only a rotate and translate. Along
        /// with the Dimensions, it fully defines the box
        /// </summary>
        /// <value>The transform.</value>
        public Matrix4x4 TransformFromUnitBox
        {
            get
            {
                if (_transformFromUnitBox.IsNull())
                    _transformFromUnitBox = new Matrix4x4(Vectors[0], Vectors[1], Vectors[2], TranslationFromOrigin);
                return _transformFromUnitBox;
            }
        }
        private Matrix4x4 _transformFromUnitBox = Matrix4x4.Null;


        /// <summary>
        /// Gets the transformation from the global frame to this box. This is only a rotate and translate. Along
        /// with the Dimensions, it fully defines the box
        /// </summary>
        /// <value>The transform.</value>
        public Matrix4x4 TransformToUnitBox
        {
            get
            {
                if (_transformToUnitBox.IsNull())
                {
                    var sX = 1 / Dimensions.X;
                    var sY = 1 / Dimensions.Y;
                    var sZ = 1 / Dimensions.Z;
                    _transformToUnitBox = new Matrix4x4(sX * Directions[0].X, sY * Directions[1].X, sZ * Directions[2].X,
                        sX * Directions[0].Y, sY * Directions[1].Y, sZ * Directions[2].Y,
                        sX * Directions[0].Z, sY * Directions[1].Z, sZ * Directions[2].Z,
                        -sX * TranslationFromOrigin.Dot(Directions[0]),
                        -sY * TranslationFromOrigin.Dot(Directions[1]),
                        -sZ * TranslationFromOrigin.Dot(Directions[2]));
                }
                return _transformToUnitBox;
            }
        }
        private Matrix4x4 _transformToUnitBox = Matrix4x4.Null;


        //the following properties are set with a lazy private fields
        /// <summary>
        /// The Directions normal are the three unit vectors that describe the orientation of the box.
        /// These directions also correspond to the first three rows of the transform matrix (not including
        /// the perspective terms of course).
        /// correspond with that direction.
        /// </summary>
        /// <value>The directions.</value>
        [JsonIgnore]
        public Vector3[] Directions
        {
            get
            {
                return _directions ??= [Vectors[0] / Dimensions[0], Vectors[1] / Dimensions[1], Vectors[2] / Dimensions[2]];
            }
        }
        private Vector3[] _directions;



        /// <summary>
        /// Corner vertices are ordered as follows, where - = low and + = high along directions 0, 1, and 2 respectively.
        /// [0] = ---, [1] = +-- , [2] = ++- , [3] = -+-, [4] = --+ , [5] = +-+, [6] = +++, [7] = -++
        /// </summary>
        /// <value>The corners.</value>
        [JsonIgnore]
        public Vector3[] Corners
        {
            get
            {
                return _corners ??= MakeCornerPoints(Vectors, TranslationFromOrigin);
            }
        }

        /// <summary>
        /// The corners
        /// </summary>
        private Vector3[] _corners;

        /// <summary>
        /// Adds the corner vertices to the bounding box.
        /// The dimensions are also updated, in the event that the points of faces were altered (replaced or shifted).
        /// </summary>
        /// <returns>BoundingBox.</returns>
        public static Vector3[] MakeCornerPoints(Vector3[] vectors, Vector3 origin)
        {
            ///     Corner vertices are ordered as follows, where - = low and + = high along directions 0, 1, and 2 respectively.
            ///     [0] = ---, [1] = +-- , [2] = ++- , [3] = -+-, [4] = --+ , [5] = +-+, [6] = +++, [7] = -++
            var corners = new Vector3[8];
            // ---
            corners[0] = origin;
            // +--
            corners[1] = corners[0] + vectors[0];
            // ++-
            corners[2] = corners[1] + vectors[1];
            // -+-
            corners[3] = corners[0] + vectors[1];
            // --+
            corners[4] = corners[0] + vectors[2];
            // +-+
            corners[5] = corners[1] + vectors[2];
            // +++
            corners[6] = corners[2] + vectors[2];
            // -++
            corners[7] = corners[3] + vectors[2];
            return corners;
        }

        /// <summary>
        /// The center point
        /// </summary>
        /// <value>The center.</value>
        [JsonIgnore]
        public Vector3 Center
        {
            get
            {
                if (_center.IsNull())
                    _center = TranslationFromOrigin + 0.5 * (Vectors[0] + Vectors[1] + Vectors[2]);
                return _center;
            }
        }
        private Vector3 _center = Vector3.Null;


        /// <summary>
        /// The dimensions of the bounding box. The 3 values correspond to length of box in each of the
        /// three directions.
        /// </summary>
        /// <value>The dimensions.</value>
        public Vector3 Dimensions
        {
            get
            {
                if (_dimensions.IsNull())
                    _dimensions = new Vector3(Vectors[0].Length(), Vectors[1].Length(), Vectors[2].Length());
                return _dimensions;

            }
        }
        private Vector3 _dimensions = Vector3.Null;
        /// <summary>
        /// The volume of the bounding box.
        /// </summary>
        /// <value>The volume.</value>
        [JsonIgnore]
        public double Volume => Dimensions[0] * Dimensions[1] * Dimensions[2];

        [JsonIgnore]
        public double DiagonalLength => Math.Sqrt(Dimensions[0] * Dimensions[0] + 
            Dimensions[1] * Dimensions[1] + Dimensions[2] * Dimensions[2]);

        /// <summary>
        /// The Solid Representation of the bounding box. This is not set by defualt.
        /// </summary>
        /// <returns>TessellatedSolid.</returns>
        public TessellatedSolid AsTessellatedSolid()
        {
            if (_tessellatedSolid == null)
            {
                var vertices = Corners.Select(p => new Vertex(p)).ToArray();
                var faces = new[]
                {
                    // negative-X faces
                    new TriangleFace(vertices[0],vertices[4],vertices[7]),
                    new TriangleFace(vertices[0],vertices[7],vertices[3]),
                    // positive-X faces
                    new TriangleFace(vertices[6],vertices[5],vertices[1]),
                    new TriangleFace(vertices[6],vertices[1],vertices[2]),
                    // negative-Y faces
                    new TriangleFace(vertices[0],vertices[1],vertices[5]),
                    new TriangleFace(vertices[0],vertices[5],vertices[4]),
                    // positive-Y faces
                    new TriangleFace(vertices[6],vertices[2],vertices[3]),
                    new TriangleFace(vertices[6],vertices[3],vertices[7]),
                    // negative-Z faces
                    new TriangleFace(vertices[0],vertices[3],vertices[2]),
                    new TriangleFace(vertices[0],vertices[2],vertices[1]),
                    // positive-Z faces
                    new TriangleFace(vertices[6],vertices[7],vertices[4]),
                    new TriangleFace(vertices[6],vertices[4],vertices[5])
                };
                for (int i = 0; i < 12; i++)
                    faces[i].IndexInList = i;
                var tessellatedSolidBuildOptions = new TessellatedSolidBuildOptions();
                tessellatedSolidBuildOptions.CopyElementsPassedToConstructor = false;
                _tessellatedSolid = new TessellatedSolid(faces, vertices, tessellatedSolidBuildOptions);
                _tessellatedSolid.Primitives = new List<PrimitiveSurface>();
                for (var i = 0; i < 12; i += 2)
                    _tessellatedSolid.Primitives.Add(new Plane(new List<TriangleFace> { faces[i], faces[i + 1] }));
            }
            return _tessellatedSolid;
        }

        /// <summary>
        /// The tessellated solid
        /// </summary>
        private TessellatedSolid _tessellatedSolid;

        [JsonIgnore]
        public double ShortestDimension => SortedDimensionsShortToLong[0];

        [JsonIgnore]
        public double LongestDimension => SortedDimensionsShortToLong[2];

        /// <summary>
        /// The direction indices sorted by the dimensions from shortest to longest.
        /// </summary>
        /// <value>The length of the sorted direction indices by.</value>
        [JsonIgnore]
        public int[] SortedDirectionIndicesShortToLong =>
            _sortedDirectionIndicesShortToLong ??= SortDirectionsShortToLong(Dimensions);
        private int[] _sortedDirectionIndicesShortToLong;

        /// <summary>
        /// Sorts the directions by the distance along that direction. Smallest to largest.
        /// </summary>
        public static int[] SortDirectionsShortToLong(Vector3 dimensions)
        {
            if (dimensions[0] <= dimensions[1])
            {
                if (dimensions[1] <= dimensions[2])
                    return [0, 1, 2];
                else if (dimensions[0] <= dimensions[2])
                    return [0, 2, 1];
                else return [2, 0, 1];
            }
            // then we know X>Y
            else if (dimensions[1] > dimensions[2])
                return [2, 1, 0];
            else if (dimensions[0] <= dimensions[2])
                return [1, 0, 2];
            else return [1, 2, 0];
        }
        /// <summary>
        /// The direction indices sorted by the dimensions from smallest to greatest.
        /// </summary>
        /// <value>The length of the sorted directions by.</value>
        [JsonIgnore]
        public Vector3[] SortedDirectionsShortToLong =>
        [
            Directions[SortedDirectionIndicesShortToLong[0]],
            Directions[SortedDirectionIndicesShortToLong[1]],
            Directions[SortedDirectionIndicesShortToLong[2]]
        ];

        /// <summary>
        /// The sorted dimensions from smallest to greatest.
        /// </summary>
        /// <value>The sorted dimensions.</value>
        [JsonIgnore]
        public double[] SortedDimensionsShortToLong =>
            [
            Dimensions[SortedDirectionIndicesShortToLong[0]],
            Dimensions[SortedDirectionIndicesShortToLong[1]],
            Dimensions[SortedDirectionIndicesShortToLong[2]]
            ];

        /// <summary>
        /// The direction indices sorted by the dimensions from shortest to longest.
        /// </summary>
        /// <value>The length of the sorted direction indices by.</value>
        [JsonIgnore]
        public int[] SortedDirectionIndicesLongToShort =>
            _sortedDirectionIndicesLongToShort ??= SortDirectionsLongToShort(Dimensions);
        private int[] _sortedDirectionIndicesLongToShort;

        /// <summary>
        /// Sorts the directions by the distance along that direction. Smallest to largest.
        /// </summary>
        public static int[] SortDirectionsLongToShort(Vector3 dimensions)
        {
            if (dimensions[0] >= dimensions[1])
            {
                if (dimensions[1] >= dimensions[2])
                    return [0, 1, 2];
                else if (dimensions[0] >= dimensions[2])
                    return [0, 2, 1];
                else return [2, 0, 1];
            }
            // then we know X<Y
            else if (dimensions[1] < dimensions[2])
                return [2, 1, 0];
            else if (dimensions[0] >= dimensions[2])
                return [1, 0, 2];
            else return [1, 2, 0];
        }
        /// <summary>
        /// The direction indices sorted by the dimensions from smallest to greatest.
        /// </summary>
        /// <value>The length of the sorted directions by.</value>
        [JsonIgnore]
        public Vector3[] SortedDirectionsLongToShort =>
        [
            Directions[SortedDirectionIndicesLongToShort[0]],
            Directions[SortedDirectionIndicesLongToShort[1]],
            Directions[SortedDirectionIndicesLongToShort[2]]
        ];

        /// <summary>
        /// The sorted dimensions from smallest to greatest.
        /// </summary>
        /// <value>The sorted dimensions.</value>
        [JsonIgnore]
        public double[] SortedDimensionsLongToShort =>
            [
            Dimensions[SortedDirectionIndicesLongToShort[0]],
            Dimensions[SortedDirectionIndicesLongToShort[1]],
            Dimensions[SortedDirectionIndicesLongToShort[2]]
            ];



        #endregion Properties

        /// <summary>
        /// Copies the bounding box to a new one.
        /// </summary>
        /// <returns>BoundingBox.</returns>
        public virtual BoundingBox Copy()
        {
            return new BoundingBox(Vectors.ToArray(), TranslationFromOrigin);
        }



        /// <summary>
        /// Moves the face outward by the provided distance (or inward if distance is negative).
        /// Note that this returns a new bounding box and does not alter the original.
        /// </summary>
        /// <param name="directionIndex">Index of the direction.</param>
        /// <param name="forward">if set to <c>true</c> [forward].</param>
        /// <param name="distance">The distance to move the face by (negative will move the face inward).</param>
        public BoundingBox MoveFaceOutward(int directionIndex, bool forward, double distance)
        {
            var newVectors = new Vector3[3];
            for (int i = 0; i < 3; i++)
            {
                if (i == directionIndex)
                    newVectors[i] = Vectors[i] + distance * Directions[i];
                else
                    newVectors[i] = Vectors[i];
            }
            var newOrigin = forward ? TranslationFromOrigin :
                TranslationFromOrigin - distance * Directions[directionIndex];
            return new BoundingBox(newVectors, newOrigin);
        }
        /// <summary>
        /// Moves the face outward by the provided distance (or inward if distance is negative).
        /// </summary>
        /// <param name="direction">the cartesian direction to move</param>
        /// <param name="distance">The distance to move the face by (negative will move the face inward).</param>

        public BoundingBox MoveFaceOutward(CartesianDirections direction, double distance)
        {
            var intDir = (int)direction;
            return MoveFaceOutward(Math.Abs(intDir) - 1, intDir > 0, distance);
        }

        /// <summary>
        /// Returns the zero or two points that the line intersects with the bounding box.
        /// </summary>
        /// <param name="lineAnchor"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        public IEnumerable<(Vector3 intersection, double lineT)> LineIntersection(Vector3 lineAnchor, Vector3 direction)
        {
            var lowerPt = TranslationFromOrigin;
            var upperPt = TranslationFromOrigin + Dimensions;
            var directions = new[] { -Directions[0], -Directions[1], -Directions[2], Directions[0], Directions[1], Directions[2] };
            var planeDistancesToOrigin = (Directions.Select(d => -d.Dot(lowerPt))
                .Concat(Directions.Select(d => d.Dot(upperPt)))).ToList();
            var numPointsFound = 0;
            for (int i = 0; i < 6; i++)
            {
                var intersectPoint = MiscFunctions.PointOnPlaneFromLine(directions[i], planeDistancesToOrigin[i], lineAnchor,
                    direction, out var t);
                if (intersectPoint.IsNull()) continue;
                var outsideOfBox = false;
                for (int j = 0; j < 6; j++)
                {
                    if (i == j) continue;
                    var ipDistance = intersectPoint.Dot(directions[j]);
                    if (double.IsNaN(ipDistance) || ipDistance > planeDistancesToOrigin[j])
                    {
                        outsideOfBox = true;
                        break;
                    }
                }
                if (outsideOfBox) continue;
                yield return (intersectPoint, t);
                numPointsFound++;
                if (numPointsFound == 2) yield break;
            }
        }

        /// <summary>
        /// Checks if the given point is inside the bounding box.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public bool IsInside(Vector3 point, double offset = 0)
        {
            var anchorDot = point.Dot(Directions[0]);
            var planeDot = TranslationFromOrigin.Dot(Directions[0]);
            if (anchorDot < planeDot - offset || anchorDot > planeDot + Dimensions[0] + offset) return false;

            anchorDot = point.Dot(Directions[1]);
            planeDot = TranslationFromOrigin.Dot(Directions[1]);
            if (anchorDot < planeDot - offset || anchorDot > planeDot + Dimensions[1] + offset) return false;

            anchorDot = point.Dot(Directions[2]);
            planeDot = TranslationFromOrigin.Dot(Directions[2]);
            if (anchorDot < planeDot - offset || anchorDot > planeDot + Dimensions[2] + offset) return false;

            return true;
        }

        public bool Intersects(BoundingBox other)
        {
            if (IsAxisAligned && other.IsAxisAligned)
                return Bounds[0].X < other.Bounds[1].X &&
                    Bounds[1].X > other.Bounds[0].X &&
                    Bounds[0].Y < other.Bounds[1].Y &&
                    Bounds[1].Y > other.Bounds[0].Y &&
                    Bounds[0].Z < other.Bounds[1].Z &&
                    Bounds[1].Z > other.Bounds[0].Z;
            else
            {
                var corners = other.Corners;
                for (int i = 0; i < 8; i++)
                    if (IsInside(corners[i])) return true;
                corners = Corners;
                for (int i = 0; i < 8; i++)
                    if (other.IsInside(corners[i])) return true;
                return false;
            }
        }
    }
}