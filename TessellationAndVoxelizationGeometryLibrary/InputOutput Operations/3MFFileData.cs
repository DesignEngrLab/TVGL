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
            ThreeMFFileData threeMFData=null;
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
                        XmlSerializer serializer = new XmlSerializer(typeof (ThreeMFFileData), defaultNamespace);
                        threeMFData = (ThreeMFFileData) serializer.Deserialize(reader);
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
                var solid = threeMFData.resources.objects.First(obj => obj.id == item.objectid);
                var material = threeMFData.resources.materials.FirstOrDefault(mat => mat.id == solid.MaterialID) ??
                               new Material { colorid = -1, name = "unspecified" };
                var defaultColorXml = threeMFData.resources.colors.FirstOrDefault(col => col.id == material.colorid);
                var defaultColor = defaultColorXml == null ? new Color(Constants.DefaultColor) : defaultColorXml.color;
                var verts = solid.mesh.vertices.Select(v => new[] { v.x, v.y, v.z }).ToList();
                var transform = item.transformMatrix;
                if (transform != null)
                {
                    foreach (var vert in verts)
                    {
                        var newCoord = transform.multiply(new[] { vert[0], vert[1], vert[2], 1 });
                        vert[0] = newCoord[0]; vert[1] = newCoord[1]; vert[2] = newCoord[2];
                    }
                }
                results.Add(new TessellatedSolid(threeMFData.Name + "_" + solid.name + "_" + solid.id, verts,
                             solid.mesh.triangles.Select(t => new[] { t.v1, t.v2, t.v3 }).ToList(),
                            new[] { defaultColor }));
            }
            return results;
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