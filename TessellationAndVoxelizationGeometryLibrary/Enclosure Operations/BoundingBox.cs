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
        /// Initializes a new instance of the <see cref="BoundingBox"/> class.
        /// </summary>
        /// <param name="volume">The volume.</param>
        /// <param name="extremeVertices">The extreme vertices.</param>
        /// <param name="directions"></param>
        internal BoundingBox(double volume, Vertex[] extremeVertices, double[][] directions)
        { 
            CornerVertices = new Vertex[8];
            Volume = volume;
            Directions = new[] { directions[0].normalize(), directions[1].normalize(), directions[2].normalize() };
            ExtremeVertices = extremeVertices; //list of vertices in order of pairs with the directions

            //Find Corners
            var normalMatrix = new[,] {{Directions[0][0],Directions[0][1],Directions[0][2]}, 
                                        {Directions[1][0],Directions[1][1],Directions[1][2]},
                                        {Directions[2][0],Directions[2][1],Directions[2][2]}};
            var count = 0;
            for (var i = 0; i < 2; i++)
            {
                var distance1 = extremeVertices[i].Position.dotProduct(Directions[0]);
                for (var j = 0; j < 2; j++)
                {
                    var distance2 = extremeVertices[j + 2].Position.dotProduct(Directions[1]);
                    for (var k = 0; k < 2; k++)
                    {
                        var distance3 = extremeVertices[k + 4].Position.dotProduct(Directions[2]);
                        var distance = new[] {distance1, distance2, distance3};
                        var position = distance.multiply(normalMatrix.transpose());
                       
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
        /// Initializes a new instance of the <see cref="BoundingBox"/> class.
        /// </summary>
        internal BoundingRectangle(double area, double bestAngle, List<double[]> directions, List<Point[]> pointPairs)
        {
            Area = area;
            PointPairs = pointPairs;
            BestAngle = bestAngle;
            Directions = directions;
        }
    }

}
