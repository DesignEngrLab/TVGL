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
using amf;

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
                    tessellatedSolids = OpenSTL(s, inParallel); // Standard Tessellation or StereoLithography
                    break;
                //case "3ds": return IO.Open3DS(s);   //3D Studio
                //case "lwo": return IO.OpenLWO(s);  //Lightwave
                //case "obj": return IO.OpenOBJ(s); //Wavefront
                //case "objx": return IO.OpenOBJX(s);  //Wavefront
                case "amf":
                    tessellatedSolids = OpenAMF(s, inParallel);
                    break;
                case "off":
                    tessellatedSolids = OpenOFF(s, inParallel); // http://en.wikipedia.org/wiki/OFF_(file_format)
                    break;
                default:
                    throw new Exception(
                        "Cannot determine format from extension (not .stl, .3ds, .lwo, .obj, .objx, or .off.");
            }
            Debug.WriteLine("number of solids = " + tessellatedSolids.Count);
            foreach (var tessellatedSolid in tessellatedSolids)
            {
                Debug.WriteLine("number of vertices = " + tessellatedSolid.NumberOfVertices);
                Debug.WriteLine("number of edges = " + tessellatedSolid.NumberOfEdges);
                Debug.WriteLine("number of faces = " + tessellatedSolid.NumberOfFaces);
                Debug.WriteLine("Euler operator (should be a small +/- value) = " +
                                (tessellatedSolid.NumberOfVertices - tessellatedSolid.NumberOfEdges +
                                 tessellatedSolid.NumberOfFaces));
                Debug.WriteLine("Edges / Faces (should be 1.5) = " +
                                (tessellatedSolid.NumberOfEdges / (double)tessellatedSolid.NumberOfFaces));
            }
            
            return tessellatedSolids;
        }

        /// <summary>
        ///     Opens the STL.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <param name="inParallel">The in parallel.</param>
        /// <returns>TessellatedSolid.</returns>
        private static List<TessellatedSolid> OpenSTL(Stream s, bool inParallel = true)
        {
            var now = DateTime.Now;
            List<STLFileData> stlData;
            // Try to read in BINARY format
            if (STLFileData.TryReadBinary(s, out stlData))
                Debug.WriteLine("Successfully read in binary STL file (" + (DateTime.Now - now) + ").");
            else
            {
                // Reset position of stream
                s.Position = 0;
                // Read in ASCII format
                if (STLFileData.TryReadAscii(s, out stlData))
                    Debug.WriteLine("Successfully read in ASCII STL file (" + (DateTime.Now - now) + ").");
                else
                {
                    Debug.WriteLine("Unable to read in STL file (" + (DateTime.Now - now) + ").");
                    return null;
                }
            }
            var results = new List<TessellatedSolid>();
            foreach (var stlFileData in stlData)
                results.Add(new TessellatedSolid(stlFileData.Name, stlFileData.Normals, stlFileData.Vertices,
                     (stlFileData.HasColorSpecified ? stlFileData.Colors : null)));
            return results;
        }


        private static List<TessellatedSolid> OpenOFF(Stream s, bool inParallel = true)
        {
            var now = DateTime.Now;
            OFFFileData offData;
            // Try to read in BINARY format
            if (OFFFileData.TryReadBinary(s, out offData))
                Debug.WriteLine("Successfully read in binary OFF file (" + (DateTime.Now - now) + ").");
            else
            {
                // Reset position of stream
                s.Position = 0;
                // Read in ASCII format
                if (OFFFileData.TryReadAscii(s, out offData))
                    Debug.WriteLine("Successfully read in ASCII OFF file (" + (DateTime.Now - now) + ").");
                else
                {
                    Debug.WriteLine("Unable to read in OFF file (" + (DateTime.Now - now) + ").");
                    return null;
                }
            }
            return new List<TessellatedSolid>
            {
                new TessellatedSolid(offData.Name, offData.Vertices, offData.FaceToVertexIndices,
                    (offData.HasColorSpecified ? offData.Colors : null))
            };
        }

        private static List<TessellatedSolid> OpenAMF(Stream s, bool inParallel = true)
        {
            var now = DateTime.Now;
            AMFFileData amfData;
            // Try to read in BINARY format
            if (AMFFileData.TryUnzippedXMLRead(s, out amfData))
                Debug.WriteLine("Successfully read in AMF file (" + (DateTime.Now - now) + ").");
            else
            {
                // Reset position of stream
                s.Position = 0;
                // Read in ASCII format
                //if (amf.TryZippedXMLRead(s, out amfData))
                //    Debug.WriteLine("Successfully unzipped and read in ASCII OFF file (" + (DateTime.Now - now) + ").");
                //else
                //{
                //    Debug.WriteLine("Unable to read in AMF file (" + (DateTime.Now - now) + ").");
                //    return null;
                //}
            }
            var results = new List<TessellatedSolid>();
            foreach (var amfObject in amfData.Objects)
            {
                List<Color> colors = null;
                if (amfObject.mesh.volume.color != null)
                {
                    colors = new List<Color>();
                    var solidColor = new Color(amfObject.mesh.volume.color);
                    foreach (var amfTriangle in amfObject.mesh.volume.Triangles)
                        colors.Add((amfTriangle.color != null) ? new Color(amfTriangle.color) : solidColor);
                }
                else if (amfObject.mesh.volume.Triangles.Any(t => t.color != null))
                {
                    colors = new List<Color>();
                    var solidColor = new Color(Constants.DefaultColor);
                    foreach (var amfTriangle in amfObject.mesh.volume.Triangles)
                        colors.Add((amfTriangle.color != null) ? new Color(amfTriangle.color) : solidColor);
                }
                results.Add(new TessellatedSolid(amfData.Name,
                    amfObject.mesh.vertices.Vertices.Select(v => v.coordinates.AsArray).ToList(),
                    amfObject.mesh.volume.Triangles.Select(t => t.VertexIndices).ToList(),
                    colors));
            }
            return results;
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
            doubles = new double[match.Groups.Count-1];
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
        /// <param name="fileType">Type of the file.</param>
        public static void Save(Stream stream, FileType fileType)
        {
            /*
                 if (fileType == FileType.Text)
                 {
                     using (StreamWriter writer = new StreamWriter(stream, Encoding.ASCII))
                     {
                         //Write the header.
                         writer.WriteLine(this.ToString());

                         //Write each facet.
                         foreach (var o in Facets)
                             o.Write(writer);

                         //Write the footer.
                         writer.Write("end " + this.ToString());
                     }
                 }
                 else
                 {

                     using (BinaryWriter writer = new BinaryWriter(stream, Encoding.ASCII, true))
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
                     }
                 }
             * */
        }


        /// <summary>
        ///     Saves the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="fileType">Type of the file.</param>
        public static void Save(string path, FileType fileType)
        {
            //if (string.IsNullOrWhiteSpace(path))
            //    throw new ArgumentNullException("path");

            //Directory.CreateDirectory(Path.GetDirectoryName(path));

            //using (Stream stream = File.Create(path))
            //    Save(stream, fileType);
        }


        /// <summary>
        ///     Writes the coordinates.
        /// </summary>
        /// <param name="coordinates">The coordinates.</param>
        /// <param name="writer">The writer.</param>
        private static void WriteCoordinates(double[] coordinates, StreamWriter writer)
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