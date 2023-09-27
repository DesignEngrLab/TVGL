// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="TessellationBaseClass.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************


namespace TVGL
{
    /// <summary>
    /// Class TessellationBaseClass.
    /// </summary>
    public abstract class TessellationBaseClass
    {
        /// <summary>
        /// Index of the face in the tesselated solid face list
        /// </summary>
        /// <value>The index in list.</value>
        public int IndexInList { get; set; }

        /// <summary>
        /// Gets a value indicating whether [it is part of the convex hull].
        /// </summary>
        /// <value><c>true</c> if [it is part of the convex hull]; otherwise, <c>false</c>.</value>
        public bool PartOfConvexHull { get; internal set; }

        /// <summary>
        /// Gets the curvature.
        /// </summary>
        /// <value>The curvature.</value>
        public abstract CurvatureType Curvature { get; internal set; }

        /// <summary>
        /// Gets the normal.
        /// </summary>
        /// <value>The normal.</value>
        public abstract Vector3 Normal { get; }
    }
}