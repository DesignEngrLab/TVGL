// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Matt Campbell
// Created          : 02-27-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 06-05-2014
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using StarMathLib;

namespace TVGL.IOFunctions
{
    // http://en.wikipedia.org/wiki/PLY_(file_format)
    internal class PLYFileData : IO
    {
        /// <summary>
        ///     The last color
        /// </summary>
        private Color _lastColor;

        public PLYFileData()
        {
            Vertices = new List<double[]>();
            FaceToVertexIndices = new List<int[]>();
            Colors = new List<Color>();
        }

        /// <summary>
        ///     Gets the has color specified.
        /// </summary>
        /// <value>The has color specified.</value>
        public Boolean HasColorSpecified { get; private set; }

        /// <summary>
        ///     Gets or sets the colors.
        /// </summary>
        /// <value>The colors.</value>
        public List<Color> Colors { get; private set; }

        /// <summary>
        ///     Gets or sets the Vertices.
        /// </summary>
        /// <value>The vertices.</value>
        public List<double[]> Vertices { get; private set; }

        /// <summary>
        ///     Gets the face to vertex indices.
        /// </summary>
        /// <value>The face to vertex indices.</value>
        public List<int[]> FaceToVertexIndices { get; private set; }

        /// <summary>
        ///     Gets the file header.
        /// </summary>
        /// <value>The header.</value>
        public string Name { get; private set; }

        public int NumVertices { get; private set; }
        public int NumFaces { get; private set; }
        public int NumEdges { get; private set; }
        public Boolean ContainsHomogeneousCoordinates { get; private set; }
        public Boolean ContainsTextureCoordinates { get; private set; }
        public Boolean ContainsColors { get; private set; }
        public Boolean ContainsNormals { get; private set; }


        internal static List<TessellatedSolid> Open(Stream s, bool inParallel = true)
        {
            var now = DateTime.Now;
            PLYFileData plyData;
            // Try to read in BINARY format
            if (PLYFileData.TryReadBinary(s, out plyData))
                Debug.WriteLine("Successfully read in binary PLY file (" + (DateTime.Now - now) + ").");
            else
            {
                // Reset position of stream
                s.Position = 0;
                // Read in ASCII format
                if (PLYFileData.TryReadAscii(s, out plyData))
                    Debug.WriteLine("Successfully read in ASCII PLY file (" + (DateTime.Now - now) + ").");
                else
                {
                    Debug.WriteLine("Unable to read in PLY file (" + (DateTime.Now - now) + ").");
                    return null;
                }
            }
            return new List<TessellatedSolid>
            {
                new TessellatedSolid(plyData.Name, plyData.Vertices, plyData.FaceToVertexIndices,
                    (plyData.HasColorSpecified ? plyData.Colors : null))
            };
        }
        internal static bool TryReadAscii(Stream stream, out PLYFileData plyData)
        {
            var reader = new StreamReader(stream);
            plyData = new PLYFileData();
            var line = ReadLine(reader);
            if (!line.Contains("ply") && !line.Contains("PLY"))
                return false;
            plyData.ContainsNormals = line.Contains("N");
            plyData.ContainsColors = line.Contains("C");
            plyData.ContainsTextureCoordinates = line.Contains("ST");
            plyData.ContainsHomogeneousCoordinates = line.Contains("4");

            double[] point;
            if (TryParseDoubleArray(ReadLine(reader), out point))
            {
                plyData.NumVertices = (int)Math.Round(point[0], 0);
                plyData.NumFaces = (int)Math.Round(point[1], 0);
                plyData.NumEdges = (int)Math.Round(point[2], 0);
            }
            else return false;

            for (var i = 0; i < plyData.NumVertices; i++)
            {
                line = ReadLine(reader);
                if (TryParseDoubleArray(line, out point))
                {
                    if (plyData.ContainsHomogeneousCoordinates
                        && !point[3].IsNegligible())
                        plyData.Vertices.Add(new[]
                        {
                            point[0]/point[3],
                            point[1]/point[3],
                            point[2]/point[3]
                        });
                    else plyData.Vertices.Add(point);
                }
                else return false;
            }
            for (var i = 0; i < plyData.NumFaces; i++)
            {
                line = ReadLine(reader);
                double[] numbers;
                if (!TryParseDoubleArray(line, out numbers)) return false;

                var numVerts = (int)Math.Round(numbers[0], 0);
                var vertIndices = new int[numVerts];
                for (var j = 0; j < numVerts; j++)
                    vertIndices[j]=(int)Math.Round(numbers[1 + j], 0);
                plyData.FaceToVertexIndices.Add(vertIndices);

                if (numbers.GetLength(0) == 1 + numVerts + 3)
                {
                    var r = (float)numbers[1 + numVerts];
                    var g = (float)numbers[2 + numVerts];
                    var b = (float)numbers[3 + numVerts];
                    var currentColor = new Color(1f, r, g, b);
                    plyData.HasColorSpecified = true;
                    if (plyData._lastColor == null || !plyData._lastColor.Equals(currentColor))
                        plyData._lastColor = currentColor;
                }
                plyData.Colors.Add(plyData._lastColor);
            }
            plyData.Name = getNameFromStream(stream);
            return true;
        }

        /// <summary>
        ///     Tries the read binary.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="plyData">The ply data.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        /// <exception cref="System.IO.EndOfStreamException">Incomplete file</exception>
        internal static bool TryReadBinary(Stream stream, out PLYFileData plyData)
        {
            plyData = null;
            return false;
            throw new NotImplementedException();
        }

        internal static bool Save(Stream stream, IList<TessellatedSolid> solids)
        {
            throw new NotImplementedException();
        }
    }
}