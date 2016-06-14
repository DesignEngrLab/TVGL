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
        private const string defXMLNameSpaceModel = "http://schemas.microsoft.com/3dmanufacturing/core/2015/02";

        private const string defXMLNameSpaceContentTypes =
            "http://schemas.openxmlformats.org/package/2006/content-types";

        private const string defXMLNameSpaceRelationships =
            "http://schemas.openxmlformats.org/package/2006/relationships";


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

        internal new List<string> Comments
        {
            get
            {
                var result = metadata.Select(m => m.type + " ==> " + m.Value).ToList();
                result.AddRange(_comments);
                return result;
            }
        }

        /// <summary>
        /// Gets or sets the requiredextensions.
        /// </summary>
        /// <value>The requiredextensions.</value>
        public string requiredextensions { get; set; }


        /// <param name="s">The s.</param>
        /// <param name="filename">The filename.</param>
        /// <param name="inParallel">if set to <c>true</c> [in parallel].</param>
        /// <returns>List&lt;TessellatedSolid&gt;.</returns>
        internal new static List<TessellatedSolid> OpenSolids(Stream s, string filename, bool inParallel = true)
        {
#if net40
            throw new NotSupportedException("The loading or saving of .3mf files are not supported in the .NET4.0 version of TVGL.");
#else
            var now = DateTime.Now;
            var result = new List<TessellatedSolid>();
            var archive = new ZipArchive(s);
            foreach (var modelFile in archive.Entries.Where(f => f.FullName.EndsWith(".model")))
            {
                var modelStream = modelFile.Open();
                result.AddRange(OpenModelFile(modelStream, filename));
            }
            return result;
#endif
        }

        internal static List<TessellatedSolid> OpenModelFile(Stream s, string filename)
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
                    threeMFData.FileName = filename;
                    var results = new List<TessellatedSolid>();
                    threeMFData.Name = GetNameFromFileName(filename);
                    var nameIndex =
                        threeMFData.metadata.FindIndex(
                            md => md != null && (md.type.Equals("name", StringComparison.CurrentCultureIgnoreCase) ||
                                                 md.type.Equals("title", StringComparison.CurrentCultureIgnoreCase)));
                    if (nameIndex != -1)
                    {
                        threeMFData.Name = threeMFData.metadata[nameIndex].Value;
                        threeMFData.metadata.RemoveAt(nameIndex);
                    }
                    foreach (var item in threeMFData.build.Items)
                    {
                        results.AddRange(TessellatedSolidsFromIDAndTransform(item.objectid, item.transformMatrix,
                            threeMFData.resources, threeMFData.Name + "_"));
                    }

                    Message.output("Successfully read in 3Dmodel file (" + (DateTime.Now - now) + ").", 3);
                    return results;
                }
            }
            catch (Exception exception)
            {
                Message.output("Unable to read in 3Dmodel file.", 1);
                return null;
            }
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
            return new TessellatedSolid(verts,
                mesh.triangles.Select(t => new[] { t.v1, t.v2, t.v3 }).ToList(), colors);//todo
        }

        /// <summary>
        ///     Saves the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="solids">The solids.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        internal static bool Save(Stream stream, IList<TessellatedSolid> solids)
        {
#if net40
            throw new NotSupportedException("The loading or saving of .3mf files are not supported in the .NET4.0 version of TVGL.");
#else
            ZipArchiveEntry entry;
            Stream entryStream;
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                entry = archive.CreateEntry("3D/3dmodel.model");
                using (entryStream = entry.Open())
                    SaveModel(entryStream, solids);
                archive.CreateEntry("Metadata/thumbnail.png");
                entry = archive.CreateEntry("_rels/.rels");
                using (entryStream = entry.Open())
                    SaveRelationships(entryStream);
                entry = archive.CreateEntry("[Content_Types].xml");
                using (entryStream = entry.Open())
                    SaveContentTypes(entryStream);
            }
            return true;
