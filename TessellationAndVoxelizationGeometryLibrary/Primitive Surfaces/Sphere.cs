// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 04-18-2016
//
// Last Modified By : Design Engineering Lab
// Last Modified On : 04-18-2016
// ***********************************************************************
// <copyright file="Sphere.cs" company="Design Engineering Lab">
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
    ///     Class Sphere.
    /// </summary>
    public class Sphere : PrimitiveSurface
    {
        /// <summary>
        ///     Checks if the face is a member of the sphere
        /// </summary>
        /// <param name="face">The face.</param>
        /// <returns>Boolean.</returns>
        public override bool IsNewMemberOf(PolygonalFace face)
        {
            if (Faces.Contains(face)) return false;
            if (Math.Abs(face.Normal.dotProduct(face.Center.subtract(Center)) - 1) >
                Constants.ErrorForFaceInSurface)
                return false;
            foreach (var v in face.Vertices)
                if (Math.Abs(MiscFunctions.DistancePointToPoint(v.Position, Center) - Radius) >
                    Constants.ErrorForFaceInSurface*Radius)
                    return false;
            return true;
        }

        /// <summary>
        ///     Adds face to sphere
        /// </summary>
        /// <param name="face">The face.</param>
        public override void UpdateWith(PolygonalFace face)
        {
            double[] pointOnLine;
            var distance = MiscFunctions.DistancePointToLine(Center, face.Center, face.Normal, out pointOnLine);
            var fractionToMove = 1/Faces.Count;
            var moveVector = pointOnLine.subtract(Center);
            Center =
                Center.add(new[]
                {
                    moveVector[0]*fractionToMove*distance, moveVector[1]*fractionToMove*distance,
                    moveVector[2]*fractionToMove*distance
                });


            var totalOfRadii = Vertices.Sum(v => MiscFunctions.DistancePointToPoint(Center, v.Position));
            Radius = totalOfRadii/Vertices.Count;
            base.UpdateWith(face);
        }

        /// <summary>
        /// Transforms the shape by the provided transformation matrix.
        /// </summary>
        /// <param name="transformMatrix">The transform matrix.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public override void Transform(double[,] transformMatrix)
        {
            throw new NotImplementedException();
        }

        #region Constructor

        /// <summary>
        ///     Primitive Sphere
        /// </summary>
        /// <param name="facesAll">The faces all.</param>
        public Sphere(IEnumerable<PolygonalFace> facesAll)
            : base(facesAll)
        {
            Type = PrimitiveSurfaceType.Sphere;
            var faces = ListFunctions.FacesWithDistinctNormals(facesAll.ToList());
            var n = faces.Count;
            var centers = new List<double[]>();
            double[] center;
            double t1, t2;
            var signedDistances = new List<double>();
            MiscFunctions.SkewedLineIntersection(faces[0].Center, faces[0].Normal,
                faces[n - 1].Center, faces[n - 1].Normal, out center, out t1, out t2);
            if (!center.Any(double.IsNaN) || center.IsNegligible())
            {
                centers.Add(center);
                signedDistances.Add(t1);
                signedDistances.Add(t2);
            }
            for (var i = 1; i < n; i++)
            {
                MiscFunctions.SkewedLineIntersection(faces[i].Center, faces[i].Normal,
                    faces[i - 1].Center, faces[i - 1].Normal, out center, out t1, out t2);
                if (!center.Any(double.IsNaN) || center.IsNegligible())
                {
                    centers.Add(center);
                    signedDistances.Add(t1);
                    signedDistances.Add(t2);
                }
            }
            center = new double[3];
            center = centers.Aggregate(center, (current, c) => current.add(c));
            center = center.divide(centers.Count);
            /* determine is positive or negative */
            var numNeg = signedDistances.Count(d => d < 0);
            var numPos = signedDistances.Count(d => d > 0);
            var isPositive = numNeg > numPos;
            var radii = new List<double>();
            foreach (var face in faces)
                radii.AddRange(face.Vertices.Select(v => MiscFunctions.DistancePointToPoint(v.Position, center)));
            var averageRadius = radii.Average();

            Center = center;
            IsPositive = isPositive;
            Radius = averageRadius;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Sphere" /> class.
        /// </summary>
        /// <param name="edge">The edge.</param>
        internal Sphere(Edge edge)
            : this(new List<PolygonalFace>(new[] {edge.OwnedFace, edge.OtherFace}))
        {
            Type = PrimitiveSurfaceType.Sphere;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Is the sphere positive? (false is negative)
        /// </summary>
        public bool IsPositive;

        /// <summary>
        ///     Gets the center.
        /// </summary>
        /// <value>The center.</value>
        public double[] Center { get; internal set; }

        /// <summary>
        ///     Gets the radius.
        /// </summary>
        /// <value>The radius.</value>
        public double Radius { get; internal set; }

        #endregion
    }
}