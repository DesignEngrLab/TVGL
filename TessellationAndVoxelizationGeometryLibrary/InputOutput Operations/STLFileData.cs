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
        ///     Initializes a new instance of the <see cref="STLFileData" /> class.
        /// </summary>
        public STLFileData()
        {
            Normals = new List<double[]>();
            Vertices = new List<List<double[]>>();
            Colors = new List<Color>();
        }

        /// <summary>
        ///     Gets the has color specified.
        /// </summary>
        /// <value>The has color specified.</value>
        public bool HasColorSpecified { get; private set; }

        /// <summary>
        ///     Gets or sets the colors.
        /// </summary>
        /// <value>The colors.</value>
        public List<Color> Colors { get; set; }

        /// <summary>
        ///     Gets or sets the Vertices.
        /// </summary>
        /// <value>The vertices.</value>
        public List<List<double[]>> Vertices { get; set; }

        /// <summary>
        ///     Gets or sets the normals.
        /// </summary>
        /// <value>The normals.</value>
        public List<double[]> Normals { get; set; }

        /// <summary>
        ///     Gets the file header.
        /// </summary>
        /// <value>The header.</value>
        public string Name { get; private set; }

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

        /// <summary>
        ///     Saves the ASCII.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="solids">The solids.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        /// <exception cref="NotImplementedException"></exception>
        internal static bool SaveASCII(Stream stream, IList<TessellatedSolid> solids)
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

        /// <summary>
        ///     Saves the binary.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="solids">The solids.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        /// <exception cref="NotImplementedException"></exception>
        internal static bool SaveBinary(Stream stream, IList<TessellatedSolid> solids)
        {
            throw new NotImplementedException();
            /*using (BinaryWriter writer = new BinaryWriter(stream, Encoding.ASCII, true))
            {
                byte[] header = Encoding.ASCII.GetBytes("Binary STL generated by STLdotNET. QuantumConceptsCorp.com");
                byte[] headerFull = new byte[80];

                Buffer.BlockCopy(header, 0, headerFull, 0, Math.Min(header.Length, headerFull.Length));

                //Write the header and facet count.
                writer.Write(headerFull);
                writer.Write((UInt32)this.Facets.Count);

                //Write each facet.
                foreach (var o in Facets)
                    o.Write(writer);
            }*/
        }

        /// <summary>
        ///     Reads the model in ASCII format from the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="stlData">The STL data.</param>
        /// <returns>True if the model was loaded successfully.</returns>
        internal static bool TryReadAscii(Stream stream,string filename, out List<STLFileData> stlData)
        {
            var defaultName = filename + "_";
            var solidNum = 0;
            var reader = new StreamReader(stream);
            stlData = new List<STLFileData>();
            var stlSolid = new STLFileData();
            while (!reader.EndOfStream)
            {
                var line = ReadLine(reader);
                string id, values;
                ParseLine(line, out id, out values);
                switch (id)
                {
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
            return true;
        }


        /// <summary>
        ///     Tries the read binary.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="stlData">The STL data.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        /// <exception cref="EndOfStreamException">Incomplete file</exception>
        /// <exception cref="System.IO.EndOfStreamException">Incomplete file</exception>
        internal static bool TryReadBinary(Stream stream,string filename, out List<STLFileData> stlData)
        {
            var length = stream.Length;
            stlData = null;
            var stlSolid1 = new STLFileData();
            if (length < 84)
            {
                throw new EndOfStreamException("Incomplete file");
            }
            var decoder = new UTF8Encoding();

            var reader = new BinaryReader(stream);
            stlSolid1.Name = decoder.GetString(reader.ReadBytes(80), 0, 80).Trim(' ');
            stlSolid1.Name = stlSolid1.Name.Replace("solid", "").Trim(' ');
            if (string.IsNullOrWhiteSpace(stlSolid1.Name))
                stlSolid1.Name = filename;
            var numberTriangles = ReadUInt32(reader);

            if (length - 84 != numberTriangles * 50)
            {
                return false;
            }

            //this.Meshes.Add(new MeshBuilder(true, true));
            //this.Materials.Add(this.DefaultMaterial);

            for (var i = 0; i < numberTriangles; i++)
            {
                stlSolid1.ReadTriangle(reader);
            }
            stlData = new List<STLFileData>(new[] { stlSolid1 });
            return true;
        }

        #region STL Binary Reading Functions

        /// <summary>
        /// Opens the specified s.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <param name="filename">The filename.</param>
        /// <param name="inParallel">if set to <c>true</c> [in parallel].</param>
        /// <returns>List&lt;TessellatedSolid&gt;.</returns>
        internal new static List<TessellatedSolid> Open(Stream s, string filename, bool inParallel = true)
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
                results.Add(new TessellatedSolid(stlFileData.Name, stlFileData.Normals, stlFileData.Vertices,
                    stlFileData.HasColorSpecified ? stlFileData.Colors : null));
            Message.output(
                "Successfully read in " + typeString + " file called " + filename + " in " +
                (DateTime.Now - now).TotalSeconds + " seconds.", 4);
            return results;
        }

        /// <summary>
        ///     Reads a triangle from a binary STL file.
        /// </summary>
        /// <param name="reader">The reader.</param>
        private void ReadTriangle(BinaryReader reader)
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
    }
}