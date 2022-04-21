// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)


namespace TVGL
{
    public abstract class TessellationBaseClass
    {
        /// <summary>
        ///     Index of the face in the tesselated solid face list
        /// </summary>
        /// <value>The index in list.</value>
        public int IndexInList { get; set; }

        /// <summary>
        ///     Gets a value indicating whether [it is part of the convex hull].
        /// </summary>
        /// <value><c>true</c> if [it is part of the convex hull]; otherwise, <c>false</c>.</value>
        public bool PartOfConvexHull { get; internal set; }

        /// <summary>
        ///     Gets the curvature.
        /// </summary>
        /// <value>The curvature.</value>
        public abstract CurvatureType Curvature { get; }

        /// <summary>
        ///     Gets the normal.
        /// </summary>
        /// <value>The normal.</value>
        public abstract Vector3 Normal { get; }
    }
}