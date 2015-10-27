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
using StarMathLib;


namespace TVGL
{
    /// <summary>
    /// The BoundingBox struct is a simple structure for representing an arbitrarily oriented box
    /// or 3D prismatic rectangle. It simply includes the orientation as three unit vectors in 
    /// "Directions", the extreme vertices, and the volume.
    /// </summary>
    public struct BoundingBox
    {
        /// <summary>
        /// The volume of the bounding box.
        /// </summary>
        public double Volume;
        /// <summary>
        /// The area normal along the depth on the bounding box.
        /// </summary>
        public double Area;
        /// <summary>
        /// The depth of the bounding box.
        /// </summary>
        public double Depth;
        /// <summary>
        /// The length of the bounding box / bounding rectangle.
        /// </summary>
        public double Length;
        /// <summary>
        /// The width of the bounding box / bounding rectangle..
        /// </summary>
        public double Width;
        /// <summary>
        /// The extreme vertices which are vertices of the tessellated solid that are on the faces
        /// of the bounding box. These are not the corners of the bounding box.
        /// </summary>
        public Vertex[] ExtremeVertices;

        /// <summary>
        /// The Directions normal are the three unit vectors that describe the orientation of the box.
        /// </summary>
        public double[][] Directions;

        /// <summary>
        /// The corner points
        /// </summary>
        public Vertex[] CornerVertices;

        /// <summary>
        /// Vertices that correspond to edge from rotating calipers
        /// </summary>
        public Vertex[] EdgeVertices;

        /// <summary>
        /// Vector directions of the edge from rotating calipers
        /// </summary>
        public double[] EdgeVector;

        /// <summary>
        /// Initializes a new instance of the <see cref="BoundingBox"/> class.
        /// </summary>
        /// <param name="volume">The volume.</param>
        /// <param name="extremeVertices">The extreme vertices.</param>
        /// <param name="directions"></param>
        internal BoundingBox(double depth, double area, Vertex[] extremeVertices, double[][] directions, Vertex[] edgeVertices = null)
        {
            EdgeVertices = edgeVertices;
            EdgeVector = EdgeVertices[1].Position.subtract(EdgeVertices[0].Position);
            CornerVertices = new Vertex[8];
            Area = area;
            Depth = depth;
            Volume = depth*area;
            Directions = new[] { directions[0].normalize(), directions[1].normalize(), directions[2].normalize() };
            ExtremeVertices = extremeVertices; //list of vertices in order of pairs with the directions
            var lengthVector = extremeVertices[3].Position.subtract(extremeVertices[2].Position);
            Length = Math.Abs(lengthVector.dotProduct(Directions[1]));
            var widthVector = extremeVertices[5].Position.subtract(extremeVertices[4].Position);
            Width = Math.Abs(widthVector.dotProduct(Directions[2]));
            if (Math.Abs(Area-Length*Width) > 1E-8) throw new Exception();

            //Find Corners
            var normalMatrix = new[,] {{Directions[0][0],Directions[1][0],Directions[2][0]}, 
                                        {Directions[0][1],Directions[1][1],Directions[2][1]},
                                        {Directions[0][2],Directions[1][2],Directions[2][2]}};
            var count = 0;
            for (var i = 0; i < 2; i++)
            {
                var tempVect = normalMatrix.transpose().multiply(extremeVertices[i].Position);
                var xPrime = tempVect[0];
                for (var j = 0; j < 2; j++)
                {
                    tempVect = normalMatrix.transpose().multiply(extremeVertices[j + 2].Position);
                    var yPrime = tempVect[1];
                    for (var k = 0; k < 2; k++)
                    {
                        tempVect = normalMatrix.transpose().multiply(extremeVertices[k + 4].Position);
                        var zPrime = tempVect[2];
                        var offAxisPosition = new[] {xPrime, yPrime, zPrime };
                        //Rotate back into primary coordinates
                        var position = normalMatrix.multiply(offAxisPosition);
                        CornerVertices[count] = new Vertex(position);
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
        public List<Point[]> PointPairs;

        /// <summary>
        /// The angle that this bounding box was is rotated in the xy plane. 
        /// </summary>
        public double BestAngle;

        /// <summary>
        /// Vector directions of length and width of rectangle
        /// </summary>
        public List<double[]> Directions;

        /// <summary>
        /// Vertices that correspond to edge from rotating calipers
        /// </summary>
        public Vertex[] EdgeVertices;

        /// <summary>
        /// Vector directions of the edge from rotating calipers
        /// </summary>
        public double[] EdgeVector;

        /// <summary>
        /// Initializes a new instance of the <see cref="BoundingBox"/> class.
        /// </summary>
        internal BoundingRectangle(double area, double bestAngle, List<double[]> directions, List<Point[]> pointPairs, Vertex[] edgeVertices)
        {
            Area = area;
            PointPairs = pointPairs;
            BestAngle = bestAngle;
            Directions = directions;
            EdgeVertices = edgeVertices;
            EdgeVector = EdgeVertices[1].Position.subtract(EdgeVertices[0].Position);
        }
    }
}
