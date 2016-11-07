﻿// ***********************************************************************
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
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Reflection;
using System.ServiceModel.Channels;
using System.Text;
using System.Xml.Serialization;

namespace TVGL.IOFunctions
{
    /// <summary>
    ///     The IO or input/output class contains static functions for saving and loading files in common formats.
    ///     Note that as a Portable class library, these IO functions cannot interact with your file system. In order
    ///     to load or save, the filename is not enough. One needs to provide the stream.
    /// </summary>
    public abstract class IO
    {

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        internal string Name { get; set; }
        /// <summary>
        /// Gets or sets the name of the file.
        /// </summary>
        /// <value>
        /// The name of the file.
        /// </value>
        internal string FileName { get; set; }

        /// <summary>
        /// Gets or sets the unit.
        /// </summary>
        /// <value>The unit.</value>
        [XmlIgnore]
        public UnitType Units { get; set; }

        /// <summary>
        /// Gets or sets the units as string.
        /// </summary>
        /// <value>The units as string.</value>
        [XmlAttribute("unit")]
        public string UnitsAsString
        {
            get
            {
                return Enum.GetName(typeof(UnitType), Units);
            }
            set
            {
                Units = ParseUnits(value);
            }
        }

        /// <summary>
        /// Gets or sets the language.
        /// </summary>
        /// <value>The language.</value>
        [XmlAttribute("lang")]
        public string Language { get; set; }

        /// <summary>
        /// Gets or sets the comments.
        /// </summary>
        /// <value>
        /// The comments.
        /// </value>
        internal List<string> Comments => _comments;
        /// <summary>
        /// The _comments
        /// </summary>
        protected List<string> _comments = new List<string>();

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
            var extension = GetExtensionFromFileName(filename);
            List<TessellatedSolid> tessellatedSolids;
            if (inParallel) throw new Exception("This function has been recently removed.");
            switch (extension)
            {
                case "stl":
                    tessellatedSolids = STLFileData.OpenSolids(s, filename); // Standard Tessellation or StereoLithography
                    break;
                case "3mf":
#if net40
                    throw new NotSupportedException("The loading or saving of .3mf files are not supported in the .NET4.0 version of TVGL.");
#else
                    tessellatedSolids = ThreeMFFileData.OpenSolids(s, filename);
                    break;
#endif
                case "model":
                    tessellatedSolids = ThreeMFFileData.OpenModelFile(s, filename);
                    break;
                case "amf":
                    tessellatedSolids = AMFFileData.OpenSolids(s, filename);
                    break;
                case "off":
                    var offTS = OFFFileData.OpenSolid(s, filename);
                    if (offTS == null) tessellatedSolids = null;
                    else tessellatedSolids = new List<TessellatedSolid> { offTS };
                    // http://en.wikipedia.org/wiki/OFF_(file_format)
                    break;
                case "ply":
                    var plyTS = PLYFileData.OpenSolid(s, filename);
                    if (plyTS == null) tessellatedSolids = null;
                    else tessellatedSolids = new List<TessellatedSolid> { plyTS };
                    break;
                case "shell":
                    tessellatedSolids = ShellFileData.OpenSolids(s, filename);
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
        /// Opens from string.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="name">The name.</param>
        /// <returns>List&lt;TessellatedSolid&gt;.</returns>
        public static List<TessellatedSolid> OpenFromString(string data, string name = "")
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(data);
            writer.Flush();
            stream.Position = 0;
            return Open(stream, name);
        }
        /// <summary>
        /// Opens from string.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="fileType">Type of the file.</param>
        /// <returns>List&lt;TessellatedSolid&gt;.</returns>
        public static List<TessellatedSolid> OpenFromString(string data, FileType fileType)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(data);
            writer.Flush();
            stream.Position = 0;
            var extensions = new[] { "", "STL", "STL", "3mf", "model", "amf", "off", "ply", "ply" };
            var name = "data." + extensions[(int)fileType];
            return Open(stream, name);
        }

