// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 02-27-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 02-18-2015
// ***********************************************************************
// <copyright file="Cylinder.cs" company="Design Engineering Lab">
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
    ///     The class for Cylinder primitives.
    /// </summary>
    public class Cylinder : PrimitiveSurface
    {
        /// <summary>
        ///     Determines whether [is new member of] [the specified face].
        /// </summary>
        /// <param name="face">The face.</param>
        /// <returns><c>true</c> if [is new member of] [the specified face]; otherwise, <c>false</c>.</returns>
        public override bool IsNewMemberOf(PolygonalFace face)
        {
            if (Faces.Contains(face)) return false;
            if (Math.Abs(face.Normal.dotProduct(Axis)) > Constants.ErrorForFaceInSurface)
                return false;
            foreach (var v in face.Vertices)
                if (Math.Abs(MiscFunctions.DistancePointToLine(v.Position, Anchor, Axis) - Radius) >
                    Constants.ErrorForFaceInSurface*Radius)
                    return false;
            return true;
        }

        /// <summary>
        ///     Updates the with.
        /// </summary>
        /// <param name="face">The face.</param>
        public override void UpdateWith(PolygonalFace face)
        {
            var numFaces = Faces.Count;
            double[] inBetweenPoint;
            var distance = MiscFunctions.SkewedLineIntersection(face.Center, face.Normal, Anchor, Axis,
                out inBetweenPoint);
            var fractionToMove = 1/numFaces;
            var moveVector = Anchor.crossProduct(face.Normal);
            if (moveVector.dotProduct(face.Center.subtract(inBetweenPoint)) < 0)
                moveVector = moveVector.multiply(-1);
            moveVector.normalizeInPlace();
            /**** set new Anchor (by averaging in with last n values) ****/
            Anchor =
                Anchor.add(new[]
                {
                    moveVector[0]*fractionToMove*distance, moveVector[1]*fractionToMove*distance,
                    moveVector[2]*fractionToMove*distance
                });

            /* to adjust the Axis, we will average the cross products of the new face with all the old faces */
            var totalAxis = new double[3];
            for (var i = 0; i < numFaces; i++)
            {
                var newAxis = face.Normal.crossProduct(Faces[i].Normal);
                if (newAxis.dotProduct(Axis) < 0)
                    newAxis.multiply(-1);
                totalAxis = totalAxis.add(newAxis);
            }
            var numPrevCrossProducts = numFaces*(numFaces - 1)/2;
            totalAxis = totalAxis.add(Axis.multiply(numPrevCrossProducts));
            /**** set new Axis (by averaging in with last n values) ****/
            Axis = totalAxis.divide(numFaces + numPrevCrossProducts).normalize();
            foreach (var v in face.Vertices)
                if (!Vertices.Contains(v))
                    Vertices.Add(v);
            var totalOfRadii = Vertices.Sum(v => MiscFunctions.DistancePointToLine(v.Position, Anchor, Axis));
            /**** set new Radius (by averaging in with last n values) ****/
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
            var homoCoord = new[] { Anchor[0], Anchor[1], Anchor[2], 1.0 };
            homoCoord = transformMatrix.multiply(homoCoord);
            Anchor[0] = homoCoord[0]; Anchor[1] = homoCoord[1]; Anchor[2] = homoCoord[2];

             homoCoord = new[] { Axis[0], Axis[1], Axis[2], 1.0 };
            homoCoord = transformMatrix.multiply(homoCoord);
            Axis[0] = homoCoord[0]; Axis[1] = homoCoord[1]; Axis[2] = homoCoord[2];

            //how to adjust the radii?
            throw new NotImplementedException();
        }

        #region Properties

        /// <summary>
        ///     Is the cylinder positive? (false is negative)
        /// </summary>
        public bool IsPositive;

        /// <summary>
        ///     Gets the anchor.
        /// </summary>
        /// <value>The anchor.</value>
        public double[] Anchor { get; internal set; }

        /// <summary>
        ///     Gets the direction.
        /// </summary>
        /// <value>The direction.</value>
        public double[] Axis { get; internal set; }

        /// <summary>
        ///     Gets the radius.
        /// </summary>
        /// <value>The radius.</value>
        public double Radius { get; internal set; }

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="Cylinder" /> class.
        /// </summary>
        /// <param name="facesAll">The faces all.</param>
        /// <param name="axis">The axis.</param>
        public Cylinder(IEnumerable<PolygonalFace> facesAll, double[] axis)
            : base(facesAll)
        {
            Type = PrimitiveSurfaceType.Cylinder;
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
            /* move center to origin plane */
            var distBackToOrigin = -1*axis.dotProduct(center);
            center = center.subtract(axis.multiply(distBackToOrigin));
            /* determine is positive or negative */
            var numNeg = signedDistances.Count(d => d < 0);
            var numPos = signedDistances.Count(d => d > 0);
            var isPositive = numNeg > numPos;
            var radii = new List<double>();
            foreach (var face in faces)
                radii.AddRange(face.Vertices.Select(v => MiscFunctions.DistancePointToLine(v.Position, center, axis)));
            var averageRadius = radii.Average();

            Axis = axis;
            Anchor = center;
            IsPositive = isPositive;
            Radius = averageRadius;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Cylinder" /> class.
        /// </summary>
        /// <param name="edge">The edge.</param>
        /// <exception cref="System.Exception">Edge used to define cylinder is flat.</exception>
        internal Cylinder(Edge edge)
            : base(new List<PolygonalFace>(new[] {edge.OwnedFace, edge.OtherFace}))
        {
            Type = PrimitiveSurfaceType.Cylinder;
            var axis = edge.OwnedFace.Normal.crossProduct(edge.OtherFace.Normal);
            var length = axis.norm2();
            if (length.IsNegligible()) throw new Exception("Edge used to define cylinder is flat.");
            axis.normalizeInPlace();
            var v1 = edge.From;
            var v2 = edge.To;
            var v3 = edge.OwnedFace.Vertices.First(v => v != v1 && v != v2);
            var v4 = edge.OtherFace.Vertices.First(v => v != v1 && v != v2);
            double[] center;
            MiscFunctions.SkewedLineIntersection(edge.OwnedFace.Center, edge.OwnedFace.Normal,
                edge.OtherFace.Center, edge.OtherFace.Normal, out center);
            /* determine is positive or negative */
            var isPositive = edge.Curvature == CurvatureType.Convex;
            /* move center to origin plane */
            var distToOrigin = axis.dotProduct(center);
            if (distToOrigin < 0)
            {
                distToOrigin *= -1;
                axis.multiply(-1);
            }
            center = new[]
            {
                center[0] - distToOrigin*axis[0],
                center[1] - distToOrigin*axis[1],
                center[2] - distToOrigin*axis[2]
            };
            var d1 = MiscFunctions.DistancePointToLine(v1.Position, center, axis);
            var d2 = MiscFunctions.DistancePointToLine(v2.Position, center, axis);
            var d3 = MiscFunctions.DistancePointToLine(v3.Position, center, axis);
            var d4 = MiscFunctions.DistancePointToLine(v4.Position, center, axis);
            var averageRadius = (d1 + d2 + d3 + d4)/4;
            var outerEdges = new List<Edge>(edge.OwnedFace.Edges);
            outerEdges.AddRange(edge.OtherFace.Edges);
            outerEdges.Remove(edge);
            outerEdges.Remove(edge);

            Axis = axis;
            Anchor = center;
            IsPositive = isPositive;
            Radius = averageRadius;
        }

        #endregion
    }
}