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
using System.Reflection;

namespace TVGL.IOFunctions
{
    // http://en.wikipedia.org/wiki/PLY_(file_format)
    /// <summary>
    ///     Class PLYFileData.
    /// </summary>
    internal class PLYFileData : IO
    {
        #region Properties and Fields
        #region Color Related
        private bool hasColorSpecified;

        private List<ColorElements> uniformColorDescriptor;
        private List<Type> uniformColorElementType;
        private Color uniformColor;

        private List<ColorElements> faceColorDescriptor;
        private List<Type> faceColorElementType;
        private List<Color> faceColors;

        private List<ColorElements> vertexColorDescriptor;
        private List<Type> vertexColorElementType;
        private List<Color> vertexColors;
        #endregion

        /// <summary>
        ///     The read in order
        /// </summary>
        private List<ShapeElement> readInOrder;

        /// <summary>
        ///     Gets or sets the Vertices.
        /// </summary>
        /// <value>The vertices.</value>
        private List<double[]> vertices;

        /// <summary>
        ///     Gets the face to vertex indices.
        /// </summary>
        /// <value>The face to vertex indices.</value>
        private List<int[]> faceToVertexIndices;

        private List<int> vertexCoordinateOrder;
        private List<Type> vertexTypes;
        private Type vertexAmountType;
        private Type vertexIndexType;

        /// <summary>
        ///     Gets the number vertices.
        /// </summary>
        /// <value>The number vertices.</value>
        private int numVertices;

        /// <summary>
        ///     Gets the number faces.
        /// </summary>
        /// <value>The number faces.</value>
        private int numFaces;

        /// <summary>
        ///     Gets the number edges.
        /// </summary>
        /// <value>The number edges.</value>
        private int numEdges;

        private FormatEndiannessType endiannessType;

        #endregion
        #region Open Solids

        /// <summary>
        /// Opens the solid.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="name">The name.</param>
        /// <returns>TessellatedSolid.</returns>
        internal static TessellatedSolid OpenSolid(string data, string name = "")
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(data);
            writer.Flush();
            stream.Position = 0;
            return OpenSolid(stream, name);
        }

        /// <summary>
        /// Opens the specified s.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <param name="filename">The filename.</param>
        /// <returns>List&lt;TessellatedSolid&gt;.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        internal static TessellatedSolid OpenSolid(Stream s, string filename)
        {
            var now = DateTime.Now;
            var reader = new StreamReader(s);
            var fileTypeString = "ASCII";
            var plyData = new PLYFileData { FileName = filename, Name = GetNameFromFileName(filename) };
            var line = reader.ReadLine();
            if (!line.Contains("ply") && !line.Contains("PLY"))
                return null;
            var charPos = line.Length + 1;
            charPos += plyData.ReadHeader(reader);
            if (plyData.endiannessType == FormatEndiannessType.ascii)
                plyData.ReadMesh(reader);
            else
            {
                fileTypeString = "binary";
                var binaryReader = new BinaryReader(s);
                binaryReader.BaseStream.Seek(charPos, SeekOrigin.Begin);
                plyData.ReadMesh(binaryReader);
            }
            plyData.FixColors();
            Message.output("Successfully read in " + fileTypeString + " PLY file (" + (DateTime.Now - now) + ").", 3);
            return new TessellatedSolid(plyData.vertices, plyData.faceToVertexIndices, plyData.faceColors,
                InferUnitsFromComments(plyData.Comments), plyData.Name, filename, plyData.Comments, plyData.Language);
        }

        private void FixColors()
        {
            if (faceColors == null)
                faceColors = new List<Color>();
            if (!faceColors.Any() && uniformColor != null)
                faceColors.Add(uniformColor);
            else if (vertexColors != null)
            {
                for (int i = 0; i < numFaces; i++)
                {
                    if (faceColors.Count == i) faceColors.Add(null);
                    if (faceColors[i] != null) continue;
                    float a = 0, r = 0, g = 0, b = 0;
                    foreach (var vertIndex in faceToVertexIndices[i])
                    {
                        a += vertexColors[vertIndex].Af;
                        r += vertexColors[vertIndex].Rf;
                        g += vertexColors[vertIndex].Gf;
                        b += vertexColors[vertIndex].Bf;
                    }
                    faceColors[i] = new Color(a / 4f, r / 4f, g / 4f, b / 4f);
                }
            }
            else
                for (int i = 0; i < numFaces; i++)
                {
                    if (faceColors.Count == i) faceColors.Add(null);
                    if (faceColors[i] == null) faceColors[i] = uniformColor;
                }
        }

