// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

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
        public Vector3 Center
        {
            get
            {
                if (_center.IsNull()) CalculateCenter();
                return _center;
            }
            set => _center = value;
        }

        protected abstract void CalculateCenter();

        protected Vector3 _center = Vector3.Null;

        /// <summary>
        ///     Gets the volume.
        /// </summary>
        /// <value>The volume.</value>
        public double Volume
        {
            get
            {
                if (double.IsNaN(_volume)) CalculateVolume();
                return _volume;
            }
        }

        protected abstract void CalculateVolume();

        protected double _volume = double.NaN;

        /// <summary>
        ///     Gets the surface area.
        /// </summary>
        /// <value>The surface area.</value>
        public double SurfaceArea
        {
            get
            {
                if (double.IsNaN(_surfaceArea)) CalculateSurfaceArea();
                return _surfaceArea;
            }
        }

        protected abstract void CalculateSurfaceArea();

        protected double _surfaceArea = double.NaN;

        /// <summary>
        /// Gets or sets the inertia tensor.
        /// </summary>
        /// <value>The inertia tensor.</value>
        [JsonIgnore]
        public Matrix3x3 InertiaTensor
        {
            get
            {
                if (_inertiaTensor.IsNull()) CalculateInertiaTensor();
                return _inertiaTensor;
            }
        }

        protected abstract void CalculateInertiaTensor();

        protected Matrix3x3 _inertiaTensor = Matrix3x3.Null;

        /// <summary>
        ///     Gets the bounds.
        /// </summary>
        /// <value>The bounds.</value>
        public Vector3[] Bounds { get; set; }

        public double XMin => Bounds[0].X;
        public double XMax => Bounds[1].X;
        public double YMin => Bounds[0].Y;
        public double YMax => Bounds[1].Y;
        public double ZMin => Bounds[0].Z;
        public double ZMax => Bounds[1].Z;

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
        ///     The solid color
        /// </summary>
        public Color SolidColor { get; set; }

        /// <summary>
        ///     Gets or sets the primitive objects that make up the solid
        /// </summary>
        [JsonProperty(ItemTypeNameHandling = TypeNameHandling.Auto, TypeNameHandling = TypeNameHandling.Auto)]
        public List<PrimitiveSurface> Primitives { get; set; }

        public double SameTolerance { get; set; }

        #endregion Fields and Properties

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
            Bounds = new Vector3[2];
        }

        #endregion Constructor

        /// <summary>
        /// Transforms the specified transform matrix.
        /// </summary>
        /// <param name="transformMatrix">The transform matrix.</param>
        public abstract void Transform(Matrix4x4 transformMatrix);

        /// <summary>
        /// Gets a new solid by transforming its vertices.
        /// </summary>
        /// <param name="transformationMatrix"></param>
        /// <returns></returns>
        public abstract Solid TransformToNewSolid(Matrix4x4 transformationMatrix);

        protected Solid(Solid originalToBeCopied)
        {
            _center = originalToBeCopied._center;
            _volume = originalToBeCopied._volume;
            _surfaceArea = originalToBeCopied._surfaceArea;
            _inertiaTensor = originalToBeCopied._inertiaTensor;
            if (originalToBeCopied.Bounds != null)
                Bounds = new[] { originalToBeCopied.Bounds[0], originalToBeCopied.Bounds[1] };
            Name = originalToBeCopied.Name + "_copy";
            if (originalToBeCopied.Comments != null)
            {
                Comments = new List<string>();
                foreach (var comment in originalToBeCopied.Comments)
                    Comments.Add(comment);
            }
            FileName = originalToBeCopied.FileName;
            Units = originalToBeCopied.Units;
            Language = originalToBeCopied.Language;
            HasUniformColor = originalToBeCopied.HasUniformColor;
            SolidColor = originalToBeCopied.SolidColor;
            SameTolerance = originalToBeCopied.SameTolerance;
            if (originalToBeCopied.ConvexHull != null)
                ConvexHull = originalToBeCopied.ConvexHull;
        }

        // everything else gets stored here
        [JsonExtensionData]
        protected IDictionary<string, JToken> serializationData;
    }
}