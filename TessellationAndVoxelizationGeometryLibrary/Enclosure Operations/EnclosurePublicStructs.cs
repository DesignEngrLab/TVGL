// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Matt Campbell
// Created          : 02-27-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 02-12-2015
// ***********************************************************************
// <copyright file="BoundingBox.cs" company="">
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
    /// The BoundingBox struct is a simple structure for representing an arbitrarily oriented box
    /// or 3D prismatic rectangle. It simply includes the orientation as three unit vectors in 
    /// "Directions", the extreme vertices, and the volume.
    /// </summary>
    public class BoundingBox
    {
        /// <summary>
        /// The volume of the bounding box.
        /// </summary>
        public double Volume;

        /// <summary>
        /// The dimensions of the bounding box. The 3 values correspond to the 3 direction.
        /// </summary>
        public double[] Dimensions;

        /// <summary>
        /// The PointsOnFaces is an array of 6 lists which are vertices of the tessellated solid that are on the faces
        /// of the bounding box. These are not the corners of the bounding box. They are in the order of direction1-low,
        /// direction1-high, direction2-low, direction2-high, direction3-low, direction3-high.
        /// </summary>
        public List<Vertex>[] PointsOnFaces;

        /// <summary>
        /// The Directions normal are the three unit vectors that describe the orientation of the box.
        /// </summary>
        public double[][] Directions;


        public BoundingBox()
        {
        }

        /// <summary>
        /// The corner points
        /// </summary>
        public Point[] CornerVertices;


        /// <summary>
        /// Initializes a new instance of the <see cref="BoundingBox"/> class.
        /// </summary>
        /// <param name="volume">The volume.</param>
        /// <param name="extremeVertices">The extreme vertices.</param>
        /// <param name="directions"></param>
        internal BoundingBox(double[] dimensions,double[][] directions, List<Vertex>[] pointsOnFaces)
        {
            Dimensions = dimensions;
            Volume = dimensions[0]*dimensions[1]*dimensions[2];
            if (directions!=null)
            Directions = directions.Select(d=>d.normalize()).ToArray();
            PointsOnFaces = pointsOnFaces;
            if (pointsOnFaces == null) return;
            //Find Corners
            CornerVertices = new Point[8] ;
            var normalMatrix = new[,] {{Directions[0][0],Directions[1][0],Directions[2][0]}, 
                                        {Directions[0][1],Directions[1][1],Directions[2][1]},
                                        {Directions[0][2],Directions[1][2],Directions[2][2]}};
            var count = 0;
            for (var i = 0; i < 2; i++)
            {
                var tempVect = normalMatrix.transpose().multiply(PointsOnFaces[i][0].Position);
                var xPrime = tempVect[0];
                for (var j = 0; j < 2; j++)
                {
                    tempVect = normalMatrix.transpose().multiply(PointsOnFaces[j + 2][0].Position);
                    var yPrime = tempVect[1];
                    for (var k = 0; k < 2; k++)
                    {
                        tempVect = normalMatrix.transpose().multiply(PointsOnFaces[k + 4][0].Position);
                        var zPrime = tempVect[2];
                        var offAxisPosition = new[] {xPrime, yPrime, zPrime };
                        //Rotate back into primary coordinates
                        var position = normalMatrix.multiply(offAxisPosition);
                        CornerVertices[count] = new Point(position);
                        count++;
                    }
                }
            }
        }
    }

    

    /// <summary>
    /// Bounding rectangle information based on area and point pairs.
    /// </summary>
    public struct BoundingRectangle
    {
        /// <summary>
        /// The Area of the bounding box.
        /// </summary>
        public double Area;

        /// <summary>
        /// The point pairs that define the bounding rectangle limits
        /// </summary>
        public List<Point>[] PointsOnSides;
        
        /// <summary>
        /// Vector directions of length and width of rectangle
        /// </summary>
        public double[][] Directions;
        public double[] Dimensions;
    }


    /// <summary>
    /// Public circle structure, given a center point and radius
    /// </summary>
    public struct BoundingCircle
    {
        /// <summary>
        /// Center Point of circle
        /// </summary>
        public Point Center;

        /// <summary>
        /// Radius of circle
        /// </summary>
        public double Radius;

        /// <summary>
        /// Area of circle
        /// </summary>
        public double Area;

        /// <summary>
        /// Circumference of circle
        /// </summary>
        public double Circumference;

        /// <summary>
        /// Creates a circle, given a radius. Center point is optional
        /// </summary>
        /// <param name="radius"></param>
        /// <param name="center"></param>
        public BoundingCircle(double radius, Point center = null)
        {
            Center = center;
            Radius = radius;
            Area = Math.PI * radius * radius;
            Circumference = 2 * Math.PI * radius;
        }
    }
    public struct BoundingCylder
    {
        public double[] Axis;
        public BoundingCircle CenterCircle;
        public double HalfLength;
        public double Volume;
    }
}