        /// <summary>
        ///     Reads the header.
        /// </summary>
        /// <param name="reader">The reader.</param>
        private int ReadHeader(StreamReader reader)
        {
            readInOrder = new List<ShapeElement>();
            uniformColorDescriptor = new List<ColorElements>();
            uniformColorElementType = new List<Type>();
            vertexTypes = new List<Type>();
            vertexCoordinateOrder = new List<int>();
            var position = 0;
            string line;
            do
            {
                do
                {
                    line = reader.ReadLine();
                    position += line.Length + 1;
                    if (reader.EndOfStream) break;
                } while (string.IsNullOrWhiteSpace(line));
                line = line.Trim();
                string id, values;
                ParseLine(line, out id, out values);
                if (id.Equals("comment"))
                    Comments.Add(values);
                else if (id.Equals("format"))
                    Enum.TryParse(values.Split(' ')[0], true, out endiannessType);
                else if (id.Equals("element"))
                {
                    string numberString;
                    ParseLine(values, out id, out numberString);
                    int numberInt;
                    var successfulParse = int.TryParse(numberString, out numberInt);
                    if (id.Equals("vertex"))
                    {
                        readInOrder.Add(ShapeElement.Vertex);
                        if (numberInt == 0) throw new ArgumentException("Zero or unknown number of vertices in PLY file.");
                        numVertices = numberInt;
                    }
                    else if (id.Equals("face"))
                    {
                        readInOrder.Add(ShapeElement.Face);
                        if (numberInt == 0) throw new ArgumentException("Zero or unknown number of faces in PLY file.");
                        numFaces = numberInt;
                    }
                    else if (id.Equals("edge"))
                    {
                        readInOrder.Add(ShapeElement.Edge);
                        numEdges = numberInt;
                    }
                    else if (id.Equals("uniform_color"))
                    {
                        readInOrder.Add(ShapeElement.Uniform_Color);
                    }
                }
                else if (id.Equals("property"))
                {
                    var shapeElement = readInOrder.Last();
                    switch (shapeElement)
                    {
                        #region Vertex
                        case ShapeElement.Vertex:
                            {
                                string typeString, propertyString;
                                Type type;
                                ParseLine(values, out typeString, out propertyString);
                                if (!TryParseNumberTypeFromString(typeString, out type))
                                    throw new ArgumentException("Unable to parse " + typeString + " as a type of number");
                                vertexTypes.Add(type);

                                if (propertyString.StartsWith("x", StringComparison.CurrentCultureIgnoreCase))
                                    vertexCoordinateOrder.Add(0);
                                else if (propertyString.StartsWith("y", StringComparison.CurrentCultureIgnoreCase))
                                    vertexCoordinateOrder.Add(1);
                                else if (propertyString.StartsWith("z", StringComparison.CurrentCultureIgnoreCase))
                                    vertexCoordinateOrder.Add(2);
                                else
                                {
                                    vertexCoordinateOrder.Add(-1);
                                    ColorElements colorElt;
                                    if (propertyString.Equals("red", StringComparison.CurrentCultureIgnoreCase)
                                        || propertyString.Equals("r", StringComparison.CurrentCultureIgnoreCase))
                                        colorElt = ColorElements.Red;
                                    else if (propertyString.Equals("blue", StringComparison.CurrentCultureIgnoreCase)
                                             || propertyString.Equals("b", StringComparison.CurrentCultureIgnoreCase))
                                        colorElt = ColorElements.Blue;
                                    else if (propertyString.Equals("green", StringComparison.CurrentCultureIgnoreCase)
                                             || propertyString.Equals("g", StringComparison.CurrentCultureIgnoreCase))
                                        colorElt = ColorElements.Green;
                                    else if (propertyString.Equals("opacity", StringComparison.CurrentCultureIgnoreCase)
                                             || propertyString.StartsWith("transp", StringComparison.CurrentCultureIgnoreCase)
                                             || propertyString.StartsWith("alph", StringComparison.CurrentCultureIgnoreCase)
                                             || propertyString.Equals("a", StringComparison.CurrentCultureIgnoreCase))
                                        colorElt = ColorElements.Opacity;
                                    else if (propertyString.Contains("red"))
                                        colorElt = ColorElements.Red;
                                    else if (propertyString.Contains("blue"))
                                        colorElt = ColorElements.Blue;
                                    else if (propertyString.Contains("green"))
                                        colorElt = ColorElements.Green;
                                    else continue;
                                    // the continue ensures that the following line will only be processed if it the property
                                    // was identified as a color
                                    if (vertexColorDescriptor == null)
                                    {
                                        vertexColorDescriptor = new List<ColorElements>();
                                        vertexColors = new List<Color>();
                                        vertexColorElementType = new List<Type>();
                                    }
                                    vertexColorDescriptor.Add(colorElt);
                                    Type colorType;
                                    if (TryParseNumberTypeFromString(typeString, out colorType))
                                        vertexColorElementType.Add(colorType);

                                }
                                break;
                            }
                        #endregion

                        #region Face
                        case ShapeElement.Face:
                            {
                                string typeString, restString;
                                ParseLine(values, out typeString, out restString);
                                if (typeString.Equals("list"))
                                {
                                    if (!restString.Contains("vertex_index") && !restString.Contains("vertex_indices"))
                                        throw new ArgumentException("The faces in PLY are specified in unknown manner: " +
                                                                    restString);
                                    var words = restString.Split(' ');
                                    Type type;
                                    if (TryParseNumberTypeFromString(words[0], out type))
                                        vertexAmountType = type;
                                    else
                                        throw new ArgumentException("The number of vertex in the PLY face definition are of unknown type: "
                                                                    + words[0]);
                                    if (TryParseNumberTypeFromString(words[1], out type))
                                        vertexIndexType = type;
                                    else
                                        throw new ArgumentException("The vertex indices in the PLY face definition are of unknown type: "
                                                                    + words[1]);
                                    continue;
                                }
                                ColorElements colorElt;
                                if (restString.Equals("red", StringComparison.CurrentCultureIgnoreCase)
                                    || restString.Equals("r", StringComparison.CurrentCultureIgnoreCase))
                                    colorElt = ColorElements.Red;
                                else if (restString.Equals("blue", StringComparison.CurrentCultureIgnoreCase)
                                         || restString.Equals("b", StringComparison.CurrentCultureIgnoreCase))
                                    colorElt = ColorElements.Blue;
                                else if (restString.Equals("green", StringComparison.CurrentCultureIgnoreCase)
                                         || restString.Equals("g", StringComparison.CurrentCultureIgnoreCase))
                                    colorElt = ColorElements.Green;
                                else if (restString.Equals("opacity", StringComparison.CurrentCultureIgnoreCase)
                                         || restString.StartsWith("transp", StringComparison.CurrentCultureIgnoreCase)
                                         || restString.StartsWith("alph", StringComparison.CurrentCultureIgnoreCase)
                                         || restString.Equals("a", StringComparison.CurrentCultureIgnoreCase))
                                    colorElt = ColorElements.Opacity;
                                else if (restString.Contains("red"))
                                    colorElt = ColorElements.Red;
                                else if (restString.Contains("blue"))
                                    colorElt = ColorElements.Blue;
                                else if (restString.Contains("green"))
                                    colorElt = ColorElements.Green;
                                else continue;
                                // the continue ensures that the following line will only be processed if it the property
                                // was identified as a color
                                if (faceColorDescriptor == null)
                                {
                                    faceColorDescriptor = new List<ColorElements>();
                                    faceColorElementType = new List<Type>();
                                    faceColors = new List<Color>();
                                }
                                faceColorDescriptor.Add(colorElt);
                                Type colorType;
                                if (TryParseNumberTypeFromString(typeString, out colorType))
                                    faceColorElementType.Add(colorType);
                                break;
                            }
                        #endregion

                        #region Uniform_Color
                        case ShapeElement.Uniform_Color:
                            {
                                string typeString, restString;
                                ColorElements colorElt;
                                ParseLine(values, out typeString, out restString);
                                if (restString.Equals("red", StringComparison.CurrentCultureIgnoreCase)
                                    || restString.Equals("r", StringComparison.CurrentCultureIgnoreCase))
                                    colorElt = ColorElements.Red;
                                else if (restString.Equals("blue", StringComparison.CurrentCultureIgnoreCase)
                                         || restString.Equals("b", StringComparison.CurrentCultureIgnoreCase))
                                    colorElt = ColorElements.Blue;
                                else if (restString.Equals("green", StringComparison.CurrentCultureIgnoreCase)
                                         || restString.Equals("g", StringComparison.CurrentCultureIgnoreCase))
                                    colorElt = ColorElements.Green;
                                else if (restString.Equals("opacity", StringComparison.CurrentCultureIgnoreCase)
                                         || restString.StartsWith("transp", StringComparison.CurrentCultureIgnoreCase)
                                             || restString.StartsWith("alph", StringComparison.CurrentCultureIgnoreCase)
                                         || restString.Equals("a", StringComparison.CurrentCultureIgnoreCase))
                                    colorElt = ColorElements.Opacity;
                                else if (restString.Contains("red"))
                                    colorElt = ColorElements.Red;
                                else if (restString.Contains("blue"))
                                    colorElt = ColorElements.Blue;
                                else if (restString.Contains("green"))
                                    colorElt = ColorElements.Green;
                                else continue;
                                // the continue ensures that the following line will only be processed if it the property
                                // was identified as a color
                                if (uniformColorDescriptor == null)
                                {
                                    uniformColorDescriptor = new List<ColorElements>();
                                    uniformColorElementType = new List<Type>();
                                }
                                uniformColorDescriptor.Add(colorElt);
                                Type colorType;
                                if (TryParseNumberTypeFromString(typeString, out colorType))
                                    uniformColorElementType.Add(colorType);
                                break;
                            }

                        #endregion

                        case ShapeElement.Edge:
                            Message.output("Unprocessed properties for edge elements: " + values);
                            break;
                    }
                }
            } while (!line.Equals("end_header"));
            return position;
        }

