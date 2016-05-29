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
        
        /// <summary>
        /// Opens the specified s.
        /// </summary>
<<<<<<< HEAD
        /// <value>The name.</value>
        public string Name { get; set; }

        internal static List<TessellatedSolid> Open3MF(Stream originalStream, bool inParallel = true)
        {
            var result = new List<TessellatedSolid>();
            /*
            var zipStorer = ZipStorer.Open(originalStream);
            var dir = zipStorer.ReadCentralDir();
            var modelFiles = dir.Where(f => f.FilenameInZip.EndsWith(".model"));
            foreach (ZipFileEntry modelFile in modelFiles)
            {
                var s = new MemoryStream();
                zipStorer.ExtractFile(modelFile, s);
                result.AddRange(OpenModelFile(s, inParallel));
            }*/
            return result;
=======
        /// <param name="s">The s.</param>
        /// <param name="filename">The filename.</param>
        /// <param name="inParallel">if set to <c>true</c> [in parallel].</param>
        /// <returns>List&lt;TessellatedSolid&gt;.</returns>
        internal new static List<TessellatedSolid> Open(Stream s, string filename, bool inParallel = true)
        {
            var now = DateTime.Now;
            ThreeMFFileData threeMFData;
            // Try to read in BINARY format
            if (TryUnzippedXMLRead(s, out threeMFData))
                Message.output("Successfully read in ThreeMF file (" + (DateTime.Now - now) + ").", 3);
            else
            {
                // Reset position of stream
                s.Position = 0;
                // Read in ASCII format
                //if (threeMF.TryZippedXMLRead(s, out threeMFData))
                //    Message.output("Successfully unzipped and read in ASCII OFF file (" + (DateTime.Now - now) + ").",3);
                //else
                //{
                //    Message.output("Unable to read in ThreeMF file (" + (DateTime.Now - now) + ").",1);
                //    return null;
                //}
            }
            var results = new List<TessellatedSolid>();
            foreach (var threeMFObject in threeMFData.Objects)
            {
                results.Add(new TessellatedSolid(filename,
                    threeMFObject.mesh.vertices.Select(v => new[] {v.x, v.y, v.z}).ToList(),
                    threeMFObject.mesh.triangles.Select(t => new[] {t.v1, t.v2, t.v3}).ToList(),
                    null));
            }
            return results;
>>>>>>> master
        }

        internal static List<TessellatedSolid> OpenModelFile(Stream s, bool inParallel)
        {
            var now = DateTime.Now;
            ThreeMFFileData threeMFData;
            try
            {
<<<<<<< HEAD
                var streamReader = new StreamReader(s);
                var threeMFDeserializer = new XmlSerializer(typeof(ThreeMFFileData));
                threeMFData = (ThreeMFFileData)threeMFDeserializer.Deserialize(streamReader);
                threeMFData.Name = getNameFromStream(s);
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
                results.Add(new TessellatedSolid(threeMFData.Name,
                    solid.mesh.vertices.Select(v =>new[] {v.x,v.y,v.z}).ToList(),
                    solid.mesh.triangles.Select(t =>new[] {t.v1,t.v2,t.v3}).ToList(),
                   new [] { color}));
            }
            return results;
=======
                var streamReader = new StreamReader(stream);
                var threeMFDeserializer = new XmlSerializer(typeof (ThreeMFFileData));
                threeMFFileData = (ThreeMFFileData) threeMFDeserializer.Deserialize(streamReader);
            }
            catch (Exception exception)
            {
                Message.output("Unable to read ThreeMF file:" + exception, 1);
                return false;
            }
            return true;
        }

        /// <summary>
        ///     Tries the zipped XML read.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="threeMFFileData">The threeMF file data.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        /// <exception cref="NotImplementedException"></exception>
        /// <exception cref="System.NotImplementedException"></exception>
        internal static bool TryZippedXMLRead(Stream stream, out ThreeMFFileData threeMFFileData)
        {
            throw new NotImplementedException();
>>>>>>> master
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