#endif
        }

        /// <summary>
        ///     Saves the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="solids">The solids.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        internal static bool SaveModel(Stream stream, IList<TessellatedSolid> solids)
        {
            var objects = new List<threemfclasses.Object>();
            var baseMats = new BaseMaterials { id = 1 };
            for (int i = 0; i < solids.Count; i++)
            {
                var solid = solids[i];
                var thisObject = new threemfclasses.Object { name = solid.Name, id = i + 2 };
                // this is "+ 2" since the id's start with 1 instead of 0 plus BaseMaterials is typically 1, so start at 2.
                var triangles = new List<Triangle>();

                foreach (var face in solid.Faces)
                {
                    var colString = (face.Color ?? solid.SolidColor ?? new Color(Constants.DefaultColor)).ToString();
                    int colorIndex = baseMats.bases.FindIndex(col => col.colorString.Equals(colString));
                    if (colorIndex == -1)
                    {
                        colorIndex = baseMats.bases.Count;
                        baseMats.bases.Add(new Base { colorString = colString });
                    }
                    triangles.Add(new Triangle
                    {
                        v1 = face.Vertices[0].IndexInList,
                        v2 = face.Vertices[1].IndexInList,
                        v3 = face.Vertices[2].IndexInList,
                        pid = 1,
                        p1 = colorIndex
                    });
                }
                thisObject.mesh = new Mesh
                {
                    vertices = solid.Vertices.Select(v => new threemfclasses.Vertex
                    { x = v.X, y = v.Y, z = v.Z }).ToList(),
                    triangles = triangles
                };
                objects.Add(thisObject);
            }
            ThreeMFFileData threeMFData = new ThreeMFFileData
            {
                Units = solids[0].Units,
                build = new Build { Items = objects.Select(o => new Item { objectid = o.id }).ToList() },
                resources =
                    new Resources
                    {
                        basematerials = (new[] { baseMats }).ToList(), //colors = colors, materials = materials,
                        objects = objects
                    }
            };
            try
            {
                using (var writer = XmlWriter.Create(stream))
                {
                    var serializer = new XmlSerializer(typeof(ThreeMFFileData), defXMLNameSpaceModel);
                    serializer.Serialize(writer, threeMFData);
                }
                Message.output("Successfully wrote 3MF file to stream.", 3);
                return true;
            }
            catch (Exception exception)
            {
                Message.output("Unable to write in model file.", 1);
                return false;
            }
        }

        private static void SaveRelationships(Stream stream)
        {
            //[XmlArrayItem("vertex", IsNullable = false)]
            var rels = new Relationships
            {
                rels = new[]
            {
                new Relationship
                {
                    Target = "/3D/3dmodel.model",
                    Id = "rel-1",
                    Type = "http://schemas.microsoft.com/3dmanufacturing/2013/01/3dmodel"
                },
                new Relationship
                {
                    Target = "/Metadata/thumbnail.png",
                    Id = "rel0",
                    Type = "http://schemas.openxmlformats.org/package/2006/relationships/metadata/thumbnail"
                }
            }
            };

            using (var writer = XmlWriter.Create(stream))
            {
                var serializer = new XmlSerializer(typeof(Relationships), defXMLNameSpaceRelationships);
                serializer.Serialize(writer, rels);
            }
        }

        private static void SaveContentTypes(Stream stream)
        {
            var defaults = new List<Default>
            {
                new Default
                {
                    Extension = "rels",
                    ContentType = "application/vnd.openxmlformats-package.relationships+xml"
                },
                new Default
                {
                    Extension = "model",
                    ContentType = "application/vnd.ms-package.3dmanufacturing-3dmodel+xml"
                },
                new Default {Extension = "png", ContentType = "image/png"},

            };
            var types = new Types { Defaults = defaults };

            using (var writer = XmlWriter.Create(stream))
            {
                var serializer = new XmlSerializer(typeof(Types), defXMLNameSpaceContentTypes);
                serializer.Serialize(writer, types);
            }
        }
    }
}