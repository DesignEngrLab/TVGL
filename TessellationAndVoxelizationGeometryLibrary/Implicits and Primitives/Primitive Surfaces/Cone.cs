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
using TVGL.Numerics;

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

        internal Cone()
        { Type = PrimitiveSurfaceType.Cone; }
        /// <summary>
        ///     Cone
        /// </summary>
        /// <param name="facesAll">The faces all.</param>
        /// <param name="axis">The axis.</param>
        /// <param name="aperture">The aperture.</param>
        public Cone(List<PolygonalFace> facesAll, Vector3 axis, double aperture)
            : base(facesAll)
        {
            Type = PrimitiveSurfaceType.Cone;
            Axis = axis;
            Aperture = aperture;
            var faces = MiscFunctions.FacesWithDistinctNormals(facesAll);
            var numFaces = faces.Count;
            var axisRefPoints = new List<Vector3>();
            var n1 = faces[0].Normal.Cross(axis);
            var n2 = faces[^1].Normal.Cross(axis);
            MiscFunctions.LineIntersectingTwoPlanes(n1, faces[0].Center.Dot(n1),
                n2, faces[^1].Center.Dot(n2), axis, out var axisRefPoint);
            if (!axisRefPoint.IsNull() && !axisRefPoint.IsNegligible())
                axisRefPoints.Add(axisRefPoint);
            for (var i = 1; i < numFaces; i++)
            {
                n1 = faces[i].Normal.Cross(axis);
                n2 = faces[i - 1].Normal.Cross(axis);
                MiscFunctions.LineIntersectingTwoPlanes(n1, faces[i].Center.Dot(n1),
                    n2, faces[i - 1].Center.Dot(n2), axis, out axisRefPoint);
                if (!axisRefPoint.IsNull() && !axisRefPoint.IsNegligible())
                    axisRefPoints.Add(axisRefPoint);
            }
            axisRefPoint = Vector3.Zero;
            axisRefPoint = axisRefPoints.Aggregate(axisRefPoint, (current, c) => current + c);
            axisRefPoint = axisRefPoint.Divide(axisRefPoints.Count);
            /*re-attach to plane through origin */
            var distBackToOrigin = -1 * axis.Dot(axisRefPoint);
            axisRefPoint = axisRefPoint-(axis * distBackToOrigin);
            // approach to find  Apex    
            var numApices = 0;
            var apexDistance = 0.0;
            for (var i = 1; i < numFaces; i++)
            {
                var distToAxis = MiscFunctions.DistancePointToLine(faces[i].Center, axisRefPoint, axis);
                var distAlongAxis = axis.Dot(faces[i].Center);
                distAlongAxis += distToAxis / Math.Tan(aperture);
                if (double.IsNaN(distAlongAxis)) continue;
                numApices++;
                apexDistance += distAlongAxis;
            }
            apexDistance /= numApices;
            Apex = axisRefPoint + (axis * apexDistance);
            /* determine is positive or negative */
            var v2Apex = Apex.Subtract(faces[0].Center);
            IsPositive = v2Apex.Dot(axis) >= 0;
        }

        /// <summary>
        ///     Gets the aperture.
        /// </summary>
        /// <value>The aperture.</value>
        public double Aperture { get;  set; }

        /// <summary>
        ///     Gets the apex.
        /// </summary>
        /// <value>The apex.</value>
        public Vector3 Apex { get;  set; }

        /// <summary>
        ///     Gets the axis.
        /// </summary>
        /// <value>The axis.</value>
        public Vector3 Axis { get;  set; }


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
        public override void Transform(Matrix4x4 transformMatrix)
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