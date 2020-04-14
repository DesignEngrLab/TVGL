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
        #region Constructor Defined Fields
        /// <summary>
        ///     The dimensions of the bounding box. The 3 values correspond to the 3 direction.
        ///     If this was a bounding box along a given direction, the first dimension will
        ///     correspond with the distance along that direction.
        /// </summary>
        public Vector3 Dimensions { get; private set; }

        /// <summary>
        ///     The Directions normal are the three unit vectors that describe the orientation of the box.
        ///     If this was a bounding box along a given direction, the first direction will
        ///     correspond with that direction.
        /// </summary>
        public Vector3[] Directions { get; private set; }

        /// <summary>
        /// The minimum plane distance to the origin. This is the simplest way to locate the box in space.
        /// These three values along with the direction vectors indicate the three planes of the lower
        /// corner of the box. If you add dimensions to this, you would get the maximum plane distances
        /// </summary>
        public Vector3 MinPlaneDistanceToOrigin { get; private set; }

        public BoundingBox(Vector3 dimensions, Vector3[] directions, Vector3 minplaneDistAnceToOrigin)
        {
            Dimensions = dimensions;
            Directions = directions.Select(d => d.Normalize()).ToArray();
            MinPlaneDistanceToOrigin = minplaneDistAnceToOrigin;
        }
        public BoundingBox(Vector3 dimensions, Vector3[] directions, Vector3 minPointOnDirection0,
            Vector3 minPointOnDirection1, Vector3 minPointOnDirection2)
            : this(dimensions, directions, new Vector3(
                minPointOnDirection0.Dot(directions[0]),
                minPointOnDirection1.Dot(directions[1]),
                minPointOnDirection2.Dot(directions[2])))
        { }

        #endregion

        #region Properties
        /// <summary>
        ///     The PointsOnFaces is an array of 6 lists which are vertices of the tessellated solid 
        ///     that are on the faces of the bounding box. They are in the order of direction1-low,
        ///     direction1-high, direction2-low, direction2-high, direction3-low, direction3-high.
        /// </summary>
        public List<Vertex>[] PointsOnFaces { get; internal set; }

        #region the following properties are set with a lazy private field
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
            set
            {
                corners = value;
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
                Directions[0], MinPlaneDistanceToOrigin[0],
                Directions[1], MinPlaneDistanceToOrigin[1],
                Directions[2], MinPlaneDistanceToOrigin[2]);
            // +--
            corners[1] = MiscFunctions.PointCommonToThreePlanes(
                Directions[0], MinPlaneDistanceToOrigin[0] + 0.5 * Dimensions[0],
                Directions[1], MinPlaneDistanceToOrigin[1],
                Directions[2], MinPlaneDistanceToOrigin[2]);
            // ++-
            corners[2] = MiscFunctions.PointCommonToThreePlanes(
                Directions[0], MinPlaneDistanceToOrigin[0] + 0.5 * Dimensions[0],
                Directions[1], MinPlaneDistanceToOrigin[1] + 0.5 * Dimensions[1],
                Directions[2], MinPlaneDistanceToOrigin[2]);
            // -+-
            corners[3] = MiscFunctions.PointCommonToThreePlanes(
                Directions[0], MinPlaneDistanceToOrigin[0],
                Directions[1], MinPlaneDistanceToOrigin[1] + 0.5 * Dimensions[1],
                Directions[2], MinPlaneDistanceToOrigin[2]);
            // --+
            corners[4] = MiscFunctions.PointCommonToThreePlanes(
                Directions[0], MinPlaneDistanceToOrigin[0],
                Directions[1], MinPlaneDistanceToOrigin[1],
                Directions[2], MinPlaneDistanceToOrigin[2] + 0.5 * Dimensions[2]);
            // +-+
            corners[5] = MiscFunctions.PointCommonToThreePlanes(
                Directions[0], MinPlaneDistanceToOrigin[0] + 0.5 * Dimensions[0],
                Directions[1], MinPlaneDistanceToOrigin[1],
                Directions[2], MinPlaneDistanceToOrigin[2] + 0.5 * Dimensions[2]);
            // +++
            corners[6] = MiscFunctions.PointCommonToThreePlanes(
                Directions[0], MinPlaneDistanceToOrigin[0] + 0.5 * Dimensions[0],
                Directions[1], MinPlaneDistanceToOrigin[1] + 0.5 * Dimensions[1],
                Directions[2], MinPlaneDistanceToOrigin[2] + 0.5 * Dimensions[2]);
            // -++
            corners[7] = MiscFunctions.PointCommonToThreePlanes(
                Directions[0], MinPlaneDistanceToOrigin[0],
                Directions[1], MinPlaneDistanceToOrigin[1] + 0.5 * Dimensions[1],
                Directions[2], MinPlaneDistanceToOrigin[2] + 0.5 * Dimensions[2]);
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
                    center = new Vector3();
                    center = Corners.Aggregate(center, (c, v) => c + v).Divide(8.0);
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
        public double Volume => Dimensions.X * Dimensions.Y * Dimensions.Z;

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
                    _tessellatedSolid = Extrude.ExtrusionSolidFrom3DLoops(new [] { Corners.Take(4).ToArray() },
                        Directions[2], Dimensions[2]);
                return _tessellatedSolid;
            }
        }
        private TessellatedSolid _tessellatedSolid;

        /// <summary>
        ///     The direction indices sorted by the distance along that direction. This is not set by defualt. 
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
        ///     The direction indices sorted by the distance along that direction. This is not set by defualt. 
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
        ///     The sorted dimensions. This is not set by defualt. 
        /// </summary>
        public IList<double> SortedDimensions
        {
            get
            {
                if (_sortedDimensions == null)
                    SetSortedDirections();
                return _sortedDimensions;
            }
        }
        private IList<double> _sortedDimensions;

        /// <summary>
        /// Sorts the directions by the distance along that direction. Smallest to largest.
        /// </summary>
        private void SetSortedDirections()
        {
            var dimensions = new Dictionary<int, double>
            {
                {0, Dimensions[0]},
                {1, Dimensions[1]},
                {2, Dimensions[2]}
            };

            // Order by values. Use LINQ to specify sorting by value.
            var sortedDimensions = from pair in dimensions
                                   orderby pair.Value ascending
                                   select pair;

            //Set the sorted lists
            _sortedDirectionIndicesByLength = sortedDimensions.Select(pair => pair.Key).ToList();
            _sortedDirectionsByLength = new List<Vector3>();
            _sortedDimensions = new List<double>();
            foreach (var index in _sortedDirectionIndicesByLength)
            {
                _sortedDirectionsByLength.Add(Directions[index]);
                _sortedDimensions.Add(Dimensions[index]);
            }
        }
        #endregion
        #endregion


        public BoundingBox Copy()
        {
            var copy = new BoundingBox(this.Dimensions, (Vector3[])this.Directions.Clone(),
               this.MinPlaneDistanceToOrigin);

            //Recreate the solid representation if one existing in the original
            if (_tessellatedSolid != null) copy._tessellatedSolid = _tessellatedSolid;
            if (!this.center.IsNull()) copy.center = this.center;
            if (this.corners != null) copy.corners = (Vector3[])this.corners.Clone();
            if (this.PointsOnFaces != null)
            {
                copy.PointsOnFaces = new List<Vertex>[8];
                for (int i = 0; i < 8; i++)
                    copy.PointsOnFaces[i] = new List<Vertex>(this.PointsOnFaces[i]);
            }
            return copy;
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
            var positiveFace = face > 0;
            var direction = Math.Abs((int)face) - 1;
            if (positiveFace)
                Dimensions = Dimensions + Vector3.UnitVector(direction) * distance;
            else MinPlaneDistanceToOrigin = MinPlaneDistanceToOrigin - Vector3.UnitVector(direction) * distance;
            if (PointsOnFaces != null)
            {
                //direction1-low,
                ///     direction1-high, direction2-low, direction2-high, direction3-low, direction3-high.
                var vertexOnPlaneIndex = 2 * direction + (positiveFace ? 1 : 0);
                PointsOnFaces[vertexOnPlaneIndex].Clear();
            }
            ResetLazyFields();
        }
        private void ResetLazyFields()
        {
            center = Vector3.Null;
            this.corners = null;
            this._sortedDimensions = null;
            this._sortedDirectionIndicesByLength = null;
            this._sortedDirectionsByLength = null;
            this._tessellatedSolid = null;
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
        ///     The point pairs that define the bounding rectangle limits
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