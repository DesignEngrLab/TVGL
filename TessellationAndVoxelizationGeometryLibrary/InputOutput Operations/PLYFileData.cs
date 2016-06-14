// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 02-27-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 05-28-2016
// ***********************************************************************
// <copyright file="PLYFileData.cs" company="Design Engineering Lab">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TVGL.IOFunctions
{
    // http://en.wikipedia.org/wiki/PLY_(file_format)
    /// <summary>
    ///     Class PLYFileData.
    /// </summary>
    internal class PLYFileData : IO
    {
        #region Constructor
        /// <summary>
        ///     Initializes a new instance of the <see cref="PLYFileData" /> class.
        /// </summary>
        private PLYFileData()
        {
            Vertices = new List<double[]>();
            FaceToVertexIndices = new List<int[]>();
            Colors = new List<Color>();
        }
        #endregion
        #region Properties and Fields
        /// <summary>
        ///     The last color
        /// </summary>
        private Color _lastColor;

        /// <summary>
        ///     The color descriptor
        /// </summary>
        private List<ColorElements> ColorDescriptor;

        /// <summary>
        ///     The color is float
        /// </summary>
        private bool ColorIsFloat;

        /// <summary>
        ///     The read in order
        /// </summary>
        private List<ShapeElement> ReadInOrder;

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
        #endregion
        #region Open Solids
        /// <summary>
        /// Opens the specified s.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <param name="filename">The filename.</param>
        /// <param name="inParallel">if set to <c>true</c> [in parallel].</param>
        /// <returns>List&lt;TessellatedSolid&gt;.</returns>
        internal static TessellatedSolid OpenSolid(Stream s, string filename)
        {
            var now = DateTime.Now;
            try
            {
                var reader = new StreamReader(s);
                var plyData = new PLYFileData { FileName = filename, Name = GetNameFromFileName(filename) };
                var line = ReadLine(reader);
                if (!line.Contains("ply") && !line.Contains("PLY"))
                    return null;
                plyData.ReadHeader(reader);
                foreach (var shapeElement in plyData.ReadInOrder)
                {
                    bool successful;
                    switch (shapeElement)
                    {
                        case ShapeElement.Vertex:
                            successful = plyData.ReadVertices(reader);
                            break;
                        case ShapeElement.Face:
                            successful = plyData.ReadFaces(reader);
                            break;
                        case ShapeElement.Edge:
                            successful = plyData.ReadEdges(reader);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    if (!successful) return null;
                }
                Message.output("Successfully read in ASCII PLY file (" + (DateTime.Now - now) + ").", 3);
                return new TessellatedSolid(plyData.Vertices, plyData.FaceToVertexIndices,
                    plyData.HasColorSpecified ? plyData.Colors : null,InferUnitsFromComments(plyData.Comments),
                  plyData.Name,plyData.FileName,plyData.Comments,plyData.Language);
            }
            catch
            {
                Message.output("Unable to read in PLY file (" + (DateTime.Now - now) + ").", 1);
                return null;
            }
        }
        /// <summary>
        ///     Reads the edges.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private bool ReadEdges(StreamReader reader)
        {
            for (var i = 0; i < NumEdges; i++)
                ReadLine(reader);
            // Nothing happens in this function. The way TVGL functions, edges are implicitly defined
            // from the faces and vertices. I suppose this is a deficiency in TVGL, but I do not necessarily
            // feel compelled to change it. What would be worth storing in the edges? thickness? color? curves?
            return true;
        }
        /// <summary>
        ///     Reads the faces.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private bool ReadFaces(StreamReader reader)
        {
            for (var i = 0; i < NumFaces; i++)
            {
                var line = ReadLine(reader);
                double[] numbers;
                if (!TryParseDoubleArray(line, out numbers)) return false;

                var numVerts = (int)Math.Round(numbers[0], 0);
                var vertIndices = new int[numVerts];
                for (var j = 0; j < numVerts; j++)
                    vertIndices[j] = (int)Math.Round(numbers[1 + j], 0);
                FaceToVertexIndices.Add(vertIndices);

                if (ColorDescriptor.Any())
                {
                    if (numbers.GetLength(0) >= 1 + numVerts + ColorDescriptor.Count)
                    {
                        float a = 0, r = 0, g = 0, b = 0;
                        for (var j = 0; j < ColorDescriptor.Count; j++)
                        {
                            var colorElements = ColorDescriptor[j];
                            var value = (float)numbers[1 + numVerts + j];
                            switch (colorElements)
                            {
                                case ColorElements.Red:
                                    r = ColorIsFloat ? value : value / 255f;
                                    break;
                                case ColorElements.Green:
                                    g = ColorIsFloat ? value : value / 255f;
                                    break;
                                case ColorElements.Blue:
                                    b = ColorIsFloat ? value : value / 255f;
                                    break;
                                case ColorElements.Opacity:
                                    a = ColorIsFloat ? value : value / 255f;
                                    break;
                            }
                        }
                        var currentColor = new Color(a, r, g, b);
                        HasColorSpecified = true;
                        if (_lastColor == null || !_lastColor.Equals(currentColor))
                            _lastColor = currentColor;
                    }
                    if (_lastColor != null) Colors.Add(_lastColor);
                }
            }
            return true;
        }

        /// <summary>
        ///     Reads the vertices.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private bool ReadVertices(StreamReader reader)
        {
            for (var i = 0; i < NumVertices; i++)
            {
                var line = ReadLine(reader);
                double[] point;
                if (TryParseDoubleArray(line, out point))
                    Vertices.Add(point);
                else return false;
            }
            return true;
        }

        /// <summary>
        ///     Reads the header.
        /// </summary>
        /// <param name="reader">The reader.</param>
        private void ReadHeader(StreamReader reader)
        {
            ReadInOrder = new List<ShapeElement>();
            ColorDescriptor = new List<ColorElements>();
            string line;
            do
            {
                line = ReadLine(reader);
                string id, values;
                ParseLine(line, out id, out values);
                if (id.Equals("comment")) Comments.Add(values);
                else if (id.Equals("element"))
                {
                    string numberString;
                    ParseLine(values, out id, out numberString);
                    int numberInt;
                    var successfulParse = int.TryParse(numberString, out numberInt);
                    if (!successfulParse) continue;
                    if (id.Equals("vertex"))
                    {
                        ReadInOrder.Add(ShapeElement.Vertex);
                        NumVertices = numberInt;
                    }
                    else if (id.Equals("face"))
                    {
                        ReadInOrder.Add(ShapeElement.Face);
                        NumFaces = numberInt;
                    }
                    else if (id.Equals("edge"))
                    {
                        ReadInOrder.Add(ShapeElement.Edge);
                        NumEdges = numberInt;
                    }
                }
                else if (id.Equals("property") && ReadInOrder.Last() == ShapeElement.Face)
                {
                    string typeString, restString;
                    ParseLine(values, out typeString, out restString);
                    ColorIsFloat = typeString.StartsWith("float", StringComparison.OrdinalIgnoreCase)
                                   || typeString.StartsWith("double", StringComparison.OrdinalIgnoreCase);
                    // doesn't seem like much point in checking this, it comes in many
                    // varieties like uint8 int32 vertex_indices, but it'll read in just fine
                    //if (typeString.Equals("list") && restString.Contains("uchar int vertex_index"))
                    //    expectingFaceToHaveListOfVertices = true;
                    if (restString.Equals("red", StringComparison.OrdinalIgnoreCase)
                        || restString.Equals("r", StringComparison.OrdinalIgnoreCase))
                        ColorDescriptor.Add(ColorElements.Red);
                    else if (restString.Equals("blue", StringComparison.OrdinalIgnoreCase)
                             || restString.Equals("b", StringComparison.OrdinalIgnoreCase))
                        ColorDescriptor.Add(ColorElements.Blue);
                    else if (restString.Equals("green", StringComparison.OrdinalIgnoreCase)
                             || restString.Equals("g", StringComparison.OrdinalIgnoreCase))
                        ColorDescriptor.Add(ColorElements.Green);
                    else if (restString.Equals("opacity", StringComparison.OrdinalIgnoreCase)
                             || restString.StartsWith("transp", StringComparison.OrdinalIgnoreCase)
                             || restString.Equals("a", StringComparison.OrdinalIgnoreCase))
                        ColorDescriptor.Add(ColorElements.Opacity);
                    else if (restString.Contains("red"))
                        ColorDescriptor.Add(ColorElements.Red);
                    else if (restString.Contains("blue"))
                        ColorDescriptor.Add(ColorElements.Blue);
                    else if (restString.Contains("green"))
                        ColorDescriptor.Add(ColorElements.Green);
                    else continue;
                }
            } while (!line.Equals("end_header"));
        }
        #endregion
        #region
        /// <summary>
        /// Saves the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="solid">The solid.</param>
        /// <returns>
        ///   <c>true</c> if XXXX, <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="NotImplementedException"></exception>
        internal static bool SaveSolid(Stream stream, TessellatedSolid solid)
        {
            var defineColors = !(solid.HasUniformColor && solid.SolidColor.Equals(new Color(Constants.DefaultColor)));
            var colorString = " " + solid.SolidColor.R + " " + solid.SolidColor.G + " " + solid.SolidColor.B + " " +
                                             solid.SolidColor.A;
            try
            {
                using (var writer = new StreamWriter(stream))
                {
                    writer.WriteLine("ply");
                    writer.WriteLine("format ascii 1.0");
                    writer.WriteLine("comment " + tvglDateMarkText);
                    writer.WriteLine("element vertex " + solid.NumberOfVertices);
                    writer.WriteLine("property double x");
                    writer.WriteLine("property double y");
                    writer.WriteLine("property double z");
                    writer.WriteLine("element face " + solid.NumberOfFaces);
                    writer.WriteLine("property list uint8 int32 vertex_indices");
                    if (defineColors)
                    {
                        writer.WriteLine("property uchar red");
                        writer.WriteLine("property uchar green");
                        writer.WriteLine("property uchar blue");
                        writer.WriteLine("property uchar opacity");
                    }
                    writer.WriteLine("end_header");

                    foreach (var vertex in solid.Vertices)
                        writer.WriteLine(vertex.X + " " + vertex.Y + " " + vertex.Z);
                    foreach (var face in solid.Faces)
                    {
                        var faceString = face.Vertices.Count.ToString();
                        foreach (var v in face.Vertices)
                            faceString += " " + v.IndexInList;
                        if (defineColors)
                        {
                            if (face.Color != null)
                                faceString += " " + face.Color.R + " " + face.Color.G + " " + face.Color.B + " " +
                                          face.Color.A;
                            else
                                faceString += colorString;
                        }
                        writer.WriteLine(faceString);
                    }
                }
                Message.output("Successfully wrote PLY file to stream.", 3);
                return true;
            }
            catch (Exception exception)
            {
                Message.output("Unable to write in model file.", 1);
                return false;
            }
        }
        #endregion
    }
}