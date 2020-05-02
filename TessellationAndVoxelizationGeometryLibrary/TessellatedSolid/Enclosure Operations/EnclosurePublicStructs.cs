// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 02-27-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 02-12-2016
// ***********************************************************************
// <copyright file="BoundingBox.cs" company="Design Engineering Lab">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using TVGL.Numerics;
using TVGL.TwoDimensional;

namespace TVGL
{
    /// <summary>
    ///     The BoundingBox is a simple structure for representing an arbitrarily oriented box
    ///     or 3D prismatic rectangle. It simply includes the orientation as three unit vectors in
    ///     "Directions2D", the extreme vertices, and the volume.
    /// </summary>
    public class BoundingBox
    {
        #region Constructors
        // These first three define Dimensions and Transform. note how the first invokes the 
        // second and the second invokes the third.
        /// <summary>
        /// Initializes a new instance of the <see cref="BoundingBox"/> class.
        /// </summary>
        /// <param name="dimensions">The dimensions.</param>
        /// <param name="directions">The directions.</param>
        /// <param name="minPointOnDirection0">The minimum point on direction0.</param>
        /// <param name="minPointOnDirection1">The minimum point on direction1.</param>
        /// <param name="minPointOnDirection2">The minimum point on direction2.</param>
        public BoundingBox(double[] dimensions, Vector3[] directions, Vector3 minPointOnDirection0,
            Vector3 minPointOnDirection1, Vector3 minPointOnDirection2)
            : this(dimensions, directions, new Vector3(
                minPointOnDirection0.Dot(directions[0].Normalize()),
                minPointOnDirection1.Dot(directions[1].Normalize()),
                minPointOnDirection2.Dot(directions[2].Normalize())))
        { }
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

        public BoundingBox(double[] dimensions, Vector3[] directions, IList<IEnumerable<Vertex>> pointsOnFaces)
            : this(dimensions, directions, pointsOnFaces[0].First().Coordinates, pointsOnFaces[2].First().Coordinates,
                 pointsOnFaces[4].First().Coordinates)
        {
            PointsOnFaces = pointsOnFaces.Select(pof => pof.ToArray()).ToArray();
        }

        #endregion
        #region Constructor Defined Fields
        /// <summary>
        ///     The dimensions of the bounding box. The 3 values correspond to the 3 direction.
        ///     If this was a bounding box along a given direction, the first dimension will
        ///     correspond with the distance along that direction.
        /// </summary>
        public double[] Dimensions { get; }

        /// <summary>
        /// Gets the transformation from the global frame to this box. This is only a rotate and translate. Along
        /// with the Dimensions, it fully defines the box
        /// </summary>
        /// <value>The transform.</value>
        public Matrix4x4 Transform { get; private set; }

        /// <summary>
        ///     The PointsOnFaces is an array of 6 lists which are vertices of the tessellated solid 
        ///     that are on the faces of the bounding box. They are in the order of direction1-low,
        ///     direction1-high, direction2-low, direction2-high, direction3-low, direction3-high.
        /// </summary>
        public Vertex[][] PointsOnFaces { get; private set; }

        #endregion

        #region Properties
        //the following properties are set with a lazy private fields
        /// <summary>
        ///     The Directions normal are the three unit vectors that describe the orientation of the box.
        ///     If this was a bounding box along a given direction, the first direction will
        ///     correspond with that direction.
        /// </summary>
        public Vector3[] Directions
        {
            get
            {
                if (_directions == null)
                    _directions = new[] { Transform.XBasisVector, Transform.YBasisVector, Transform.ZBasisVector };
                return _directions;
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
                if (corners == null) MakeCornerPoints();
                return corners;
            }
        }
        private Vector3[] corners;
        /// <summary>
        ///     Adds the corner vertices to the bounding box.
        ///     The dimensions are also updated, in the event that the points of faces were altered (replaced or shifted).
        /// </summary>
        /// <returns>BoundingBox.</returns>
        private void MakeCornerPoints()
        {
            ///     Corner vertices are ordered as follows, where - = low and + = high along directions 0, 1, and 2 respectively.
            ///     [0] = ---, [1] = +-- , [2] = ++- , [3] = -+-, [4] = --+ , [5] = +-+, [6] = +++, [7] = -++
            corners = new Vector3[8];
            // ---
            corners[0] = MiscFunctions.PointCommonToThreePlanes(
                Directions[0], TranslationFromOrigin[0],
                Directions[1], TranslationFromOrigin[1],
                Directions[2], TranslationFromOrigin[2]);
            // +--
            corners[1] = MiscFunctions.PointCommonToThreePlanes(
                Directions[0], TranslationFromOrigin[0] + 0.5 * Dimensions[0],
                Directions[1], TranslationFromOrigin[1],
                Directions[2], TranslationFromOrigin[2]);
            // ++-
            corners[2] = MiscFunctions.PointCommonToThreePlanes(
                Directions[0], TranslationFromOrigin[0] + 0.5 * Dimensions[0],
                Directions[1], TranslationFromOrigin[1] + 0.5 * Dimensions[1],
                Directions[2], TranslationFromOrigin[2]);
            // -+-
            corners[3] = MiscFunctions.PointCommonToThreePlanes(
                Directions[0], TranslationFromOrigin[0],
                Directions[1], TranslationFromOrigin[1] + 0.5 * Dimensions[1],
                Directions[2], TranslationFromOrigin[2]);
            // --+
            corners[4] = MiscFunctions.PointCommonToThreePlanes(
                Directions[0], TranslationFromOrigin[0],
                Directions[1], TranslationFromOrigin[1],
                Directions[2], TranslationFromOrigin[2] + 0.5 * Dimensions[2]);
            // +-+
            corners[5] = MiscFunctions.PointCommonToThreePlanes(
                Directions[0], TranslationFromOrigin[0] + 0.5 * Dimensions[0],
                Directions[1], TranslationFromOrigin[1],
                Directions[2], TranslationFromOrigin[2] + 0.5 * Dimensions[2]);
            // +++
            corners[6] = MiscFunctions.PointCommonToThreePlanes(
                Directions[0], TranslationFromOrigin[0] + 0.5 * Dimensions[0],
                Directions[1], TranslationFromOrigin[1] + 0.5 * Dimensions[1],
                Directions[2], TranslationFromOrigin[2] + 0.5 * Dimensions[2]);
            // -++
            corners[7] = MiscFunctions.PointCommonToThreePlanes(
                Directions[0], TranslationFromOrigin[0],
                Directions[1], TranslationFromOrigin[1] + 0.5 * Dimensions[1],
                Directions[2], TranslationFromOrigin[2] + 0.5 * Dimensions[2]);
        }

