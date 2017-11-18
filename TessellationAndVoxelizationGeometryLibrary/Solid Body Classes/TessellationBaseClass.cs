using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TVGL.Voxelization;

namespace TVGL
{
    public abstract class TessellationBaseClass
    {
        /// <summary>
        ///     Index of the face in the tesselated solid face list
        /// </summary>
        /// <value>The index in list.</value>
        public int IndexInList { get; internal set; }
        /// <summary>
        ///     Gets a value indicating whether [it is part of the convex hull].
        /// </summary>
        /// <value><c>true</c> if [it is part of the convex hull]; otherwise, <c>false</c>.</value>
        public bool PartOfConvexHull { get; internal set; }

        /// <summary>
        ///     Gets the curvature.
        /// </summary>
        /// <value>The curvature.</value>
        public CurvatureType Curvature { get; internal set; }

        /// <summary>
        ///     Gets the normal.
        /// </summary>
        /// <value>The normal.</value>
        public double[] Normal { get; internal set; }


        public HashSet<Voxel> Voxels { get; protected set; }

        internal void AddVoxel(Voxel v)
        {
            if (Voxels == null) Voxels = new HashSet<Voxel> { v };
            else if (!Voxels.Contains(v)) Voxels.Add(v);
        }

    }
}
