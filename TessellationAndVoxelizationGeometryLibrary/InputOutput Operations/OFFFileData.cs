// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 02-27-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 05-28-2016
// ***********************************************************************
// <copyright file="OFFFileData.cs" company="Design Engineering Lab">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using StarMathLib;

namespace TVGL.IOFunctions
{
    // http://en.wikipedia.org/wiki/OFF_(file_format)
    /// <summary>
    ///     Class OFFFileData.
    /// </summary>
    internal class OFFFileData : IO
    {
        /// <summary>
        ///     The last color
        /// </summary>
        private Color _lastColor;

        /// <summary>
        ///     Initializes a new instance of the <see cref="OFFFileData" /> class.
        /// </summary>
        public OFFFileData()
        {
            Vertices = new List<double[]>();
            FaceToVertexIndices = new List<int[]>();
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
        public List<Color> Colors { get; }

        /// <summary>
        ///     Gets or sets the Vertices.
        /// </summary>
        /// <value>The vertices.</value>
        public List<double[]> Vertices { get; }

        /// <summary>
        ///     Gets the face to vertex indices.
        /// </summary>
        /// <value>The face to vertex indices.</value>
        public List<int[]> FaceToVertexIndices { get; }

        /// <summary>
        ///     Gets the file header.
        /// </summary>
        /// <value>The header.</value>
        public string Name { get; private set; }

        /// <summary>
        ///     Gets the number vertices.
        /// </summary>
        /// <value>The number vertices.</value>
        public int NumVertices { get; private set; }

        /// <summary>
        ///     Gets the number faces.
        /// </summary>
        /// <value>The number faces.</value>
        public int NumFaces { get; private set; }

        /// <summary>
        ///     Gets the number edges.
        /// </summary>
        /// <value>The number edges.</value>
        public int NumEdges { get; private set; }

        /// <summary>
        ///     Gets the contains homogeneous coordinates.
        /// </summary>
        /// <value>The contains homogeneous coordinates.</value>
        public bool ContainsHomogeneousCoordinates { get; private set; }

        /// <summary>
        ///     Gets the contains texture coordinates.
        /// </summary>
        /// <value>The contains texture coordinates.</value>
        public bool ContainsTextureCoordinates { get; private set; }

        /// <summary>
        ///     Gets the contains colors.
        /// </summary>
        /// <value>The contains colors.</value>
        public bool ContainsColors { get; private set; }

        /// <summary>
        ///     Gets the contains normals.
        /// </summary>
        /// <value>The contains normals.</value>
        public bool ContainsNormals { get; private set; }


        /// <summary>
        ///     Opens the specified s.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <param name="inParallel">if set to <c>true</c> [in parallel].</param>
        /// <returns>List&lt;TessellatedSolid&gt;.</returns>
        internal static List<TessellatedSolid> Open(Stream s, bool inParallel = true)
        {
            var now = DateTime.Now;
            OFFFileData offData;
            // Try to read in BINARY format
            if (TryReadBinary(s, out offData))
                Message.output("Successfully read in binary OFF file (" + (DateTime.Now - now) + ").", 3);
            else
            {
                // Reset position of stream
                s.Position = 0;
                // Read in ASCII format
                if (TryReadAscii(s, out offData))
                    Message.output("Successfully read in ASCII OFF file (" + (DateTime.Now - now) + ").", 3);
                else
                {
                    Message.output("Unable to read in OFF file (" + (DateTime.Now - now) + ").", 1);
                    return null;
                }
            }
            return new List<TessellatedSolid>
            {
                new TessellatedSolid(offData.Name, offData.Vertices, offData.FaceToVertexIndices,
                    offData.HasColorSpecified ? offData.Colors : null)
            };
        }

        /// <summary>
        ///     Tries the read ASCII.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="offData">The off data.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        internal static bool TryReadAscii(Stream stream, out OFFFileData offData)
        {
            var reader = new StreamReader(stream);
            offData = new OFFFileData();
            var line = ReadLine(reader);
            if (!line.Contains("off") && !line.Contains("OFF"))
                return false;
            offData.ContainsNormals = line.Contains("N");
            offData.ContainsColors = line.Contains("C");
            offData.ContainsTextureCoordinates = line.Contains("ST");
            offData.ContainsHomogeneousCoordinates = line.Contains("4");

            double[] point;
            if (TryParseDoubleArray(ReadLine(reader), out point))
            {
                offData.NumVertices = (int) Math.Round(point[0], 0);
                offData.NumFaces = (int) Math.Round(point[1], 0);
                offData.NumEdges = (int) Math.Round(point[2], 0);
            }
            else return false;

            for (var i = 0; i < offData.NumVertices; i++)
            {
                line = ReadLine(reader);
                if (TryParseDoubleArray(line, out point))
                {
                    if (offData.ContainsHomogeneousCoordinates
                        && !point[3].IsNegligible())
                        offData.Vertices.Add(new[]
                        {
                            point[0]/point[3],
                            point[1]/point[3],
                            point[2]/point[3]
                        });
                    else offData.Vertices.Add(point);
                }
                else return false;
            }
            for (var i = 0; i < offData.NumFaces; i++)
            {
                line = ReadLine(reader);
                double[] numbers;
                if (!TryParseDoubleArray(line, out numbers)) return false;

                var numVerts = (int) Math.Round(numbers[0], 0);
                var vertIndices = new int[numVerts];
                for (var j = 0; j < numVerts; j++)
                    vertIndices[j] = (int) Math.Round(numbers[1 + j], 0);
                offData.FaceToVertexIndices.Add(vertIndices);

                if (numbers.GetLength(0) == 1 + numVerts + 3)
                {
                    var r = (float) numbers[1 + numVerts];
                    var g = (float) numbers[2 + numVerts];
                    var b = (float) numbers[3 + numVerts];
                    var currentColor = new Color(1f, r, g, b);
                    offData.HasColorSpecified = true;
                    if (offData._lastColor == null || !offData._lastColor.Equals(currentColor))
                        offData._lastColor = currentColor;
                }
                offData.Colors.Add(offData._lastColor);
            }
            offData.Name = getNameFromStream(stream);
            return true;
        }

        /// <summary>
        ///     Tries the read binary.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="offData">The off data.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        /// <exception cref="NotImplementedException"></exception>
        /// <exception cref="System.NotImplementedException"></exception>
        /// <exception cref="System.IO.EndOfStreamException">Incomplete file</exception>
        internal static bool TryReadBinary(Stream stream, out OFFFileData offData)
        {
            offData = null;
            return false;
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
    }
}