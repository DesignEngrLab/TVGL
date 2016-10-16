// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 02-27-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 05-28-2016
// ***********************************************************************
// <copyright file="AMFFileData.cs" company="Design Engineering Lab">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using StarMathLib;
using TVGL.IOFunctions.amfclasses;

namespace TVGL.IOFunctions
{
    /// <summary>
    ///     Class AMFFileData.
    /// </summary>
    [XmlRoot("amf")]
#if help
    internal class AMFFileData : IO
#else
    public class AMFFileData : IO
#endif
    {
        #region Constructor
        /// <summary>
        ///     Initializes a new instance of the <see cref="AMFFileData" /> class.
        /// </summary>
#if help
    internal AMFFileData()
#else
        public AMFFileData()
#endif
        {
            Objects = new List<AMF_Object>();
            Textures = new List<AMF_Texture>();
        }
        #endregion

        #region Fields and Properties
#if help
        internal List<AMF_Texture> Textures { get; set; }
        internal double version { get; set; }
        internal List<AMF_Object> Objects { get; set; }
        internal List<AMF_Constellation> Constellations { get; set; }
#else
        /// <summary>
        ///     Gets or sets the constellations.
        /// </summary>
        /// <value>The constellations.</value>
        [XmlElement("constellation")]
        public List<AMF_Constellation> Constellations { get; set; }

        /// <summary>
        ///     Gets or sets the objects.
        /// </summary>
        /// <value>The objects.</value>
        [XmlElement("object")]
        public List<AMF_Object> Objects { get; set; }

        /// <summary>
        ///     Gets or sets the textures.
        /// </summary>
        /// <value>The textures.</value>
        [XmlElement("texture")]
        public List<AMF_Texture> Textures { get; set; }

        /// <summary>
        ///     Gets or sets the version.
        /// </summary>
        /// <value>The version.</value>
        public double version { get; set; }
#endif
        #endregion
        #region Open Solids
        /// <summary>
        /// Opens the specified s.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <param name="filename">The filename.</param>
        /// <returns>List&lt;TessellatedSolid&gt;.</returns>
        internal static List<TessellatedSolid> OpenSolids(Stream s, string filename)
        {
            var now = DateTime.Now;
            AMFFileData amfData;
            try
            {
                var streamReader = new StreamReader(s);
                var amfDeserializer = new XmlSerializer(typeof(AMFFileData));
                amfData = (AMFFileData)amfDeserializer.Deserialize(streamReader);
                Message.output("Successfully read in AMF file (" + (DateTime.Now - now) + ").", 3);
            }
            catch (Exception exception)
            {
                Message.output("Unable to read in AMF file (" + (DateTime.Now - now) + ").", 1);
                Message.output("Exception: " + exception.Message, 3);
                return null;
            }
            amfData.FileName = filename;
            amfData.Name = GetNameFromFileName(filename);
            var results = new List<TessellatedSolid>();
            var objectDict = new Dictionary<int, AMF_Object>();
            foreach (var amfObject in amfData.Objects)
                objectDict.Add(amfObject.id, amfObject);
            var objectsUsed = new List<int>();
            foreach (var amfConstellation in amfData.Constellations)
            {
                foreach (var amfInstance in amfConstellation.Instances)
                {
                    if (!objectDict.ContainsKey(amfInstance.objectid)) continue;
                    results.Add(amfData.CreateSolid(objectDict[amfInstance.objectid], amfInstance));
                    objectsUsed.Add(amfInstance.objectid);
                }
            }
            if (objectsUsed.Any()) return results;
            foreach (var amfObject in amfData.Objects)
                results.Add(amfData.CreateSolid(amfObject));
            return results;
        }

        private TessellatedSolid CreateSolid(AMF_Object amfObject, AMF_Instance amfInstance = null)
        {
            List<Color> colors = null;
            if (amfObject.color != null)
            {
                colors = new List<Color>();
                var solidColor = new Color(amfObject.color);
                foreach (var amfTriangle in amfObject.mesh.volume.Triangles)
                    colors.Add(amfTriangle.color != null ? new Color(amfTriangle.color) : solidColor);
            }
            else if (amfObject.mesh.volume.color != null)
            {
                colors = new List<Color>();
                var solidColor = new Color(amfObject.mesh.volume.color);
                foreach (var amfTriangle in amfObject.mesh.volume.Triangles)
                    colors.Add(amfTriangle.color != null ? new Color(amfTriangle.color) : solidColor);
            }
            else if (amfObject.mesh.volume.Triangles.Any(t => t.color != null))
            {
                colors = new List<Color>();
                var solidColor = new Color(Constants.DefaultColor);
                foreach (var amfTriangle in amfObject.mesh.volume.Triangles)
                    colors.Add(amfTriangle.color != null ? new Color(amfTriangle.color) : solidColor);
            }
            var name = this.Name;
            var nameIndex =
                amfObject.metadata.FindIndex(md => md != null && md.type.Equals("name", StringComparison.CurrentCultureIgnoreCase));
            if (nameIndex != -1)
            {
                name = amfObject.metadata[nameIndex].Value;
                amfObject.metadata.RemoveAt(nameIndex);
            }
            var vertices = amfObject.mesh.vertices.Vertices.Select(v => v.coordinates.AsArray).ToList();
            if (amfInstance != null &&
                (amfInstance.deltaxSpecified || amfInstance.deltaySpecified || amfInstance.deltazSpecified
                 || amfInstance.rxSpecified || amfInstance.rySpecified || amfInstance.rzSpecified))
            {
                var tMatrix =
                    StarMath.RotationX(amfInstance.rx)
                        .multiply(StarMath.RotationY(amfInstance.ry))
                        .multiply(StarMath.RotationZ(amfInstance.rz));
                tMatrix =
                    StarMath.Translate(amfInstance.deltax, amfInstance.deltay, amfInstance.deltaz).multiply(tMatrix);
                foreach (var coord in vertices)
                {
                    var coordWith1 = new[] { coord[0], coord[1], coord[2], 1.0 };
                    coordWith1 = tMatrix.multiply(coordWith1);
                    coord[0] = coordWith1[0];
                    coord[1] = coordWith1[1];
                    coord[2] = coordWith1[2];
                }
            }
            return new TessellatedSolid(vertices,
                amfObject.mesh.volume.Triangles.Select(t => t.VertexIndices).ToList(),
                colors, this.Units, name + "_" + amfObject.id, this.FileName,
                amfObject.metadata.Select(md => md.ToString()).ToList(), this.Language);
        }

        #endregion
        #region Save Solids
        /// <summary>
        /// Saves the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="solids">The solids.</param>
        /// <returns>
        ///   <c>true</c> if XXXX, <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="NotImplementedException"></exception>
        internal static bool SaveSolids(Stream stream, IList<TessellatedSolid> solids)
        {
            var amfFileData = new AMFFileData(solids);
            try
            {
                using (var writer = XmlWriter.Create(stream))
                {
                    writer.WriteComment(tvglDateMarkText);
                    if (!string.IsNullOrWhiteSpace(solids[0].FileName))
                        writer.WriteComment("Originally loaded from " + solids[0].FileName);
                    var serializer = new XmlSerializer(typeof(AMFFileData));
                    serializer.Serialize(writer, amfFileData);
                }
                Message.output("Successfully wrote AMF file to stream.", 3);
                return true;
            }
            catch (Exception exception)
            {
                Message.output("Unable to write in model file.", 1);
                Message.output("Exception: " + exception.Message, 3);
                return false;
            }
        }
        // this is used by the save method above
        private AMFFileData(IList<TessellatedSolid> solids) : this()
        {
            this.Name = solids[0].Name.TrimEnd('_', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0');
            this.FileName = solids[0].FileName;
            this.Units = solids[0].Units;
            this.Language = solids[0].Language;
            for (int i = 0; i < solids.Count; i++)
            {
                var solid = solids[i];
                var metaData = new List<AMF_Metadata>();
                if (solid.Comments != null)
                {
                    foreach (var comment in solid.Comments.Where(string.IsNullOrWhiteSpace))
                    {
                        var arrowIndex = comment.IndexOf("==>");
                        if (arrowIndex == -1) this.Comments.Add(comment);
                        else
                        {
                            var endOfType = arrowIndex - 1;
                            var beginOfValue = arrowIndex + 3;  //todo: check this -1 and +3
                            metaData.Add(new AMF_Metadata
                            {
                                type = comment.Substring(0, endOfType),
                                Value = comment.Substring(beginOfValue)
                            });
                        }
                    }
                }
                var vertexList = new AMF_Vertices
                {
                    Vertices = solid.Vertices.Select(v => new AMF_Vertex
                    {
                        coordinates = new AMF_Coordinates { x = v.X, y = v.Y, z = v.Z }
                    }).ToList()
                };
                var volume = new AMF_Volume();
                if (solid.HasUniformColor)
                {
                    var colorFromSolid = solid.SolidColor ?? new Color(Constants.DefaultColor);
                    volume.color = new AMF_Color
                    {
                        a = colorFromSolid.Af,
                        b = colorFromSolid.Bf,
                        g = colorFromSolid.Gf,
                        r = colorFromSolid.Rf
                    };
                    volume.Triangles = solid.Faces.Select(f => new AMF_Triangle
                    {
                        v1 = f.Vertices[0].IndexInList,
                        v2 = f.Vertices[1].IndexInList,
                        v3 = f.Vertices[2].IndexInList
                    }).ToList();
                }
                else
                {
                    volume.Triangles = solid.Faces.Select(f => new AMF_Triangle
                    {
                        v1 = f.Vertices[0].IndexInList,
                        v2 = f.Vertices[1].IndexInList,
                        v3 = f.Vertices[2].IndexInList,
                        color = new AMF_Color
                        {
                            a = f.Color.Af,
                            b = f.Color.Bf,
                            g = f.Color.Gf,
                            r = f.Color.Rf
                        }
                    }).ToList();
                }
                Objects.Add(new AMF_Object
                {
                    id = i,
                    mesh = new AMF_Mesh { vertices = vertexList, volume = volume },
                    metadata = metaData
                });
            }
        }
        #endregion
    }
}