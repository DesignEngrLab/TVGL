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
using StarMathLib;

namespace TVGL
{
    /// <summary>
    ///     The BoundingBox struct is a simple structure for representing an arbitrarily oriented box
    ///     or 3D prismatic rectangle. It simply includes the orientation as three unit vectors in
    ///     "Directions2D", the extreme vertices, and the volume.
    /// </summary>
    public struct BoundingBox
    {
        /// <summary>
        ///     The volume of the bounding box.
        /// </summary>
        public double Volume;

        /// <summary>
        ///     The dimensions of the bounding box. The 3 values correspond to the 3 direction.
        ///     If this was a bounding box along a given direction, the first dimension will
        ///     correspond with the distance along that direction.
        /// </summary>
        public double[] Dimensions;

        /// <summary>
        ///     The PointsOnFaces is an array of 6 lists which are vertices of the tessellated solid that are on the faces
        ///     of the bounding box. These are not the corners of the bounding box. They are in the order of direction1-low,
        ///     direction1-high, direction2-low, direction2-high, direction3-low, direction3-high.
        /// </summary>
        public List<Vertex>[] PointsOnFaces;

        /// <summary>
        ///     The Directions normal are the three unit vectors that describe the orientation of the box.
        ///     If this was a bounding box along a given direction, the first direction will
        ///     correspond with that direction.
        /// </summary>
        public double[][] Directions;

        /// <summary>
        ///     The corner points
        /// </summary>
        public Vertex[] CornerVertices;

        /// <summary>
        ///     The center point
        /// </summary>
        public Vertex Center;

        /// <summary>
        ///     The Solid Representation of the bounding box. This is not set by defualt. 
        /// </summary>
        public TessellatedSolid SolidRepresentation;

        /// <summary>
        ///     Sets the Solid Representation of the bounding box
        /// </summary>
        public void SetSolidRepresentation()
        {
            if (CornerVertices == null || !CornerVertices.Any())
            {
                SetCornerVertices();
            }
            SolidRepresentation = Extrude.FromLoops(new List<List<Vertex>>() {CornerVertices.Take(4).ToList()}, Directions[2], Dimensions[2]);
        }

        private IList<int> _sortedDirectionIndicesByLength;
        /// <summary>
        ///     The direction indices sorted by the distance along that direction. This is not set by defualt. 
        /// </summary>
        public IList<int> SortedDirectionIndicesByLength
        {
            get
            {
                if (_sortedDirectionsListsHaveBeenSet) return _sortedDirectionIndicesByLength;
                //Else
                SetSortedDirections();
                return _sortedDirectionIndicesByLength;
            }
        }

        private IList<double[]> _sortedDirectionsByLength;
        /// <summary>
        ///     The direction indices sorted by the distance along that direction. This is not set by defualt. 
        /// </summary>
        public IList<double[]> SortedDirectionsByLength
        {
            get
            {
                if (_sortedDirectionsListsHaveBeenSet) return _sortedDirectionsByLength;
                //Else
                SetSortedDirections();
                return _sortedDirectionsByLength;
            }
        }

        private IList<double> _sortedDimensions;
        /// <summary>
        ///     The sorted dimensions. This is not set by defualt. 
        /// </summary>
        public IList<double> SortedDimensions
        {
            get
            {
                if (_sortedDirectionsListsHaveBeenSet) return _sortedDimensions;
                //Else
                SetSortedDirections();
                return _sortedDimensions;
            }
        }

        /// <summary>
        /// If false, need to call SetSortedDirections() to get above three properties.
        /// Default for booleans is false.
        /// </summary>
        private bool _sortedDirectionsListsHaveBeenSet;

        /// <summary>
        /// Sorts the directions by the distance along that direction. Smallest to largest.
        /// </summary>
        public void SetSortedDirections()
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
            _sortedDirectionsByLength = new List<double[]>();
            _sortedDimensions = new List<double>();
            foreach (var index in _sortedDirectionIndicesByLength)
            {
                _sortedDirectionsByLength.Add(Directions[index]);
                _sortedDimensions.Add(Dimensions[index]);
            }

            _sortedDirectionsListsHaveBeenSet = true;
        }

        /// <summary>
        ///     Adds the corner vertices (actually 3d points) to the bounding box
        /// </summary>
        /// <returns>BoundingBox.</returns>
        public void SetCornerVertices()
        {
            ////////////////////////////////////////
            //First, get the bottom corner.
            ////////////////////////////////////////

            //Collect all the points on faces
            var allPointsOnFaces = new List<Vertex>();
            foreach (var setOfPoints in PointsOnFaces)
            {
                allPointsOnFaces.AddRange(setOfPoints);
            }

            if(!allPointsOnFaces.Any()) throw new Exception("Must set the points on the faces prior to setting the corner vertices " +
                                                            "(Or set the corner vertices with the convex hull");
            SetCornerVertices(allPointsOnFaces);
        }


