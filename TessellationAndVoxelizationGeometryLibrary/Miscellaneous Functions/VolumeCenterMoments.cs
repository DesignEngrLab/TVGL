// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-14-2023
// ***********************************************************************
// <copyright file="MiscFunctions.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using StarMathLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;

namespace TVGL
{
    /// <summary>
    /// Miscellaneous Functions for TVGL
    /// </summary>
    public static partial class MiscFunctions
    {

        /// <summary>
        /// The one third
        /// </summary>
        const double oneThird = 1.0 / 3.0;
        /// <summary>
        /// The one twelth
        /// </summary>
        const double oneTwelth = 1.0 / 12.0;
        /// <summary>
        /// Calculates the volume and center.
        /// </summary>
        /// <param name="faces">The faces.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <param name="volume">The volume.</param>
        /// <param name="center">The center.</param>
        public static void CalculateVolumeAndCenter(this IEnumerable<TriangleFace> faces, double tolerance, out double volume, out Vector3 center)
        {
            center = new Vector3();
            volume = 0.0;
            double currentVolumeTerm;
            double xCenter = 0, yCenter = 0, zCenter = 0;
            if (faces == null) return;
            foreach (var face in faces)
            {
                if (face.Area.IsNegligible(tolerance)) continue; //Ignore faces with zero area, since their Normals are not set.
                                                                 // this is the volume of a tetrahedron from defined by the face and the origin {0,0,0}. The origin would be part of the second term
                                                                 // in the dotproduct, "face.Normal.Dot(face.A.Coordinates - ORIGIN))", but clearly there is no need to subtract
                                                                 // {0,0,0}. Note that the volume of the tetrahedron could be negative. This is fine as it ensures that the origin has no influence
                                                                 // on the volume.
                var a = face.A; var b = face.B; var c = face.C;// get once, so we don't have as many gets from an array.
                                                               //The actual tetrehedron volume should be divided by three, but we can just process that at the end.
                volume += currentVolumeTerm = face.Area * face.Normal.Dot(a.Coordinates);
                xCenter += (a.X + b.X + c.X) * currentVolumeTerm;
                yCenter += (a.Y + b.Y + c.Y) * currentVolumeTerm;
                zCenter += (a.Z + b.Z + c.Z) * currentVolumeTerm;
                // center is found by a weighted sum of the centers of each tetrahedron. The weighted sum coordinate are collected here.
            }

            //Divide the volume by 3 and the center by 4. Since center is also mutliplied by the currentVolume, it is actually divided by 3 * 4 = 12;                
            volume *= oneThird;
            center = new Vector3(xCenter * oneTwelth, yCenter * oneTwelth, zCenter * oneTwelth) / volume;
        }

        /// <summary>
        /// The one sixtieth
        /// </summary>
        const double oneSixtieth = 1.0 / 60.0;

        /// <summary>
        /// Calculates the inertia tensor.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        public static Matrix3x3 CalculateInertiaTensor(this IEnumerable<TriangleFace> Faces, Vector3 Center)
        {
            //var matrixA = new double[3, 3];
            var matrixCtotal = new Matrix3x3();
            var canonicalMatrix = new Matrix3x3(oneSixtieth, 0.5 * oneSixtieth, 0.5 * oneSixtieth,
                0.5 * oneSixtieth, oneSixtieth, 0.5 * oneSixtieth,
                0.5 * oneSixtieth, 0.5 * oneSixtieth, oneSixtieth);
            foreach (var face in Faces)
            {
                var matrixA = new Matrix3x3(
                   face.A.Coordinates[0] - Center[0],
                   face.A.Coordinates[1] - Center[1],
                   face.A.Coordinates[2] - Center[2],

                   face.B.Coordinates[0] - Center[0],
                   face.B.Coordinates[1] - Center[1],
                   face.B.Coordinates[2] - Center[2],

                   face.C.Coordinates[0] - Center[0],
                   face.C.Coordinates[1] - Center[1],
                   face.C.Coordinates[2] - Center[2]);

                var matrixC = matrixA.Transpose() * canonicalMatrix;
                matrixC = matrixC * matrixA * matrixA.GetDeterminant();
                matrixCtotal = matrixCtotal + matrixC;
            }
            // todo fix this calculation
            //var translateMatrix = new double[,] { { 0 }, { 0 }, { 0 } };
            ////what is this crazy equations?
            //var matrixCprime =
            //    (translateMatrix * -1)
            //         * (translateMatrix.Transpose())
            //         + (translateMatrix * ((translateMatrix * -1).transpose()))
            //         + ((translateMatrix * -1) * ((translateMatrix * -1).transpose())
            //         * Volume);
            //matrixCprime = matrixCprime + matrixCtotal;
            //var result = Matrix4x4.Identity * (matrixCprime[0, 0] + matrixCprime[1, 1] + matrixCprime[2, 2]);
            //return result.Subtract(matrixCprime);
            throw new NotImplementedException();
        }

    }
}