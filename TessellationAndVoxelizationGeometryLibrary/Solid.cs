// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 02-27-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 03-07-2015
// ***********************************************************************
// <copyright file="TessellatedSolid.cs" company="Design Engineering Lab">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using MIConvexHull;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StarMathLib;
using TVGL.IOFunctions;

namespace TVGL
{
    /// <summary>
    ///     Class TessellatedSolid.
    /// </summary>
    /// <tags>help</tags>
    /// <remarks>
    ///     This is the currently the <strong>main</strong> class within TVGL all filetypes are read in as a TessellatedSolid,
    ///     and
    ///     all interesting operations work on the TessellatedSolid.
    /// </remarks>
    [JsonObject(MemberSerialization.OptOut)]
    public abstract class Solid
    {
        #region Fields and Properties

        /// <summary>
        ///     Gets the center.
        /// </summary>
        /// <value>The center.</value>
        public double[] Center { get; set; }


        /// <summary>
        ///     Gets the bounds.
        /// </summary>
        /// <value>The bounds.</value>
        public double[][] Bounds { get; set; } = new double[2][];

        public double XMin { get => Bounds[0][0]; protected set => Bounds[0][0] = value; }
        public double XMax { get => Bounds[1][0]; protected set => Bounds[1][0] = value; }
        public double YMin { get => Bounds[0][1]; protected set => Bounds[0][1] = value; }
        public double YMax { get => Bounds[1][1]; protected set => Bounds[1][1] = value; }
        public double ZMin { get => Bounds[0][2]; protected set => Bounds[0][2] = value; }
        public double ZMax { get => Bounds[1][2]; protected set => Bounds[1][2] = value; }


        /// <summary>
        ///     Gets the volume.
        /// </summary>
        /// <value>The volume.</value>
        public double Volume { get; set; }

        /// <summary>
        ///     Gets and sets the mass.
        /// </summary>
        /// <value>The mass.</value>
        public double Mass { get; set; }

        /// <summary>
        ///     Gets the surface area.
        /// </summary>
        /// <value>The surface area.</value>
        public double SurfaceArea { get; set; }

        /// <summary>
        ///     The name of solid
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// The comments
        /// </summary>
        public List<string> Comments { get; set; }

        /// <summary>
        /// The file name
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the units.
        /// </summary>
        /// <value>The units.</value>
        public UnitType Units { get; set; }

        /// <summary>
        /// The language
        /// </summary>
        public string Language { get; set; }

        /// <summary>
        ///     Gets the convex hull.
        /// </summary>
        /// <value>The convex hull.</value>
        [JsonIgnore]
        public TVGLConvexHull ConvexHull { get; set; }

        /// <summary>
        ///     The has uniform color
        /// </summary>
        public bool HasUniformColor { get; set; }


        /// <summary>
        /// Gets or sets the inertia tensor.
        /// </summary>
        /// <value>The inertia tensor.</value>
        [JsonIgnore]
        public virtual double[,] InertiaTensor { get; set; }
        internal double[,] _inertiaTensor;


        /// <summary>
        ///     The solid color
        /// </summary>
        public Color SolidColor { get; set; }

        /// <summary>
        ///     Gets or sets the primitive objects that make up the solid
        /// </summary>
        [JsonProperty(ItemTypeNameHandling = TypeNameHandling.Auto, TypeNameHandling = TypeNameHandling.Auto)]
        public List<PrimitiveSurface> Primitives { get; set; }

        public double SameTolerance { get; set; }

        #endregion

        #region Constructor

        protected Solid(UnitType units = UnitType.unspecified, string name = "",
            string filename = "", List<string> comments = null, string language = "")
        {
            Name = name;
            FileName = filename;
            Comments = new List<string>();
            if (comments != null)
                Comments.AddRange(comments);
            Language = language;
            Units = units;
            HasUniformColor = true;
            SolidColor = new Color(Constants.DefaultColor);
            Bounds = new double[2][];
            Bounds[0] = new double[3];
            Bounds[1] = new double[3];
        }

        #endregion

        /// <summary>
        /// Transforms the specified transform matrix.
        /// </summary>
        /// <param name="transformMatrix">The transform matrix.</param>
        public abstract void Transform(double[,] transformMatrix);
        // here's a good reference for this: http://www.cs.brandeis.edu/~cs155/Lecture_07_6.pdf


        /// <summary>
        /// Gets a new solid by transforming its vertices.
        /// </summary>
        /// <param name="transformationMatrix"></param>
        /// <returns></returns>
        public abstract Solid TransformToNewSolid(double[,] transformationMatrix);

        public abstract Solid Copy();



        // everything else gets stored here
        [JsonExtensionData]
        protected IDictionary<string, JToken> serializationData;

        //protected abstract void OnSerializingMethod(StreamingContext context);
        //protected abstract void OnDeserializedMethod(StreamingContext context);

    }
}