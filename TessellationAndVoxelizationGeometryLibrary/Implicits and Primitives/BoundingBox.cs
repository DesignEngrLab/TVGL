// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using System;
using System.Collections.Generic;
using System.Linq;
using TVGL.Numerics;

namespace TVGL
{
    /// <summary>
    /// Class BoundingBox with Generic (T) is for times when you have points you want to save
    /// on the boundary of the bounding box. these points are of type T, which is constrained
    /// to be an IVertex3D - currently instantiated by Vertex and Vector3
    /// Implements the <see cref="TVGL.BoundingBox" />
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="TVGL.BoundingBox" />
    public class BoundingBox<T> : BoundingBox where T : IVertex3D
    {
        public BoundingBox(double[] dimensions, Matrix4x4 transform) : base(dimensions, transform)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BoundingBox{T}"/> class.
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
        public BoundingBox(double[] dimensions, Vector3[] directions, T minPointOnDirection0,
        T minPointOnDirection1, T minPointOnDirection2)
        : base(dimensions, directions,
             MiscFunctions.PointCommonToThreePlanes(directions[0].Normalize(),
                 minPointOnDirection0.Dot(directions[0].Normalize()), directions[1].Normalize(),
                 minPointOnDirection1.Dot(directions[1].Normalize()), directions[2].Normalize(),
                 minPointOnDirection2.Dot(directions[2].Normalize())))
        { }


        /// <summary>
        ///     The PointsOnFaces is an array of 6 lists which are vertices of the tessellated solid
        ///     that are on the faces of the bounding box. They are in the order of direction1-low,
        ///     direction1-high, direction2-low, direction2-high, direction3-low, direction3-high.
        /// </summary>
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
    ///     The BoundingBox is a simple class for representing an arbitrarily oriented box
    ///     or 3D prismatic rectangle. It is defined by 1) the Matrix4x4 transform which includes the
    ///     rotation and translation from the gloval frame, 2) the dimension of the box at this coordinate
    ///     frame (the box is only in the positive octant of this local frame), 3) (optional) any points
    ///     of the enclosing solid that touch the box.
    /// </summary>
    public class BoundingBox
    {
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="BoundingBox"/> class.
        /// </summary>
        /// <param name="dimensions">The dimensions.</param>
        /// <param name="directions">The directions.</param>
        /// <param name="translationFromOrigin">The translation to the lowest corner from the  origin.</param>
        public BoundingBox(double[] dimensions, Vector3[] directions, Vector3 translationFromOrigin)
          : this(dimensions, new Matrix4x4(directions[0].Normalize(), directions[1].Normalize(),
              directions[2].Normalize(), translationFromOrigin))
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="BoundingBox"/> class.
        /// </summary>
        /// <param name="dimensions">The dimensions.</param>
        /// <param name="transform">The transform.</param>
        public BoundingBox(double[] dimensions, Matrix4x4 transform)
        {
            Dimensions = dimensions;
            Transform = transform;
        }

        #endregion Constructors

        #region Constructor Defined Fields

        /// <summary>
        ///     The dimensions of the bounding box. The 3 values correspond to length of box in each of the
        ///     three directions.
        /// </summary>
        public double[] Dimensions { get; }

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
        ///     The Directions normal are the three unit vectors that describe the orientation of the box.
        ///     These directions also correspond to the first three rows of the transform matrix (not including
        ///     the perspective terms of course).
        ///     correspond with that direction.
        /// </summary>
        public Vector3[] Directions
        {
            get
            {
                return _directions ??= new[]
                    {Transform.XBasisVector, Transform.YBasisVector, Transform.ZBasisVector};
            }
        }

        private Vector3[] _directions;

