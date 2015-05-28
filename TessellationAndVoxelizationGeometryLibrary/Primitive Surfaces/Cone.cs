
using System;
using System.Collections.Generic;
using System.Linq;
using StarMathLib;
using TVGL.Tessellation;

namespace TVGL
{
    /// <summary>
    /// The class for Cone primitives.
    /// </summary>
    public class Cone : PrimitiveSurface
    {
        /// <summary>
        /// Is the cone positive? (false is negative)
        /// </summary>
        public Boolean IsPositive;

        /// <summary>
        /// Gets the aperture.
        /// </summary>
        /// <value>
        /// The aperture.
        /// </value>
        public double Aperture { get; internal set; }
        /// <summary>
        /// Gets the apex.
        /// </summary>
        /// <value>
        /// The apex.
        /// </value>
        public double[] Apex { get; internal set; }
        /// <summary>
        /// Gets the axis.
        /// </summary>
        /// <value>
        /// The axis.
        /// </value>
        public double[] Axis { get; internal set; }


        internal override bool IsNewMemberOf(PolygonalFace face)
        {
            return false;
            // todo
            throw new NotImplementedException();
        }

        internal override void UpdateWith(PolygonalFace face)
        {
            base.UpdateWith(face);
        }
        internal Cone(List<PolygonalFace> facesAll, double[] Axis, double Aperture)
            : base(facesAll)
        {
            var faces = ListFunctions.FacesWithDistinctNormals(facesAll);
            var numFaces = faces.Count;
            var centers = new List<double[]>();
            double[] center;
            var n1 = faces[0].Normal.crossProduct(Axis);
            var n2 = faces[numFaces - 1].Normal.crossProduct(Axis);
            GeometryFunctions.LineIntersectingTwoPlanes(n1, faces[0].Center.dotProduct(n1),
              n2, faces[numFaces - 1].Center.dotProduct(n2), Axis, out center);
            if (!center.Any(double.IsNaN) || StarMath.IsNegligible(center))
                centers.Add(center);
            for (var i = 1; i < numFaces; i++)
            {
                n1 = faces[0].Normal.crossProduct(Axis);
                n2 = faces[numFaces - 1].Normal.crossProduct(Axis);
                GeometryFunctions.LineIntersectingTwoPlanes(n1, faces[0].Center.dotProduct(n1),
                  n2, faces[numFaces - 1].Center.dotProduct(n2), Axis, out center);
                if (!center.Any(double.IsNaN) || StarMath.IsNegligible(center))
                    centers.Add(center);
            }
            center = new double[3];
            center = centers.Aggregate(center, (current, c) => current.add(c));
            center = center.divide(centers.Count);
            /*re-attach to plane through origin */
            var distBackToOrigin = -1 * Axis.dotProduct(center);
            center = center.subtract(Axis.multiply(distBackToOrigin));
            /* determine is positive or negative */
            var isPositive = (faces[0].Normal.dotProduct(Axis) >= 0.0);
            // approach to find  Apex    
            var numApices = 0;
            var apexDistance = 0.0;
            for (var i = 1; i < numFaces; i++)
            {
                var distToAxis = GeometryFunctions.DistancePointToLine(faces[i].Center, center, Axis);
                var distAlongAxis = Axis.dotProduct(faces[i].Center);
                distAlongAxis += distToAxis / Math.Tan(Aperture);
                if (double.IsNaN(distAlongAxis))
                {
                    numApices++;
                    apexDistance += distAlongAxis;
                }
                apexDistance /= numApices;
            }
            Apex = center.add(Axis.multiply(apexDistance));
            this.Axis = Axis;
            this.Aperture = Aperture;
            IsPositive = isPositive;
        }
    }
}
