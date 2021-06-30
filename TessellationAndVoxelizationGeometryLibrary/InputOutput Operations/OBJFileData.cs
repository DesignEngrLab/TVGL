// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TVGL.Numerics;

namespace TVGL.IOFunctions
{
    // http://en.wikipedia.org/wiki/OFF_(file_format)
    /// <summary>
    ///     Class OFFFileData.
    /// </summary>
    internal class OBJFileData : IO
    {
        #region Constructor

        /// <summary>
        ///     Initializes a new instance of the <see cref="OFFFileData" /> class.
        /// </summary>
        private OBJFileData()
        {
            Vertices = new List<Vector3>();
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
        private List<Vector3> Vertices { get; }

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
                // Read in ASCII format
                if (TryReadAscii(s, out var objData))
                    Message.output("Successfully read in ASCII OFF file (" + (DateTime.Now - now) + ").", 3);
                else
                {
                    Message.output("Unable to read in OFF file (" + (DateTime.Now - now) + ").", 1);
                    return null;
                }
            
            return new TessellatedSolid(objData.Vertices, objData.FaceToVertexIndices, true,
                objData.HasColorSpecified ? objData.Colors : null, InferUnitsFromComments(objData.Comments),
                Path.GetFileNameWithoutExtension(filename), filename, objData.Comments,
                objData.Language);
        }

        /// <summary>
        ///     Tries the read ASCII.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="objData">The off data.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private static bool TryReadAscii(Stream stream, out OBJFileData objData)
        {
            var reader = new StreamReader(stream);
            objData = new OBJFileData();
            var line = ReadLine(reader);
            if (!line.Contains("off") && !line.Contains("OFF"))
                return false;
            objData.ContainsNormals = line.Contains("N");
            objData.ContainsColors = line.Contains("C");
            objData.ContainsTextureCoordinates = line.Contains("ST");
            objData.ContainsHomogeneousCoordinates = line.Contains("4");

            line = ReadLine(reader);
            while (line.StartsWith("#"))
            {
                line.Remove(0, 1);
                if (!string.IsNullOrWhiteSpace(line))
                    objData.Comments.Add(line.Substring(1));
                line = ReadLine(reader);
            }
            if (TryParseDoubleArray(line, out var point))
            {
                objData.NumVertices = (int)Math.Round(point[0], 0);
                objData.NumFaces = (int)Math.Round(point[1], 0);
                objData.NumEdges = (int)Math.Round(point[2], 0);
            }
            else return false;

            for (var i = 0; i < objData.NumVertices; i++)
            {
                line = ReadLine(reader);
                while (line.StartsWith("#"))
                {
                    line.Remove(0, 1);
                    if (!string.IsNullOrWhiteSpace(line))
                        objData.Comments.Add(line.Substring(1));
                    line = ReadLine(reader);
                }
                if (TryParseDoubleArray(line, out point))
                {
                    if (objData.ContainsHomogeneousCoordinates
                        && !point[3].IsNegligible())
                        objData.Vertices.Add(new Vector3(
                            point[0] / point[3],
                            point[1] / point[3],
                            point[2] / point[3]
                        ));
                    else objData.Vertices.Add(new Vector3(point[0], point[1], point[2]));
                }
                else return false;
            }
            for (var i = 0; i < objData.NumFaces; i++)
            {
                line = ReadLine(reader);
                while (line.StartsWith("#"))
                {
                    line.Remove(0, 1);
                    if (!string.IsNullOrWhiteSpace(line))
                        objData.Comments.Add(line.Substring(1));
                    line = ReadLine(reader);
                }
                if (!TryParseDoubleArray(line, out var numbers)) return false;


                if (!numbers.Any())
                {
                    objData.NumFaces = i;
                    break;
                }

                var numVerts = (int)Math.Round(numbers[0], 0);
                var vertIndices = new int[numVerts];
                for (var j = 0; j < numVerts; j++)
                    vertIndices[j] = (int)Math.Round(numbers[1 + j], 0);
                objData.FaceToVertexIndices.Add(vertIndices);

                if (numbers.GetLength(0) == 1 + numVerts + 3)
                {
                    var r = (float)numbers[1 + numVerts];
                    var g = (float)numbers[2 + numVerts];
                    var b = (float)numbers[3 + numVerts];
                    var currentColor = new Color(1f, r, g, b);
                    objData.HasColorSpecified = true;
                    if (objData._lastColor == null || !objData._lastColor.Equals(currentColor))
                        objData._lastColor = currentColor;
                }
                objData.Colors.Add(objData._lastColor);
            }
            return true;
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
                writer.WriteLine("#  " + TvglDateMarkText);
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
                writer.Flush();
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