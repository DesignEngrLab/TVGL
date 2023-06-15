using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVGL
{
    /// <summary>Provides options for how a tessellated solid is constructed.</summary>
    public class TessellatedSolidBuildOptions
    {
        /// <summary>
        /// Singleton for options that does the most: slowest, largest size. But models have everything.
        /// Actually, the defaults are nearly the same as Default with the exception of PredefineAllEdges.
        /// </summary>
        public static TessellatedSolidBuildOptions Default { get; } = new TessellatedSolidBuildOptions();

        /// <summary>
        /// Singleton for options that does the least: fastest, smallest size. All options are false.
        /// </summary>
        public static TessellatedSolidBuildOptions Minimal { get; } =
            new TessellatedSolidBuildOptions
            {
                AutomaticallyRepairHoles = false,
                AutomaticallyInvertNegativeSolids = false,
                AutomaticallyRepairBadFaces = false,
                CopyElementsPassedToConstructor = false,
                DefineConvexHull = false,
                FindNonsmoothEdges = false,
                PredefineAllEdges = DefineEdgesOptions.False,
            };


        /// <summary>Gets or sets whether holes in the tessellated solid will be automatically patched when reading in. 
        /// Note that this should be false if tessellated is not a solid, but rather a surface.</summary>
        public bool AutomaticallyRepairHoles { get; set; } = true;

        /// <summary>Gets or sets whether continuity issues in the tessellated solid will be automatically repaired. 
        /// This includes flipping faces that have opposite normals, and resolving negligible faces. This does not
        /// patch major holes (like "") but it can fix cracks - where vertices/edges are duplicated for separate
        /// faces.</summary>
        public bool AutomaticallyRepairBadFaces { get; set; } = true;

        /// <summary>Gets or sets whether the model will be inverted if the volume is negative. Generally, this is
        /// advised, but if the model is known to be a partial surface (in which case the volume may naturally be
        /// negative), or if a void-shape is intended, then do not call this fix.</summary>
        public bool AutomaticallyInvertNegativeSolids { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the tessellation elements (faces, vertices, primitives) provided in the 
        /// constructor are to be used in the model. Only two constructors use vertex or face elements from another model.
        /// If those input faces/vertices are to be copied in making this one, then set this to true. If that model is 
        /// intentionally being altered destructively, then this can be false.
        /// </summary>
        public bool CopyElementsPassedToConstructor { get; set; } = true;
        /// <summary>Gets or sets whether or not the convex hull should be found for the solid. This is a relatively quick process
        /// and should only be skipped if loading time must be kept to a minimum.
        /// </summary>
        public bool DefineConvexHull { get; set; } = true;
        /// <summary>
        /// Gets or sets a value indicating whether the nonsmooth edges are to be found during initialization. This requires that 
        /// the edges be defined. These nonsmooth edges are useful in finding the boundaries of surfaces within a solid.
        /// </summary>
        public bool FindNonsmoothEdges { get; set; } = true;

        /// <summary>Gets or sets whether all edges should be pre-defined for the solid no matter how large it is. 
        /// When false, edges will only be defined when the model has less 10000 edges.
        /// </summary>
        public DefineEdgesOptions PredefineAllEdges { get; set; } = DefineEdgesOptions.IfNotLargeModel;
    }

    public enum DefineEdgesOptions
    {
        False,
        True,
        IfNotLargeModel
    }
}