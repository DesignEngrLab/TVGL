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
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Serialization;

namespace TVGL
{
    /// <summary>
    /// The IO or input/output class contains static functions for saving and loading files in common formats.
    /// Note that as a Portable class library, these IO functions cannot interact with your file system. In order
    /// to load or save, the filename is not enough. One needs to provide the stream.
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
        /// <value>The name of the file.</value>
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
            get => Enum.GetName(typeof(UnitType), Units);
            set => Units = ParseUnits(value);
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
        /// <value>The comments.</value>
        internal List<string> Comments => _comments;

        /// <summary>
        /// The _comments
        /// </summary>
        protected List<string> _comments = new List<string>();

        #region Open/Load/Read


        /// <summary>
        /// Opens the specified filename.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <param name="solids">The solids.</param>
        /// <exception cref="System.IO.FileNotFoundException">The file was not found at: " + filename</exception>
        public static void Open(string filename, out TessellatedSolid[] solids, TessellatedSolidBuildOptions tsBuildOptions = null)
        {
            if (File.Exists(filename))
                using (var fileStream = File.OpenRead(filename))
                    Open(fileStream, filename, out solids, tsBuildOptions);
            else throw new FileNotFoundException("The file was not found at: " + filename);
        }

        /// <summary>
        /// Opens the specified filename.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <param name="solids">The solids.</param>
        /// <exception cref="System.IO.FileNotFoundException">The file was not found at: " + filename</exception>
        public static void OpenZip(string filename, out TessellatedSolid[] solids, TessellatedSolidBuildOptions tsBuildOptions = null)
        {
            if (File.Exists(filename))
                using (var fileStream = File.OpenRead(filename))
                    Open(fileStream, filename, out solids, tsBuildOptions);
            else throw new FileNotFoundException("The file was not found at: " + filename);
        }


        /// <summary>
        /// Opens the specified filename.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns>Solid.</returns>
        /// <exception cref="System.IO.FileNotFoundException">The file was not found at: " + filename</exception>
        public static Solid Open(string filename, TessellatedSolidBuildOptions tsBuildOptions = null)
        {
            Solid s;
            Open(filename, out s, tsBuildOptions);
            return s;
        }

        /// <summary>
        /// Opens the 3D solid or solids from a provided file name.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <param name="solid">The solid.</param>
        /// <returns>A list of TessellatedSolids.</returns>
        /// <exception cref="System.IO.FileNotFoundException">The file was not found at: " + filename</exception>
        public static void Open<T>(string filename, out T solid, TessellatedSolidBuildOptions tsBuildOptions = null)
            where T : Solid
        {
            if (File.Exists(filename))
                using (var fileStream = File.OpenRead(filename))
                    Open(fileStream, filename, out solid, tsBuildOptions);
            else throw new FileNotFoundException("The file was not found at: " + filename);
        }

        /// <summary>
        /// Opens the specified s.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <param name="filename">The filename.</param>
        /// <param name="tessellatedSolids">The tessellated solids.</param>
        /// <exception cref="System.Exception">Attempting to open multiple solids with a " + extension.ToString() + " file.</exception>
        public static void Open(Stream s, string filename, out TessellatedSolid[] tessellatedSolids,
            TessellatedSolidBuildOptions tsBuildOptions = null)
        {
            //try
            //{
            var extension = GetFileTypeFromExtension(Path.GetExtension(filename));
            switch (extension)
            {
                case FileType.STL_ASCII:
                case FileType.STL_Binary:
                    tessellatedSolids = STLFileData.OpenSolids(s, filename, tsBuildOptions); // Standard Tessellation or StereoLithography
                    break;

                case FileType.ThreeMF:
                    tessellatedSolids = ThreeMFFileData.OpenSolids(s, filename, tsBuildOptions);
                    break;

                case FileType.Model3MF:
                    tessellatedSolids = ThreeMFFileData.OpenModelFile(s, filename, tsBuildOptions);
                    break;

                case FileType.AMF:
                    tessellatedSolids = AMFFileData.OpenSolids(s, filename, tsBuildOptions);
                    break;

                case FileType.OBJ:
                    tessellatedSolids = OBJFileData.OpenSolids(s, filename, tsBuildOptions);
                    break;

                case FileType.OFF:
                case FileType.PLY_ASCII:
                case FileType.PLY_Binary:
                    tessellatedSolids = new[] { PLYFileData.OpenSolid(s, filename, tsBuildOptions) };
                    break;
                case FileType.TVGL:
                    OpenTVGL(s, out var solidAssembly);
                    tessellatedSolids = solidAssembly.RootAssembly.AllTessellatedSolidsInGlobalCoordinateSystem();
                    break;
                case FileType.TVGLz:
                    OpenTVGLz(s, out solidAssembly);
                    tessellatedSolids = solidAssembly.RootAssembly.AllTessellatedSolidsInGlobalCoordinateSystem();
                    break;
                default:
                    Message.output(filename + " is not a recognized 3D format.");
                    tessellatedSolids = new TessellatedSolid[0];
                    break;
            }
            //}
            //catch (Exception exc)
            //{
            //    throw new Exception("Cannot open file. Message: " + exc.Message);
            //}
        }


