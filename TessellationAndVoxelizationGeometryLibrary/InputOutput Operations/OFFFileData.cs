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
using System.IO;
using StarMathLib;

namespace TVGL.IOFunctions
{
    // http://en.wikipedia.org/wiki/OFF_(file_format)
    internal class OFFFileData : IO
    {
        /// <summary>
        ///     The last color
        /// </summary>
        private Color _lastColor;

        public OFFFileData()
        {
            Vertices = new List<double[]>();
            FaceToVertexIndices = new List<List<int>>();
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
        public List<List<int>> FaceToVertexIndices { get; private set; }

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
                offData.NumVertices = (int)Math.Round(point[0], 0);
                offData.NumFaces = (int)Math.Round(point[1], 0);
                offData.NumEdges = (int)Math.Round(point[2], 0);
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

                var numVerts = (int)Math.Round(numbers[0], 0);
                var vertIndices = new List<int>();
                for (var j = 0; j < numVerts; j++)
                    vertIndices.Add((int)Math.Round(numbers[1 + j], 0));
                offData.FaceToVertexIndices.Add(vertIndices);

                if (numbers.GetLength(0) == 1 + numVerts + 3)
                {
                    var r = (float)numbers[1 + numVerts];
                    var g = (float)numbers[2 + numVerts];
                    var b = (float)numbers[3 + numVerts];
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
        /// <exception cref="System.NotImplementedException"></exception>
        /// <exception cref="System.IO.EndOfStreamException">Incomplete file</exception>
        internal static bool TryReadBinary(Stream stream, out OFFFileData offData)
        {
            offData = null;
            return false;
            throw new NotImplementedException();
        }
    }
}