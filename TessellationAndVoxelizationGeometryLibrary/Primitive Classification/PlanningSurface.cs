// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 04-18-2016
//
// Last Modified By : Design Engineering Lab
// Last Modified On : 04-18-2016
// ***********************************************************************
// <copyright file="PlanningSurface.cs" company="Design Engineering Lab">
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
        ///     Class PlanningSurface.
        /// </summary>
        internal class PlanningSurface
        {
            /// <summary>
            ///     The faces
            /// </summary>
            internal List<FaceWithScores> Faces;

            /// <summary>
            ///     Initializes a new instance of the <see cref="PlanningSurface" /> class.
            /// </summary>
            /// <param name="SurfaceType">Type of the surface.</param>
            /// <param name="Faces">The faces.</param>
            internal PlanningSurface(PrimitiveSurfaceType SurfaceType, params FaceWithScores[] Faces)
            {
                this.SurfaceType = SurfaceType;
                this.Faces = new List<FaceWithScores>(Faces);
                foreach (var polygonalFace in Faces)
                    Area += polygonalFace.Face.Area;
            }

            /// <summary>
            ///     Gets the type of the surface.
            /// </summary>
            /// <value>The type of the surface.</value>
            internal PrimitiveSurfaceType SurfaceType { get; }

            /// <summary>
            ///     Gets or sets the area.
            /// </summary>
            /// <value>The area.</value>
            internal double Area { get; set; }

            /// <summary>
            ///     Gets or sets the negative probability.
            /// </summary>
            /// <value>The negative probability.</value>
            internal double NegativeProbability { get; set; }

            /// <summary>
            ///     Gets the metric.
            /// </summary>
            /// <value>The metric.</value>
            internal double Metric
            {
                get
                {
                    double TypeMultiplier;
                    switch (SurfaceType)
                    {
                        case PrimitiveSurfaceType.Flat:
                            TypeMultiplier = 20;
                            break;
                        case PrimitiveSurfaceType.Cylinder:
                            TypeMultiplier = 10;
                            break;
                        case PrimitiveSurfaceType.Sphere:
                            TypeMultiplier = 5;
                            break;
                        default:
                            TypeMultiplier = 1;
                            break;
                    }
                    return TypeMultiplier*Area*(1 - NegativeProbability);
                }
            }

            /// <summary>
            ///     Adds the specified f.
            /// </summary>
            /// <param name="f">The f.</param>
            internal void Add(FaceWithScores f)
            {
                Faces.Add(f);
                Area += f.Face.Area;
                NegativeProbability *= 1 - f.FaceCat[SurfaceType];
            }

            /// <summary>
            ///     Removes the specified f.
            /// </summary>
            /// <param name="f">The f.</param>
            internal void Remove(FaceWithScores f)
            {
                Faces.Remove(f);
                Area -= f.Face.Area;
                NegativeProbability /= 1 - f.FaceCat[SurfaceType];
            }
        }
    }
}