        /// <summary>
        /// Opens the specified s.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <param name="filename">The filename.</param>
        /// <returns>Solid.</returns>
        private static void Open<T>(Stream s, string filename, out T solid, TessellatedSolidBuildOptions tsBuildOptions = null)
            where T : Solid
        {
            var extension = GetFileTypeFromExtension(Path.GetExtension(filename));
            Solid solidInner = null;
            if (extension == FileType.TVGL || extension == FileType.TVGLz)
            {
                OpenTVGL(s, out solid, tsBuildOptions);
                return;
            }
            switch (extension)
            {
                case FileType.STL_ASCII:
                case FileType.STL_Binary:
                    solidInner = STLFileData.OpenSolids(s, filename, tsBuildOptions)[0]; // Standard Tessellation or StereoLithography
                    break;
                case FileType.ThreeMF:
                    solidInner = ThreeMFFileData.OpenSolids(s, filename, tsBuildOptions)[0];
                    break;
                case FileType.Model3MF:
                    solidInner = ThreeMFFileData.OpenModelFile(s, filename, tsBuildOptions)[0];
                    break;
                case FileType.AMF:
                    solidInner = AMFFileData.OpenSolids(s, filename, tsBuildOptions)[0];
                    break;
                case FileType.OBJ:
                    solidInner = OBJFileData.OpenSolids(s, filename, tsBuildOptions)[0];
                    break;
                case FileType.OFF:
                    solidInner = OFFFileData.OpenSolid(s, filename, tsBuildOptions);
                    break;
                case FileType.PLY_ASCII:
                case FileType.PLY_Binary:
                    solidInner = PLYFileData.OpenSolid(s, filename, tsBuildOptions);
                    break;
            }
            solid = (T)Convert.ChangeType(solidInner, typeof(T));
        }


        #region Open TVGL
        /// <summary>
        /// Opens the solid (TessellatedSolid, CrossSectionSolid, VoxelizedSolid) from the stream.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <param name="solid">The solid.</param>
        private static void OpenTVGL<T>(Stream s, out T solid, TessellatedSolidBuildOptions tsBuildOptions = null) where T : Solid
        {
            var serializer = new JsonSerializer
            {
                TypeNameHandling = TypeNameHandling.Objects,
                Context = new StreamingContext(StreamingContextStates.Other, tsBuildOptions)
            };
            using var reader = new JsonTextReader(new StreamReader(s));
            solid = serializer.Deserialize<T>(reader);
        }

        /// <summary>
        /// Opens the TVGL.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <param name="solidAssembly">The solid assembly.</param>
        private static void OpenTVGL(Stream s, out SolidAssembly solidAssembly, TessellatedSolidBuildOptions tsBuildOptions = null)
        {
            var serializer = new JsonSerializer
            {
                TypeNameHandling = TypeNameHandling.Objects,
                Context = new StreamingContext(StreamingContextStates.Other, tsBuildOptions)
            };
            using var reader = new JsonTextReader(new StreamReader(s));
            solidAssembly = serializer.Deserialize<SolidAssembly>(reader);
        }

        /// <summary>
        /// Opens the TVG lz.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <param name="solidAssembly">The solid assembly.</param>
        private static void OpenTVGLz(Stream s, out SolidAssembly solidAssembly, TessellatedSolidBuildOptions tsBuildOptions = null)
        {
            var serializer = new JsonSerializer
            {
                TypeNameHandling = TypeNameHandling.Objects,
                Context = new StreamingContext(StreamingContextStates.Other, tsBuildOptions)
            };
            using var archive = new ZipArchive(s, ZipArchiveMode.Read, true);
            using var reader = new JsonTextReader(new StreamReader(archive.Entries[0].Open()));
            solidAssembly = serializer.Deserialize<SolidAssembly>(reader);
        }

