// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 04-18-2016
//
// Last Modified By : Design Engineering Lab
// Last Modified On : 04-18-2016
// ***********************************************************************
// <copyright file="FaceWithScores.cs" company="Design Engineering Lab">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System.Collections.Generic;

namespace TVGL
{
    /// <summary>
    ///     Class Primitive_Classification.
    /// </summary>
    public static partial class Primitive_Classification
    {
        /// <summary>
        ///     Class FaceWithScores.
        /// </summary>
        internal class FaceWithScores
        {
            /// <summary>
            ///     The face
            /// </summary>
            internal PolygonalFace Face;

            /// <summary>
            ///     Initializes a new instance of the <see cref="FaceWithScores" /> class.
            /// </summary>
            /// <param name="face">The face.</param>
            internal FaceWithScores(PolygonalFace face)
            {
                Face = face;
            }


            ////////////////////////////////////////////////////////////////////
            /// <summary>
            ///     gets the Area * edge ratio of the face. Edge ratio is the the length of longest
            ///     edge to the length of the shortest edge
            /// </summary>
            /// <value>AreaRatio</value>
            internal double AreaRatio { get; set; }

            /// <summary>
            ///     Dictionary with possible face category obtained from different
            ///     combinatons  of its edges' groups
            /// </summary>
            /// <value>Face Category</value>
            internal Dictionary<PrimitiveSurfaceType, double> FaceCat { get; set; }

            /// <summary>
            ///     Dictionary with faceCat on its key and the combinaton which makes the category on its value
            /// </summary>
            /// <value>Category to combination</value>
            internal Dictionary<PrimitiveSurfaceType, int[]> CatToCom { get; set; }

            /// <summary>
            ///     Dictionary with edge combinations on key and edges obtained from face rules on its value
            /// </summary>
            /// <value>Combination to Edges</value>
            internal Dictionary<int[], Edge[]> ComToEdge { get; set; }

            /// <summary>
            ///     Dictionary with faceCat on key and edges lead to the category on its value
            /// </summary>
            /// <value>Edges lead to desired category</value>
            internal Dictionary<PrimitiveSurfaceType, List<Edge>> CatToELDC { get; set; }
        }

        /// <summary>
        ///     Class EdgeWithScores.
        /// </summary>
        internal class EdgeWithScores
        {
            /// <summary>
            ///     The edge
            /// </summary>
            internal Edge Edge;

            /// <summary>
            ///     Initializes a new instance of the <see cref="EdgeWithScores" /> class.
            /// </summary>
            /// <param name="edge">The edge.</param>
            internal EdgeWithScores(Edge edge)
            {
                Edge = edge;
            }

            /// <summary>
            ///     A dictionary with 5 groups of Flat, Cylinder, Sphere, Flat to Curve and Sharp Edge and their
            ///     probabilities.
            /// </summary>
            /// <value>Group Probability</value>
            internal Dictionary<int, double> CatProb { get; set; }
        }
    }
}