        private void ReadMesh(StreamReader reader)
        {
            foreach (var shapeElement in readInOrder)
            {
                bool successful;
                switch (shapeElement)
                {
                    case ShapeElement.Vertex:
                        successful = ReadVertices(reader);
                        break;
                    case ShapeElement.Face:
                        successful = ReadFaces(reader);
                        break;
                    case ShapeElement.Uniform_Color:
                        successful = ReadUniformColor(reader);
                        break;
                    case ShapeElement.Edge:
                        successful = ReadEdges(reader);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                if (!successful) throw new ArgumentOutOfRangeException("Unable to read PLY mesh. Error in " + shapeElement);
            }
        }
        private void ReadMesh(BinaryReader reader)
        {
            foreach (var shapeElement in readInOrder)
            {
                bool successful;
                switch (shapeElement)
                {
                    case ShapeElement.Vertex:
                        successful = ReadVertices(reader);
                        break;
                    case ShapeElement.Face:
                        successful = ReadFaces(reader);
                        break;
                    case ShapeElement.Uniform_Color:
                        successful = ReadUniformColor(reader);
                        break;
                    case ShapeElement.Edge:
                        successful = ReadEdges(reader);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                if (!successful) Message.output("Error found in reading PLY mesh. Error in " + shapeElement);
            }
        }

        private bool ReadUniformColor(StreamReader reader)
        {
            var line = ReadLine(reader);
            var words = line.Split(' ');
            float a = 0, r = 0, g = 0, b = 0;
            for (var j = 0; j < uniformColorDescriptor.Count; j++)
            {
                var value = readNumberAsFloat(words[j], uniformColorElementType[j]);
                if (float.IsNaN(value)) return false;
                if (uniformColorElementType[j] != typeof(float) && uniformColorElementType[j] != typeof(double))
                    value = value / 255f;
                switch (uniformColorDescriptor[j])
                {
                    case ColorElements.Red: r = value; break;
                    case ColorElements.Green: g = value; break;
                    case ColorElements.Blue: b = value; break;
                    case ColorElements.Opacity: a = value; break;
                }
            }
            uniformColor = new Color(a, r, g, b);
            return true;
        }


        /// <summary>
        ///     Reads the edges.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private bool ReadEdges(StreamReader reader)
        {
            for (var i = 0; i < numEdges; i++)
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
            faceToVertexIndices = new List<int[]>();
            for (var i = 0; i < numFaces; i++)
            {
                var line = ReadLine(reader);
                var words = line.Split(' ');
                var numVerts = readNumberAsInt(words[0], vertexAmountType);
                var vertIndices = new int[numVerts];
                for (var j = 0; j < numVerts; j++)
                    vertIndices[j] = readNumberAsInt(words[1 + j], vertexIndexType);
                faceToVertexIndices.Add(vertIndices);

                if (faceColorDescriptor != null)
                {
                    if (words.Length >= 1 + numVerts + faceColorDescriptor.Count)
                    {
                        float a = 0, r = 0, g = 0, b = 0;
                        for (var j = 0; j < faceColorDescriptor.Count; j++)
                        {
                            var value = readNumberAsFloat(words[1 + numVerts + j], faceColorElementType[j]);
                            if (faceColorElementType[j] != typeof(float) && faceColorElementType[j] != typeof(double))
                                value = value / 255f;
                            switch (faceColorDescriptor[j])
                            {
                                case ColorElements.Red:
                                    r = value;
                                    break;
                                case ColorElements.Green:
                                    g = value;
                                    break;
                                case ColorElements.Blue:
                                    b = value;
                                    break;
                                case ColorElements.Opacity:
                                    a = value;
                                    break;
                            }
                        }
                        faceColors.Add(new Color(a, r, g, b));
                    }
                    else faceColors.Add(null);
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
            float a = 0, r = 0, g = 0, b = 0;
            vertices = new List<double[]>();
            var numD = vertexTypes.Count;
            for (var i = 0; i < numVertices; i++)
            {
                var line = ReadLine(reader);
                var words = line.Split(' ');
                var point = new double[numD];
                var colorIndexer = 0;
                for (int j = 0; j < numD; j++)
                {
                    if (vertexCoordinateOrder[j] >= 0)
                        point[vertexCoordinateOrder[j]] = readNumberAsDouble(words[j], vertexTypes[j]);
                    else if (vertexColorDescriptor != null)
                    {
                        var value = readNumberAsFloat(words[j], vertexColorElementType[colorIndexer]);
                        if (vertexColorElementType[colorIndexer] != typeof(float)
                            && vertexColorElementType[colorIndexer] != typeof(double))
                            value = value / 255f;
                        switch (vertexColorDescriptor[colorIndexer])
                        {
                            case ColorElements.Red:
                                r = value;
                                break;
                            case ColorElements.Green:
                                g = value;
                                break;
                            case ColorElements.Blue:
                                b = value;
                                break;
                            case ColorElements.Opacity:
                                a = value;
                                break;
                        }
                        vertexColors.Add(new Color(a, r, g, b));
                        colorIndexer++;
                    }
                }
                if (point.Any(double.IsNaN)) return false;
                vertices.Add(point);
            }
            return true;
        }

        /// <summary>
        ///     Reads the edges.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private bool ReadEdges(BinaryReader reader)
        {
            throw new NotImplementedException();
            /*
                         for (var i = 0; i < NumEdges; i++)
                            ReadLine(reader);
                        // Nothing happens in this function. The way TVGL functions, edges are implicitly defined
                        // from the faces and vertices. I suppose this is a deficiency in TVGL, but I do not necessarily
                        // feel compelled to change it. What would be worth storing in the edges? thickness? color? curves?
                       */
        }



        private bool ReadUniformColor(BinaryReader reader)
        {
            float a = 0, r = 0, g = 0, b = 0;
            for (var j = 0; j < uniformColorDescriptor.Count; j++)
            {
                var value = readNumberAsFloat(reader, uniformColorElementType[j], endiannessType);
                if (float.IsNaN(value)) return false;
                if (uniformColorElementType[j] != typeof(float) && uniformColorElementType[j] != typeof(double))
                    value = value / 255f;
                switch (uniformColorDescriptor[j])
                {
                    case ColorElements.Red: r = value; break;
                    case ColorElements.Green: g = value; break;
                    case ColorElements.Blue: b = value; break;
                    case ColorElements.Opacity: a = value; break;
                }
            }
            uniformColor = new Color(a, r, g, b);
            return true;
        }

        /// <summary>
        ///     Reads the faces.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private bool ReadFaces(BinaryReader reader)
        {
            faceToVertexIndices = new List<int[]>();
            for (var i = 0; i < numFaces; i++)
            {
                var numVerts = readNumberAsInt(reader, vertexAmountType, endiannessType);
                var vertIndices = new int[numVerts];
                for (var j = 0; j < numVerts; j++)
                    vertIndices[j] = readNumberAsInt(reader, vertexIndexType, endiannessType);
                faceToVertexIndices.Add(vertIndices);

                if (faceColorDescriptor != null)
                {
                    float a = 0, r = 0, g = 0, b = 0;
                    for (var j = 0; j < faceColorDescriptor.Count; j++)
                    {
                        var value = readNumberAsFloat(reader, faceColorElementType[j], endiannessType);
                        if (faceColorElementType[j] != typeof(float) && faceColorElementType[j] != typeof(double))
                            value = value / 255f;
                        switch (faceColorDescriptor[j])
                        {
                            case ColorElements.Red: r = value; break;
                            case ColorElements.Green: g = value; break;
                            case ColorElements.Blue: b = value; break;
                            case ColorElements.Opacity: a = value; break;
                        }
                    }
                    faceColors.Add(new Color(a, r, g, b));
                }
            }
            return true;
        }
        /// <summary>
        ///     Reads the vertices.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private bool ReadVertices(BinaryReader reader)
        {
            float a = 0, r = 0, g = 0, b = 0;
            vertices = new List<double[]>();
            var numD = vertexTypes.Count;
            for (var i = 0; i < numVertices; i++)
            {
                var point = new double[numD];
                var colorIndexer = 0;
                for (int j = 0; j < numD; j++)
                {
                    if (vertexCoordinateOrder[j] >= 0)
                        point[vertexCoordinateOrder[j]] = readNumberAsDouble(reader, vertexTypes[j], endiannessType);
                    else if (vertexColorDescriptor != null)
                    {
                        var value = readNumberAsFloat(reader, vertexColorElementType[colorIndexer], endiannessType);
                        if (vertexColorElementType[colorIndexer] != typeof(float)
                            && vertexColorElementType[colorIndexer] != typeof(double))
                            value = value / 255f;
                        switch (vertexColorDescriptor[colorIndexer])
                        {
                            case ColorElements.Red:
                                r = value;
                                break;
                            case ColorElements.Green:
                                g = value;
                                break;
                            case ColorElements.Blue:
                                b = value;
                                break;
                            case ColorElements.Opacity:
                                a = value;
                                break;
                        }
                        vertexColors.Add(new Color(a, r, g, b));
                        colorIndexer++;
                    }
                    else vertexColors.Add(null);
                }
                if (point.Any(double.IsNaN)) return false;
                vertices.Add(point);
            }
            return true;
        }

        #endregion
        #region Save
        /// <summary>
        /// Saves the solid ASCII.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="solid">The solid.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        internal static bool SaveSolidASCII(ref string data, TessellatedSolid solid)
        {
            var stream = new MemoryStream();
            if (!SaveSolidASCII(stream, solid)) return false;
            var reader = new StreamReader(stream);
            data += reader.ReadToEnd();
            return true;
        }
        /// <summary>
        /// Saves the solid binary.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="solid">The solid.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        internal static bool SaveSolidBinary(ref string data, TessellatedSolid solid)
        {
            var stream = new MemoryStream();
            if (!SaveSolidBinary(stream, solid)) return false;
            var reader = new StreamReader(stream);
            data += reader.ReadToEnd();
            return true;
        }
        /// <summary>
        /// Saves the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="solid">The solid.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        internal static bool SaveSolidASCII(Stream stream, TessellatedSolid solid)
        {
            try
            {
                using (var writer = new StreamWriter(stream))
                {
                    WriteHeader(writer, solid, false);
                    foreach (var vertex in solid.Vertices)
                        writer.WriteLine(vertex.X + " " + vertex.Y + " " + vertex.Z);

                    var defineColors = !(solid.HasUniformColor && solid.SolidColor.Equals(new Color(Constants.DefaultColor)));
                    foreach (var face in solid.Faces)
                    {
                        var faceString = face.Vertices.Count.ToString();
                        foreach (var v in face.Vertices)
                            faceString += " " + v.IndexInList;
                        if (defineColors)
                            faceString += " " + face.Color.R + " " + face.Color.G + " " + face.Color.B + " " +
                                          face.Color.A;
                        writer.WriteLine(faceString);
                    }
                    if (solid.HasUniformColor)
                        writer.WriteLine(solid.SolidColor.R + " " + solid.SolidColor.G + " " + solid.SolidColor.B + " " +
                                                     solid.SolidColor.A);
                }
                Message.output("Successfully wrote PLY file to stream.", 3);
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
        /// Saves the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="solid">The solid.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        internal static bool SaveSolidBinary(Stream stream, TessellatedSolid solid)
        {
            try
            {
                using (var writer = new StreamWriter(stream))
                    WriteHeader(writer, solid, false);
                using (var writer = new BinaryWriter(stream))
                {
                    foreach (var vertex in solid.Vertices)
                    {
                        writer.Write(BitConverter.GetBytes(vertex.X));
                        writer.Write(BitConverter.GetBytes(vertex.Y));
                        writer.Write(BitConverter.GetBytes(vertex.Z));
                    }

                    var defineColors = !(solid.HasUniformColor && solid.SolidColor.Equals(new Color(Constants.DefaultColor)));
                    foreach (var face in solid.Faces)
                    {
                        writer.Write(BitConverter.GetBytes(face.Vertices.Count));
                        foreach (var v in face.Vertices)
                            writer.Write(BitConverter.GetBytes(v.IndexInList));
                        if (defineColors)
                        {
                            writer.Write(face.Color.R);
                            writer.Write(face.Color.G);
                            writer.Write(face.Color.B);
                            writer.Write(face.Color.A);
                        }
                    }
                    if (solid.HasUniformColor)
                    {
                        writer.Write(solid.SolidColor.R);
                        writer.Write(solid.SolidColor.G);
                        writer.Write(solid.SolidColor.B);
                        writer.Write(solid.SolidColor.A);
                    }
                }
                Message.output("Successfully wrote PLY file to stream.", 3);
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
        /// Writes the header.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="solid">The solid.</param>
        /// <param name="isBinary">if set to <c>true</c> [is binary].</param>
        private static void WriteHeader(StreamWriter writer, TessellatedSolid solid, bool isBinary)
        {
            var hasFaceColors = !(solid.HasUniformColor && solid.SolidColor.Equals(new Color(Constants.DefaultColor)));

            writer.WriteLine("ply");
            if (isBinary)
                writer.WriteLine("format binary_little_endian 1.0");
            else writer.WriteLine("format ascii 1.0");
            writer.WriteLine("comment  " + tvglDateMarkText);
            if (!string.IsNullOrWhiteSpace(solid.Name))
                writer.WriteLine("comment  Name : " + solid.Name);
            if (!string.IsNullOrWhiteSpace(solid.FileName))
                writer.WriteLine("comment  Originally loaded from : " + solid.FileName);
            if (solid.Units != UnitType.unspecified)
                writer.WriteLine("comment  Units : " + solid.Units);
            if (!string.IsNullOrWhiteSpace(solid.Language))
                writer.WriteLine("comment  Lang : " + solid.Language);
            if (solid.Comments != null)
                foreach (var comment in solid.Comments.Where(string.IsNullOrWhiteSpace))
                    writer.WriteLine("comment  " + comment);
            writer.WriteLine("element vertex " + solid.NumberOfVertices);
            writer.WriteLine("property double x");
            writer.WriteLine("property double y");
            writer.WriteLine("property double z");
            writer.WriteLine("element face " + solid.NumberOfFaces);
            writer.WriteLine("property list uint8 int32 vertex_indices");
            if (hasFaceColors)
            {
                writer.WriteLine("property uchar red");
                writer.WriteLine("property uchar green");
                writer.WriteLine("property uchar blue");
                writer.WriteLine("property uchar opacity");
            }
            if (solid.HasUniformColor)
            {
                writer.WriteLine("element uniform_color");
                writer.WriteLine("property uchar red");
                writer.WriteLine("property uchar green");
                writer.WriteLine("property uchar blue");
                writer.WriteLine("property uchar opacity");
            }
            writer.WriteLine("end_header");
        }
        #endregion
    }
}