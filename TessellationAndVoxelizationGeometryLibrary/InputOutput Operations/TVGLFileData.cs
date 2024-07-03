// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-07-2023
// ***********************************************************************
// <copyright file="IOFunctions.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.IO.Compression;
using System.IO.Pipes;
using System.Reflection;

namespace TVGL
{
    /// <summary>
    /// The IO or input/output class contains static functions for saving and loading files in common formats.
    /// Note that as a Portable class library, these IO functions cannot interact with your file system. In order
    /// to load or save, the filename is not enough. One needs to provide the stream.
    /// </summary>
    public class TVGLFileData : IO
    {
        #region Open/Load/Read
        /// <summary>
        /// Opens the TVGLz.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <param name="solidAssembly">The solid assembly.</param>
        //internal static void OpenTVGLz(Stream s, out SolidAssembly solidAssembly, TessellatedSolidBuildOptions tsBuildOptions = null)
        //{
        //    using var archive = new ZipArchive(s, ZipArchiveMode.Read, true);
        //    var file = archive.GetEntry("TVGL");
        //    var stream = file.Open();
        //    OpenTVGL(stream, out solidAssembly, tsBuildOptions);
        //}
        /// <summary>
        /// Opens the TVGLz.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <param name="solid">The solid assembly.</param>
        internal static bool OpenTVGLz<T>(Stream s, out T solid, TessellatedSolidBuildOptions tsBuildOptions = null)
        where T : Solid
        {
            using var archive = new ZipArchive(s, ZipArchiveMode.Read, true);
            var file = archive.GetEntry("TVGL");
            var stream = file.Open();
            return OpenTVGL(stream, out solid, tsBuildOptions);
        }
        /// <summary>
        /// Opens the TVGLz.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <param name="solid">The solid assembly.</param>
        internal static bool OpenTVGLz(Stream s, out SolidAssembly solid, TessellatedSolidBuildOptions tsBuildOptions = null)
        {
            using var archive = new ZipArchive(s, ZipArchiveMode.Read, true);
            var file = archive.GetEntry("TVGL");
            var stream = file.Open();
            return OpenTVGL(stream, out solid, tsBuildOptions);
        }

        /// <summary>
        /// Opens the solid (TessellatedSolid, CrossSectionSolid, VoxelizedSolid) from the stream.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <param name="solid">The solid.</param>
        internal static bool OpenTVGL(Stream s, out SolidAssembly solidAssembly, TessellatedSolidBuildOptions tsBuildOptions = null)
        {
            using var reader = new JsonTextReader(new StreamReader(s));
            SolidAssembly.StreamRead(reader, out solidAssembly, tsBuildOptions);
            return true;
        }
        /// <summary>
        /// Opens the solid (TessellatedSolid, CrossSectionSolid, VoxelizedSolid) from the stream.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <param name="solid">The solid.</param>
        internal static bool OpenTVGL<T>(Stream s, out T solid, TessellatedSolidBuildOptions tsBuildOptions = null)
        where T : Solid
        {
            using var reader = new JsonTextReader(new StreamReader(s));
            SolidAssembly.StreamRead(reader, out var solidAssembly, tsBuildOptions);

            var solidInner = GetMostSignificantSolid(solidAssembly, out _);
            solid = solidInner as T;

            return solid != null;
        }

        #endregion
        #region Save/Write

        internal static bool SaveToTVGLz(Stream stream, Solid solid)
        {
            Stream entryStream;
            using var archive = new ZipArchive(stream, ZipArchiveMode.Create);
            var entry = archive.CreateEntry("TVGL");
            using (entryStream = entry.Open())
                return SaveToTVGL(entryStream, solid);
        }

        internal static bool SaveToTVGLz(Stream stream, SolidAssembly solidAssembly)
        {
            Stream entryStream;
            using var archive = new ZipArchive(stream, ZipArchiveMode.Create);
            var entry = archive.CreateEntry("TVGL");
            using (entryStream = entry.Open())
                return SaveToTVGL(entryStream, solidAssembly);
        }

        /// <summary>
        /// Saves to TVGL.
        /// </summary>
        /// <param name="fileStream">The file stream.</param>
        /// <param name="solidAssembly">The solid assembly.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        internal static bool SaveToTVGL(Stream fileStream, SolidAssembly solidAssembly)
        {
            try
            {
                using (var streamWriter = new StreamWriter(fileStream))
                using (var writer = new JsonTextWriter(streamWriter))
                {
                    writer.Formatting = Formatting.None;
                    solidAssembly.StreamWrite(writer);
                    writer.Close();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Saves to TVGL.
        /// </summary>
        /// <param name="fileStream">The file stream.</param>
        /// <param name="solidAssembly">The solid assembly.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        internal static bool SaveToTVGL(Stream stream, Solid solid)
        {
            try
            {
                using (var streamWriter = new StreamWriter(stream))
                using (var writer = new JsonTextWriter(streamWriter))
                {
                    writer.Formatting = Formatting.None;
                    writer.WriteStartObject();
                    writer.WritePropertyName("TessellatedSolid");
                    var ts = (TessellatedSolid)solid;
                    var useOnSerialization = false;//Don't serialize the TessellatedSolids directly. We are doing to do this with a stream.
                    //var jsonString = ((string)JsonConvert.SerializeObject(ts, Newtonsoft.Json.Formatting.None));//ignore last character so that the object is not closed
                                                                                                                //JSON uses a key value structure. We already wrote the key, now we write the value to end this object. This adds its own comma.
                    //writer.WriteRawValue(jsonString);
                    writer.WriteStartObject();
                    {
                        ts.StreamWrite(writer);
                    }
                    writer.WriteEndObject();
                    writer.Close();
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        //try
        //{
        //    JsonSerializer serializer = new JsonSerializer
        //    {
        //        NullValueHandling = NullValueHandling.Ignore,
        //        DefaultValueHandling = DefaultValueHandling.Ignore,
        //        TypeNameHandling = TypeNameHandling.Auto,
        //        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        //    };
        //    var sw = new StreamWriter(stream);
        //    using (var writer = new JsonTextWriter(sw))
        //    {
        //        var jObject = JObject.FromObject(solid, serializer);
        //        var solidType = solid.GetType();
        //        jObject.AddFirst(new JProperty("TVGLSolidType", solid.GetType().FullName));
        //        if (!Assembly.GetExecutingAssembly().Equals(solidType.Assembly))
        //            jObject.AddFirst(new JProperty("InAssembly", solidType.Assembly.Location));
        //        jObject.WriteTo(writer);
        //    }
        //    return true;
        //}
        //catch
        //{
        //    return false;
        //}

        #endregion Save/Write

    }
}