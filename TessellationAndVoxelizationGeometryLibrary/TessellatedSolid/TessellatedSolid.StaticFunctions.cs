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
using TVGL.Numerics;

namespace TVGL
{
    /// <summary>
    ///     Class TessellatedSolid.
    /// </summary>
    /// <tags>help</tags>
    /// <remarks>
    ///     This partial class file is focused on static functions that relate to Tessellated Solid.
    /// </remarks>
    public partial class TessellatedSolid : Solid
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
        internal static void DefineCenterVolumeAndSurfaceArea(IList<PolygonalFace> faces, out Vector3 center,
            out double volume, out double surfaceArea)
        {
            surfaceArea = faces.Sum(face => face.Area);
            volume = CalculateVolume(faces, out center);
        }

        /// <summary>
        /// Find the volume of a tesselated solid.
        /// </summary>
        /// <param name="faces"></param>
        /// <param name="center"></param>
        /// <returns></returns>
        public static double CalculateVolume(IList<PolygonalFace> faces, out Vector3 center)
        {
            double oldVolume;
            var volume = 0.0;
            var iterations = 0;
            Vector3 oldCenter1 = new Vector3();
            center = new Vector3();
            do
            {
                oldVolume = volume;
                var oldCenter2 = oldCenter1;
                oldCenter1 = center;
                volume = 0;
                foreach (var face in faces)
                {
                    if (face.Area.IsNegligible()) continue; //Ignore faces with zero area, since their Normals are not set.
                    var tetrahedronVolume = face.Area * (face.Normal.Dot(face.Vertices[0].Position.Subtract(oldCenter1))) / 3;
                    // this is the volume of a tetrahedron from defined by the face and the origin {0,0,0}. The origin would be part of the second term
                    // in the dotproduct, "face.Normal.Dot(face.Vertices[0].Position.Subtract(ORIGIN))", but clearly there is no need to subtract
                    // {0,0,0}. Note that the volume of the tetrahedron could be negative. This is fine as it ensures that the origin has no influence
                    // on the volume.
                    volume += tetrahedronVolume;
                    center = new Vector3(
                        (oldCenter1[0] + face.Vertices[0].X + face.Vertices[1].X + face.Vertices[2].X) * tetrahedronVolume / 4,
                        (oldCenter1[1] + face.Vertices[0].Y + face.Vertices[1].Y + face.Vertices[2].Y) * tetrahedronVolume / 4,
                        (oldCenter1[2] + face.Vertices[0].Z + face.Vertices[1].Z + face.Vertices[2].Z) * tetrahedronVolume / 4);
                    // center is found by a weighted sum of the centers of each tetrahedron. The weighted sum coordinate are collected here.
                }
                if (iterations > 10 || volume < 0) center = 0.5 * (oldCenter1 + oldCenter2);
                else center = center.Divide(volume);
                iterations++;
            } while (Math.Abs(oldVolume - volume) > Constants.BaseTolerance && iterations <= 20);
            return volume;
        }
        #endregion

        #region Define Inertia Tensor
        const double oneSixtieth = 1.0 / 60.0;


        private static double[,] DefineInertiaTensor(IEnumerable<PolygonalFace> Faces, Vector3 Center, double Volume)
        {
            //var matrixA = new double[3, 3];
            var matrixCtotal = new Matrix3x3();
            var canonicalMatrix = new Matrix3x3(oneSixtieth, 0.5 * oneSixtieth, 0.5 * oneSixtieth,
                0.5 * oneSixtieth, oneSixtieth, 0.5 * oneSixtieth,
                0.5 * oneSixtieth, 0.5 * oneSixtieth, oneSixtieth);
            foreach (var face in Faces)
            {
                var matrixA = new Matrix3x3(
                   face.Vertices[0].Position[0] - Center[0],
                   face.Vertices[0].Position[1] - Center[1],
                   face.Vertices[0].Position[2] - Center[2],

                   face.Vertices[1].Position[0] - Center[0],
                   face.Vertices[1].Position[1] - Center[1],
                   face.Vertices[1].Position[2] - Center[2],

                   face.Vertices[2].Position[0] - Center[0],
                   face.Vertices[2].Position[1] - Center[1],
                   face.Vertices[2].Position[2] - Center[2]);

                var matrixC = matrixA.Transpose() * canonicalMatrix;
                matrixC = matrixC * matrixA * matrixA.GetDeterminant();
                matrixCtotal = matrixCtotal + matrixC;
            }

            var translateMatrix = new double[,] { { 0 }, { 0 }, { 0 } };
            //what is this crazy equations?
            var matrixCprime =
                (translateMatrix * -1)
                     * (translateMatrix.transpose())
                     + (translateMatrix * ((translateMatrix * -1).transpose()))
                     + ((translateMatrix * -1) * ((translateMatrix * -1).transpose())
                     * Volume);
            matrixCprime = matrixCprime + matrixCtotal;
            var result = Matrix4x4.Identity * (matrixCprime[0, 0] + matrixCprime[1, 1] + matrixCprime[2, 2]);
            return result.Subtract(matrixCprime);
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