        /// <summary>
        ///     The center point
        /// </summary>
        public Vector3 Center
        {
            get
            {
                if (center.IsNull())
                {
                    // since the transform would change 0,0,0 into the bottom corner
                    // it would also change Dimensions into the opposite corner. 
                    // there the center can be found by transforming half the dimensions
                    center = (0.5 * new Vector3(Dimensions)).Transform(Transform);
                }
                return center;
            }
            set
            {
                center = value;
            }
        }
        private Vector3 center = Vector3.Null;

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
        public TessellatedSolid AsTessellatedSolid
        {
            get
            {
                if (_tessellatedSolid == null)
                    _tessellatedSolid = Extrude.ExtrusionSolidFrom3DLoops(new[] { Corners.Take(4).ToArray() },
                        Directions[2], Dimensions[2]);
                return _tessellatedSolid;
            }
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
            center = Vector3.Null;
            corners = null;
            _directions = null;
            _translationFromOrigin = Vector3.Null;
            _sortedDimensions = null;
            _sortedDirectionIndicesByLength = null;
            _sortedDirectionsByLength = null;
            _tessellatedSolid = null;
        }
        #endregion


        public BoundingBox Copy()
        {
            if (PointsOnFaces != null)
                return new BoundingBox(this.Dimensions, this.Directions, PointsOnFaces);
            else return new BoundingBox(this.Dimensions, this.Transform);
        }

        public BoundingBox MoveFaceOutwardToNewSolid(Vector3 direction, double distance)
        {
            var copy = this.Copy();
            copy.MoveFaceOutward(direction, distance);
            return copy;
        }

        public void MoveFaceOutward(Vector3 direction, double distance)
        {
            var unitDir = direction.Normalize();
            CartesianDirections cartesian = CartesianDirections.ZNegative;
            if (unitDir.Dot(Directions[0]).IsPracticallySame(1.0, 0.1)) cartesian = CartesianDirections.XPositive;
            if (unitDir.Dot(Directions[1]).IsPracticallySame(1.0, 0.1)) cartesian = CartesianDirections.YPositive;
            if (unitDir.Dot(Directions[2]).IsPracticallySame(1.0, 0.1)) cartesian = CartesianDirections.ZPositive;
            if (unitDir.Dot(Directions[0]).IsPracticallySame(-1.0, 0.1)) cartesian = CartesianDirections.XNegative;
            if (unitDir.Dot(Directions[1]).IsPracticallySame(-1.0, 0.1)) cartesian = CartesianDirections.YNegative;
            MoveFaceOutward(cartesian, distance);
        }
        public BoundingBox MoveFaceOutwardToNewSolid(CartesianDirections face, double distance)
        {
            var copy = this.Copy();
            copy.MoveFaceOutward(face, distance);
            return copy;
        }
        public void MoveFaceOutward(CartesianDirections face, double distance)
        {
            if (distance.IsNegligible()) return;
            var negativeFace = face < 0;
            var direction = Math.Abs((int)face) - 1;
            Dimensions[direction] += distance;
            if (PointsOnFaces != null)
            {
                //direction1-low,
                ///     direction1-high, direction2-low, direction2-high, direction3-low, direction3-high.
                var vertexOnPlaneIndex = 2 * direction + (!negativeFace ? 1 : 0);
                PointsOnFaces[vertexOnPlaneIndex] = Array.Empty<Vertex>();
            }
            if (negativeFace)
            {
                var translate = TranslationFromOrigin - Vector3.UnitVector(direction) * distance;
                Transform = new Matrix4x4(Transform.XBasisVector, Transform.YBasisVector,
                    Transform.ZBasisVector, translate);
            }

            ResetLazyFields();
        }
    }

