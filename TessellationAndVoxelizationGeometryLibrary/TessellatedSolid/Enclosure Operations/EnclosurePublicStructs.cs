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


namespace TVGL
{
    /// <summary>
    ///     The BoundingBox is a simple structure for representing an arbitrarily oriented box
    ///     or 3D prismatic rectangle. It simply includes the orientation as three unit vectors in
    ///     "Directions2D", the extreme vertices, and the volume.
    /// </summary>
    public class BoundingBox
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
        public Vector2 Dimensions;

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
        public Vector3[] Directions;

        /// <summary>
        ///     Corner vertices are ordered as follows, where - = low and + = high along directions 0, 1, and 2 respectively.
        ///     [0] = +++, [1] = +-+, [2] = +--, [3] = ++-, [4] = -++, [5] = --+, [6] = ---, [7] = -+-
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

        private IList<Vector3> _sortedDirectionsByLength;
        /// <summary>
        ///     The direction indices sorted by the distance along that direction. This is not set by defualt. 
        /// </summary>
        public IList<Vector3> SortedDirectionsByLength
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
            _sortedDirectionsByLength = new List<Vector2>();
            _sortedDimensions = new List<double>();
            foreach (var index in _sortedDirectionIndicesByLength)
            {
                _sortedDirectionsByLength.Add(Directions[index]);
                _sortedDimensions.Add(Dimensions[index]);
            }

