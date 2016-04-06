// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Matt Campbell
// Created          : 02-27-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 03-06-2015
// ***********************************************************************
// <copyright file="IOFunctions.cs" company="">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace TVGL.IOFunctions
{
    /// <summary>
    ///  The IO or input/output class contains static functions for saving and loading files in common formats.
    ///  Note that as a Portable class library, these IO functions cannot interact with your file system. In order
    ///  to load or save, the filename is not enough. One needs to provide the stream. 
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
                    tessellatedSolids = STLFileData.Open(s, inParallel); // Standard Tessellation or StereoLithography
                    break;
                case "ply":
                    tessellatedSolids = PLYFileData.Open(s, inParallel); // Standard Tessellation or StereoLithography
                    break;
                //case "3ds": return IO.Open3DS(s);   //3D Studio
                //case "lwo": return IO.OpenLWO(s);  //Lightwave
                //case "obj": return IO.OpenOBJ(s); //Wavefront
                //case "objx": return IO.OpenOBJX(s);  //Wavefront
                case "amf":
                    tessellatedSolids = AMFFileData.Open(s, inParallel);
                    break;
                case "off":
                    tessellatedSolids = OFFFileData.Open(s, inParallel); // http://en.wikipedia.org/wiki/OFF_(file_format)
                    break;
                default:
                    throw new Exception(
                        "Cannot determine format from extension (not .stl, .ply, .3ds, .lwo, .obj, .objx, or .off.");
            }
            Message.output("number of solids = " + tessellatedSolids.Count,3);
            foreach (var tessellatedSolid in tessellatedSolids)
            {
                Message.output("number of vertices = " + tessellatedSolid.NumberOfVertices,4);
                Message.output("number of edges = " + tessellatedSolid.NumberOfEdges,4);
                Message.output("number of faces = " + tessellatedSolid.NumberOfFaces,4);
            }

            return tessellatedSolids;
        }

        /// <summary>
        /// Gets the name from stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>System.String.</returns>
        protected static string getNameFromStream(Stream stream)
        {
            var type = stream.GetType();
            var namePropertyInfo = type.GetProperty("Name");
            var name = (string)namePropertyInfo.GetValue(stream, null);
            var lastDirectorySeparator = name.LastIndexOf("\\");
            var fileExtensionIndex = name.LastIndexOf(".");
            return (lastDirectorySeparator < fileExtensionIndex)
                ? name.Substring(lastDirectorySeparator + 1, fileExtensionIndex - lastDirectorySeparator - 1)
                : name.Substring(lastDirectorySeparator + 1, name.Length - lastDirectorySeparator - 1);
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
        /// Tries to parse a vertex from a string.
        /// </summary>
        /// <param name="line">The input string.</param>
        /// <param name="doubles">The vertex point.</param>
        /// <returns>True if parsing was successful.</returns>
        protected static bool TryParseDoubleArray(string line, out double[] doubles)
        {
            var strings = line.Split(' ').ToList();
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
            } while (string.IsNullOrWhiteSpace(line) || line.StartsWith("\0") || line.StartsWith("#") || line.StartsWith("!")
                                      || line.StartsWith("$"));
            return line.Trim(' ');
        }

        #endregion

        #region Save/Write

        /// <summary>
        ///     Saves the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="solids"></param>
        /// <param name="fileType">Type of the file.</param>
        public static bool Save(Stream stream, IList<TessellatedSolid> solids, FileType fileType)
        {
            switch (fileType)
            {
                case FileType.STL_ASCII: return STLFileData.SaveASCII(stream, solids);
                case FileType.STL_Binary: return STLFileData.SaveBinary(stream, solids);
                case FileType.AMF: return AMFFileData.Save(stream, solids);
                case FileType.ThreeMF: return ThreeMFFileData.Save(stream, solids);
                case FileType.OFF: return OFFFileData.Save(stream, solids);
                case FileType.PLY: return PLYFileData.Save(stream, solids);
                default: return false;
            }
        }

        /// <summary>
        ///     Writes the coordinates.
        /// </summary>
        /// <param name="coordinates">The coordinates.</param>
        /// <param name="writer">The writer.</param>
        private static void WriteCoordinates(IList<double> coordinates, StreamWriter writer)
        {
            writer.WriteLine("\t\t\t" + coordinates[0] + " " + coordinates[1] + " " + coordinates[2]);
        }

        /// <summary>
        ///     Writes the coordinates.
        /// </summary>
        /// <param name="coordinates">The coordinates.</param>
        /// <param name="writer">The writer.</param>
        private static void WriteCoordinates(double[] coordinates, BinaryWriter writer)
        {
            writer.Write(coordinates[0]);
            writer.Write(coordinates[1]);
            writer.Write(coordinates[2]);
        }

        #endregion
    }
}