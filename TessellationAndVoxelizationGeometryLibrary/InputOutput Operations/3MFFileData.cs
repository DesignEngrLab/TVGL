// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 02-27-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 05-28-2016
// ***********************************************************************
// <copyright file="3MFFileData.cs" company="Design Engineering Lab">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using System.IO.Compression;
using System.Xml;
using StarMathLib;
using TVGL.IOFunctions.threemfclasses;

namespace TVGL.IOFunctions
{
    /// <summary>
    ///     Class ThreeMFFileData.
    /// </summary>
    [XmlRoot("model")]
#if help
    internal class ThreeMFFileData : IO
#else
    public class ThreeMFFileData : IO
#endif
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ThreeMFFileData" /> class.
        /// </summary>
        public ThreeMFFileData()
        {
            metadata = new List<Metadata>();
        }
        /// <summary>
        /// Gets or sets the metadata.
        /// </summary>
        /// <value>The metadata.</value>
        [XmlElement]
        public List<Metadata> metadata { get; set; }
        /// <summary>
        /// Gets or sets the resources.
        /// </summary>
        /// <value>The resources.</value>
        [XmlElement]
        public Resources resources { get; set; }
        /// <summary>
        /// Gets or sets the build.
        /// </summary>
        /// <value>The build.</value>
        [XmlElement]
        public Build build { get; set; }

        /// <summary>
        /// Gets or sets the unit.
        /// </summary>
        /// <value>The unit.</value>
        [DefaultValue(Unit.unspecified)]
        [XmlAttribute]
        public Unit unit { get; set; }

        /// <summary>
        /// Gets or sets the language.
        /// </summary>
        /// <value>The language.</value>
        [XmlAttribute("lang")]
        public string language { get; set; }
        /// <summary>
        /// Gets or sets the requiredextensions.
        /// </summary>
        /// <value>The requiredextensions.</value>
        public string requiredextensions { get; set; }

        public string Name { get; set; }

        /// <param name="s">The s.</param>
        /// <param name="filename">The filename.</param>
        /// <param name="inParallel">if set to <c>true</c> [in parallel].</param>
        /// <returns>List&lt;TessellatedSolid&gt;.</returns>
        internal new static List<TessellatedSolid> Open(Stream s, string filename, bool inParallel = true)
        {
            var now = DateTime.Now;
            var result = new List<TessellatedSolid>();
            var archive = new ZipArchive(s);
            foreach (var modelFile in archive.Entries.Where(f => f.FullName.EndsWith(".model")))
            {
                var modelStream = modelFile.Open();
                result.AddRange(OpenModelFile(modelStream, filename, inParallel));
            }
            return result;
        }

        internal static List<TessellatedSolid> OpenModelFile(Stream s, string filename, bool inParallel)
        {
            var now = DateTime.Now;
            ThreeMFFileData threeMFData = null;
            try
            {
                var settings = new XmlReaderSettings
                {
                    IgnoreComments = true,
                    IgnoreProcessingInstructions = true,
                    IgnoreWhitespace = true
                };
                using (var reader = XmlReader.Create(s, settings))
                {
                    if (reader.IsStartElement("model"))
                    {
                        string defaultNamespace = reader["xmlns"];
                        XmlSerializer serializer = new XmlSerializer(typeof(ThreeMFFileData), defaultNamespace);
                        threeMFData = (ThreeMFFileData)serializer.Deserialize(reader);
                    }
                }
                Message.output("Successfully read in 3MF file (" + (DateTime.Now - now) + ").", 3);
            }
            catch (Exception exception)
            {
                Message.output("Unable to read in model file (" + (DateTime.Now - now) + ").", 1);
                return null;
            }
            var results = new List<TessellatedSolid>();
            threeMFData.Name = GetNameFromFileName(filename);
            var nameIndex =
                threeMFData.metadata.FindIndex(md => md != null && (md.type.Equals("name", StringComparison.CurrentCultureIgnoreCase) ||
            md.type.Equals("title", StringComparison.CurrentCultureIgnoreCase)));
            if (nameIndex != -1) threeMFData.Name = threeMFData.metadata[nameIndex].Value;
            foreach (var item in threeMFData.build.Items)
            {
                results.AddRange(TessellatedSolidsFromIDAndTransform(item.objectid, item.transformMatrix, threeMFData.resources, threeMFData.Name + "_"));
            }
            return results;
        }
        private static IEnumerable<TessellatedSolid> TessellatedSolidsFromIDAndTransform(int objectid, double[,] transformMatrix, Resources resources, string name)
        {
            var solid = resources.objects.First(obj => obj.id == objectid);
            List<TessellatedSolid> result = TessellatedSolidsFromObject(solid, resources, name);
            if (transformMatrix != null)
                foreach (var ts in result)
                    ts.Transform(transformMatrix);
            return result;
        }
        private static List<TessellatedSolid> TessellatedSolidsFromObject(threemfclasses.Object obj, Resources resources, string name)
        {
            name += obj.name + "_" + obj.id;
            var result = new List<TessellatedSolid>();
            if (obj.mesh != null) result.Add(TessellatedSolidFromMesh(obj.mesh, obj.MaterialID, name, resources));
            foreach (var comp in obj.components)
            {
                result.AddRange(TessellatedSolidsFromComponent(comp, resources, name));
            }
            return result;
        }
        private static IEnumerable<TessellatedSolid> TessellatedSolidsFromComponent(Component comp, Resources resources, string name)
        { return TessellatedSolidsFromIDAndTransform(comp.objectid, comp.transformMatrix, resources, name); }
        private static TessellatedSolid TessellatedSolidFromMesh(Mesh mesh, int materialID, string name, Resources resources)
        {
            Color defaultColor = new Color(Constants.DefaultColor);
            if (materialID >= 0)
            {
                var material = resources.materials.FirstOrDefault(mat => mat.id == materialID);
                if (material != null)
                {
                    var defaultColorXml =
                        resources.colors.FirstOrDefault(col => col.id == material.colorid);
                    if (defaultColorXml != null) defaultColor = defaultColorXml.color;
                }
            }
            var verts = mesh.vertices.Select(v => new[] { v.x, v.y, v.z }).ToList();

            Color[] colors = null;
            var uniformColor = true;
            var numTriangles = mesh.triangles.Count;
            for (int j = 0; j < numTriangles; j++)
            {
                var triangle = mesh.triangles[j];
                if (triangle.pid == -1) continue;
                if (triangle.p1 == -1) continue;
                var baseMaterial =
                    resources.basematerials.FirstOrDefault(bm => bm.id == triangle.pid);
                if (baseMaterial == null) continue;
                var baseColor = baseMaterial.bases[triangle.p1];
                if (j == 0)
                {
                    defaultColor = baseColor.color;
                    continue;
                }
                if (uniformColor && baseColor.color.Equals(defaultColor)) continue;
                uniformColor = false;
                if (colors == null) colors = new Color[mesh.triangles.Count];
                colors[j] = baseColor.color;
            }
            if (uniformColor) colors = new[] { defaultColor };
            else
                for (int j = 0; j < numTriangles; j++)
                    if (colors[j] == null) colors[j] = defaultColor;
            return new TessellatedSolid(name, verts,
                mesh.triangles.Select(t => new[] { t.v1, t.v2, t.v3 }).ToList(), colors);
        }

        /// <summary>
        ///     Saves the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="solids">The solids.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        /// <exception cref="NotImplementedException"></exception>
        internal static bool Save(Stream stream, IList<TessellatedSolid> solids)
        {
            throw new NotImplementedException();
        }
    }
}