// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 02-27-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 05-28-2016
// ***********************************************************************
// <copyright file="STLFileData.cs" company="Design Engineering Lab">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TVGL.IOFunctions
{
    /// <summary>
    ///     Provides an importer for StereoLithography .StL files.
    /// </summary>
    /// <remarks>The format is documented on <a href="http://en.wikipedia.org/wiki/STL_(file_format)">Wikipedia</a>.</remarks>
    internal class STLFileData : IO
    {
        #region Constructor

        /// <summary>
        ///     Initializes a new instance of the <see cref="STLFileData" /> class.
        /// </summary>
        private STLFileData()
        {
            Normals = new List<double[]>();
            Vertices = new List<List<double[]>>();
            Colors = new List<Color>();
            Units = UnitType.unspecified;
        }

        #endregion

        #region Fields and Properties

        /// <summary>
        ///     The regular expression used to parse normal vectors.
        /// </summary>
        private static readonly Regex NormalRegex = new Regex(@"normal\s*(\S*)\s*(\S*)\s*(\S*)");

        /// <summary>
        ///     The regular expression used to parse vertices.
        /// </summary>
        private static readonly Regex VertexRegex = new Regex(@"vertex\s*(\S*)\s*(\S*)\s*(\S*)");

        /// <summary>
        ///     The last color
        /// </summary>
        private Color _lastColor;

        /// <summary>
        ///     Gets the has color specified.
        /// </summary>
        /// <value>The has color specified.</value>
        private bool HasColorSpecified { get; set; }

        /// <summary>
        ///     Gets or sets the colors.
        /// </summary>
        /// <value>The colors.</value>
        private List<Color> Colors { get; }

        /// <summary>
        ///     Gets or sets the Vertices.
        /// </summary>
        /// <value>The vertices.</value>
        private List<List<double[]>> Vertices { get; }

        /// <summary>
        ///     Gets or sets the normals.
        /// </summary>
        /// <value>The normals.</value>
        private List<double[]> Normals { get; }

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
            var typeString = "";
            var now = DateTime.Now;
            List<STLFileData> stlData;
            // Try to read in BINARY format
            if (TryReadBinary(s, filename, out stlData))
                typeString = "binary STL";
            else
            {
                // Reset position of stream
                s.Position = 0;
                // Read in ASCII format
                if (TryReadAscii(s, filename, out stlData))
                    typeString = "ASCII STL";
                else
                {
                    Message.output("Unable to read in STL file called {0}", filename, 1);
                    return null;
                }
            }
            var results = new List<TessellatedSolid>();
            foreach (var stlFileData in stlData)
                results.Add(new TessellatedSolid(stlFileData.Normals, stlFileData.Vertices,
                    stlFileData.HasColorSpecified ? stlFileData.Colors : null, stlFileData.Units,
                    stlFileData.Name, filename, stlFileData.Comments, stlFileData.Language));
            Message.output(
                "Successfully read in " + typeString + " file called " + filename + " in " +
                (DateTime.Now - now).TotalSeconds + " seconds.", 4);
            return results;
        }

        #region ASCII

        /// <summary>
        /// Reads the model in ASCII format from the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="filename">The filename.</param>
        /// <param name="stlData">The STL data.</param>
        /// <returns>True if the model was loaded successfully.</returns>
        internal static bool TryReadAscii(Stream stream, string filename, out List<STLFileData> stlData)
        {
            var defaultName = GetNameFromFileName(filename) + "_";
            var solidNum = 0;
            var reader = new StreamReader(stream);
            stlData = new List<STLFileData>();
            var stlSolid = new STLFileData { FileName = filename };
            var comments = new List<string>();
            while (!reader.EndOfStream)
            {
                var line = ReadLine(reader);
                comments.Add(line);
                string id, values;
                ParseLine(line, out id, out values);
                switch (id)
                {
                    case "#":
                        stlSolid.Comments.Add(values);
                        break;
                    case "solid":
                        if (string.IsNullOrWhiteSpace(values))
                            stlSolid.Name = defaultName + ++solidNum;
                        stlSolid.Name = values.Trim(' ');
                        break;
                    case "facet":
                        stlSolid.ReadFacet(reader, values);
                        break;
                    case "endsolid":
                        stlData.Add(stlSolid);
                        stlSolid = new STLFileData();
                        break;
                }
            }
            stlSolid.Units = InferUnitsFromComments(comments);
            return true;
        }

        /// <summary>
        ///     Reads a facet.
        /// </summary>
        /// <param name="reader">The stream reader.</param>
        /// <param name="normal">The normal.</param>
        /// <exception cref="IOException">
        ///     Unexpected line.
        ///     or
        ///     Unexpected line.
        ///     or
        ///     Unexpected line.
        /// </exception>
        private void ReadFacet(StreamReader reader, string normal)
        {
            double[] n;
            if (!TryParseDoubleArray(NormalRegex, normal, out n))
                throw new IOException("Unexpected line.");
            var points = new List<double[]>();
            if (!ReadExpectedLine(reader, "outer loop"))
                throw new IOException("Unexpected line.");
            while (true)
            {
                var line = ReadLine(reader);
                double[] point;
                if (TryParseDoubleArray(VertexRegex, line, out point))
                {
                    points.Add(point);
                    continue;
                }

                string id, values;
                ParseLine(line, out id, out values);

                if (id == "endloop")
                {
                    break;
                }
            }
            if (!ReadExpectedLine(reader, "endfacet"))
                throw new IOException("Unexpected line.");
            Normals.Add(n);
            Vertices.Add(points);
        }

        #endregion

        #region STL Binary Reading Functions

        /// <summary>
        /// Tries the read binary.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="filename">The filename.</param>
        /// <param name="stlData">The STL data.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        /// <exception cref="System.IO.EndOfStreamException">Incomplete file</exception>
        /// <exception cref="EndOfStreamException">Incomplete file</exception>
        internal static bool TryReadBinary(Stream stream, string filename, out List<STLFileData> stlData)
        {
            var length = stream.Length;
            stlData = new List<STLFileData>();
            var stlSolid1 = new STLFileData();
            if (length < 84)
            {
                throw new EndOfStreamException("Incomplete file");
            }
            var decoder = new UTF8Encoding();

            var reader = new BinaryReader(stream);
            var comments = decoder.GetString(reader.ReadBytes(80), 0, 80).Trim(' ');
            stlSolid1.Comments.Add(comments);
            stlSolid1.Name = comments.Split(' ')[0];
            stlSolid1.Units = InferUnitsFromComments(stlSolid1.Comments);
            stlSolid1.FileName = filename;
            if (string.IsNullOrWhiteSpace(stlSolid1.Name))
                stlSolid1.Name = GetNameFromFileName(filename);
            do
            {
                var numFaces = ReadUInt32(reader);
                if (length - 84 != numFaces * 50)
                    return false;
                for (var i = 0; i < numFaces; i++)
                    stlSolid1.ReadFacet(reader);
                stlData.Add(stlSolid1);
                var last = stlSolid1;
                stlSolid1 = new STLFileData
                {
                    Name = last.Name,
                    Units = last.Units,
                    FileName = last.FileName
                };
                stlSolid1.Comments.Add(comments);
            } while (reader.BaseStream.Position < length);
            return true;
        }

        /// <summary>
        ///     Reads a triangle from a binary STL file.
        /// </summary>
        /// <param name="reader">The reader.</param>
        private void ReadFacet(BinaryReader reader)
        {
            var ni = ReadFloatToDouble(reader);
            var nj = ReadFloatToDouble(reader);
            var nk = ReadFloatToDouble(reader);

            var n = new[] { ni, nj, nk };

            var x1 = ReadFloatToDouble(reader);
            var y1 = ReadFloatToDouble(reader);
            var z1 = ReadFloatToDouble(reader);
            var v1 = new[] { x1, y1, z1 };

            var x2 = ReadFloatToDouble(reader);
            var y2 = ReadFloatToDouble(reader);
            var z2 = ReadFloatToDouble(reader);
            var v2 = new[] { x2, y2, z2 };

            var x3 = ReadFloatToDouble(reader);
            var y3 = ReadFloatToDouble(reader);
            var z3 = ReadFloatToDouble(reader);
            var v3 = new[] { x3, y3, z3 };

            var attrib = Convert.ToString(ReadUInt16(reader), 2).PadLeft(16, '0').ToCharArray();
            var hasColor = attrib[0].Equals('1');

            if (hasColor)
            {
                var blue = attrib[15].Equals('1') ? 1 : 0;
                blue = attrib[14].Equals('1') ? blue + 2 : blue;
                blue = attrib[13].Equals('1') ? blue + 4 : blue;
                blue = attrib[12].Equals('1') ? blue + 8 : blue;
                blue = attrib[11].Equals('1') ? blue + 16 : blue;
                var b = blue * 8;

                var green = attrib[10].Equals('1') ? 1 : 0;
                green = attrib[9].Equals('1') ? green + 2 : green;
                green = attrib[8].Equals('1') ? green + 4 : green;
                green = attrib[7].Equals('1') ? green + 8 : green;
                green = attrib[6].Equals('1') ? green + 16 : green;
                var g = green * 8;

                var red = attrib[5].Equals('1') ? 1 : 0;
                red = attrib[4].Equals('1') ? red + 2 : red;
                red = attrib[3].Equals('1') ? red + 4 : red;
                red = attrib[2].Equals('1') ? red + 8 : red;
                red = attrib[1].Equals('1') ? red + 16 : red;
                var r = red * 8;

                var currentColor = new Color(Convert.ToByte(r), Convert.ToByte(g), Convert.ToByte(b));
                HasColorSpecified = true;
                if (!_lastColor.Equals(currentColor))
                    _lastColor = currentColor;
            }
            Colors.Add(_lastColor);
            Normals.Add(n);
            Vertices.Add(new List<double[]> { v1, v2, v3 });
        }

        /// <summary>
        ///     Reads a float (4 byte)
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>The float.</returns>
        private static double ReadFloatToDouble(BinaryReader reader)
        {
            var bytes = reader.ReadBytes(4);
            return BitConverter.ToSingle(bytes, 0);
        }

        /// <summary>
        ///     Reads a 16-bit unsigned integer.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>The unsigned integer.</returns>
        private static ushort ReadUInt16(BinaryReader reader)
        {
            var bytes = reader.ReadBytes(2);
            return BitConverter.ToUInt16(bytes, 0);
        }

        /// <summary>
        ///     Reads a 32-bit unsigned integer.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>The unsigned integer.</returns>
        private static uint ReadUInt32(BinaryReader reader)
        {
            var bytes = reader.ReadBytes(4);
            return BitConverter.ToUInt32(bytes, 0);
        }

        #endregion

        #endregion

        #region Save

        /// <summary>
        ///     Saves the ASCII.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="solids">The solids.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        /// <exception cref="NotImplementedException"></exception>
        internal static bool SaveASCII(Stream stream, IList<TessellatedSolid> solids)
        {
            using (var writer = new StreamWriter(stream))
            {
                foreach (var solid in solids)
                    if (!SaveASCII(writer, solid)) return false;
                writer.WriteLine("#  " + tvglDateMarkText);
                if (!string.IsNullOrWhiteSpace(solids[0].FileName))
                    writer.WriteLine("#  Originally loaded from : " + solids[0].FileName);
                if (solids[0].Units != UnitType.unspecified)
                    writer.WriteLine("#  Units : " + solids[0].Units);
                if (!string.IsNullOrWhiteSpace(solids[0].Language))
                    writer.WriteLine("#  Lang : " + solids[0].Language);
                foreach (var comment in solids[0].Comments.Where(string.IsNullOrWhiteSpace))
                    writer.WriteLine("#  " + comment);
            }
            return true;
        }

        private static bool SaveASCII(StreamWriter writer, TessellatedSolid solid)
        {
            try
            {
                writer.WriteLine("solid " + solid.Name);
                foreach (var face in solid.Faces)
                {
                    writer.WriteLine("\tfacet normal " + face.Normal[0] + " " + face.Normal[1] + " " + face.Normal[2]);
                    writer.WriteLine("\t\touter loop");
                    writer.WriteLine("\t\t\tvertex " + face.Vertices[0].X + " " + face.Vertices[0].Y + " " +
                                     face.Vertices[0].Z);
                    writer.WriteLine("\t\t\tvertex " + face.Vertices[1].X + " " + face.Vertices[1].Y + " " +
                                     face.Vertices[1].Z);
                    writer.WriteLine("\t\t\tvertex " + face.Vertices[2].X + " " + face.Vertices[2].Y + " " +
                                     face.Vertices[2].Z);
                    writer.WriteLine("\t\tendloop");
                    writer.WriteLine("\tendfacet");
                }
                writer.WriteLine("endsolid " + solid.Name);
                Message.output("Successfully wrote STL file to stream.", 4);
                return true;
            }
            catch (Exception exception)
            {
                Message.output("Unable to write in model file.", 1);
                Message.output("Exception: " + exception.Message, 3);
                return false;
            }
        }

        /// <summary>
        ///     Saves the binary.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="solids">The solids.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        /// <exception cref="NotImplementedException"></exception>
        internal static bool SaveBinary(Stream stream, IList<TessellatedSolid> solids)
        {
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                var headerString = GetNameFromFileName(solids[0].FileName);
                if (string.IsNullOrWhiteSpace(headerString)) headerString = solids[0].Name;
                headerString += tvglDateMarkText;
                if (solids[0].Units != UnitType.unspecified)
                    headerString += solids[0].Units.ToString();
                foreach (var comment in solids[0].Comments.Where(string.IsNullOrWhiteSpace))
                    headerString += " " + comment;
                if (headerString.Length > 80) headerString = headerString.Substring(0, 80);
                var headerBytes = Encoding.UTF8.GetBytes(headerString);
                writer.Write(headerBytes);
                foreach (var solid in solids)
                {
                    var defaultColor = new Color(Constants.DefaultColor);
                    var defineColors = !(solid.HasUniformColor && defaultColor.Equals(solid.SolidColor));
                    writer.Write((uint)solid.NumberOfFaces);
                    foreach (var face in solid.Faces)
                        WriteFacet(writer, face, defineColors, defaultColor);
                }
            }
            return true;
        }


        private static void WriteFacet(BinaryWriter writer, PolygonalFace face, bool defineColors, Color defaultColor)
        {
            writer.Write(BitConverter.GetBytes((float)face.Normal[0]));
            writer.Write(BitConverter.GetBytes((float)face.Normal[1]));
            writer.Write(BitConverter.GetBytes((float)face.Normal[2]));
            writer.Write(BitConverter.GetBytes((float)face.Vertices[0].X));
            writer.Write(BitConverter.GetBytes((float)face.Vertices[0].Y));
            writer.Write(BitConverter.GetBytes((float)face.Vertices[0].Z));
            writer.Write(BitConverter.GetBytes((float)face.Vertices[1].X));
            writer.Write(BitConverter.GetBytes((float)face.Vertices[1].Y));
            writer.Write(BitConverter.GetBytes((float)face.Vertices[1].Z));
            writer.Write(BitConverter.GetBytes((float)face.Vertices[2].X));
            writer.Write(BitConverter.GetBytes((float)face.Vertices[2].Y));
            writer.Write(BitConverter.GetBytes((float)face.Vertices[2].Z));
            var colorBytes = (ushort)0;
            if (defineColors)
            {
                colorBytes += 32768;
                var red = (ushort)(face.Color.R / 8);
                colorBytes += red;
                var green = (ushort)(face.Color.G / 8);
                colorBytes += (ushort)(green * 32);
                var blue = (ushort)(face.Color.B / 8);
                colorBytes += (ushort)(blue * 1024);
            }
            writer.Write(BitConverter.GetBytes(colorBytes));
        }

        #endregion
    }
}

