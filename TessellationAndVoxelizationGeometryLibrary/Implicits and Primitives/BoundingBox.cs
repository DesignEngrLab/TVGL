﻿// ***********************************************************************
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
        /// Initializes a new instance of the <see cref="BoundingBox{T}"/> class.
        /// </summary>
        /// <param name="dimensions">The dimensions.</param>
        /// <param name="transform">The transform.</param>
        public BoundingBox(double[] dimensions, Matrix4x4 transform) : base(dimensions, transform)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BoundingBox{T}" /> class.
        /// </summary>
        /// <param name="dimensions">The dimensions.</param>
        /// <param name="directions">The directions.</param>
        /// <param name="pointsOnFaces">The points on faces.</param>
        public BoundingBox(double[] dimensions, Vector3[] directions, IList<IEnumerable<T>> pointsOnFaces)
    : this(dimensions, directions, pointsOnFaces[0].First(), pointsOnFaces[2].First(),
         pointsOnFaces[4].First())
        {
            PointsOnFaces = pointsOnFaces.Select(pof => pof.ToArray()).ToArray();
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="BoundingBox{T}"/> class.
        /// </summary>
        /// <param name="dimensions">The dimensions.</param>
        /// <param name="directions">The directions.</param>
        /// <param name="minPointOnDirection0">The minimum point on direction0.</param>
        /// <param name="minPointOnDirection1">The minimum point on direction1.</param>
        /// <param name="minPointOnDirection2">The minimum point on direction2.</param>
        public BoundingBox(double[] dimensions, Vector3[] directions, T minPointOnDirection0,
        T minPointOnDirection1, T minPointOnDirection2)
        : base(dimensions, directions,
             MiscFunctions.PointCommonToThreePlanes(directions[0].Normalize(),
                 minPointOnDirection0.Dot(directions[0].Normalize()), directions[1].Normalize(),
                 minPointOnDirection1.Dot(directions[1].Normalize()), directions[2].Normalize(),
                 minPointOnDirection2.Dot(directions[2].Normalize())))
        { }


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
            var copiedBB = new BoundingBox<T>(this.Dimensions.ToArray(), this.Transform);
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
    /// rotation and translation from the gloval frame, 2) the dimension of the box at this coordinate
    /// frame (the box is only in the positive octant of this local frame), 3) (optional) any points
    /// of the enclosing solid that touch the box.
    /// </summary>
    public class BoundingBox
    {
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="BoundingBox" /> class.
        /// </summary>
        /// <param name="dimensions">The dimensions.</param>
        /// <param name="directions">The directions.</param>
        /// <param name="translationFromOrigin">The translation to the lowest corner from the  origin.</param>
        public BoundingBox(double[] dimensions, Vector3[] directions, Vector3 translationFromOrigin)
          : this(dimensions, new Matrix4x4(directions[0].Normalize(), directions[1].Normalize(),
              directions[2].Normalize(), translationFromOrigin))
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="BoundingBox" /> class.
        /// </summary>
        /// <param name="dimensions">The dimensions.</param>
        /// <param name="transform">The transform.</param>
        public BoundingBox(double[] dimensions, Matrix4x4 transform)
        {
            Dimensions = new Vector3(dimensions[0], dimensions[1], dimensions[2]);
            Transform = transform;
        }

        public BoundingBox(Vector3 min, Vector3 max)
        {
            Dimensions = max - min;
            Transform = new Matrix4x4(Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ, min);
        }

        public BoundingBox(double minX, double minY, double minZ, double maxX, double maxY, double maxZ)
        {
            Dimensions = new Vector3(maxX - minX, maxY - minY, maxZ - minZ);
            Transform = new Matrix4x4(Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ, new Vector3(minX, minY, minZ));
        }
        #endregion Constructors

        #region Constructor Defined Fields

        /// <summary>
        /// The dimensions of the bounding box. The 3 values correspond to length of box in each of the
        /// three directions.
        /// </summary>
        /// <value>The dimensions.</value>
        public Vector3 Dimensions { get; set; }
        /// <summary>
        /// Gets the bounds. The 0-th Vector3 is the minX, minY, and minZ, the 1st is the maxX, maxY, and maxZ
        /// </summary>
        /// <value>The bounds.</value>
        public Vector3[] Bounds
        {
            get
            {
                return new[] { TranslationFromOrigin, TranslationFromOrigin + Dimensions };
            }
        }

        /// <summary>
        /// Gets the transformation from the global frame to this box. This is only a rotate and translate. Along
        /// with the Dimensions, it fully defines the box
        /// </summary>
        /// <value>The transform.</value>
        public Matrix4x4 Transform { get; private set; }

        #endregion Constructor Defined Fields

        #region Properties

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
                return _directions ??= new[]
                    {Transform.XBasisVector, Transform.YBasisVector, Transform.ZBasisVector};
            }
        }

        /// <summary>
        /// The directions
        /// </summary>
        private Vector3[] _directions;

        /// <summary>
        /// The minimum plane distance to the origin. This is the simplest way to locate the box in space.
        /// These three values along with the direction vectors indicate the three planes of the lower
        /// corner of the box. If you add dimensions to this, you would get the maximum plane distances
        /// </summary>
        /// <value>The translation from origin.</value>
        [JsonIgnore]
        public Vector3 TranslationFromOrigin
        {
            get
            {
                if (_translationFromOrigin.IsNull())
                    _translationFromOrigin = Transform.TranslationAsVector;
                return _translationFromOrigin;
            }
        }

        /// <summary>
        /// The translation from origin
        /// </summary>
        private Vector3 _translationFromOrigin = Vector3.Null;

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
                if (_corners == null) MakeCornerPoints();
                return _corners;
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
        private void MakeCornerPoints()
        {
            ///     Corner vertices are ordered as follows, where - = low and + = high along directions 0, 1, and 2 respectively.
            ///     [0] = ---, [1] = +-- , [2] = ++- , [3] = -+-, [4] = --+ , [5] = +-+, [6] = +++, [7] = -++
            _corners = new Vector3[8];
            // ---
            _corners[0] = TranslationFromOrigin;
            // +--
            _corners[1] = _corners[0] + Directions[0] * Dimensions[0];
            // ++-
            _corners[2] = _corners[1] + Directions[1] * Dimensions[1];
            // -+-
            _corners[3] = _corners[0] + Directions[1] * Dimensions[1];
            // --+
            _corners[4] = _corners[0] + Directions[2] * Dimensions[2];
            // +-+
            _corners[5] = _corners[1] + Directions[2] * Dimensions[2];
            // +++
            _corners[6] = _corners[2] + Directions[2] * Dimensions[2];
            // -++
            _corners[7] = _corners[3] + Directions[2] * Dimensions[2];
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
                {
                    // since the transform would change 0,0,0 into the bottom corner
                    // it would also change Dimensions into the opposite corner.
                    // there the center can be found by transforming half the dimensions
                    _center = (0.5 * new Vector3(Dimensions)).Multiply(Transform);
                }
                return _center;
            }
            set => _center = value;
        }

        /// <summary>
        /// The center
        /// </summary>
        private Vector3 _center = Vector3.Null;

        /// <summary>
        /// The volume of the bounding box.
        /// </summary>
        /// <value>The volume.</value>
        [JsonIgnore]
        public double Volume => Dimensions[0] * Dimensions[1] * Dimensions[2];

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
                var random = new Random(0);
                var tessellatedSolidBuildOptions = new TessellatedSolidBuildOptions();
                tessellatedSolidBuildOptions.CopyElementsPassedToConstructor = false;
                _tessellatedSolid = new TessellatedSolid(faces, vertices, tessellatedSolidBuildOptions, new[] {
                    new Color(0.6f,(float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble()) });
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

        /// <summary>
        /// The direction indices sorted by the dimensions from smallest to greatest.
        /// </summary>
        /// <value>The length of the sorted direction indices by.</value>
        [JsonIgnore]
        public IList<int> SortedDirectionIndicesByLength
        {
            get
            {
                if (_sortedDirectionIndicesByLength == null)
                    SetSortedDirections();
                return _sortedDirectionIndicesByLength;
            }
        }

        /// <summary>
        /// The sorted direction indices by length
        /// </summary>
        private IList<int> _sortedDirectionIndicesByLength;

        /// <summary>
        /// The direction indices sorted by the dimensions from smallest to greatest.
        /// </summary>
        /// <value>The length of the sorted directions by.</value>
        [JsonIgnore]
        public IList<Vector3> SortedDirectionsByLength
        {
            get
            {
                if (_sortedDirectionsByLength == null)
                    SetSortedDirections();
                return _sortedDirectionsByLength;
            }
        }

        /// <summary>
        /// The sorted directions by length
        /// </summary>
        private IList<Vector3> _sortedDirectionsByLength;

        /// <summary>
        /// The sorted dimensions from smallest to greatest.
        /// </summary>
        /// <value>The sorted dimensions.</value>
        [JsonIgnore]
        public double[] SortedDimensions
        {
            get
            {
                if (_sortedDimensions == null)
                    SetSortedDirections();
                return _sortedDimensions;
            }
        }

        /// <summary>
        /// The sorted dimensions
        /// </summary>
        private double[] _sortedDimensions;

        /// <summary>
        /// Sorts the directions by the distance along that direction. Smallest to largest.
        /// </summary>
        private void SetSortedDirections()
        {
            if (Dimensions[0] <= Dimensions[1])
            {
                if (Dimensions[1] <= Dimensions[2])
                    _sortedDirectionIndicesByLength = [0, 1, 2];
                else if (Dimensions[0] <= Dimensions[2])
                    _sortedDirectionIndicesByLength = [0, 2, 1];
                else
                    _sortedDirectionIndicesByLength = [2, 0, 1];
            }
            // then X>Y
            else if (Dimensions[1] > Dimensions[2])
                _sortedDirectionIndicesByLength = [2, 1, 0];
            else if (Dimensions[0] <= Dimensions[2])
                _sortedDirectionIndicesByLength = [1, 0, 2];
            else
                _sortedDirectionIndicesByLength = [1, 2, 0];

            _sortedDimensions = [
                Dimensions[_sortedDirectionIndicesByLength[0]],
                Dimensions[_sortedDirectionIndicesByLength[1]],
                Dimensions[_sortedDirectionIndicesByLength[2]] ];
            _sortedDirectionsByLength = [
                Directions[_sortedDirectionIndicesByLength[0]],
                Directions[_sortedDirectionIndicesByLength[1]],
                Directions[_sortedDirectionIndicesByLength[2]] ];
        }

        /// <summary>
        /// Resets the lazy fields.
        /// </summary>
        private void ResetLazyFields()
        {
            _center = Vector3.Null;
            _corners = null;
            _directions = null;
            _translationFromOrigin = Vector3.Null;
            _sortedDimensions = null;
            _sortedDirectionIndicesByLength = null;
            _sortedDirectionsByLength = null;
            _tessellatedSolid = null;
        }

        #endregion Properties

        /// <summary>
        /// Copies the bounding box to a new one.
        /// </summary>
        /// <returns>BoundingBox.</returns>
        public virtual BoundingBox Copy()
        {
            return new BoundingBox(this.Dimensions.ToArray(), this.Transform);
        }

        /// <summary>
        /// The small angle
        /// </summary>
        private const double smallAngle = 0.0038;  // current value is one minus cosine of 5 Degrees

        /// <summary>
        /// Moves a face outward by the provided distance (or inward if distance is negative).
        /// This does not alter the bounding box, but returns a new one.
        /// </summary>
        /// <param name="direction">The direction vector corresponding to the face normal (this must be within 5 degrees or it won't apply).</param>
        /// <param name="distance">The distance to move the face by (negative will move the face inward).</param>
        /// <returns>BoundingBox.</returns>
        public BoundingBox MoveFaceOutwardToNewSolid(Vector3 direction, double distance)
        {
            var copy = this.Copy();
            copy.MoveFaceOutward(direction, distance);
            return copy;
        }

        /// <summary>
        /// Moves the face outward by the provided distance (or inward if distance is negative).
        /// </summary>
        /// <param name="direction">The direction vector corresponding to the face normal (this must be within 5 degrees or it won't apply).</param>
        /// <param name="distance">The distance to move the face by (negative will move the face inward).</param>
        public void MoveFaceOutward(Vector3 direction, double distance)
        {
            var cartesian = MiscFunctions.SnapDirectionToCartesian(direction, out _);
            MoveFaceOutward(cartesian, distance);
        }

        /// <summary>
        /// Moves a face outward by the provided distance (or inward if distance is negative).
        /// This does not alter the bounding box, but returns a new one.
        /// </summary>
        /// <param name="directionIndex">Index of the direction.</param>
        /// <param name="forward">if set to <c>true</c> [forward].</param>
        /// <param name="distance">The distance to move the face by (negative will move the face inward).</param>
        /// <returns>BoundingBox.</returns>
        public BoundingBox MoveFaceOutwardToNewSolid(int directionIndex, bool forward, double distance)
        {
            var copy = this.Copy();
            copy.MoveFaceOutward(directionIndex, forward, distance);
            return copy;
        }

        /// <summary>
        /// Moves the face outward by the provided distance (or inward if distance is negative).
        /// </summary>
        /// <param name="directionIndex">Index of the direction.</param>
        /// <param name="forward">if set to <c>true</c> [forward].</param>
        /// <param name="distance">The distance to move the face by (negative will move the face inward).</param>
        public void MoveFaceOutward(int directionIndex, bool forward, double distance)
        {
            //The bounding box is oriented from its origin (TranslationFromOrigin) outward
            //along the given directions. When offsetting along a direction, just make that dimension bigger.
            //When offsetting in the reverse of a direction, make the dimension bigger and then shift the origin.
            if (distance.IsNegligible()) return;
            var delta = new Vector3(directionIndex == 0 ? distance : 0,
                                    directionIndex == 1 ? distance : 0,
                                    directionIndex == 2 ? distance : 0);
            Dimensions += delta;
            if (!forward)
            {
                var translate = TranslationFromOrigin - delta;
                Transform = new Matrix4x4(Transform.XBasisVector, Transform.YBasisVector,
                    Transform.ZBasisVector, translate);
            }
            ResetLazyFields();
        }
        /// <summary>
        /// Moves the face outward by the provided distance (or inward if distance is negative).
        /// </summary>
        /// <param name="direction">the cartesian direction to move</param>
        /// <param name="distance">The distance to move the face by (negative will move the face inward).</param>

        public void MoveFaceOutward(CartesianDirections direction, double distance)
        {
            var intDir = (int)direction;
            MoveFaceOutward(Math.Abs(intDir) - 1, intDir > 0, distance);
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
        public double MinX
        {
            get
            {
                if (Transform.XBasisVector == Vector3.UnitX)
                    return TranslationFromOrigin.X;
                else throw new NotImplementedException();
            }
        }
        public double MaxX
        {
            get
            {
                if (Transform.XBasisVector == Vector3.UnitX)
                    return Dimensions.X + TranslationFromOrigin.X;
                else throw new NotImplementedException();
            }
        }
        public double MinY
        {
            get
            {
                if (Transform.YBasisVector == Vector3.UnitY)
                    return TranslationFromOrigin.Y;
                else throw new NotImplementedException();
            }
        }
        public double MaxY
        {
            get
            {
                if (Transform.YBasisVector == Vector3.UnitY)
                    return Dimensions.Y + TranslationFromOrigin.Y;
                else throw new NotImplementedException();
            }
        }
        public double MinZ
        {
            get
            {
                if (Transform.ZBasisVector == Vector3.UnitZ)
                    return TranslationFromOrigin.Z;
                else throw new NotImplementedException();
            }
        }
        public double MaxZ
        {
            get
            {
                if (Transform.ZBasisVector == Vector3.UnitZ)
                    return Dimensions.Z + TranslationFromOrigin.Z;
                else throw new NotImplementedException();
            }
        }

        public bool Intersects(BoundingBox other)
        {
            return MinX < other.MaxX &&
                MaxX > other.MinX &&
                MinY < other.MaxY &&
                MaxY > other.MinY &&
                MinZ < other.MaxZ &&
                MaxZ > other.MinZ;
        }
    }
}