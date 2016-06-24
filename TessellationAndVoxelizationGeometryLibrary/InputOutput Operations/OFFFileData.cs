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
using System.Linq;
using StarMathLib;

namespace TVGL.IOFunctions
{
    // http://en.wikipedia.org/wiki/OFF_(file_format)
    /// <summary>
    ///     Class OFFFileData.
    /// </summary>
    internal class OFFFileData : IO
    {
        #region Constructor

        /// <summary>
        ///     Initializes a new instance of the <see cref="OFFFileData" /> class.
        /// </summary>
        private OFFFileData()
        {
            Vertices = new List<double[]>();
            FaceToVertexIndices = new List<int[]>();
            Colors = new List<Color>();
        }

        #endregion

        #region Fields and Properties

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
        private List<double[]> Vertices { get; }

        /// <summary>
        ///     Gets the face to vertex indices.
        /// </summary>
        /// <value>The face to vertex indices.</value>
        private List<int[]> FaceToVertexIndices { get; }

        /// <summary>
        ///     Gets the number vertices.
        /// </summary>
        /// <value>The number vertices.</value>
        private int NumVertices { get; set; }

        /// <summary>
        ///     Gets the number faces.
        /// </summary>
        /// <value>The number faces.</value>
        private int NumFaces { get; set; }

        /// <summary>
        ///     Gets the number edges.
        /// </summary>
        /// <value>The number edges.</value>
        private int NumEdges { get; set; }

        /// <summary>
        ///     Gets the contains homogeneous coordinates.
        /// </summary>
        /// <value>The contains homogeneous coordinates.</value>
        private bool ContainsHomogeneousCoordinates { get; set; }

        /// <summary>
        ///     Gets the contains texture coordinates.
        /// </summary>
        /// <value>The contains texture coordinates.</value>
        private bool ContainsTextureCoordinates { get; set; }

        /// <summary>
        ///     Gets the contains colors.
        /// </summary>
        /// <value>The contains colors.</value>
        private bool ContainsColors { get; set; }

        /// <summary>
        ///     Gets the contains normals.
        /// </summary>
        /// <value>The contains normals.</value>
        private bool ContainsNormals { get; set; }

        #endregion

        #region Open Solid

        /// <summary>
        /// Opens the specified s.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <param name="filename">The filename.</param>
        /// <returns>
        /// List&lt;TessellatedSolid&gt;.
        /// </returns>
        internal static TessellatedSolid OpenSolid(Stream s, string filename)
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
            return new TessellatedSolid(offData.Vertices, offData.FaceToVertexIndices,
                offData.HasColorSpecified ? offData.Colors : null,
                InferUnitsFromComments(offData.Comments), GetNameFromFileName(filename), filename, offData.Comments,
                offData.Language);
        }

        /// <summary>
        ///     Tries the read ASCII.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="offData">The off data.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private static bool TryReadAscii(Stream stream, out OFFFileData offData)
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
            line = ReadLine(reader);
            while (line.StartsWith("#"))
            {
                line.Remove(0, 1);
                if (!string.IsNullOrWhiteSpace(line))
                    offData.Comments.Add(line.Substring(1));
                line = ReadLine(reader);
            }
            if (TryParseDoubleArray(line, out point))
            {
                offData.NumVertices = (int)Math.Round(point[0], 0);
                offData.NumFaces = (int)Math.Round(point[1], 0);
                offData.NumEdges = (int)Math.Round(point[2], 0);
            }
            else return false;

            for (var i = 0; i < offData.NumVertices; i++)
            {
                line = ReadLine(reader);
                while (line.StartsWith("#"))
                {
                    line.Remove(0, 1);
                    if (!string.IsNullOrWhiteSpace(line))
                        offData.Comments.Add(line.Substring(1));
                    line = ReadLine(reader);
                }
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
                while (line.StartsWith("#"))
                {
                    line.Remove(0, 1);
                    if (!string.IsNullOrWhiteSpace(line))
                        offData.Comments.Add(line.Substring(1));
                    line = ReadLine(reader);
                }
                double[] numbers;
                if (!TryParseDoubleArray(line, out numbers)) return false;

                var numVerts = (int)Math.Round(numbers[0], 0);
                var vertIndices = new int[numVerts];
                for (var j = 0; j < numVerts; j++)
                    vertIndices[j] = (int)Math.Round(numbers[1 + j], 0);
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
        private static bool TryReadBinary(Stream stream, out OFFFileData offData)
        {
            offData = null;
            return false;
            throw new NotImplementedException();
        }

        #endregion

        #region Save Solid

        /// <summary>
        /// Saves the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="solid">The solid.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        /// <exception cref="NotImplementedException"></exception>
        internal static bool SaveSolid(Stream stream, TessellatedSolid solid)
        {
            try
            {
                var colorsDefined =
                    !(solid.HasUniformColor && solid.SolidColor.Equals(new Color(Constants.DefaultColor)));
                var colorString = solid.SolidColor.Rf + " " + solid.SolidColor.Gf + " " + solid.SolidColor.Bf;
                var writer = new StreamWriter(stream);
                if (colorsDefined)
                    writer.WriteLine("off C");
                else
                    writer.WriteLine("off");
                writer.WriteLine("#  " + tvglDateMarkText);
                if (!string.IsNullOrWhiteSpace(solid.Name))
                    writer.WriteLine("#  Name : " + solid.Name);
                if (!string.IsNullOrWhiteSpace(solid.FileName))
                    writer.WriteLine("#  Originally loaded from : " + solid.FileName);
                if (solid.Units != UnitType.unspecified)
                    writer.WriteLine("#  Units : " + solid.Units);
                if (!string.IsNullOrWhiteSpace(solid.Language))
                    writer.WriteLine("#  Lang : " + solid.Language);
                if (solid.Comments != null)
                    foreach (var comment in solid.Comments.Where(string.IsNullOrWhiteSpace))
                        writer.WriteLine("#  " + comment);
                writer.WriteLine(solid.NumberOfVertices + " " + solid.NumberOfFaces + " " + solid.NumberOfEdges);
                writer.WriteLine();
                foreach (var v in solid.Vertices)
                    writer.WriteLine(v.X + " " + v.Y + " " + v.Z);
                writer.WriteLine();
                foreach (var face in solid.Faces)
                {
                    var faceString = face.Vertices.Count.ToString();
                    foreach (var v in face.Vertices)
                        faceString += " " + v.IndexInList;
                    if (colorsDefined)
                    {
                        if (face.Color != null)
                            faceString += " " + face.Color.R + " " + face.Color.G + " " + face.Color.B + " " +
                                          face.Color.A;
                        else
                            faceString += colorString;
                    }
                    writer.WriteLine(faceString);
                }
                Message.output("Successfully wrote OFF file to stream.", 3);
                return true;
            }
            catch (Exception exception)
            {
                Message.output("Unable to write in model file.", 1);
                Message.output("Exception: " + exception.Message, 3);
                return false;
            }
        }

        #endregion
    }
}