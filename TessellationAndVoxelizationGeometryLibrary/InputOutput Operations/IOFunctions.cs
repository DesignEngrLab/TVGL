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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
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
        #region Single Solid

        /// <summary>
        /// Opens the specified filename.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns>Solid.</returns>
        /// <exception cref="System.IO.FileNotFoundException">The file was not found at: " + filename</exception>
        public static Solid Open(string filename, TessellatedSolidBuildOptions tsBuildOptions = null)
        {
            Open(filename, out Solid s, tsBuildOptions);
            return s;
        }

        /// <summary>
        /// Opens the 3D solid or solids from a provided file name.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <param name="solid">The solid.</param>
        /// <returns>A list of TessellatedSolids.</returns>
        /// <exception cref="System.IO.FileNotFoundException">The file was not found at: " + filename</exception>
        public static bool Open<T>(string filename, out T solid, TessellatedSolidBuildOptions tsBuildOptions = null)
            where T : Solid
        {
            if (File.Exists(filename))
                using (var fileStream = File.OpenRead(filename))
                    return Open(fileStream, filename, out solid, tsBuildOptions);
            else throw new FileNotFoundException("The file was not found at: " + filename);
        }


        /// <summary>
        /// Opens the specified s.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <param name="filename">The filename.</param>
        /// <returns>Solid.</returns>
        private static bool Open<T>(Stream s, string filename, out T solid, TessellatedSolidBuildOptions tsBuildOptions = null)
            where T : Solid
        {
            var extension = GetFileTypeFromExtension(Path.GetExtension(filename));
            Solid solidInner = null;

            switch (extension)
            {
                case FileType.STL_ASCII:
                case FileType.STL_Binary:
                    solidInner = GetMostSignificantSolid(STLFileData.OpenSolids(s, filename, tsBuildOptions), out _); // Standard Tessellation or StereoLithography
                    break;
                case FileType.ThreeMF:
                    solidInner = GetMostSignificantSolid(ThreeMFFileData.OpenSolids(s, filename, tsBuildOptions), out _);
                    break;
                case FileType.Model3MF:
                    solidInner = GetMostSignificantSolid(ThreeMFFileData.OpenModelFile(s, filename, tsBuildOptions), out _);
                    break;
                case FileType.AMF:
                    solidInner = GetMostSignificantSolid(AMFFileData.OpenSolids(s, filename, tsBuildOptions), out _);
                    break;
                case FileType.OBJ:
                    solidInner = GetMostSignificantSolid(OBJFileData.OpenSolids(s, filename, tsBuildOptions), out _);
                    break;
                case FileType.OFF:
                    solidInner = OFFFileData.OpenSolid(s, filename, tsBuildOptions);
                    break;
                case FileType.PLY_ASCII:
                case FileType.PLY_Binary:
                    solidInner = PLYFileData.OpenSolid(s, filename, tsBuildOptions);
                    break;
                case FileType.TVGL:
                    return TVGLFileData.OpenTVGL(s, out solid, tsBuildOptions);
                case FileType.TVGLz:
                    return TVGLFileData.OpenTVGLz(s, out solid, tsBuildOptions);
            }
            solid = solidInner as T;
            return solid != null;
        }
        /// <summary>
        /// Returns the most significant solids, based on a number of faces and convex hull volume.
        /// </summary>
        /// <param name="solidAssembly"></param>
        /// <param name="significantSolids"></param>
        /// <returns></returns>
        public static TessellatedSolid GetMostSignificantSolid(SolidAssembly solidAssembly, out IEnumerable<TessellatedSolid> significantSolids)
        => GetMostSignificantSolid(solidAssembly.Solids.Where(p => p is TessellatedSolid).Cast<TessellatedSolid>(), out significantSolids);

        /// <summary>
        /// Returns the most significant solids, based on a number of faces and convex hull volume.
        /// </summary>
        /// <param name="solidAssembly"></param>
        /// <param name="significantSolids"></param>
        /// <returns></returns>
        public static TessellatedSolid GetMostSignificantSolid(IEnumerable<TessellatedSolid> solids, out IEnumerable<TessellatedSolid> significantSolids)
        {
            var maxVolume = solids.Max(p => p.ConvexHull.Volume);
            var maxNumFaces = solids.Max(p => p.NumberOfFaces);
            //Minimum number of primitives is either 4 OR if no solids have 4 primitives, it is the max number of primitives
            var minPrimitivesRequired = Math.Min(solids.Max(p => p.NumberOfPrimitives), 4);
            significantSolids = solids.Where(p => (p.ConvexHull.Volume > maxVolume * .1 || p.NumberOfFaces > maxNumFaces * .3) && p.NumberOfPrimitives >= minPrimitivesRequired);
            if (significantSolids.Count() > 1)
                Debug.WriteLine("Model contains " + significantSolids.Count() + " significant solid bodies. Attempting analysis on largest part in assembly.");
            else
                Debug.WriteLine("Model contains " + solids.Count() + " solid bodies, but only one is significant. Attempting analysis on largest part in assembly.");
            return significantSolids.MaxBy(p => p.ConvexHull.Volume);
        }

        /// <summary>
        /// Opens from string.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="fileType">Type of the file.</param>
        /// <param name="solid">The solid.</param>
        public static bool OpenFromString(string data, FileType fileType, out TessellatedSolid solid)
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(data));
            //using (var reader = new StreamReader(stream))
            //{
            //    reader.Read(data.AsSpan());
            //    reader.Flush();
            //}
            //stream.Coordinates = 0;
            var name = "data." + GetExtensionFromFileType(fileType);
            return Open(stream, name, out solid);
        }
        /// <summary>
        /// Opens from string.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="solid">The solid.</param>
        public static bool OpenFromString<T>(string data, out T solid, TessellatedSolidBuildOptions tsBuildOptions = null) where T : Solid
        {
            var stream = new MemoryStream();
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(data);
                writer.Flush();
            }
            stream.Position = 0;
            return Open(stream, "", out solid, tsBuildOptions);
        }


        #endregion
        #region Open Array of Solids
        /// <summary>
        /// Opens the specified filename.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <param name="solids">The solids.</param>
        /// <exception cref="System.IO.FileNotFoundException">The file was not found at: " + filename</exception>
        public static bool Open(string filename, out TessellatedSolid[] solids, TessellatedSolidBuildOptions tsBuildOptions = null)
        {
            if (File.Exists(filename))
                using (var fileStream = File.OpenRead(filename))
                    return Open(fileStream, filename, out solids, tsBuildOptions);
            else throw new FileNotFoundException("The file was not found at: " + filename);
        }
        /// <summary>
        /// Opens the specified s.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <param name="filename">The filename.</param>
        /// <param name="tessellatedSolids">The tessellated solids.</param>
        /// <exception cref="System.Exception">Attempting to open multiple solids with a " + extension.ToString() + " file.</exception>
        public static bool Open(Stream s, string filename, out TessellatedSolid[] tessellatedSolids,
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
                    TVGLFileData.OpenTVGL(s, out SolidAssembly solidAssembly);
                    tessellatedSolids = solidAssembly.RootAssembly.AllTessellatedSolidsInGlobalCoordinateSystem();
                    break;
                case FileType.TVGLz:
                    TVGLFileData.OpenTVGLz(s, out solidAssembly);
                    tessellatedSolids = solidAssembly.RootAssembly.AllTessellatedSolidsInGlobalCoordinateSystem();
                    break;
                default:
                    Message.output(filename + " is not a recognized 3D format.");
                    tessellatedSolids = Array.Empty<TessellatedSolid>();
                    break;
            }
            //}
            //catch (Exception exc)
            //{
            //    throw new Exception("Cannot open file. Message: " + exc.Message);
            //}
            return tessellatedSolids != null;
        }

        /// <summary>
        /// Opens from string.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="fileType">Type of the file.</param>
        /// <param name="solids">The solids.</param>
        public static bool OpenFromString(string data, FileType fileType, out TessellatedSolid[] solids)
        {
            var stream = new MemoryStream();
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(data);
                writer.Flush();
            }
            stream.Position = 0;
            var name = "data." + GetExtensionFromFileType(fileType);
            return Open(stream, name, out solids);
        }
        #endregion
        #region Solid Assembly
        /// <summary>
        /// Opens the specified filename.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <param name="solids">The solids.</param>
        /// <exception cref="System.IO.FileNotFoundException">The file was not found at: " + filename</exception>
        public static bool Open(string filename, out SolidAssembly solids, TessellatedSolidBuildOptions tsBuildOptions = null)
        {
            if (File.Exists(filename))
                using (var fileStream = File.OpenRead(filename))
                    return Open(fileStream, filename, out solids, tsBuildOptions);
            else throw new FileNotFoundException("The file was not found at: " + filename);
        }
        /// <summary>
        /// Opens the specified s.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <param name="filename">The filename.</param>
        /// <param name="solidAssembly">The tessellated solids.</param>
        /// <exception cref="System.Exception">Attempting to open multiple solids with a " + extension.ToString() + " file.</exception>
        public static bool Open(Stream s, string filename, out SolidAssembly solidAssembly,
            TessellatedSolidBuildOptions tsBuildOptions = null)
        {
            solidAssembly = null;
            try
            {
                var extension = GetFileTypeFromExtension(Path.GetExtension(filename));
                switch (extension)
                {
                    case FileType.STL_ASCII:
                    case FileType.STL_Binary:
                        solidAssembly = new SolidAssembly(STLFileData.OpenSolids(s, filename, tsBuildOptions)); // Standard Tessellation or StereoLithography
                        break;

                    case FileType.ThreeMF:
                        solidAssembly = new SolidAssembly(ThreeMFFileData.OpenSolids(s, filename, tsBuildOptions));
                        break;

                    case FileType.Model3MF:
                        solidAssembly = new SolidAssembly(ThreeMFFileData.OpenModelFile(s, filename, tsBuildOptions));
                        break;

                    case FileType.AMF:
                        solidAssembly = new SolidAssembly(AMFFileData.OpenSolids(s, filename, tsBuildOptions));
                        break;

                    case FileType.OBJ:
                        solidAssembly = new SolidAssembly(OBJFileData.OpenSolids(s, filename, tsBuildOptions));
                        break;

                    case FileType.OFF:
                    case FileType.PLY_ASCII:
                    case FileType.PLY_Binary:
                        solidAssembly = new SolidAssembly([PLYFileData.OpenSolid(s, filename, tsBuildOptions)]);
                        break;
                    case FileType.TVGL:
                        TVGLFileData.OpenTVGL(s, out solidAssembly);
                        break;
                    case FileType.TVGLz:
                        TVGLFileData.OpenTVGLz(s, out solidAssembly);
                        break;
                    default:
                        Message.output(filename + " is not a recognized 3D format.");
                        solidAssembly = null;
                        break;
                }
            }
            catch (Exception exc)
            {
                Message.output("Cannot open file. Message: " + exc.Message);
                return false;
            }
            return solidAssembly != null;
        }

        /// <summary>
        /// Opens from string.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="fileType">Type of the file.</param>
        /// <param name="solids">The solids.</param>
        public static bool OpenFromString(string data, FileType fileType, out SolidAssembly solids)
        {
            var stream = new MemoryStream();
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(data);
                writer.Flush();
            }
            stream.Position = 0;
            var name = "data." + GetExtensionFromFileType(fileType);
            return Open(stream, name, out solids);
        }

        #endregion
        #region Open Polygon
        /// <summary>
        /// Opens the specified filename.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <param name="polygon">The polygon.</param>
        /// <exception cref="System.IO.FileNotFoundException">The file was not found at: " + filename</exception>
        public static bool Open(string filename, out Polygon polygon)
        {
            if (!File.Exists(filename)) throw new FileNotFoundException("The file was not found at: " + filename);
            using var fileStream = File.OpenRead(filename);
            using var sr = new StreamReader(fileStream);
            using var reader = new JsonTextReader(sr);
            var serializer = new JsonSerializer();
            polygon = serializer.Deserialize<Polygon>(reader);
            return true;
        }
        #endregion
        #region Methods used by Open functions
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
        #endregion
        #endregion Open/Load/Read

        #region Save/Write
        #region Single Solid
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
            if (!string.IsNullOrWhiteSpace(dir)) filename = Path.Combine(dir, filename);
            using var fileStream = File.OpenWrite(filename);
            return Save(fileStream, solid, fileType);
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
                    return TVGLFileData.SaveToTVGL(stream, solid);
                case FileType.TVGLz:
                    return TVGLFileData.SaveToTVGLz(stream, solid);

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
        #endregion
        #region Array of Solids
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
                    return TVGLFileData.SaveToTVGL(stream, new SolidAssembly(solids));

                case FileType.TVGLz:
                    throw new NotImplementedException("Need to provide filepath instead of stream.");

                default:
                    throw new NotSupportedException(
                        "Saving to the " + fileType.ToString() + "format is not yet supported in TVGL.");
            }
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
        #endregion
        #region Solid Assembly
        /// <summary>
        /// Saves the specified solids to a file.
        /// </summary>
        /// <param name="solids">The solids.</param>
        /// <param name="filename">The filename.</param>
        /// <param name="fileType">Type of the file.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public static bool Save(SolidAssembly solidAssembly, string filename, FileType fileType = FileType.unspecified)
        {
            if (fileType == FileType.unspecified)
                fileType = GetFileTypeFromExtension(Path.GetExtension(filename));
            if (fileType == FileType.TVGL || fileType == FileType.TVGLz)
            {
                filename = Path.ChangeExtension(filename, GetExtensionFromFileType(fileType));
                using var fileStream = File.OpenWrite(filename);
                return Save(fileStream, solidAssembly, fileType);
            }
            else
            {
                Message.output("The fileType must be TVGL or TVGLz to save as a SolidAssembly.");
                return false;
            }
        }
        /// <summary>
        /// Saves the solids as a string.
        /// </summary>
        /// <param name="solids">The solids.</param>
        /// <param name="fileType">Type of the file.</param>
        /// <returns>System.String.</returns>
        public static string SaveToString(SolidAssembly solidAssembly)
        {
            using var stream = new MemoryStream();
            if (!Save(stream, solidAssembly, FileType.TVGL)) return "";
            var byteArray = stream.ToArray();
            return Encoding.Unicode.GetString(byteArray, 0, byteArray.Length);
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
        public static bool Save(Stream stream, SolidAssembly solidAssembly, FileType fileType = FileType.TVGL)
        {
            if (solidAssembly.NumberOfSolidBodies == 0) return false;
            if (fileType != FileType.TVGL && fileType != FileType.TVGLz)
            {
                if (solidAssembly.NumberOfSolidBodies == 1)
                    return Save(stream, solidAssembly.Solids[0], fileType);
                else
                {
                    Message.output("The fileType must be TVGL or TVGLz to save as a SolidAssembly.");
                    return false;
                }
            }
            if (fileType == FileType.TVGLz)
                return TVGLFileData.SaveToTVGLz(stream, solidAssembly);
            else //if (fileType == FileType.TVGL)
                return TVGLFileData.SaveToTVGL(stream, solidAssembly);
        }
        #endregion
        #region Polygon
        /// <summary>
        /// Saves the specified polygon.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="filename">The filename.</param>
        public static bool Save(Polygon polygon, string filename)
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
            for (int i = 0; i < NumberOfRetries; ++i)
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
                catch (IOException e) when (i < NumberOfRetries - 1)
                {
                    // You may check error code to filter some exceptions, not every error
                    // can be recovered.
                    Thread.Sleep(DelayOnRetry);
                }
                catch (IOException e) when (i == NumberOfRetries - 1)
                { return false; }
            }
            return true;
        }

        #endregion
        #region Methods Used by Save functions
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

        #endregion Save/Write
        #endregion
    }
}