// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 02-27-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 05-28-2016
// ***********************************************************************
// <copyright file="IOFunctions.cs" company="Design Engineering Lab">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Reflection;

namespace TVGL.IOFunctions
{
    /// <summary>
    ///     The IO or input/output class contains static functions for saving and loading files in common formats.
    ///     Note that as a Portable class library, these IO functions cannot interact with your file system. In order
    ///     to load or save, the filename is not enough. One needs to provide the stream.
    /// </summary>
    public class IO
    {
        #region Open/Load/Read

        /// <summary>
        ///     Opens the specified stream, s. Note that as a Portable class library
        /// </summary>
        /// <param name="s">The s.</param>
        /// <param name="filename">The filename.</param>
        /// <param name="inParallel">The in parallel.</param>
        /// <returns>TessellatedSolid.</returns>
        /// <exception cref="Exception">
        ///     Cannot open file without extension (e.g. f00.stl).
        ///     or
        ///     This function has been recently removed.
        ///     or
        ///     Cannot determine format from extension (not .stl, .ply, .3ds, .lwo, .obj, .objx, or .off.
        /// </exception>
        /// <exception cref="System.Exception">
        ///     Cannot open file without extension (e.g. f00.stl).
        ///     or
        ///     Cannot determine format from extension (not .stl, .3ds, .lwo, .obj, .objx, or .off.
        /// </exception>
        public static List<TessellatedSolid> Open(Stream s, string filename, bool inParallel = false)
        {
            var lastIndexOfDot = filename.LastIndexOf('.');
            if (lastIndexOfDot < 0 || lastIndexOfDot >= filename.Length - 1)
                throw new Exception("Cannot open file without extension (e.g. f00.stl).");
            var extension = filename.Substring(lastIndexOfDot + 1, filename.Length - lastIndexOfDot - 1).ToLower();
            List<TessellatedSolid> tessellatedSolids;
            if (inParallel) throw new Exception("This function has been recently removed.");
            switch (extension)
            {
                case "stl":
                    tessellatedSolids = STLFileData.Open(s, filename, inParallel); // Standard Tessellation or StereoLithography
                    break;
                case "ply":
                    tessellatedSolids = PLYFileData.Open(s, filename, inParallel); // Standard Tessellation or StereoLithography
                    break;
                case "3mf":
#if net40
                    throw new NotSupportedException("The loading or saving of .3mf files are not allowed in the .NET4.0 version of TVGL.");
#else
                    tessellatedSolids = ThreeMFFileData.Open(s, filename, inParallel);
#endif
                    break;
                case "model":
#if net40
                    throw new NotSupportedException("The loading or saving of .3mf files are not allowed in the .NET4.0 version of TVGL.");
#else
                    tessellatedSolids = ThreeMFFileData.OpenModelFile(s, filename, inParallel);
#endif
                    break;
                case "amf":
                    tessellatedSolids = AMFFileData.Open(s, filename, inParallel);
                    break;
                case "off":
                    tessellatedSolids = OFFFileData.Open(s, filename, inParallel);
                    // http://en.wikipedia.org/wiki/OFF_(file_format)
                    break;
                case "shell":
                    tessellatedSolids = ShellFileData.Open(s, filename, inParallel);
                    break;
                default:
                    throw new Exception(
                        "Cannot determine format from extension (not .stl, .ply, .3ds, .lwo, .obj, .objx, or .off.");
            }
            if (tessellatedSolids == null || tessellatedSolids.Count == 0) return null;
            Message.output("number of solids = " + tessellatedSolids.Count, 3);
            foreach (var tessellatedSolid in tessellatedSolids)
            {
                Message.output("number of vertices = " + tessellatedSolid.NumberOfVertices, 4);
                Message.output("number of edges = " + tessellatedSolid.NumberOfEdges, 4);
                Message.output("number of faces = " + tessellatedSolid.NumberOfFaces, 4);
            }

            return tessellatedSolids;
        }

        /// <summary>
        /// Gets the name from the filename.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns>System.String.</returns>
        protected static string GetNameFromFileName(string filename)
        {
            var startIndex = filename.LastIndexOf('/') + 1;
            var endIndex = filename.IndexOf('.', startIndex);
            if (endIndex == -1) endIndex = filename.Length - 1;
            return filename.Substring(startIndex, endIndex - startIndex);
        }
        /// <summary>
        ///     Parses the ID and values from the specified line.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <param name="id">The id.</param>
        /// <param name="values">The values.</param>
        protected static void ParseLine(string line, out string id, out string values)
        {
            line = line.Trim(' ');
            var idx = line.IndexOf(' ');
            if (idx == -1)
            {
                id = line;
                values = string.Empty;
            }
            else
            {
                id = line.Substring(0, idx).ToLower();
                values = line.Substring(idx + 1);
            }
        }