            _sortedDirectionsListsHaveBeenSet = true;
        }

        /// <summary>
        ///     Adds the corner vertices to the bounding box.
        ///     The dimensions are also updated, in the event that the points of faces were altered (replaced or shifted).
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
        ///     Adds the corner vertices to the bounding box.
        ///     This method is used when the face vertices are not known.
        ///     The dimensions are also updated, in the event that the points of faces were altered (replaced or shifted).
        /// </summary>
        /// <returns>BoundingBox.</returns>
        public void SetCornerVertices(List<Vertex> verticesOfInterest)
        {
            var cornerVertices = new Vertex[8];

            //Get the low extreme vertices along each direction
            List<Vertex> vLows, vHighs;
            Dimensions[0] = MinimumEnclosure.GetLengthAndExtremeVertices(Directions[0], verticesOfInterest, out vLows, out vHighs);
            var v0 = new Vertex(vLows.First().Coordinates);
            Dimensions[1] = MinimumEnclosure.GetLengthAndExtremeVertices(Directions[1], verticesOfInterest, out vLows, out vHighs);
            var v1 = new Vertex(vLows.First().Coordinates);
            Dimensions[2] = MinimumEnclosure.GetLengthAndExtremeVertices(Directions[2], verticesOfInterest, out vLows, out vHighs);
            var v2 = new Vertex(vLows.First().Coordinates);

            //Start with v0 and move along direction[1] by projection
            var vector0To1 = v1.Coordinates.Subtract(v0.Coordinates);
            var projectionOntoD1 = Directions[1] * Directions[1].Dot(vector0To1);
            var v4 = v0.Coordinates + projectionOntoD1;

            //Move along direction[2] by projection
            var vector4To2 = v2.Coordinates.Subtract(v4);
            var projectionOntoD2 = Directions[2] * Directions[2].Dot(vector4To2);
            var bottomCorner = new Vertex(v4 + projectionOntoD2);

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
                var d0Vector = i == 0 ? new[] { 0.0, 0.0, 0.0 } : Directions[0] * Dimensions[0];
                for (var j = 0; j < 2; j++)
                {
                    var d1Vector = j == 0 ? new[] { 0.0, 0.0, 0.0 } : Directions[1] * Dimensions[1];
                    for (var k = 0; k < 2; k++)
                    {
                        var d2Vector = k == 0 ? new[] { 0.0, 0.0, 0.0 } : Directions[2] * Dimensions[2];
                        var newVertex = new Vertex(bottomCorner.Coordinates + d0Vector + d1Vector + d2Vector);

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
                centerPosition[0] += vertex.Coordinates[0];
                centerPosition[1] += vertex.Coordinates[1];
                centerPosition[2] += vertex.Coordinates[2];
            }
            centerPosition[0] = centerPosition[0] / cornerVertices.Count();
            centerPosition[1] = centerPosition[1] / cornerVertices.Count();
            centerPosition[2] = centerPosition[2] / cornerVertices.Count();

            CornerVertices = cornerVertices;
            Center = new Vertex(centerPosition);
        }

        public static BoundingBox Copy(BoundingBox original)
        {
            var copy = new BoundingBox
            {
                Center = original.Center.Copy(),
                Volume = original.Volume,
                Dimensions = new [] { original.Dimensions[0], original.Dimensions[1], original.Dimensions[2]},
                Directions = original.Directions, //If these change, then the copy is useless anyways
                PointsOnFaces = original.PointsOnFaces, //These are reference vertices, so they should not be copied
                CornerVertices = new Vertex[8]
            };
            //Recreated the corner vertices
            for(var i =0; i < 8; i++)
            {
                copy.CornerVertices[i] = original.CornerVertices[i].Copy();
            }
            //Recreate the solid representation if one existing in the original
            if(original.SolidRepresentation != null) copy.SetSolidRepresentation();
            return copy;
        }

        public static BoundingBox ExtendAlongDirection(BoundingBox original, Vector2 direction, double distance)
        {
            int sign = 0;
            var updateIndex = -1;
            for (var i = 0; i <= 2; i++)
            {
                var dot = direction.Dot(original.Directions[i]);
                sign = Math.Sign(dot);
                if (Math.Abs(dot).IsPracticallySame(1.0, Constants.SameFaceNormalDotTolerance))
                {
                    updateIndex = i;
                    break;
                }
            }
            if (updateIndex == -1) throw new Exception("BoundingBox may only be extended along one of its three defining directions.");

            var dimensions = new[] { original.Dimensions[0], original.Dimensions[1], original.Dimensions[2] };
            dimensions[updateIndex] += distance;
            var volume = dimensions[0] * dimensions[1] * dimensions[2];

            var result = new BoundingBox
            {
                Center = new Vertex(original.Center.Coordinates + (direction * distance / 2)),
                Dimensions = dimensions,
                Volume = volume,
                Directions = original.Directions, //If these change, then the copy is useless anyways
                PointsOnFaces = null, // these reference vertices are no longer valid.
                CornerVertices = new Vertex[8]
            };   

            //Recreate the corner vertices  
            for (var i = 0; i < 8; i++)
            {
                result.CornerVertices[i] = original.CornerVertices[i].Copy();
            }

            //And then move the vertices furthest along the direction by the given distance
            var vectorOffset = direction * distance;
            //Corner vertices are ordered as follows, where - = low and + = high along directions 0, 1, and 2 respectively.
            //[0] = +++, [1] = +-+, [2] = +--, [3] = ++-, [4] = -++, [5] = --+, [6] = ---, [7] = -+-
            int[] indicesToUpdate;
            if(updateIndex == 0)
            {
                if (sign == 1) indicesToUpdate = new int[] { 0, 1, 2, 3 };
                else indicesToUpdate = new int[] { 4, 5, 6, 7 };
            } 
            else if(updateIndex == 1)
            {
                if (sign == 1) indicesToUpdate = new int[] { 0, 3, 4, 7 };
                else indicesToUpdate = new int[] { 1, 2, 5, 6 };
            }
            else
            {
                if (sign == 1) indicesToUpdate = new int[] { 0, 1, 4, 5 };
                else indicesToUpdate = new int[] { 2, 3, 6, 7 };
            }           
            foreach (var i in indicesToUpdate)
            {
                result.CornerVertices[i] = new Vertex(result.CornerVertices[i].Coordinates + vectorOffset);
            }        

            //Recreate the solid representation if one existing in the original
            if (original.SolidRepresentation != null) result.SetSolidRepresentation();
            return result;
        }

        //Note: Corner vertices must be ordered correctly. See below where - = low and + = high along directions 0, 1, and 2 respectively.
        // [0] = +++, [1] = +-+, [2] = +--, [3] = ++-, [4] = -++, [5] = --+, [6] = ---, [7] = -+-
        public static BoundingBox FromCornerVertices(Vector2[] directions, Vertex[] cornerVertices, bool areVerticesInCorrectOrder)
        {
            if(!areVerticesInCorrectOrder) throw new Exception("Not implemented exception. Vertices must be in correct order for OBB");
            if(cornerVertices.Length != 8) throw new Exception("Must set Bounding Box using eight corner vertices in correct order");

            var dimensions = new[] { 0.0, 0.0, 0.0 };
            dimensions[0] = MinimumEnclosure.GetLengthAndExtremeVertices(directions[0], cornerVertices, out _, out _);
            dimensions[1] = MinimumEnclosure.GetLengthAndExtremeVertices(directions[1], cornerVertices, out _, out  _);
            dimensions[2] = MinimumEnclosure.GetLengthAndExtremeVertices(directions[2], cornerVertices, out _, out _);

            //Add in the center
            var centerPosition = new[] { 0.0, 0.0, 0.0 };
            centerPosition = cornerVertices.Aggregate(centerPosition, (c, v) => c + v.Coordinates)
                .Divide(cornerVertices.Count());
            
            return new BoundingBox
            {
                Center = new Vertex(centerPosition),
                Volume = dimensions[0] * dimensions[1] * dimensions[2],
                Dimensions = dimensions,
                Directions = directions, 
                PointsOnFaces = null, 
                CornerVertices = cornerVertices
            };
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
        public List<Point>[] PointsOnSides;

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
            var p1 = new Vector2(v1Max + v2Max);
            var p2 = new Vector2(v1Min + v2Max);
            var p3 = new Vector2(v1Min + v2Min);
            var p4 = new Vector2(v1Max + v2Min);
            CornerPoints = new[] { p1, p2, p3, p4 };
            var areaCheck = MiscFunctions.AreaOfPolygon(CornerPoints);
            if (areaCheck < 0.0)
            {
                CornerPoints = new[] { p4, p3, p2, p1 };
                areaCheck = -areaCheck;
            }

            //Add in the center
            var centerPosition = new[] { 0.0, 0.0 };
            foreach (var vertex in CornerPoints)
            {
                centerPosition[0] += vertex.X;
                centerPosition[1] += vertex.Y;
            }
            centerPosition[0] = centerPosition[0] / 4;
            centerPosition[1] = centerPosition[1] / 4;          

            //Check to make sure the points are ordered correctly (within 1 %)
            if(!areaCheck.IsPracticallySame(Area, 0.01*Area))
                throw new Exception("Points are ordered incorrectly");
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
        public Vector2 Axis;

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