// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 04-18-2016
//
// Last Modified By : Design Engineering Lab
// Last Modified On : 04-18-2016
// ***********************************************************************
// <copyright file="Cone.cs" company="Design Engineering Lab">
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
    ///     The class for Cone primitives.
    /// </summary>
    public class Cone : PrimitiveSurface
    {
        /// <summary>
        ///     Is the cone positive? (false is negative)
        /// </summary>
        public bool IsPositive;

        /// <summary>
        ///     Cone
        /// </summary>
        /// <param name="facesAll">The faces all.</param>
        /// <param name="axis">The axis.</param>
        /// <param name="aperture">The aperture.</param>
        public Cone(List<PolygonalFace> facesAll, double[] axis, double aperture)
            : base(facesAll)
        {
            Type = PrimitiveSurfaceType.Cone;
            Axis = axis;
            Aperture = aperture;
            var faces = ListFunctions.FacesWithDistinctNormals(facesAll);
            var numFaces = faces.Count;
            var axisRefPoints = new List<double[]>();
            double[] axisRefPoint;
            var n1 = faces[0].Normal.crossProduct(axis);
            var n2 = faces[numFaces - 1].Normal.crossProduct(axis);
            MiscFunctions.LineIntersectingTwoPlanes(n1, faces[0].Center.dotProduct(n1),
                n2, faces[numFaces - 1].Center.dotProduct(n2), axis, out axisRefPoint);
            if (!axisRefPoint.Any(double.IsNaN) && !axisRefPoint.IsNegligible())
                axisRefPoints.Add(axisRefPoint);
            for (var i = 1; i < numFaces; i++)
            {
                n1 = faces[i].Normal.crossProduct(axis);
                n2 = faces[i - 1].Normal.crossProduct(axis);
                MiscFunctions.LineIntersectingTwoPlanes(n1, faces[i].Center.dotProduct(n1),
                    n2, faces[i - 1].Center.dotProduct(n2), axis, out axisRefPoint);
                if (!axisRefPoint.Any(double.IsNaN) && !axisRefPoint.IsNegligible())
                    axisRefPoints.Add(axisRefPoint);
            }
            axisRefPoint = new double[3];
            axisRefPoint = axisRefPoints.Aggregate(axisRefPoint, (current, c) => current.add(c));
            axisRefPoint = axisRefPoint.divide(axisRefPoints.Count);
            /*re-attach to plane through origin */
            var distBackToOrigin = -1*axis.dotProduct(axisRefPoint);
            axisRefPoint = axisRefPoint.subtract(axis.multiply(distBackToOrigin));
            // approach to find  Apex    
            var numApices = 0;
            var apexDistance = 0.0;
            for (var i = 1; i < numFaces; i++)
            {
                var distToAxis = MiscFunctions.DistancePointToLine(faces[i].Center, axisRefPoint, axis);
                var distAlongAxis = axis.dotProduct(faces[i].Center);
                distAlongAxis += distToAxis/Math.Tan(aperture);
                if (double.IsNaN(distAlongAxis)) continue;
                numApices++;
                apexDistance += distAlongAxis;
            }
            apexDistance /= numApices;
            Apex = axisRefPoint.add(axis.multiply(apexDistance));
            /* determine is positive or negative */
            var v2Apex = Apex.subtract(faces[0].Center);
            IsPositive = v2Apex.dotProduct(axis) >= 0;
        }

        /// <summary>
        ///     Gets the aperture.
        /// </summary>
        /// <value>The aperture.</value>
        public double Aperture { get; internal set; }

        /// <summary>
        ///     Gets the apex.
        /// </summary>
        /// <value>The apex.</value>
        public double[] Apex { get; internal set; }

        /// <summary>
        ///     Gets the axis.
        /// </summary>
        /// <value>The axis.</value>
        public double[] Axis { get; internal set; }


        /// <summary>
        ///     Checks if face should be added to cone
        /// </summary>
        /// <param name="face">The face.</param>
        /// <returns><c>true</c> if [is new member of] [the specified face]; otherwise, <c>false</c>.</returns>
        /// <exception cref="NotImplementedException"></exception>
        public override bool IsNewMemberOf(PolygonalFace face)
        {
            return false;
            // todo
            throw new NotImplementedException();
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

        /// <summary>
        ///     Updates cone with face
        /// </summary>
        /// <param name="face">The face.</param>
        public override void UpdateWith(PolygonalFace face)
        {
            base.UpdateWith(face);
        }
    }
}