        /// <summary>
        ///     Tries to parse a vertex from a string.
        /// </summary>
        /// <param name="line">The input string.</param>
        /// <param name="doubles">The vertex point.</param>
        /// <returns>True if parsing was successful.</returns>
        protected static bool TryParseDoubleArray(string line, out double[] doubles)
        {
            var strings = line.Split(' ','\t').ToList();
            strings.RemoveAll(string.IsNullOrWhiteSpace);
            doubles = new double[strings.Count];
            for (var i = 0; i < strings.Count; i++)
            {
                if (!double.TryParse(strings[i], out doubles[i]))
                    return false;
            }
            return true;
        }

        /// <summary>
        ///     Tries to parse a vertex from a string.
        /// </summary>
        /// <param name="parser">The parser.</param>
        /// <param name="line">The input string.</param>
        /// <param name="doubles">The vertex point.</param>
        /// <returns>True if parsing was successful.</returns>
        protected static bool TryParseDoubleArray(Regex parser, string line, out double[] doubles)
        {
            var match = parser.Match(line);
            if (!match.Success)
            {
                doubles = null;
                return false;
            }
            doubles = new double[match.Groups.Count - 1];
            for (var i = 0; i < doubles.GetLength(0); i++)
            {
                if (!double.TryParse(match.Groups[i + 1].Value, out doubles[i]))
                    return false;
            }
            return true;
        }

        /// <summary>
        ///     Reads the next line with the expectation that is starts with the
        ///     "expected" string. If the line does not have the expected value,
        ///     then the StreamReader stays at the same position and
        ///     false is returned.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="expected">The expected.</param>
        /// <returns>bool.</returns>
        protected static bool ReadExpectedLine(StreamReader reader, string expected)
        {
            var startPosition = reader.BaseStream.Position;
            var line = ReadLine(reader);
            if (line != null && expected.Equals(line.Trim(' '), StringComparison.OrdinalIgnoreCase))
                return true;
            reader.BaseStream.Position = startPosition;
            return false;
        }

        /// <summary>
        ///     Reads the line.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>System.String.</returns>
        protected static string ReadLine(StreamReader reader)
        {
            string line;
            do
            {
                line = reader.ReadLine();
                if (reader.EndOfStream) break;
            } while (string.IsNullOrWhiteSpace(line) || line.StartsWith("\0") || line.StartsWith("#") ||
                     line.StartsWith("!")
                     || line.StartsWith("$"));
            return line.Trim(' ');
        }

        #endregion

        #region Save/Write

        /// <summary>
        ///     Saves the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="solids">The solids.</param>
        /// <param name="fileType">Type of the file.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public static bool Save(Stream stream, IList<TessellatedSolid> solids, FileType fileType = FileType.PLY)
        {
            if (solids.Count == 0) return false;
            switch (fileType)
            {
                case FileType.STL_ASCII:
                    return STLFileData.SaveASCII(stream, solids);
                case FileType.STL_Binary:
                    return STLFileData.SaveBinary(stream, solids);
                case FileType.AMF:
                    return AMFFileData.Save(stream, solids);
                case FileType.ThreeMF:
#if net40
                    throw new NotSupportedException("The loading or saving of .3mf files are not allowed in the .NET4.0 version of TVGL.");
#else
                    return ThreeMFFileData.Save(stream, solids);
#endif
                case FileType.OFF:
                    if (solids.Count > 1)
                        throw new NotSupportedException(
                            "The OFF format does not support saving multiple solids to a single file.");
                    else return OFFFileData.Save(stream, solids[0]);
                case FileType.PLY:
                    if (solids.Count > 1)
                        throw new NotSupportedException(
                            "The PLY format does not support saving multiple solids to a single file.");
                    else return PLYFileData.Save(stream, solids[0]);
                default:
                    return false;
            }
        }
        public static bool Save(Stream stream, TessellatedSolid solid, FileType fileType = FileType.PLY)
        {
            switch (fileType)
            {
                case FileType.STL_ASCII:
                    return STLFileData.SaveASCII(stream, new List<TessellatedSolid> { solid });
                case FileType.STL_Binary:
                    return STLFileData.SaveBinary(stream, new List<TessellatedSolid> { solid });
                case FileType.AMF:
                    return AMFFileData.Save(stream, new List<TessellatedSolid> { solid });
                case FileType.ThreeMF:
#if net40
                    throw new NotSupportedException("The loading or saving of .3mf files are not allowed in the .NET4.0 version of TVGL.");
#else
                    return ThreeMFFileData.Save(stream, new List<TessellatedSolid> { solid });
#endif
                case FileType.OFF:
                    return OFFFileData.Save(stream, solid);
                case FileType.PLY:
                    return PLYFileData.Save(stream, solid);
                default:
                    return false;
            }
        }

        protected static string tvglDateMarkText
        {
            get
            {
                var now = DateTime.Now;
                return "created by TVGL on " + now.Year + "/" + now.Month + "/" + now.Day + " at " + now.Hour + ":" +
                       now.Minute + ":" + now.Second;
            }
        }
        #endregion
    }
}