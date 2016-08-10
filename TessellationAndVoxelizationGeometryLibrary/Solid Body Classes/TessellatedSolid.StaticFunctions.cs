// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 02-27-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 06-21-2015
// ***********************************************************************
// <copyright file="TessellatedSolid.cs" company="Design Engineering Lab">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using MIConvexHull;
using StarMathLib;
using TVGL.IOFunctions;

namespace TVGL
{
    /// <summary>
    ///     Class TessellatedSolid.
    /// </summary>
    /// <tags>help</tags>
    /// <remarks>
    ///     This partial class file is focused on static functions that relate to Tessellated Solid.
    /// </remarks>
    public partial class TessellatedSolid
    {
        private static bool ContainsDuplicateIndices(List<int> orderedIndices)
        {
            for (var i = 0; i < orderedIndices.Count - 1; i++)
                if (orderedIndices[i] == orderedIndices[i + 1]) return true;
            return false;
        }
        #region Volume, Center and Surface Area
        /// <summary>
        /// Defines the center, the volume and the surface area.
        /// </summary>
        internal static void DefineCenterVolumeAndSurfaceArea(IList<PolygonalFace> faces, out double[] center,
            out double volume, out double surfaceArea)
        {
            surfaceArea = faces.Sum(face => face.Area);
            volume = CalculateVolume(faces, out center);
        }

        /// <summary>
        /// Find the volume of a tesselated solid with a slower method. 
        /// This method could be exteded to find partial volumes of a solid (e.g. volume between two planes)
        /// </summary>
        /// <param name="ts"></param>
        /// <returns></returns>
        private static double VolumeViaAreaDecomposition(TessellatedSolid ts)
        {
            var normal = new[] { 1.0, 0.0, 0.0 }; //Direction is irrellevant
            var stepSize = 0.01;
            var volume = 0.0;
            var areas = AreaDecomposition.Run(ts, normal, stepSize);
            //Trapezoidal approximation. This should be accurate since the lines betweens data points are linear
            for (var i = 1; i < areas.Count; i++)
            {
                var deltaX = areas[i][0] - areas[i - 1][0];
                if (deltaX < 0) throw new Exception("Error in your implementation. This should never occur");
                volume = volume + .5 * (areas[i][1] + areas[i - 1][1]) * deltaX;
            }
            return volume;
        }

        /// <summary>
        /// Find the volume of a tesselated solid.
        /// </summary>
        /// <param name="faces"></param>
        /// <param name="center"></param>
        /// <returns></returns>
        public static double CalculateVolume(IList<PolygonalFace> faces, out double[] center)
        {
            double oldVolume;
            var volume = 0.0;
            var iterations = 0;
            center = new double[3];
            var oldCenter1 = new double[3];
            var oldCenter2 = new double[3];
            do
            {
                oldVolume = volume;
                oldCenter2[0] = oldCenter1[0]; oldCenter2[1] = oldCenter1[1]; oldCenter2[2] = oldCenter1[2];
                oldCenter1[0] = center[0]; oldCenter1[1] = center[1]; oldCenter1[2] = center[2];
                volume = 0;
                center[0] = 0.0; center[1] = 0.0; center[2] = 0.0;
                foreach (var face in faces)
                {
                    if (face.Area.IsNegligible()) continue; //Ignore faces with zero area, since their Normals are not set.
                    var tetrahedronVolume = face.Area * (face.Normal.dotProduct(face.Vertices[0].Position.subtract(oldCenter1))) / 3;
                    // this is the volume of a tetrahedron from defined by the face and the origin {0,0,0}. The origin would be part of the second term
                    // in the dotproduct, "face.Normal.dotProduct(face.Vertices[0].Position.subtract(ORIGIN))", but clearly there is no need to subtract
                    // {0,0,0}. Note that the volume of the tetrahedron could be negative. This is fine as it ensures that the origin has no influence
                    // on the volume.
                    volume += tetrahedronVolume;
                    center[0] += (oldCenter1[0] + face.Vertices[0].X + face.Vertices[1].X + face.Vertices[2].X) * tetrahedronVolume / 4;
                    center[1] += (oldCenter1[1] + face.Vertices[0].Y + face.Vertices[1].Y + face.Vertices[2].Y) * tetrahedronVolume / 4;
                    center[2] += (oldCenter1[2] + face.Vertices[0].Z + face.Vertices[1].Z + face.Vertices[2].Z) * tetrahedronVolume / 4;
                    // center is found by a weighted sum of the centers of each tetrahedron. The weighted sum coordinate are collected here.
                }
                if (iterations > 10 || volume < 0) center = oldCenter1.add(oldCenter2).divide(2);
                else center = center.divide(volume);
                iterations++;
            } while (Math.Abs(oldVolume - volume) > Constants.BaseTolerance && iterations <= 20);
            return volume;
        }
        #endregion

        #region Define Inertia Tensor
        const double oneSixtieth = 1.0 / 60.0;

        private static double[,] DefineInertiaTensor(IEnumerable<PolygonalFace> Faces, double[] Center, double Volume)
        {
            var matrixA = StarMath.makeZero(3, 3);
            var matrixCtotal = StarMath.makeZero(3, 3);
            var canonicalMatrix = new[,]
            {
                {oneSixtieth, 0.5*oneSixtieth, 0.5*oneSixtieth},
                {0.5*oneSixtieth, oneSixtieth, 0.5*oneSixtieth}, {0.5*oneSixtieth, 0.5*oneSixtieth, oneSixtieth}
            };
            foreach (var face in Faces)
            {
                matrixA.SetRow(0,
                    new[]
                    {
                        face.Vertices[0].Position[0] - Center[0], face.Vertices[0].Position[1] - Center[1],
                        face.Vertices[0].Position[2] - Center[2]
                    });
                matrixA.SetRow(1,
                    new[]
                    {
                        face.Vertices[1].Position[0] - Center[0], face.Vertices[1].Position[1] - Center[1],
                        face.Vertices[1].Position[2] - Center[2]
                    });
                matrixA.SetRow(2,
                    new[]
                    {
                        face.Vertices[2].Position[0] - Center[0], face.Vertices[2].Position[1] - Center[1],
                        face.Vertices[2].Position[2] - Center[2]
                    });

                var matrixC = matrixA.transpose().multiply(canonicalMatrix);
                matrixC = matrixC.multiply(matrixA).multiply(matrixA.determinant());
                matrixCtotal = matrixCtotal.add(matrixC);
            }

            var translateMatrix = new double[,] { { 0 }, { 0 }, { 0 } };
            var matrixCprime =
                translateMatrix.multiply(-1)
                    .multiply(translateMatrix.transpose())
                    .add(translateMatrix.multiply(translateMatrix.multiply(-1).transpose()))
                    .add(translateMatrix.multiply(-1).multiply(translateMatrix.multiply(-1).transpose()))
                    .multiply(Volume);
            matrixCprime = matrixCprime.add(matrixCtotal);
            var result =
                StarMath.makeIdentity(3).multiply(matrixCprime[0, 0] + matrixCprime[1, 1] + matrixCprime[2, 2]);
            return result.subtract(matrixCprime);
        }
        #endregion

        private static void RemoveReferencesToVertex(Vertex vertex)
        {
            foreach (var face in vertex.Faces)
            {
                var index = face.Vertices.IndexOf(vertex);
                if (index >= 0) face.Vertices.RemoveAt(index);
            }
            foreach (var edge in vertex.Edges)
            {
                if (vertex == edge.To) edge.To = null;
                if (vertex == edge.From) edge.From = null;
            }
        }
    }
}