        #endregion

        /// <summary>
        /// Opens from string.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="fileType">Type of the file.</param>
        /// <param name="solid">The solid.</param>
        public static void OpenFromString(string data, FileType fileType, out TessellatedSolid solid)
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(data));
            //using (var reader = new StreamReader(stream))
            //{
            //    reader.Read(data.AsSpan());
            //    reader.Flush();
            //}
            //stream.Coordinates = 0;
            var name = "data." + GetExtensionFromFileType(fileType);
            Open(stream, name, out solid);
        }

        /// <summary>
        /// Opens from string.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="fileType">Type of the file.</param>
        /// <param name="solids">The solids.</param>
        public static void OpenFromString(string data, FileType fileType, out TessellatedSolid[] solids)
        {
            var stream = new MemoryStream();
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(data);
                writer.Flush();
            }
            stream.Position = 0;
            var name = "data." + GetExtensionFromFileType(fileType);
            Open(stream, name, out solids);
        }

        /// <summary>
        /// Opens from string.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="solid">The solid.</param>
        public static void OpenFromString<T>(string data, out T solid, TessellatedSolidBuildOptions tsBuildOptions = null) where T : Solid
        {
            var stream = new MemoryStream();
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(data);
                writer.Flush();
            }
            stream.Position = 0;
            Open(stream, "", out solid, tsBuildOptions);
        }

        /// <summary>
        /// Gets the file type from extension.
        /// </summary>
        /// <param name="extension">The extension.</param>
        /// <returns>FileType.</returns>
        public static FileType GetFileTypeFromExtension(string extension)
        {
            extension = extension.ToLower(CultureInfo.InvariantCulture).Trim(' ', '.');
            switch (extension)
            {
                case "stl": return FileType.STL_ASCII;
                case "3mf": return FileType.ThreeMF;
                case "model": return FileType.Model3MF;
                case "amf": return FileType.AMF;
                case "obj": return FileType.OBJ;
                case "off": return FileType.OFF;
                case "ply": return FileType.PLY_ASCII;
                case "shell": return FileType.SHELL;
                case "tvgl":
                case "json": return FileType.TVGL;
                case "tvglz": return FileType.TVGLz;
                default: return FileType.unspecified;
            }
        }

        /// <summary>
        /// Gets the type of the extension from file.
        /// </summary>
        /// <param name="fileType">Type of the file.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.NotImplementedException">Filetype " + fileType + " has not been setup for import/export within TVGL.</exception>
        public static string GetExtensionFromFileType(FileType fileType)
        {
            switch (fileType)
            {
                case FileType.STL_ASCII:
                case FileType.STL_Binary: return "stl";
                case FileType.ThreeMF: return "3mf";
                case FileType.Model3MF: return "model";
                case FileType.AMF: return "amf";
                case FileType.OBJ: return "obj";
                case FileType.OFF: return "off";
                case FileType.PLY_ASCII:
                case FileType.PLY_Binary: return "ply";
                case FileType.SHELL: return "shell";
                case FileType.TVGL: return "tvgl";
                case FileType.TVGLz: return "tvglz";
                default:
                    throw new NotImplementedException("Filetype " + fileType + " has not been setup for import/export within TVGL.");
            }
        }

        /// <summary>
        /// Parses the ID and values from the specified line.
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
                id = line.Substring(0, idx).ToLower(CultureInfo.InvariantCulture);
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
        /// Tries to parse a vertex from a string.
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
        /// Reads the next line with the expectation that is starts with the
        /// "expected" string. If the line does not have the expected value,
        /// then the StreamReader stays at the same position and
        /// false is returned.
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
        /// Reads the line.
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
            if (string.IsNullOrEmpty(line)) return "";
            return line.Trim();
        }

        /// <summary>
        /// Infers the units from comments.
        /// </summary>
        /// <param name="comments">The comments.</param>
        /// <returns>UnitType.</returns>
        protected static UnitType InferUnitsFromComments(List<string> comments)
        {
            foreach (var comment in comments)
            {
                var words = Regex.Matches(comment, "([a-z]+)");
                foreach (var word in words)
                    if (TryParseUnits(word.ToString(), out UnitType units)) return units;
            }
            return UnitType.unspecified;
        }

        /// <summary>
        /// Tries to parse units.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="units">The units.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
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
            if (TryParseUnits(input, out UnitType units)) return units;
            return UnitType.unspecified;
        }

        /// <summary>
        /// Reads the number as int.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="type">The type.</param>
        /// <param name="formatType">Type of the format.</param>
        /// <returns>System.Int32.</returns>
        internal static int ReadNumberAsInt(BinaryReader reader, Type type, FormatEndiannessType formatType)
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

        /// <summary>
        /// Reads the number as float.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="type">The type.</param>
        /// <param name="formatType">Type of the format.</param>
        /// <returns>System.Single.</returns>
        internal static float ReadNumberAsFloat(BinaryReader reader, Type type, FormatEndiannessType formatType)
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

        /// <summary>
        /// Reads the number as double.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="type">The type.</param>
        /// <param name="formatType">Type of the format.</param>
        /// <returns>System.Double.</returns>
        internal static double ReadNumberAsDouble(BinaryReader reader, Type type, FormatEndiannessType formatType)
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
        protected static bool TryParseNumberTypeFromString(string typeString, out Type type)
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

        /// <summary>
        /// Reads the number as int.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="type">The type.</param>
        /// <returns>System.Int32.</returns>
        internal static int ReadNumberAsInt(string text, Type type)
        {
            if (type == typeof(double))
            {
                if (double.TryParse(text, out var value)) return (int)Math.Round(value);
            }
            if (type == typeof(long))
            {
                if (long.TryParse(text, out var value)) return (int)value;
            }
            if (type == typeof(ulong))
            {
                if (ulong.TryParse(text, out var value)) return (int)value;
            }
            if (type == typeof(float))
            {
                if (float.TryParse(text, out var value)) return (int)Math.Round(value);
            }
            if (type == typeof(int))
            {
                if (int.TryParse(text, out var value)) return value;
            }
            if (type == typeof(uint))
            {
                if (uint.TryParse(text, out var value)) return (int)value;
            }
            if (type == typeof(short))
            {
                if (short.TryParse(text, out var value)) return value;
            }
            if (type == typeof(ushort))
            {
                if (ushort.TryParse(text, out var value)) return value;
            }
            if (type == typeof(byte))
            {
                if (byte.TryParse(text, out var value)) return value;
            }
            return int.MinValue;
        }

        /// <summary>
        /// Reads the number as float.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="type">The type.</param>
        /// <returns>System.Single.</returns>
        internal static float ReadNumberAsFloat(string text, Type type)
        {
            if (type == typeof(double))
            {
                if (double.TryParse(text, out var value)) return (float)value;
            }
            if (type == typeof(long))
            {
                if (long.TryParse(text, out var value)) return value;
            }
            if (type == typeof(ulong))
            {
                if (ulong.TryParse(text, out var value)) return value;
            }
            if (type == typeof(float))
            {
                if (float.TryParse(text, out var value)) return value;
            }
            if (type == typeof(int))
            {
                if (int.TryParse(text, out var value)) return value;
            }
            if (type == typeof(uint))
            {
                if (uint.TryParse(text, out var value)) return value;
            }
            if (type == typeof(short))
            {
                if (short.TryParse(text, out var value)) return value;
            }
            if (type == typeof(ushort))
            {
                if (ushort.TryParse(text, out var value)) return value;
            }
            if (type == typeof(byte))
            {
                if (byte.TryParse(text, out var value)) return value;
            }
            return float.NaN;
        }

        /// <summary>
        /// Reads the number as double.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="type">The type.</param>
        /// <returns>System.Double.</returns>
        internal static double ReadNumberAsDouble(string text, Type type)
        {
            if (type == typeof(double))
            {
                if (Double.TryParse(text, out var value)) return value;
            }
            if (type == typeof(long))
            {
                if (Int64.TryParse(text, out var value)) return value;
            }
            if (type == typeof(ulong))
            {
                if (UInt64.TryParse(text, out var value)) return value;
            }
            if (type == typeof(float))
            {
                if (Single.TryParse(text, out var value)) return value;
            }
            if (type == typeof(int))
            {
                if (Int32.TryParse(text, out var value)) return value;
            }
            if (type == typeof(uint))
            {
                if (UInt32.TryParse(text, out var value)) return value;
            }
            if (type == typeof(short))
            {
                if (Int16.TryParse(text, out var value)) return value;
            }
            if (type == typeof(ushort))
            {
                if (UInt16.TryParse(text, out var value)) return value;
            }
            if (type == typeof(byte))
            {
                if (Byte.TryParse(text, out var value)) return value;
            }
            return Double.NaN;
        }

        #endregion Open/Load/Read

        #region Save/Write

        /// <summary>
        /// Saves the specified solids to a file.
        /// </summary>
        /// <param name="solids">The solids.</param>
        /// <param name="filename">The filename.</param>
        /// <param name="fileType">Type of the file.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public static bool Save(IList<Solid> solids, string filename, FileType fileType = FileType.unspecified)
        {
            if (fileType == FileType.unspecified)
                fileType = GetFileTypeFromExtension(Path.GetExtension(filename));
            filename = Path.ChangeExtension(filename, GetExtensionFromFileType(fileType));
            using var fileStream = File.OpenWrite(filename);
            return Save(fileStream, solids, fileType);
        }

        /// <summary>
        /// Saves the specified solid to a file.
        /// </summary>
        /// <param name="solid">The solid.</param>
        /// <param name="filename">The filename.</param>
        /// <param name="fileType">Type of the file.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public static bool Save(Solid solid, string filename, FileType fileType = FileType.unspecified)
        {
            if (fileType == FileType.unspecified)
                fileType = GetFileTypeFromExtension(Path.GetExtension(filename));
            var dir = Path.GetDirectoryName(filename);
            filename = Path.GetFileName(Path.ChangeExtension(filename, GetExtensionFromFileType(fileType)));
            if (!string.IsNullOrWhiteSpace(dir)) filename = dir + Path.DirectorySeparatorChar + filename;
            using var fileStream = File.OpenWrite(filename);
            return Save(fileStream, solid, fileType);
        }

        /// <summary>
        /// Saves the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="solids">The solids.</param>
        /// <param name="fileType">Type of the file.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        /// <exception cref="System.NotSupportedException">The " + fileType.ToString() + "format does not support saving multiple solids to a single file.</exception>
        /// <exception cref="System.NotSupportedException">Saving to the " + fileType.ToString() + "format is not yet supported in TVGL.</exception>
        /// <exception cref="System.NotImplementedException">Need to provide filepath instread of stream.</exception>
        public static bool Save(Stream stream, IList<Solid> solids, FileType fileType = FileType.TVGL)
        {
            if (solids.Count == 0) return false;
            if (solids.Count == 1) return Save(stream, solids[0], fileType);
            switch (fileType)
            {
                case FileType.STL_ASCII:
                    return STLFileData.SaveASCII(stream, solids.Cast<TessellatedSolid>().ToArray());

                case FileType.STL_Binary:
                    return STLFileData.SaveBinary(stream, solids.Cast<TessellatedSolid>().ToArray());

                case FileType.AMF:
                    return AMFFileData.SaveSolids(stream, solids.Cast<TessellatedSolid>().ToArray());

                case FileType.OBJ:
                    return OBJFileData.SaveSolids(stream, solids.Cast<TessellatedSolid>().ToArray());

                case FileType.ThreeMF:
                    return ThreeMFFileData.Save(stream, solids.Cast<TessellatedSolid>().ToArray());

                case FileType.Model3MF:
                    return ThreeMFFileData.SaveModel(stream, solids.Cast<TessellatedSolid>().ToArray());

                case FileType.OFF:
                case FileType.PLY_ASCII:
                case FileType.PLY_Binary:
                    throw new NotSupportedException(
                        "The " + fileType.ToString() + "format does not support saving multiple solids to a single file.");

                case FileType.TVGL:
                    return SaveToTVGL(stream, new SolidAssembly(solids));

                case FileType.TVGLz:
                    throw new NotImplementedException("Need to provide filepath instread of stream.");

                default:
                    throw new NotSupportedException(
                        "Saving to the " + fileType.ToString() + "format is not yet supported in TVGL.");
            }
        }

        /// <summary>
        /// Saves the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="solid">The solid.</param>
        /// <param name="fileType">Type of the file.</param>
        /// <returns>System.Boolean.</returns>
        /// <exception cref="System.NotSupportedException">Saving to the " + fileType.ToString() + "format is not yet supported in TVGL.</exception>
        public static bool Save(Stream stream, Solid solid, FileType fileType = FileType.TVGL)
        {
            switch (fileType)
            {
                //File types that save to an array of solids
                case FileType.STL_ASCII:
                    return STLFileData.SaveASCII(stream, new[] { (TessellatedSolid)solid });

                case FileType.STL_Binary:
                    return STLFileData.SaveBinary(stream, new[] { (TessellatedSolid)solid });

                case FileType.AMF:
                    return AMFFileData.SaveSolids(stream, new[] { (TessellatedSolid)solid });

                case FileType.OBJ:
                    return OBJFileData.SaveSolids(stream, new[] { (TessellatedSolid)solid });

                case FileType.ThreeMF:
                    return ThreeMFFileData.Save(stream, new[] { (TessellatedSolid)solid });

                case FileType.Model3MF:
                    return ThreeMFFileData.SaveModel(stream, new[] { (TessellatedSolid)solid });

                case FileType.TVGL:
                case FileType.TVGLz:
                    return Save(stream, new[] { solid });

                //Filetypes that save as single solids
                case FileType.OFF:
                    return OFFFileData.SaveSolid(stream, (TessellatedSolid)solid);

                case FileType.PLY_ASCII:
                    return PLYFileData.SaveSolidASCII(stream, (TessellatedSolid)solid);

                case FileType.PLY_Binary:
                    return PLYFileData.SaveSolidBinary(stream, (TessellatedSolid)solid);

                default:
                    throw new NotSupportedException(
                        "Saving to the " + fileType.ToString() + "format is not yet supported in TVGL.");
            }
        }

        /// <summary>
        /// Saves the solid as a string.
        /// </summary>
        /// <param name="solid">The solid.</param>
        /// <param name="fileType">Type of the file.</param>
        /// <returns>System.String.</returns>
        public static string SaveToString(Solid solid, FileType fileType = FileType.unspecified)
        {
            using var stream = new MemoryStream();
            if (!Save(stream, solid, fileType)) return "";
            var byteArray = stream.ToArray();
            return System.Text.Encoding.Unicode.GetString(byteArray, 0, byteArray.Length);
        }

        /// <summary>
        /// Saves the solids as a string.
        /// </summary>
        /// <param name="solids">The solids.</param>
        /// <param name="fileType">Type of the file.</param>
        /// <returns>System.String.</returns>
        public static string SaveToString(IList<Solid> solids, FileType fileType = FileType.unspecified)
        {
            using var stream = new MemoryStream();
            if (!Save(stream, solids, fileType)) return "";
            var byteArray = stream.ToArray();
            return System.Text.Encoding.Unicode.GetString(byteArray, 0, byteArray.Length);
        }


        /// <summary>
        /// Saves the specified polygon.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="filename">The filename.</param>
        public static void Save(Polygon polygon, string filename)
        {
            var serializer = new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto,
                Formatting = Formatting.Indented,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            };

            var NumberOfRetries = 3;
            var DelayOnRetry = 1000;
            for (int i = 1; i <= NumberOfRetries; ++i)
            {
                try
                {
                    using var fileStream = File.OpenWrite(filename);
                    using var sw = new StreamWriter(fileStream);
                    using var writer = new JsonTextWriter(sw);
                    var jObject = JObject.FromObject(polygon, serializer);
                    jObject.WriteTo(writer);
                    writer.Flush();
                    break; // When done we can break loop
                }
                catch (IOException e) when (i <= NumberOfRetries)
                {
                    // You may check error code to filter some exceptions, not every error
                    // can be recovered.
                    Thread.Sleep(DelayOnRetry);
                }
            }
        }

        /// <summary>
        /// Opens the specified filename.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <param name="polygon">The polygon.</param>
        /// <exception cref="System.IO.FileNotFoundException">The file was not found at: " + filename</exception>
        public static void Open(string filename, out Polygon polygon)
        {
            if (!File.Exists(filename)) throw new FileNotFoundException("The file was not found at: " + filename);
            using var fileStream = File.OpenRead(filename);
            using var sr = new StreamReader(fileStream);
            using var reader = new JsonTextReader(sr);
            var serializer = new JsonSerializer();
            polygon = serializer.Deserialize<Polygon>(reader);
        }


        // These methods are currently not used, but it seems that encoding a doubles array as
        // a binary char array would be better than parsing the text. There would be 1) less
        // chance of roundoff,quicker conversions, and - in most cases- a smaller file.
        // however, experiment in early Jan2020, did not tend to show this. but even the
        // doubles changed value which makes me think I didn't do a good job with it.
        // Come back to this in the future?
        /// <summary>
        /// Converts the double array to string.
        /// </summary>
        /// <param name="doubles">The doubles.</param>
        /// <returns>System.String.</returns>
        internal static string ConvertDoubleArrayToString(IEnumerable<double> doubles)
        {
            var byteArray = doubles.SelectMany(BitConverter.GetBytes).ToArray();
            return System.Text.Encoding.Unicode.GetString(byteArray);
        }

        /// <summary>
        /// Converts the string to double array.
        /// </summary>
        /// <param name="doublesAsString">The doubles as string.</param>
        /// <returns>System.Double[].</returns>
        internal static double[] ConvertStringToDoubleArray(string doublesAsString)
        {
            var bytes = System.Text.Encoding.Unicode.GetBytes(doublesAsString);
            var values = new double[bytes.Length / 8];
            for (int i = 0; i < values.Length; i++)
                values[i] = BitConverter.ToDouble(bytes, i * 8);
            return values;
        }


        /// <summary>
        /// Gets the TVGL date mark text.
        /// </summary>
        /// <value>The TVGL date mark text.</value>
        protected static string TvglDateMarkText
        {
            get
            {
                var now = DateTime.Now;
                return "created by TVGL on " + now.Year + "/" + now.Month + "/" + now.Day + " at " + now.Hour + ":" +
                       now.Minute + ":" + now.Second;
            }
        }

        /// <summary>
        /// Saves to TVG lz.
        /// </summary>
        /// <param name="solidAssembly">The solid assembly.</param>
        /// <param name="filename">The filename.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public static bool SaveToTVGLz(SolidAssembly solidAssembly, string filename)
        {
            //Delete the existing file if it exists, so that our stream doesn't get corrupted
            //(i.e. if the existing location is longer than what we write, some of the original fil will remain
            var tvgl = Path.ChangeExtension(filename, GetExtensionFromFileType(FileType.TVGL));
            var tvglz = Path.ChangeExtension(filename, GetExtensionFromFileType(FileType.TVGLz));
            if (File.Exists(tvgl)) File.Delete(tvgl);
            if (File.Exists(tvglz)) File.Delete(tvglz);

            using var fileStream = File.OpenWrite(tvgl);
            if (!SaveToTVGL(fileStream, solidAssembly))
                return false;
            try
            {
                using (ZipArchive zip = ZipFile.Open(tvglz, ZipArchiveMode.Create))
                {
                    zip.CreateEntryFromFile(tvgl, "TVGL", CompressionLevel.Optimal);
                }
                //Delete temp file
                if (File.Exists(tvgl)) File.Delete(tvgl);
            }
            catch
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Saves to TVGL.
        /// </summary>
        /// <param name="fileStream">The file stream.</param>
        /// <param name="solidAssembly">The solid assembly.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private static bool SaveToTVGL(Stream fileStream, SolidAssembly solidAssembly)
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
        /// Reads the stream.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <param name="solidAssembly">The solid assembly.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public static bool ReadStream(string filename, out SolidAssembly solidAssembly, TessellatedSolidBuildOptions tsBuildOptions)
        {
            solidAssembly = null;
            if (!File.Exists(filename))
                return false;

            var extension = Path.GetExtension(filename);
            if (GetFileTypeFromExtension(extension) == FileType.TVGLz)
            {
                var unzipped = Path.ChangeExtension(filename, GetExtensionFromFileType(FileType.TVGL));
                if (File.Exists(unzipped)) { File.Delete(unzipped); }
                //Unzip the file
                using (ZipArchive archive = ZipFile.OpenRead(filename))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        entry.ExtractToFile(unzipped);
                    }
                }
                filename = unzipped;
            }
            else if (GetFileTypeFromExtension(extension) != FileType.TVGL) return false;

            try
            {
                using var s = File.Open(filename, FileMode.Open);
                using (var streamReader = new StreamReader(s))
                using (var reader = new JsonTextReader(streamReader))
                {
                    SolidAssembly.StreamRead(reader, out solidAssembly, tsBuildOptions);
                    reader.Close();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Adds the entry.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="fileContent">Content of the file.</param>
        /// <param name="archive">The archive.</param>
        private static void AddEntry(string fileName, byte[] fileContent, ZipArchive archive)
        {
            var entry = archive.CreateEntry(fileName);
            using (var stream = entry.Open())
                stream.Write(fileContent, 0, fileContent.Length);

        }
        #endregion Save/Write

        #region Load Assembly
        public static bool LoadAsSolidAssembly(string filePath, out SolidAssembly solidAssembly, TessellatedSolidBuildOptions buildOptions)
        {
            var extension = GetFileTypeFromExtension(Path.GetExtension(filePath));
            solidAssembly = null;
            //try
            //{
            if (extension == FileType.unspecified)
                throw new ArgumentException("Unknown file extension.");
            else
            {
                //unzip if needed
                if (extension == FileType.TVGLz || extension == FileType.TVGL)
                {
                    ReadStream(filePath, out solidAssembly, buildOptions);
                }
                else if (extension != FileType.unspecified)
                {
                    Open(filePath, out TessellatedSolid[] solids);
                    solidAssembly = new SolidAssembly(solids);
                }
                else
                    throw new NotSupportedException("File type is not supported: " + extension);
            }
            return true;
        }

        public static bool LoadMostSignificantAsPart(string filePath, out Solid part, TessellatedSolidBuildOptions buildOptions)
        {
            part = null;
            if (!LoadAsSolidAssembly(filePath, out var solidAssembly, buildOptions))
                return false;
            if (solidAssembly == null || solidAssembly.Solids == null || !solidAssembly.Solids.Any(s => s is TessellatedSolid))
            {
                Debug.WriteLine("No valid tessellated solids defined.");
                return false;
            }

            //Casting from the solidAssembly, Solids list puts each solid in the coordinate system
            //that it was locally defined in (not it's global position in the assembly - which we don't want).
            var tessellatedSolid = ReturnMostSignificantSolid(solidAssembly, out _);

            part = tessellatedSolid;
            return true;
        }

        public static TessellatedSolid ReturnMostSignificantSolid(SolidAssembly solidAssembly, out IEnumerable<TessellatedSolid> significantSolids)
        {
            solidAssembly.GetTessellatedSolids(out var solids, out var sheets);
            if (solids.Any())//prefer solids over sheets.
            {
                //If the file contains multiple solids, see if we can get just one solid.
                if (solids.Count() == 1)
                {
                    significantSolids = new List<TessellatedSolid> { solids.First() };
                    return solids.First();
                }
                else 
                {
                    var maxVolume = solids.Max(p => p.AxisAlignedBoundingBoxVolume);
                    var maxNumFaces = solids.Max(p => p.NumberOfFaces);
                    significantSolids = solids.Where(p => p.AxisAlignedBoundingBoxVolume > maxVolume * .1 || p.NumberOfFaces > maxNumFaces * .3);
                    if (significantSolids.Count() > 1)
                        Debug.WriteLine("Model contains " + significantSolids.Count() + " significant solid bodies. Attempting analysis on largest part in assembly.");
                    else
                        Debug.WriteLine("Model contains " + solids.Count() + " solid bodies, but only one is significant. Attempting analysis on largest part in assembly.");
                    return significantSolids.MaxBy(p => p.AxisAlignedBoundingBoxVolume);
                }    
            }
            else if(sheets.Any())
            {
                if (sheets.Count() == 1)
                {
                    significantSolids = new List<TessellatedSolid> { sheets.First() };
                    return sheets.First();          
                }
                else 
                {
                    //use AxisAlignedBoundingBoxVolume rather than volume in case volume was calculated incorrectly
                    //as is possible if the sheet is built incorrectly - full of errors. The convex hull volume 
                    //could also be considered.
                    var maxVolume = sheets.Max(p => p.AxisAlignedBoundingBoxVolume);
                    var maxNumFaces = sheets.Max(p => p.NumberOfFaces);
                    significantSolids = sheets.Where(p => p.AxisAlignedBoundingBoxVolume > maxVolume * .1 || p.NumberOfFaces > maxNumFaces * .3);
                    if (significantSolids.Count() > 1)
                        Debug.WriteLine("Model contains " + significantSolids.Count() + " significant sheet bodies. Attempting analysis on largest part in assembly.");
                    else
                        Debug.WriteLine("Model contains " + sheets.Count() + " sheet bodies, but only one is significant. Attempting analysis on largest part in assembly.");
                    return significantSolids.MaxBy(p => p.AxisAlignedBoundingBoxVolume);
                }
            }
            significantSolids = null;
            return null;
        }
        #endregion
    }
}