        /// <summary>
        ///     Adds the corner vertices (actually 3d points) to the bounding box
        ///     This method is used when the face vertices are not known.
        /// </summary>
        /// <returns>BoundingBox.</returns>
        public void SetCornerVertices(List<Vertex> verticesOfInterest )
        {
            if (CornerVertices != null) return;
            var cornerVertices = new Vertex[8];

            //Get the low extreme vertices along each direction
            List<Vertex> vLows, vHighs;
            MinimumEnclosure.GetLengthAndExtremeVertices(Directions[0], verticesOfInterest, out vLows, out vHighs);
            var v0 = new Vertex(vLows.First().Position);
            MinimumEnclosure.GetLengthAndExtremeVertices(Directions[1], verticesOfInterest, out vLows, out vHighs);
            var v1 = new Vertex(vLows.First().Position);
            MinimumEnclosure.GetLengthAndExtremeVertices(Directions[2], verticesOfInterest, out vLows, out vHighs);
            var v2 = new Vertex(vLows.First().Position);

            //Start with v0 and move along direction[1] by projection
            var vector0To1 = v1.Position.subtract(v0.Position);
            var projectionOntoD1 = Directions[1].multiply(Directions[1].dotProduct(vector0To1));
            var v4 = v0.Position.add(projectionOntoD1);

            //Move along direction[2] by projection
            var vector4To2 = v2.Position.subtract(v4);
            var projectionOntoD2 = Directions[2].multiply(Directions[2].dotProduct(vector4To2));
            var bottomCorner = new Vertex(v4.add(projectionOntoD2));

            //Double Check to make sure it is the bottom corner
            verticesOfInterest.Add(bottomCorner);
            MinimumEnclosure.GetLengthAndExtremeVertices(Directions[0], verticesOfInterest, out vLows, out vHighs);
            if (!vLows.Contains(bottomCorner)) throw new Exception("Error in defining bottom corner");
            MinimumEnclosure.GetLengthAndExtremeVertices(Directions[1], verticesOfInterest, out vLows, out vHighs);
            if (!vLows.Contains(bottomCorner)) throw new Exception("Error in defining bottom corner");
            MinimumEnclosure.GetLengthAndExtremeVertices(Directions[2], verticesOfInterest, out vLows, out vHighs);
            if (!vLows.Contains(bottomCorner)) throw new Exception("Error in defining bottom corner");

            //Create the vertices that make up the box and add them to the corner vertices array
            for (var i = 0; i < 2; i++)
            {
                var d0Vector = i == 0 ? new[] { 0.0, 0.0, 0.0 } : Directions[0].multiply(Dimensions[0]);
                for (var j = 0; j < 2; j++)
                {
                    var d1Vector = j == 0 ? new[] { 0.0, 0.0, 0.0 } : Directions[1].multiply(Dimensions[1]);
                    for (var k = 0; k < 2; k++)
                    {
                        var d2Vector = k == 0 ? new[] { 0.0, 0.0, 0.0 } : Directions[2].multiply(Dimensions[2]);
                        var newVertex = new Vertex(bottomCorner.Position.add(d0Vector).add(d1Vector).add(d2Vector));

                        //
                        var b = k == 0 ? 0 : 4;
                        if (j == 0)
                        {
                            if (i == 0) cornerVertices[b] = newVertex; //i == 0 && j== 0 && k == 0 or 1 
                            else cornerVertices[b + 1] = newVertex; //i == 1 && j == 0 && k == 0 or 1 
                        }
                        else
                        {
                            if (i == 0) cornerVertices[b + 3] = newVertex; //i == 0 && j== 1 && k == 0 or 1 
                            else cornerVertices[b + 2] = newVertex; //i == 1 && j == 1 && k == 0 or 1 
                        }
                    }
                }
            }

            //Add in the center
            var centerPosition = new[] { 0.0, 0.0, 0.0 };
            foreach (var vertex in cornerVertices)
            {
                centerPosition[0] += vertex.Position[0];
                centerPosition[1] += vertex.Position[1];
                centerPosition[2] += vertex.Position[2];
            }
            centerPosition[0] = centerPosition[0] / cornerVertices.Count();
            centerPosition[1] = centerPosition[1] / cornerVertices.Count();
            centerPosition[2] = centerPosition[2] / cornerVertices.Count();

            CornerVertices = cornerVertices;
            Center = new Vertex(centerPosition);
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
        public List<Point> CornerPoints { get; private set; }

        /// <summary>
        ///     The point pairs that define the bounding rectangle limits
        /// </summary>
        public List<Point>[] PointsOnSides;

        /// <summary>
        ///     Vector directions of length and width of rectangle
        /// </summary>
        public double[][] Directions2D;

        /// <summary>
        ///     Length and Width of Bounding Rectangle
        /// </summary>
        public double[] Dimensions;

        /// <summary>
        ///     2D Center Position of the Bounding Rectangle
        /// </summary>
        public double[] CenterPosition { get; private set; }

        /// <summary>
        /// Sets the corner points and center position for the bounding rectangle
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void SetCornerPoints()
        {
            var cornerPoints = new Point[4];
            var dir0 = new double[] {Directions2D[0][0], Directions2D[0][1], 0};
            var dir1 = new double[] { Directions2D[1][0], Directions2D[1][1], 0 };
            var extremePoints = new List<Point>();
            foreach (var pair in PointsOnSides)
            {
                extremePoints.AddRange(pair);
            }

            //Lower left point
            List<Point> bottomPoints, topPoints;
            MinimumEnclosure.GetLengthAndExtremePoints(Directions2D[0], extremePoints, out bottomPoints, out topPoints);
            var bp = bottomPoints.First().Position2D;
            var p0 = new [] {bp[0], bp[1], 0};
            MinimumEnclosure.GetLengthAndExtremePoints(Directions2D[1], extremePoints, out bottomPoints, out topPoints);
            bp = bottomPoints.First().Position2D;
            var p1 = new[] { bp[0], bp[1], 0 };

            //Start with v0 and move along direction[1] by projection
            var vector0To1 = p1.subtract(p0);
            var projectionOntoD1 = dir1.multiply(dir1.dotProduct(vector0To1));
            var p2 = p0.add(projectionOntoD1);
            var bottomCorner = new Point(p2);

            //Double Check to make sure it is the bottom corner
            extremePoints.Add(bottomCorner);
            MinimumEnclosure.GetLengthAndExtremePoints(Directions2D[0], extremePoints, out bottomPoints, out topPoints);
            if (!bottomPoints.Contains(bottomCorner)) throw new Exception("Error in defining bottom corner");
            MinimumEnclosure.GetLengthAndExtremePoints(Directions2D[1], extremePoints, out bottomPoints, out topPoints);
            if (!bottomPoints.Contains(bottomCorner)) throw new Exception("Error in defining bottom corner");

            //Create the vertices that make up the box and add them to the corner vertices array
            for (var i = 0; i < 2; i++)
            {
                var d0Vector = i == 0 ? new[] { 0.0, 0.0, 0.0 } : dir0.multiply(Dimensions[0]);
                for (var j = 0; j < 2; j++)
                {
                    var d1Vector = j == 0 ? new[] { 0.0, 0.0, 0.0 } : dir1.multiply(Dimensions[1]);
                    var newPoint = new Point(bottomCorner.Position.add(d0Vector).add(d1Vector));
                    //Put the points in the correct position to be ordered CCW, starting with the bottom corner
                    if (i == 0)
                    {
                        if (j == 0) cornerPoints[0] = newPoint; // i == 0, j == 0
                        else cornerPoints[1] = newPoint; // i == 0, j == 1
                    }
                    else if (j == 1) cornerPoints[2] = newPoint; //i == 1, j ==1
                    else cornerPoints[3] = newPoint;//i == 1, j == 0
                }
            }

            //Add in the center
            var centerPosition = new[] { 0.0, 0.0 };
            foreach (var vertex in cornerPoints)
            {
                centerPosition[0] += vertex.Position[0];
                centerPosition[1] += vertex.Position[1];
            }
            centerPosition[0] = centerPosition[0] / cornerPoints.Count();
            centerPosition[1] = centerPosition[1] / cornerPoints.Count();

            //Check to make sure the points are ordered correctly (within 1 %)
            if(!MiscFunctions.AreaOfPolygon(cornerPoints).IsPracticallySame(Area, 0.01*Area)) throw new Exception("Points are ordered incorrectly");

            CornerPoints = new List<Point>(cornerPoints);
            CenterPosition = centerPosition;
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
        public Point Center;

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
        public BoundingCircle(double radius, Point center = null)
        {
            Center = center;
            Radius = radius;
            Area = Math.PI*radius*radius;
            Circumference = Constants.TwoPi*radius;
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
        public double[] Axis;

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