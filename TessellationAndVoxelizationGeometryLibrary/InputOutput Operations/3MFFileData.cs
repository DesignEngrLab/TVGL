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
        ///     Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }


        /// <summary>
        ///     Opens the specified s.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <param name="inParallel">if set to <c>true</c> [in parallel].</param>
        /// <returns>List&lt;TessellatedSolid&gt;.</returns>
        internal static List<TessellatedSolid> Open(Stream s, bool inParallel = true)
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
                results.Add(new TessellatedSolid(threeMFData.Name,
                    threeMFObject.mesh.vertices.Select(v => new[] {v.x, v.y, v.z}).ToList(),
                    threeMFObject.mesh.triangles.Select(t => new[] {t.v1, t.v2, t.v3}).ToList(),
                    null));
            }
            return results;
        }

        /// <summary>
        ///     Tries the unzipped XML read.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="threeMFFileData">The threeMF file data.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        internal static bool TryUnzippedXMLRead(Stream stream, out ThreeMFFileData threeMFFileData)
        {
            threeMFFileData = null;
            try
            {
                var streamReader = new StreamReader(stream);
                var threeMFDeserializer = new XmlSerializer(typeof (ThreeMFFileData));
                threeMFFileData = (ThreeMFFileData) threeMFDeserializer.Deserialize(streamReader);
                threeMFFileData.Name = getNameFromStream(stream);
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