        /// <summary>
        /// The minimum plane distance to the origin. This is the simplest way to locate the box in space.
        /// These three values along with the direction vectors indicate the three planes of the lower
        /// corner of the box. If you add dimensions to this, you would get the maximum plane distances
        /// </summary>
        public Vector3 TranslationFromOrigin
        {
            get
            {
                if (_translationFromOrigin.IsNull())
                    _translationFromOrigin = Transform.TranslationAsVector;
                return _translationFromOrigin;
            }
        }

        private Vector3 _translationFromOrigin = Vector3.Null;

        /// <summary>
        ///     Corner vertices are ordered as follows, where - = low and + = high along directions 0, 1, and 2 respectively.
        ///     [0] = ---, [1] = +-- , [2] = ++- , [3] = -+-, [4] = --+ , [5] = +-+, [6] = +++, [7] = -++
        /// </summary>
        public Vector3[] Corners
        {
            get
            {
                if (_corners == null) MakeCornerPoints();
                return _corners;
            }
        }

        private Vector3[] _corners;

        /// <summary>
        ///     Adds the corner vertices to the bounding box.
        ///     The dimensions are also updated, in the event that the points of faces were altered (replaced or shifted).
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
        ///     The center point
        /// </summary>
        public Vector3 Center
        {
            get
            {
                if (_center.IsNull())
                {
                    // since the transform would change 0,0,0 into the bottom corner
                    // it would also change Dimensions into the opposite corner.
                    // there the center can be found by transforming half the dimensions
                    _center = (0.5 * new Vector3(Dimensions)).Transform(Transform);
                }
                return _center;
            }
            set => _center = value;
        }

        private Vector3 _center = Vector3.Null;

        /// <summary>
        ///     The volume of the bounding box.
        /// </summary>
        public double Volume => Dimensions[0] * Dimensions[1] * Dimensions[2];

        /// <summary>
        ///     The Solid Representation of the bounding box. This is not set by defualt.
        /// </summary>