    /// <summary>
    ///     Bounding rectangle information based on area and point pairs.
    /// </summary>
    public struct BoundingRectangle
    {
        /// <summary>
        ///     The Area of the bounding box.
        /// </summary>
        public double Area;

        /// <summary>
        ///     Gets the four points of the bounding rectangle, ordered CCW positive
        /// </summary>
        public Vector2[] CornerPoints;

        /// <summary>
        ///     The point pairs that define the bounding rectangle limits. Unlike bounding box
        ///     these go: dir1-min, dir2-max, dir1-max, dir2-min this is because you are going
        ///     around ccw
        /// </summary>
        public List<Vector2>[] PointsOnSides;

        /// <summary>
        ///     Vector direction of length 
        /// </summary>
        public Vector2 LengthDirection;

        /// <summary>
        ///     Vector direction of  width
        /// </summary>
        public Vector2 WidthDirection;

        /// <summary>
        ///     Maximum distance along Direction 1 (length)
        /// </summary>
        internal double LengthDirectionMax;

        /// <summary>
        ///     Minimum distance along Direction 1 (length)
        /// </summary>
        internal double LengthDirectionMin;

        /// <summary>
        ///     Maximum distance along Direction 2 (width)
        /// </summary>
        internal double WidthDirectionMax;

        /// <summary>
        ///     Minimum distance along Direction 2 (width)
        /// </summary>
        internal double WidthDirectionMin;

        /// <summary>
        ///     Length of Bounding Rectangle
        /// </summary>
        public double Length;

        /// <summary>
        ///     Width of Bounding Rectangle
        /// </summary>
        public double Width;

        /// <summary>
        ///     2D Center Position of the Bounding Rectangle
        /// </summary>
        public Vector2 CenterPosition;

        /// <summary>
        /// Sets the corner points and center position for the bounding rectangle
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void SetCornerPoints()
        {
            var v1Max = LengthDirection * LengthDirectionMax;
            var v1Min = LengthDirection * LengthDirectionMin;
            var v2Max = WidthDirection * WidthDirectionMax;
            var v2Min = WidthDirection * WidthDirectionMin;
            var p1 = v1Max + v2Max;
            var p2 = v1Min + v2Max;
            var p3 = v1Min + v2Min;
            var p4 = v1Max + v2Min;
            CornerPoints = new[] { p1, p2, p3, p4 };
            var areaCheck = CornerPoints.Area();
            if (areaCheck < 0.0)
            {
                CornerPoints = new[] { p4, p3, p2, p1 };
                areaCheck = -areaCheck;
            }

            //Add in the center
            var centerPosition = new[] { 0.0, 0.0 };
            var cX = 0.0;
            var cY = 0.0;
            foreach (var vertex in CornerPoints)
            {
                cX += vertex.X;
                cY += vertex.Y;
            }

            //Check to make sure the points are ordered correctly (within 1 %)
            if (!areaCheck.IsPracticallySame(Area, 0.01 * Area))
                throw new Exception("Points are ordered incorrectly");
            CenterPosition = new Vector2(0.25 * cX, 0.25 * cY);
        }
    }


    /// <summary>
    ///     Public circle structure, given a center point and radius
    /// </summary>
    public struct BoundingCircle
    {
        /// <summary>
        ///     Center Point of circle
        /// </summary>
        public Vector2 Center;

        /// <summary>
        ///     Radius of circle
        /// </summary>
        public double Radius;

        /// <summary>
        ///     Area of circle
        /// </summary>
        public double Area;

        /// <summary>
        ///     Circumference of circle
        /// </summary>
        public double Circumference;

        /// <summary>
        ///     Creates a circle, given a radius. Center point is optional
        /// </summary>
        /// <param name="radius">The radius.</param>
        /// <param name="center">The center.</param>
        public BoundingCircle(double radius, Vector2 center)
        {
            Center = center;
            Radius = radius;
            Area = Math.PI * radius * radius;
            Circumference = Constants.TwoPi * radius;
        }
    }

    /// <summary>
    ///     Public cylinder structure
    /// </summary>
    public struct BoundingCylinder
    {
        /// <summary>
        ///     Center axis along depth
        /// </summary>
        public Vector3 Axis;

        /// <summary>
        ///     Bounding Circle on one end of the cylinder
        /// </summary>
        public BoundingCircle BoundingCircle;

        /// <summary>
        ///     Height of cylinder
        /// </summary>
        public double Height;

        /// <summary>
        ///     Volume
        /// </summary>
        public double Volume;
    }
}