        /// <summary>
        /// Gets the name from the filename.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns>System.String.</returns>
        protected static string GetNameFromFileName(string filename)
        {
            var startIndex = filename.LastIndexOf('/') + 1;
            if (startIndex == -1) startIndex = filename.LastIndexOf('\\') + 1;
            var endIndex = filename.IndexOf('.', startIndex);
            if (endIndex == -1) endIndex = filename.Length - 1;
            return filename.Substring(startIndex, endIndex - startIndex);
        }
        /// <summary>
        /// Gets the name of the extension from file.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns>System.String.</returns>
        protected static string GetExtensionFromFileName(string filename)
        {
            var lastIndexOfDot = filename.LastIndexOf('.');
            if (lastIndexOfDot < 0 || lastIndexOfDot >= filename.Length - 1)
                throw new Exception("Cannot open file without extension (e.g. f00.stl).");
            return filename.Substring(lastIndexOfDot + 1).ToLower();
        }
        /// <summary>
        ///     Parses the ID and values from the specified line.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <param name="id">The id.</param>
        /// <param name="values">The values.</param>
        protected static void ParseLine(string line, out string id, out string values)
        {
            line = line.Trim();
            var idx = line.IndexOf(' ');
            if (idx == -1)
            {
                id = line;
                values = String.Empty;
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
            var strings = line.Split(' ', '\t').ToList();
            strings.RemoveAll(String.IsNullOrWhiteSpace);
            doubles = new double[strings.Count];
            for (var i = 0; i < strings.Count; i++)
            {
                if (!Double.TryParse(strings[i], out doubles[i]))
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
                if (!Double.TryParse(match.Groups[i + 1].Value, out doubles[i]))
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
            if (line != null && expected.Equals(line.Trim(' '), StringComparison.CurrentCultureIgnoreCase))
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
            } while (String.IsNullOrWhiteSpace(line));
            return line.Trim();
        }

        /// <summary>
        /// Infers the units from comments.
        /// </summary>
        /// <param name="comments">The comments.</param>
        /// <returns></returns>
        protected static UnitType InferUnitsFromComments(List<string> comments)
        {
            UnitType units;
            foreach (var comment in comments)
            {
                var words = Regex.Matches(comment, "([a-z]+)");
                foreach (var word in words)
                    if (TryParseUnits(word.ToString(), out units)) return units;
            }
            return UnitType.unspecified;
        }

        /// <summary>
        /// Tries to parse units.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="units">The units.</param>
        /// <returns></returns>
        protected static bool TryParseUnits(string input, out UnitType units)
        {
            if (Enum.TryParse(input, out units)) return true;
            if (input.Equals("milimeter", StringComparison.CurrentCultureIgnoreCase) ||
                input.Equals("millimeter", StringComparison.CurrentCultureIgnoreCase) ||
                input.Equals("milimeters", StringComparison.CurrentCultureIgnoreCase) ||
                input.Equals("millimeters", StringComparison.CurrentCultureIgnoreCase))
                units = UnitType.millimeter;
            else if (input.Equals("micrometer", StringComparison.CurrentCultureIgnoreCase) ||
                input.Equals("micrometers", StringComparison.CurrentCultureIgnoreCase) ||
                input.Equals("microns", StringComparison.CurrentCultureIgnoreCase))
                units = UnitType.micron;
            else if (input.Equals("centimeter", StringComparison.CurrentCultureIgnoreCase) ||
                input.Equals("centimeters", StringComparison.CurrentCultureIgnoreCase))
                units = UnitType.centimeter;
            else if (input.Equals("meters", StringComparison.CurrentCultureIgnoreCase) ||
                input.Equals("meter", StringComparison.CurrentCultureIgnoreCase))
                units = UnitType.meter;
            else if (input.Equals("feet", StringComparison.CurrentCultureIgnoreCase) ||
                input.Equals("foots", StringComparison.CurrentCultureIgnoreCase))
                units = UnitType.foot;
            else if (input.Equals("inches", StringComparison.CurrentCultureIgnoreCase) ||
                input.Equals("inch", StringComparison.CurrentCultureIgnoreCase))
                units = UnitType.inch;
            else return false;
            return true;
        }

        /// <summary>
        /// Parses the units.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>TVGL.UnitType.</returns>
        protected static UnitType ParseUnits(string input)
        {
            UnitType units;
            if (TryParseUnits(input, out units)) return units;
            return UnitType.unspecified;
        }


        internal static int readNumberAsInt(BinaryReader reader, Type type, FormatEndiannessType formatType)
        {
            var bigEndian = (formatType == FormatEndiannessType.binary_big_endian);

            if (type == typeof(double))
            {
                var byteArray = reader.ReadBytes(8);
                if (bigEndian) byteArray = byteArray.Reverse().ToArray();
                return (int)Math.Round(BitConverter.ToDouble(byteArray, 0));
            }
            if (type == typeof(long))
            {
                var byteArray = reader.ReadBytes(8);
                if (bigEndian) byteArray = byteArray.Reverse().ToArray();
                return (int)BitConverter.ToInt64(byteArray, 0);
            }
            if (type == typeof(ulong))
            {
                var byteArray = reader.ReadBytes(8);
                if (bigEndian) byteArray = byteArray.Reverse().ToArray();
                return (int)BitConverter.ToUInt64(byteArray, 0);
            }
            if (type == typeof(float))
            {
                var byteArray = reader.ReadBytes(4);
                if (bigEndian) byteArray = byteArray.Reverse().ToArray();
                return (int)Math.Round(BitConverter.ToSingle(byteArray, 0));
            }
            if (type == typeof(int))
            {
                var byteArray = reader.ReadBytes(4);
                if (bigEndian) byteArray = byteArray.Reverse().ToArray();
                return BitConverter.ToInt32(byteArray, 0);
            }
            if (type == typeof(uint))
            {
                var byteArray = reader.ReadBytes(4);
                if (bigEndian) byteArray = byteArray.Reverse().ToArray();
                return (int)BitConverter.ToUInt32(byteArray, 0);
            }
            if (type == typeof(short))
            {
                var byteArray = reader.ReadBytes(2);
                if (bigEndian) byteArray = byteArray.Reverse().ToArray();
                return BitConverter.ToInt16(byteArray, 0);
            }
            if (type == typeof(ushort))
            {
                var byteArray = reader.ReadBytes(2);
                if (bigEndian) byteArray = byteArray.Reverse().ToArray();
                return BitConverter.ToUInt16(byteArray, 0);
            }
            if (type == typeof(byte))
            {
                var oneByteArray = reader.ReadBytes(1);
                return oneByteArray[0];
            }
            return int.MinValue;
        }
        internal static float readNumberAsFloat(BinaryReader reader, Type type, FormatEndiannessType formatType)
        {
            var bigEndian = (formatType == FormatEndiannessType.binary_big_endian);

            if (type == typeof(double))
            {
                var byteArray = reader.ReadBytes(8);
                if (bigEndian) byteArray = byteArray.Reverse().ToArray();
                return (float)BitConverter.ToDouble(byteArray, 0);
            }
            if (type == typeof(long))
            {
                var byteArray = reader.ReadBytes(8);
                if (bigEndian) byteArray = byteArray.Reverse().ToArray();
                return BitConverter.ToInt64(byteArray, 0);
            }
            if (type == typeof(ulong))
            {
                var byteArray = reader.ReadBytes(8);
                if (bigEndian) byteArray = byteArray.Reverse().ToArray();
                return BitConverter.ToUInt64(byteArray, 0);
            }
            if (type == typeof(float))
            {
                var byteArray = reader.ReadBytes(4);
                if (bigEndian) byteArray = byteArray.Reverse().ToArray();
                return BitConverter.ToSingle(byteArray, 0);
            }
            if (type == typeof(int))
            {
                var byteArray = reader.ReadBytes(4);
                if (bigEndian) byteArray = byteArray.Reverse().ToArray();
                return BitConverter.ToInt32(byteArray, 0);
            }
            if (type == typeof(uint))
            {
                var byteArray = reader.ReadBytes(4);
                if (bigEndian) byteArray = byteArray.Reverse().ToArray();
                return BitConverter.ToUInt32(byteArray, 0);
            }
            if (type == typeof(short))
            {
                var byteArray = reader.ReadBytes(2);
                if (bigEndian) byteArray = byteArray.Reverse().ToArray();
                return BitConverter.ToInt16(byteArray, 0);
            }
            if (type == typeof(ushort))
            {
                var byteArray = reader.ReadBytes(2);
                if (bigEndian) byteArray = byteArray.Reverse().ToArray();
                return BitConverter.ToUInt16(byteArray, 0);
            }
            if (type == typeof(byte))
            {
                var oneByteArray = reader.ReadBytes(1);
                return oneByteArray[0];
            }
            return float.NaN;
        }
        internal static double readNumberAsDouble(BinaryReader reader, Type type, FormatEndiannessType formatType)
        {
            var bigEndian = (formatType == FormatEndiannessType.binary_big_endian);

            if (type == typeof(double))
            {
                var byteArray = reader.ReadBytes(8);
                if (bigEndian) byteArray = byteArray.Reverse().ToArray();
                return BitConverter.ToDouble(byteArray, 0);
            }
            if (type == typeof(long))
            {
                var byteArray = reader.ReadBytes(8);
                if (bigEndian) byteArray = byteArray.Reverse().ToArray();
                return BitConverter.ToInt64(byteArray, 0);
            }
            if (type == typeof(ulong))
            {
                var byteArray = reader.ReadBytes(8);
                if (bigEndian) byteArray = byteArray.Reverse().ToArray();
                return BitConverter.ToUInt64(byteArray, 0);
            }
            if (type == typeof(float))
            {
                var byteArray = reader.ReadBytes(4);
                if (bigEndian) byteArray = byteArray.Reverse().ToArray();
                return BitConverter.ToSingle(byteArray, 0);
            }
            if (type == typeof(int))
            {
                var byteArray = reader.ReadBytes(4);
                if (bigEndian) byteArray = byteArray.Reverse().ToArray();
                return BitConverter.ToInt32(byteArray, 0);
            }
            if (type == typeof(uint))
            {
                var byteArray = reader.ReadBytes(4);
                if (bigEndian) byteArray = byteArray.Reverse().ToArray();
                return BitConverter.ToUInt32(byteArray, 0);
            }
            if (type == typeof(short))
            {
                var byteArray = reader.ReadBytes(2);
                if (bigEndian) byteArray = byteArray.Reverse().ToArray();
                return BitConverter.ToInt16(byteArray, 0);
            }
            if (type == typeof(ushort))
            {
                var byteArray = reader.ReadBytes(2);
                if (bigEndian) byteArray = byteArray.Reverse().ToArray();
                return BitConverter.ToUInt16(byteArray, 0);
            }
            if (type == typeof(byte))
            {
                var oneByteArray = reader.ReadBytes(1);
                return oneByteArray[0];
            }
            return double.NaN;
        }


        /// <summary>
        /// Tries the parse number type from string.
        /// </summary>
        /// <param name="typeString">The type string.</param>
        /// <param name="type">The type.</param>
        /// <returns>Type.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        protected bool TryParseNumberTypeFromString(string typeString, out Type type)
        {
            if (typeString.StartsWith("float", StringComparison.CurrentCultureIgnoreCase)
                || typeString.StartsWith("single", StringComparison.CurrentCultureIgnoreCase))
                type = typeof(float);
            else if (typeString.StartsWith("double", StringComparison.CurrentCultureIgnoreCase))
                type = typeof(double);
            else if (typeString.StartsWith("long", StringComparison.CurrentCultureIgnoreCase)
                || typeString.StartsWith("int64", StringComparison.CurrentCultureIgnoreCase)
                || typeString.StartsWith("uint64", StringComparison.CurrentCultureIgnoreCase))
                type = typeof(long);
            else if (typeString.StartsWith("int32", StringComparison.CurrentCultureIgnoreCase)
                     || typeString.Equals("int", StringComparison.CurrentCultureIgnoreCase))
                type = typeof(int);
            else if (typeString.StartsWith("short", StringComparison.CurrentCultureIgnoreCase)
                     || typeString.StartsWith("int16", StringComparison.CurrentCultureIgnoreCase))
                type = typeof(short);
            else if (typeString.StartsWith("ushort", StringComparison.CurrentCultureIgnoreCase)
                || typeString.StartsWith("uint16", StringComparison.CurrentCultureIgnoreCase))
                type = typeof(ushort);
            else if (typeString.StartsWith("uint64", StringComparison.CurrentCultureIgnoreCase))
                type = typeof(ulong);
            else if (typeString.Equals("uint", StringComparison.CurrentCultureIgnoreCase))
                type = typeof(uint);
            else if (typeString.StartsWith("char", StringComparison.CurrentCultureIgnoreCase)
                     || typeString.StartsWith("uchar", StringComparison.CurrentCultureIgnoreCase)
                     || typeString.StartsWith("byte", StringComparison.CurrentCultureIgnoreCase)
                     || typeString.StartsWith("uint8", StringComparison.CurrentCultureIgnoreCase)
                     || typeString.StartsWith("int8", StringComparison.CurrentCultureIgnoreCase))
                type = typeof(byte);
            else
            {
                type = null;
                return false;
            }
            return true;
        }

        internal static int readNumberAsInt(string text, Type type)
        {
            if (type == typeof(double))
            {
                double coord;
                if (double.TryParse(text, out coord)) return (int)Math.Round(coord);
            }
            if (type == typeof(long))
            {
                long coord;
                if (long.TryParse(text, out coord)) return (int)coord;
            }
            if (type == typeof(ulong))
            {
                ulong coord;
                if (ulong.TryParse(text, out coord)) return (int)coord;
            }
            if (type == typeof(float))
            {
                float coord;
                if (float.TryParse(text, out coord)) return (int)Math.Round(coord);
            }
            if (type == typeof(int))
            {
                int coord;
                if (int.TryParse(text, out coord)) return coord;
            }
            if (type == typeof(uint))
            {
                uint coord;
                if (uint.TryParse(text, out coord)) return (int)coord;
            }
            if (type == typeof(short))
            {
                short coord;
                if (short.TryParse(text, out coord)) return coord;
            }
            if (type == typeof(ushort))
            {
                ushort coord;
                if (ushort.TryParse(text, out coord)) return coord;
            }
            if (type == typeof(byte))
            {
                byte coord;
                if (byte.TryParse(text, out coord)) return coord;
            }
            return int.MinValue;
        }
        internal static float readNumberAsFloat(string text, Type type)
        {
            if (type == typeof(double))
            {
                double coord;
                if (double.TryParse(text, out coord)) return (float)coord;
            }
            if (type == typeof(long))
            {
                long coord;
                if (long.TryParse(text, out coord)) return coord;
            }
            if (type == typeof(ulong))
            {
                ulong coord;
                if (ulong.TryParse(text, out coord)) return coord;
            }
            if (type == typeof(float))
            {
                float coord;
                if (float.TryParse(text, out coord)) return coord;
            }
            if (type == typeof(int))
            {
                int coord;
                if (int.TryParse(text, out coord)) return coord;
            }
            if (type == typeof(uint))
            {
                uint coord;
                if (uint.TryParse(text, out coord)) return coord;
            }
            if (type == typeof(short))
            {
                short coord;
                if (short.TryParse(text, out coord)) return coord;
            }
            if (type == typeof(ushort))
            {
                ushort coord;
                if (ushort.TryParse(text, out coord)) return coord;
            }
            if (type == typeof(byte))
            {
                byte coord;
                if (byte.TryParse(text, out coord)) return coord;
            }
            return float.NaN;
        }
        internal static double readNumberAsDouble(string text, Type type)
        {
            if (type == typeof(double))
            {
                double coord;
                if (Double.TryParse(text, out coord)) return coord;
            }
            if (type == typeof(long))
            {
                long coord;
                if (Int64.TryParse(text, out coord)) return coord;
            }
            if (type == typeof(ulong))
            {
                ulong coord;
                if (UInt64.TryParse(text, out coord)) return coord;
            }
            if (type == typeof(float))
            {
                float coord;
                if (Single.TryParse(text, out coord)) return coord;
            }
            if (type == typeof(int))
            {
                int coord;
                if (Int32.TryParse(text, out coord)) return coord;
            }
            if (type == typeof(uint))
            {
                uint coord;
                if (UInt32.TryParse(text, out coord)) return coord;
            }
            if (type == typeof(short))
            {
                short coord;
                if (Int16.TryParse(text, out coord)) return coord;
            }
            if (type == typeof(ushort))
            {
                ushort coord;
                if (UInt16.TryParse(text, out coord)) return coord;
            }
            if (type == typeof(byte))
            {
                byte coord;
                if (Byte.TryParse(text, out coord)) return coord;
            }
            return Double.NaN;
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
        public static bool Save(Stream stream, IList<TessellatedSolid> solids, FileType fileType = FileType.unspecified)
        {
            if (solids.Count == 0) return false;
            if (fileType == FileType.unspecified)
                fileType = (solids.Count == 1) ? FileType.PLY_Binary : FileType.AMF;
            switch (fileType)
            {
                case FileType.STL_ASCII:
                    return STLFileData.SaveASCII(stream, solids);
                case FileType.STL_Binary:
                    return STLFileData.SaveBinary(stream, solids);
                case FileType.AMF:
                    return AMFFileData.SaveSolids(stream, solids);
                case FileType.ThreeMF:
#if net40
                    throw new NotSupportedException("The loading or saving of .3mf files are not allowed in the .NET4.0 version of TVGL.");
#else
                    return ThreeMFFileData.Save(stream, solids);
#endif
                case FileType.Model3MF:
                    return ThreeMFFileData.SaveModel(stream, solids);
                case FileType.OFF:
                    if (solids.Count > 1)
                        throw new NotSupportedException(
                            "The OFF format does not support saving multiple solids to a single file.");
                    else return OFFFileData.SaveSolid(stream, solids[0]);
                case FileType.PLY_ASCII:
                    if (solids.Count > 1)
                        throw new NotSupportedException(
                            "The PLY format does not support saving multiple solids to a single file.");
                    else return PLYFileData.SaveSolidASCII(stream, solids[0]);
                case FileType.PLY_Binary:
                    if (solids.Count > 1)
                        throw new NotSupportedException(
                            "The PLY format does not support saving multiple solids to a single file.");
                    else return PLYFileData.SaveSolidBinary(stream, solids[0]);
                default:
                    return false;
            }
        }
        /// <summary>
        /// Saves the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="solid">The solid.</param>
        /// <param name="fileType">Type of the file.</param>
        /// <returns>System.Boolean.</returns>
        public static bool Save(Stream stream, TessellatedSolid solid, FileType fileType = FileType.PLY_Binary)
        {
            switch (fileType)
            {
                case FileType.STL_ASCII:
                    return STLFileData.SaveASCII(stream, new List<TessellatedSolid> { solid });
                case FileType.STL_Binary:
                    return STLFileData.SaveBinary(stream, new List<TessellatedSolid> { solid });
                case FileType.AMF:
                    return AMFFileData.SaveSolids(stream, new List<TessellatedSolid> { solid });
                case FileType.ThreeMF:
#if net40
                    throw new NotSupportedException("The loading or saving of .3mf files are not allowed in the .NET4.0 version of TVGL.");
#else
                    return ThreeMFFileData.Save(stream, new List<TessellatedSolid> { solid });
#endif
                case FileType.Model3MF:
                    return ThreeMFFileData.SaveModel(stream, new List<TessellatedSolid> { solid });
                case FileType.OFF:
                    return OFFFileData.SaveSolid(stream, solid);
                case FileType.PLY_ASCII:
                    return PLYFileData.SaveSolidASCII(stream, solid);
                case FileType.PLY_Binary:
                    return PLYFileData.SaveSolidBinary(stream, solid);
                default:
                    return false;
            }
        }


        /// <summary>
        /// Saves the solid as a string.
        /// </summary>
        /// <param name="solid">The solid.</param>
        /// <param name="fileType">Type of the file.</param>
        /// <returns>System.String.</returns>
        public static string SaveToString(TessellatedSolid solid, FileType fileType = FileType.PLY_Binary)
        {
            var stream = new MemoryStream();
            if (!Save(stream, solid, fileType)) return "";
            var byteArray = stream.ToArray();
            return System.Text.Encoding.UTF8.GetString(byteArray, 0, byteArray.Length);
        }

        /// <summary>
        /// Saves the solids as a string.
        /// </summary>
        /// <param name="solids">The solids.</param>
        /// <param name="fileType">Type of the file.</param>
        /// <returns>System.String.</returns>
        public static string SaveToString(IList<TessellatedSolid> solids, FileType fileType = FileType.unspecified)
        {
            var stream = new MemoryStream();
            if (!Save(stream, solids, fileType)) return "";
            var byteArray = stream.ToArray();
            return System.Text.Encoding.UTF8.GetString(byteArray, 0, byteArray.Length);
        }


        /// <summary>
        /// Gets the TVGL date mark text.
        /// </summary>
        /// <value>The TVGL date mark text.</value>
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