        /// <summary>
        ///     Sets the Solid Representation of the bounding box
        /// </summary>
        public TessellatedSolid AsTessellatedSolid()
        {
            if (_tessellatedSolid == null)
            {
                var vertices = Corners.Select(p => new Vertex(p)).ToArray();
                var faces = new[]
                {
                // negative-X faces
                new PolygonalFace(new []{vertices[0],vertices[4],vertices[7] }),
                new PolygonalFace(new []{vertices[0],vertices[7],vertices[3] }),
                // positive-X faces
                new PolygonalFace(new []{vertices[6],vertices[5],vertices[1] }),
                new PolygonalFace(new []{vertices[6],vertices[1],vertices[2] }),
                // negative-Y faces
                new PolygonalFace(new []{vertices[0],vertices[1],vertices[5] }),
                new PolygonalFace(new []{vertices[0],vertices[5],vertices[4] }),
                // positive-Y faces
                new PolygonalFace(new []{vertices[6],vertices[2],vertices[3] }),
                new PolygonalFace(new []{vertices[6],vertices[3],vertices[7] }),
                // negative-Z faces
                new PolygonalFace(new []{vertices[0],vertices[3],vertices[2] }),
                new PolygonalFace(new []{vertices[0],vertices[2],vertices[1] }),
                // positive-Z faces
                new PolygonalFace(new []{vertices[6],vertices[7],vertices[4] }),
                new PolygonalFace(new []{vertices[6],vertices[4],vertices[5] })
            };
                var random = new Random(0);
                _tessellatedSolid = new TessellatedSolid(faces, true, false, vertices, new[] {
                    new Color(0.6f,(float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble()) });
            }
            return _tessellatedSolid;
        }

        private TessellatedSolid _tessellatedSolid;

        /// <summary>
        ///     The direction indices sorted by the dimensions from smallest to greatest.
        /// </summary>
        public IList<int> SortedDirectionIndicesByLength
        {
            get
            {
                if (_sortedDirectionIndicesByLength == null)
                    SetSortedDirections();
                return _sortedDirectionIndicesByLength;
            }
        }

        private IList<int> _sortedDirectionIndicesByLength;

        /// <summary>
        ///     The direction indices sorted by the dimensions from smallest to greatest.
        /// </summary>
        public IList<Vector3> SortedDirectionsByLength
        {
            get
            {
                if (_sortedDirectionsByLength == null)
                    SetSortedDirections();
                return _sortedDirectionsByLength;
            }
        }

        private IList<Vector3> _sortedDirectionsByLength;

        /// <summary>
        ///     The sorted dimensions from smallest to greatest.
        /// </summary>
        public double[] SortedDimensions
        {
            get
            {
                if (_sortedDimensions == null)
                    SetSortedDirections();
                return _sortedDimensions;
            }
        }

        private double[] _sortedDimensions;

        /// <summary>
        /// Sorts the directions by the distance along that direction. Smallest to largest.
        /// </summary>
        private void SetSortedDirections()
        {
            if (Dimensions[0] <= Dimensions[1])
            {
                if (Dimensions[1] <= Dimensions[2])
                    _sortedDirectionIndicesByLength = new[] { 0, 1, 2 };
                else if (Dimensions[0] <= Dimensions[2])
                    _sortedDirectionIndicesByLength = new[] { 0, 2, 1 };
                else
                    _sortedDirectionIndicesByLength = new[] { 2, 0, 1 };
            }
            // then X>Y
            else if (Dimensions[1] > Dimensions[2])
                _sortedDirectionIndicesByLength = new[] { 2, 1, 0 };
            else if (Dimensions[0] <= Dimensions[2])
                _sortedDirectionIndicesByLength = new[] { 1, 0, 2 };
            else
                _sortedDirectionIndicesByLength = new[] { 1, 2, 0 };

            _sortedDimensions = new[] {
                Dimensions[_sortedDirectionIndicesByLength[0]],
                Dimensions[_sortedDirectionIndicesByLength[1]],
                Dimensions[_sortedDirectionIndicesByLength[2]] };
            _sortedDirectionsByLength = new[] {
                Directions[_sortedDirectionIndicesByLength[0]],
                Directions[_sortedDirectionIndicesByLength[1]],
                Directions[_sortedDirectionIndicesByLength[2]] };
        }

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
            //First, we need to find which bounding box direction this is along
            //If it is not along any of them, then return without any modification.
            var unitDir = direction.Normalize();
            int directionIndex = -1;
            bool reverse = false;
            for(var i = 0; i < 3; i++)
            {
                if (Directions[i].IsAlignedOrReverse(direction, out reverse, smallAngle))
                {
                    directionIndex = i;
                    break;
                }
            }
            if (directionIndex == -1) return;
            MoveFaceOutward(directionIndex, !reverse, distance);
        }

        /// <summary>
        /// Moves a face outward by the provided distance (or inward if distance is negative).
        /// This does not alter the bounding box, but returns a new one.
        /// </summary>
        /// <param name="face">The face as defined by the CartesianDirections enumerator. This is the local x,y,z - not the global.</param>
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
        /// <param name="direction">The direction vector corresponding to the face normal (this must be within 5 degrees or it won't apply).</param>
        /// <param name="distance">The distance to move the face by (negative will move the face inward).</param>
        public void MoveFaceOutward(int directionIndex, bool forward, double distance)
        {
            //The bounding box is oriented from its origin (TranslationFromOrigin) outward
            //along the given directions. When offsetting along a direction, just make that dimension bigger.
            //When offsetting in the reverse of a direction, make the dimension bigger and then shift the origin.
            if (distance.IsNegligible()) return;
            var direction = Directions[directionIndex];
            Dimensions[directionIndex] += distance;
            if (!forward)
            {
                var translate = TranslationFromOrigin - direction * distance;
                Transform = new Matrix4x4(Transform.XBasisVector, Transform.YBasisVector,
                    Transform.ZBasisVector, translate);
            }
            ResetLazyFields();
        }
    }
}