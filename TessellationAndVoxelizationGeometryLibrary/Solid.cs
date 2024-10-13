// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-07-2023
// ***********************************************************************
// <copyright file="Solid.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace TVGL
{
    /// <summary>
    /// Class TessellatedSolid.
    /// </summary>
    /// <tags>help</tags>
    /// <remarks>This is the currently the <strong>main</strong> class within TVGL all filetypes are read in as a TessellatedSolid,
    /// and
    /// all interesting operations work on the TessellatedSolid.</remarks>
    [JsonObject(MemberSerialization.OptOut)]
    public abstract class Solid
    {
        #region Fields and Properties
        /// <summary>
        /// Gets the solid index in the SolidAssembly.DistinctParts Array.
        /// </summary>
        /// <value>The index</value>
        public int ReferenceIndex { get; set; }

        /// <summary>
        /// Gets the center.
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

        /// <summary>
        /// Calculates the center.
        /// </summary>
        protected abstract void CalculateCenter();

        /// <summary>
        /// The center
        /// </summary>
        protected Vector3 _center = Vector3.NaN;

        /// <summary>
        /// Gets the volume.
        /// </summary>
        /// <value>The volume.</value>
        public double Volume
        {
            get
            {
                if (double.IsNaN(_volume)) CalculateVolume();
                return _volume;
            }
            set => _volume = value;
        }

        /// <summary>
        /// Calculates the volume.
        /// </summary>
        protected abstract void CalculateVolume();

        /// <summary>
        /// The volume
        /// </summary>
        protected double _volume = double.NaN;

        /// <summary>
        /// Gets the surface area.
        /// </summary>
        /// <value>The surface area.</value>
        public double SurfaceArea
        {
            get
            {
                if (double.IsNaN(_surfaceArea)) CalculateSurfaceArea();
                return _surfaceArea;
            }
            set { }
        }

        /// <summary>
        /// Calculates the surface area.
        /// </summary>
        protected abstract void CalculateSurfaceArea();

        /// <summary>
        /// The surface area
        /// </summary>
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

        /// <summary>
        /// Calculates the inertia tensor.
        /// </summary>
        protected abstract void CalculateInertiaTensor();

        /// <summary>
        /// The inertia tensor
        /// </summary>
        protected Matrix3x3 _inertiaTensor = Matrix3x3.Null;

        /// <summary>
        /// Gets the bounds.
        /// </summary>
        /// <value>The bounds.</value>
        public Vector3[] Bounds { get; set; }

        /// <summary>
        /// Gets the x minimum.
        /// </summary>
        /// <value>The x minimum.</value>
        public double XMin => Bounds[0].X;
        /// <summary>
        /// Gets the x maximum.
        /// </summary>
        /// <value>The x maximum.</value>
        public double XMax => Bounds[1].X;
        /// <summary>
        /// Gets the y minimum.
        /// </summary>
        /// <value>The y minimum.</value>
        public double YMin => Bounds[0].Y;
        /// <summary>
        /// Gets the y maximum.
        /// </summary>
        /// <value>The y maximum.</value>
        public double YMax => Bounds[1].Y;
        /// <summary>
        /// Gets the z minimum.
        /// </summary>
        /// <value>The z minimum.</value>
        public double ZMin => Bounds[0].Z;
        /// <summary>
        /// Gets the z maximum.
        /// </summary>
        /// <value>The z maximum.</value>
        public double ZMax => Bounds[1].Z;

        /// <summary>
        /// The name of solid
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// The comments
        /// </summary>
        /// <value>The comments.</value>
        public List<string> Comments { get; set; }

        /// <summary>
        /// The file name
        /// </summary>
        /// <value>The name of the file.</value>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the units.
        /// </summary>
        /// <value>The units.</value>
        public UnitType Units { get; set; }

        /// <summary>
        /// The language
        /// </summary>
        /// <value>The language.</value>
        public string Language { get; set; }

        /// <summary>
        /// Gets the convex hull.
        /// </summary>
        /// <value>The convex hull.</value>
        [JsonIgnore]
        public ConvexHull3D ConvexHull { get; set; }

        /// <summary>
        /// The has uniform color
        /// </summary>
        /// <value><c>true</c> if this instance has uniform color; otherwise, <c>false</c>.</value>
        public bool HasUniformColor { get; set; }

        /// <summary>
        /// The solid color
        /// </summary>
        /// <value>The color of the solid.</value>
        public Color SolidColor { get; set; }

        /// <summary>
        /// Gets or sets the primitive objects that make up the solid
        /// </summary>
        /// <value>The primitives.</value>
        [JsonProperty(ItemTypeNameHandling = TypeNameHandling.Auto, TypeNameHandling = TypeNameHandling.Auto)]
        public List<PrimitiveSurface> Primitives { get; set; }

        /// <summary>
        /// Gets or sets the same tolerance.
        /// </summary>
        /// <value>The same tolerance.</value>
        public double SameTolerance { get; set; }

        #endregion Fields and Properties

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="Solid"/> class.
        /// </summary>
        /// <param name="units">The units.</param>
        /// <param name="name">The name.</param>
        /// <param name="filename">The filename.</param>
        /// <param name="comments">The comments.</param>
        /// <param name="language">The language.</param>
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
        /// <param name="transformationMatrix">The transformation matrix.</param>
        /// <returns>Solid.</returns>
        public abstract Solid TransformToNewSolid(Matrix4x4 transformationMatrix);

        /// <summary>
        /// Initializes a new instance of the <see cref="Solid"/> class.
        /// </summary>
        /// <param name="originalToBeCopied">The original to be copied.</param>
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
        /// <summary>
        /// The serialization data
        /// </summary>
        [JsonExtensionData]
        protected IDictionary<string, JToken> serializationData;
    }
}