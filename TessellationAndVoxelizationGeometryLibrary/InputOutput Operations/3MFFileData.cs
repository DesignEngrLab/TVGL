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
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Xml.Serialization;
using ClassesFor_3mf_Files;
using System.IO.Compression;

namespace TVGL.IOFunctions
{
    /// <summary>
    ///     Class ThreeMFFileData.
    /// </summary>
    [XmlRoot("threeMF")]
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
            Objects = new List<CT_Object>();
        }

        /// <summary>
        ///     Gets or sets the objects.
        /// </summary>
        /// <value>The objects.</value>
        [XmlElement("object")]
        public List<CT_Object> Objects { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether [unit specified].
        /// </summary>
        /// <value><c>true</c> if [unit specified]; otherwise, <c>false</c>.</value>
        [XmlIgnore]
        public bool unitSpecified { get; set; }

        /// <summary>
        ///     Gets or sets the version.
        /// </summary>
        /// <value>The version.</value>
        public double version { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether [version specified].
        /// </summary>
        /// <value><c>true</c> if [version specified]; otherwise, <c>false</c>.</value>
        [XmlIgnore]
        public bool versionSpecified { get; set; }

        /// <summary>
        ///     Gets or sets the language.
        /// </summary>
        /// <value>The language.</value>
        public string lang { get; set; }


        /// <param name="s">The s.</param>
        /// <param name="filename">The filename.</param>
        /// <param name="inParallel">if set to <c>true</c> [in parallel].</param>
        /// <returns>List&lt;TessellatedSolid&gt;.</returns>
        internal new static List<TessellatedSolid> Open(Stream s, string filename, bool inParallel = true)
        {
            var now = DateTime.Now;
            var result = new List<TessellatedSolid>();
            var archive =new ZipArchive(s);
            foreach (var modelFile in archive.Entries.Where(f => f.FullName.EndsWith(".model")))
            {
                var modelStream = modelFile.Open();
                result.AddRange(OpenModelFile(modelStream, filename,inParallel));
            }
            return result;
        }

        internal static List<TessellatedSolid> OpenModelFile(Stream s, string filename, bool inParallel)
        {
            var now = DateTime.Now;
            ThreeMFFileData threeMFData;
            try
            {
                var streamReader = new StreamReader(s);
                var threeMFDeserializer = new XmlSerializer(typeof(ThreeMFFileData));
                threeMFData = (ThreeMFFileData)threeMFDeserializer.Deserialize(streamReader);
                Message.output("Successfully read in 3MF file (" + (DateTime.Now - now) + ").", 3);
            }
            catch (Exception exception)
            {
                Message.output("Unable to read in 3MF file (" + (DateTime.Now - now) + ").", 1);
                return null;
            }
            var results = new List<TessellatedSolid>();
            foreach (var solid in threeMFData.Objects)
            {
                Color color = new Color(KnownColors.Azure);
                /*
                 if (solid.matid)
                 if (solid.mesh.triangles.Any(t => t.color != null))
                 {
                     colors = new List<Color>();
                     var solidColor = new Color(Constants.DefaultColor);
                     foreach (var amfTriangle in solid.mesh.volume.Triangles)
                         colors.Add((amfTriangle.color != null) ? new Color(amfTriangle.color) : solidColor);
                 }
                 */
                results.Add(new TessellatedSolid(filename,
                    solid.mesh.vertices.Select(v => new[] { v.x, v.y, v.z }).ToList(),
                    solid.mesh.triangles.Select(t => new[] { t.v1, t.v2, t.v3 }).ToList(),
                   new[] { color }));
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