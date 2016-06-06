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
        private const string defXMLNameSpace = "http://schemas.microsoft.com/3dmanufacturing/core/2015/02";
       
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
        [DefaultValue(UnitType.unspecified)]
        [XmlAttribute]
        public UnitType unit { get; set; }

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

                    var results = new List<TessellatedSolid>();
                    threeMFData.Name = GetNameFromFileName(filename);
                    var nameIndex =
                        threeMFData.metadata.FindIndex(
                            md => md != null && (md.type.Equals("name", StringComparison.CurrentCultureIgnoreCase) ||
                                                 md.type.Equals("title", StringComparison.CurrentCultureIgnoreCase)));
                    if (nameIndex != -1) threeMFData.Name = threeMFData.metadata[nameIndex].Value;
                    foreach (var item in threeMFData.build.Items)
                    {
                        results.AddRange(TessellatedSolidsFromIDAndTransform(item.objectid, item.transformMatrix,
                            threeMFData.resources, threeMFData.Name + "_"));
                    }

                    Message.output("Successfully read in 3MF file (" + (DateTime.Now - now) + ").", 3);
                    return results;
                }
            }
            catch (Exception exception)
            {
                Message.output("Unable to read in model file (" + (DateTime.Now - now) + ").", 1);
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
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                ZipArchiveEntry model = archive.CreateEntry("3D/3dmodel.model");
                SaveModel(model.Open(), solids);
            }
            return true;
        }

        /// <summary>
        ///     Saves the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="solids">The solids.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        /// <exception cref="NotImplementedException"></exception>
        internal static bool SaveModel(Stream stream, IList<TessellatedSolid> solids)
        {
            var objects = new List<threemfclasses.Object>();
            var baseMats = new BaseMaterials { id = 1 };
            var materials = new List<Material>();
            var colors = new List<Color3MF>();
            foreach (var solid in solids)
            {
                var thisObject = new threemfclasses.Object { name = solid.Name };
                List<Triangle> triangles;
                if (solid.HasUniformColor)
                {
                    if (solid.Faces[0].Color != null)
                    {
                        var thisColor = colors.Find(col => col.colorString.Equals(solid.Faces[0].Color.ToString()));
                        if (thisColor != null)
                        {
                            var thisMaterial = materials.Find(mat => mat.colorid == thisColor.id);
                            thisObject.MaterialID = thisMaterial.id;
                        }
                        else
                        {
                            var newID = colors.Count + 1;
                            colors.Add(new Color3MF { id = newID, colorString = solid.Faces[0].Color.ToString() });
                            materials.Add(new Material { colorid = newID, id = materials.Count + 1 });
                            thisObject.MaterialID = materials.Count + 1;
                        }
                    }
                    triangles =
                        solid.Faces.Select(
                            f =>
                                new Triangle
                                {
                                    v1 = f.Vertices[0].IndexInList,
                                    v2 = f.Vertices[1].IndexInList,
                                    v3 = f.Vertices[2].IndexInList
                                }).ToList();
                }
                else
                {
                    triangles = new List<Triangle>();
                    foreach (var face in solid.Faces)
                    {
                        int colorIndex = baseMats.bases.FindIndex(col => col.colorString.Equals(face.Color.ToString()));
                        if (colorIndex == -1)
                        {
                            colorIndex = baseMats.bases.Count;
                            baseMats.bases.Add(new Base { colorString = face.Color.ToString() });
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
                unit = solids[0].Units,
                build = new Build { Items = objects.Select(o => new Item { objectid = o.id }).ToList() },
                resources =
                    new Resources { basematerials = (new[] { baseMats }).ToList(), colors = colors, materials = materials, objects = objects }
            };
            try
            {
                using (var writer = XmlWriter.Create(stream))
                {
                    var serializer = new XmlSerializer(typeof(ThreeMFFileData), defXMLNameSpace);
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
    }
}