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

        private List<int> CoordinateOrder;
        private List<Type> CoordinateTypes;

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

        private FormatEndiannessType endiannessType;
        private Type vertexAmountType;
        private Type vertexIndexType;
        /// <summary>
        /// The color element type
        /// </summary>
        private List<Type> colorElementType;

        #endregion
        #region Open Solids

        /// <summary>
        /// Opens the specified s.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <param name="filename">The filename.</param>
        /// <returns>List&lt;TessellatedSolid&gt;.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        internal static TessellatedSolid OpenSolid(Stream s, string filename)
        {
            var reader = new StreamReader(s);
            var plyData = new PLYFileData { FileName = filename, Name = GetNameFromFileName(filename) };
            var line = ReadLine(reader);
            if (!line.Contains("ply") && !line.Contains("PLY"))
                return null;
            plyData.ReadHeader(reader);
            if (plyData.endiannessType == FormatEndiannessType.ascii)
                return plyData.ReadMesh(reader);
            else return plyData.ReadMesh(new BinaryReader(s));
        }


        private TessellatedSolid ReadMesh(StreamReader reader)
        {
            var now = DateTime.Now;
            foreach (var shapeElement in ReadInOrder)
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
                    case ShapeElement.Edge:
                        successful = ReadEdges(reader);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                if (!successful) return null;
            }
            Message.output("Successfully read in ASCII PLY file (" + (DateTime.Now - now) + ").", 3);
            return new TessellatedSolid(Vertices, FaceToVertexIndices,
                HasColorSpecified ? Colors : null, InferUnitsFromComments(Comments),
              Name, FileName, Comments, Language);
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
                var words = line.Split(' ');
                var numVerts = (int)readNumber(words[0], vertexAmountType);
                var vertIndices = new int[numVerts];
                for (var j = 0; j < numVerts; j++)
                    vertIndices[j] = (int)readNumber(words[1 + j], vertexIndexType);
                FaceToVertexIndices.Add(vertIndices);

                if (ColorDescriptor.Any())
                {
                    if (words.Length >= 1 + numVerts + ColorDescriptor.Count)
                    {
                        float a = 0, r = 0, g = 0, b = 0;
                        for (var j = 0; j < ColorDescriptor.Count; j++)
                        {
                            var value = (float)readNumber(words[1 + numVerts + j], colorElementType[j]);
                            if (colorElementType[j] != typeof(float) && colorElementType[j] != typeof(double))
                                value = value / 255f;
                            switch (ColorDescriptor[j])
                            {
                                case ColorElements.Red: r = value; break;
                                case ColorElements.Green: g = value; break;
                                case ColorElements.Blue: b = value; break;
                                case ColorElements.Opacity: a = value; break;
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
            var numD = CoordinateOrder.Count;
            for (var i = 0; i < NumVertices; i++)
            {
                var line = ReadLine(reader);
                var words = line.Split(' ');
                double[] point = new double[numD];

                for (int j = 0; j < numD; j++)
                    point[CoordinateOrder[j]] = (double)readNumber(words[j], CoordinateTypes[j]);
                if (point.Any(double.IsNaN)) return false;
                Vertices.Add(point);
            }
            return true;
        }

        private TessellatedSolid ReadMesh(BinaryReader reader)
        {
            var now = DateTime.Now;
            foreach (var shapeElement in ReadInOrder)
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
                    case ShapeElement.Edge:
                        successful = ReadEdges(reader);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                if (!successful) return null;
            }
            Message.output("Successfully read in ASCII PLY file (" + (DateTime.Now - now) + ").", 3);
            return new TessellatedSolid(Vertices, FaceToVertexIndices,
                HasColorSpecified ? Colors : null, InferUnitsFromComments(Comments),
              Name, FileName, Comments, Language);
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
            return true;

        }
        /// <summary>
        ///     Reads the faces.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private bool ReadFaces(BinaryReader reader)
        {
            for (var i = 0; i < NumFaces; i++)
            {
                var numVerts = (int)readNumber(reader, vertexAmountType, endiannessType);
                var vertIndices = new int[numVerts];
                for (var j = 0; j < numVerts; j++)
                    vertIndices[j] = (int) readNumber(reader, vertexIndexType, endiannessType);
                FaceToVertexIndices.Add(vertIndices);

                if (ColorDescriptor.Any())
                {
                    float a = 0, r = 0, g = 0, b = 0;
                    for (var j = 0; j < ColorDescriptor.Count; j++)
                    {
                        var value = (float) readNumber(reader, colorElementType[j], endiannessType);
                        if (colorElementType[j] != typeof(float) && colorElementType[j] != typeof(double))
                            value = value / 255f;
                        switch (ColorDescriptor[j])
                        {
                            case ColorElements.Red: r = value; break;
                            case ColorElements.Green: g = value; break;
                            case ColorElements.Blue: b = value; break;
                            case ColorElements.Opacity: a = value; break;
                        }
                    }
                    var currentColor = new Color(a, r, g, b);
                    HasColorSpecified = true;
                    if (_lastColor == null || !_lastColor.Equals(currentColor))
                        _lastColor = currentColor;
                }
                if (_lastColor != null) Colors.Add(_lastColor);
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
        var numD = CoordinateOrder.Count;
        for (var i = 0; i < NumVertices; i++)
        {
            double[] point = new double[numD];

            for (int j = 0; j < numD; j++)
                point[CoordinateOrder[j]] = (double)readNumber(reader, CoordinateTypes[j], endiannessType);

            if (point.Any(double.IsNaN)) return false;
            Vertices.Add(point);
        }
        return true;
    }

    static object readNumber(BinaryReader reader, Type type, FormatEndiannessType formatType)
    {
        var bigEndian = (formatType == FormatEndiannessType.binary_little_endian);

        if (type == typeof(double))
            return TVGLBitConverter.ToDouble(reader.ReadBytes(8), 0, bigEndian);
        if (type == typeof(long))
            return TVGLBitConverter.ToInt64(reader.ReadBytes(8), 0);
        if (type == typeof(ulong))
            return TVGLBitConverter.ToUInt64(reader.ReadBytes(8), 0);
        if (type == typeof(float))
            return TVGLBitConverter.ToSingle(reader.ReadBytes(4), 0);
        if (type == typeof(int))
            return TVGLBitConverter.ToInt32(reader.ReadBytes(4), 0);
        if (type == typeof(uint))
            return TVGLBitConverter.ToUInt32(reader.ReadBytes(4), 0);
        if (type == typeof(short))
            return TVGLBitConverter.ToInt16(reader.ReadBytes(2), 0);
        if (type == typeof(ushort))
            return TVGLBitConverter.ToUInt16(reader.ReadBytes(2), 0);
        if (type == typeof(byte))
            return reader.ReadBytes(1)[0];
        return double.NaN;
    }
    static object readNumber(string text, Type type)
    {
        if (type == typeof(double))
        {
            double coord;
            if (double.TryParse(text, out coord)) return coord;
        }
        if (type == typeof(long))
        {
            long coord;
            if (long.TryParse(text, out coord)) return coord;
        }
        if (type == typeof(ulong))
        {
            ulong coord;
            if (ulong.TryParse(text, out coord)) return coord;
        }
        if (type == typeof(float))
        {
            float coord;
            if (float.TryParse(text, out coord)) return coord;
        }
        if (type == typeof(int))
        {
            int coord;
            if (int.TryParse(text, out coord)) return coord;
        }
        if (type == typeof(uint))
        {
            uint coord;
            if (uint.TryParse(text, out coord)) return coord;
        }
        if (type == typeof(short))
        {
            short coord;
            if (short.TryParse(text, out coord)) return coord;
        }
        if (type == typeof(ushort))
        {
            ushort coord;
            if (ushort.TryParse(text, out coord)) return coord;
        }
        if (type == typeof(byte))
        {
            byte coord;
            if (byte.TryParse(text, out coord)) return coord;
        }
        return double.NaN;
    }

    /// <summary>
    ///     Reads the header.
    /// </summary>
    /// <param name="reader">The reader.</param>
    private void ReadHeader(StreamReader reader)
    {
        ReadInOrder = new List<ShapeElement>();
        ColorDescriptor = new List<ColorElements>();
        CoordinateTypes = new List<Type>();
        CoordinateOrder = new List<int>();
        string line;
        do
        {
            line = ReadLine(reader);
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
                else if (id.Equals("uniform_color"))
                {
                    ReadInOrder.Add(ShapeElement.Edge);
                    NumEdges = numberInt;
                }
            }
            else if (id.Equals("property"))
            {
                if (ReadInOrder.Last() == ShapeElement.Vertex)
                {
                    string typeString, coordString;
                    Type type;
                    ParseLine(values, out typeString, out coordString);
                    if (!TryParseNumberTypeFromString(typeString, out type))
                        throw new ArgumentException("Unable to parse " + typeString + " as a type of number");
                    CoordinateTypes.Add(type);

                    if (coordString.StartsWith("x", StringComparison.CurrentCultureIgnoreCase))
                        CoordinateOrder.Add(0);
                    else if (coordString.StartsWith("y", StringComparison.CurrentCultureIgnoreCase))
                        CoordinateOrder.Add(1);
                    else if (coordString.StartsWith("z", StringComparison.CurrentCultureIgnoreCase))
                        CoordinateOrder.Add(2);
                }
                if (ReadInOrder.Last() == ShapeElement.Face)
                {
                    string typeString, restString;
                    ParseLine(values, out typeString, out restString);
                    if (typeString.Equals("list"))
                    {
                        if (!restString.Contains("vertex_index") && !restString.Contains("vertex_indices"))
                            throw new ArgumentException("The faces in PLY are specified in unknown manner: " + restString);
                        var words = restString.Split(' ');
                        Type type;
                        if (TryParseNumberTypeFromString(words[0], out type))
                            vertexAmountType = type;
                        else throw new ArgumentException("The number of vertex in the PLY face definition are of unknown type: "
                            + words[0]);
                        if (TryParseNumberTypeFromString(words[1], out type))
                            vertexIndexType = type;
                        else throw new ArgumentException("The vertex indices in the PLY face definition are of unknown type: "
                            + words[1]);
                        continue;
                    }
                    if (restString.Equals("red", StringComparison.CurrentCultureIgnoreCase)
                    || restString.Equals("r", StringComparison.CurrentCultureIgnoreCase))
                        ColorDescriptor.Add(ColorElements.Red);
                    else if (restString.Equals("blue", StringComparison.CurrentCultureIgnoreCase)
                             || restString.Equals("b", StringComparison.CurrentCultureIgnoreCase))
                        ColorDescriptor.Add(ColorElements.Blue);
                    else if (restString.Equals("green", StringComparison.CurrentCultureIgnoreCase)
                             || restString.Equals("g", StringComparison.CurrentCultureIgnoreCase))
                        ColorDescriptor.Add(ColorElements.Green);
                    else if (restString.Equals("opacity", StringComparison.CurrentCultureIgnoreCase)
                             || restString.StartsWith("transp", StringComparison.CurrentCultureIgnoreCase)
                             || restString.Equals("a", StringComparison.CurrentCultureIgnoreCase))
                        ColorDescriptor.Add(ColorElements.Opacity);
                    else if (restString.Contains("red"))
                        ColorDescriptor.Add(ColorElements.Red);
                    else if (restString.Contains("blue"))
                        ColorDescriptor.Add(ColorElements.Blue);
                    else if (restString.Contains("green"))
                        ColorDescriptor.Add(ColorElements.Green);
                    else continue;
                    // the continue ensures that the following line will only be processed if it the property
                    // was identified as a color
                    Type colorType;
                    if (TryParseNumberTypeFromString(typeString, out colorType))
                        colorElementType.Add(colorType);
                }
            }
        } while (!line.Equals("end_header"));
    }

    #endregion
    #region Save
    /// <summary>
    /// Saves the specified stream.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <param name="solid">The solid.</param>
    /// <returns>
    ///   <c>true</c> if XXXX, <c>false</c> otherwise.
    /// </returns>
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
            Message.output("Exception: " + exception.Message, 3);
            return false;
        }
    }
    #endregion
}
}