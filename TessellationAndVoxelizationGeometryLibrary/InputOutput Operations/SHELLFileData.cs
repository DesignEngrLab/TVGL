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
using System.Linq;

namespace TVGL.IOFunctions
{
    internal class ShellFileData : IO
    {
        /// <summary>
        ///     The last color
        /// </summary>
        private Color _lastColor;

        private static bool inObject = false;
        private static bool startofVertices = false;
        private static bool endofVertices = false;
        private static bool startofFacets = false;
        private static bool endofFacets = false;

        public ShellFileData()
        {
            Vertices = new List<double[]>();
            FaceToVertexIndices = new List<int[]>();
            ShellMaterial material;
        }

        /// <summary>
        ///     Gets the has color specified.
        /// </summary>
        /// <value>The has color specified.</value>
        public Boolean HasColorSpecified { get; private set; }

        private bool ColorIsFloat;
        /// <summary>
        ///     Gets or sets the colors.
        /// </summary>
        /// <value>The colors.</value>
        public List<Color> Colors { get; private set; }

        /// <summary>
        ///     Gets or sets the Vertices.
        /// </summary>
        /// <value>The vertices.</value>
        public  List<double[]> Vertices { get; private set; }

        /// <summary>
        ///     Gets the face to vertex indices.
        /// </summary>
        /// <value>The face to vertex indices.</value>
        public  List<int[]> FaceToVertexIndices { get; private set; }

        /// <summary>
        ///     Gets the file header.
        /// </summary>
        /// <value>The header.</value>
        public string Name { get; private set; }

        public List<string> Comments { get; private set; }
        private List<ShapeElement> ReadInOrder;
        private List<ColorElements> ColorDescriptor;

        public int NumVertices { get; private set; }
        public int NumFaces { get; private set; }
        public int NumEdges { get; private set; }
        public ShellMaterial Material { get; private set; }

        public struct ShellMaterial
        {
          public string materialName;
          public Color materialColor;

          public ShellMaterial(string name, Color color)
          {
              materialName = name;
              materialColor = color;
          }
        }
        
        internal static List<TessellatedSolid> Open(Stream s, bool inParallel = true)
        {
            var now = DateTime.Now;
            List<ShellFileData> shellData;
            // Read in ASCII format
            if (ShellFileData.TryReadAscii(s, out shellData))
                Message.output("Successfully read in ASCII PLY file (" + (DateTime.Now - now) + ").",3);
            else
            {
                Message.output("Unable to read in PLY file (" + (DateTime.Now - now) + ").",1);
                return null;
            }
            //return new List<TessellatedSolid>
            //{
            //    new TessellatedSolid(shellData.Name+shel, shellData.Vertices, shellData.FaceToVertexIndices,
            //        (shellData.HasColorSpecified ? shellData.Colors : null))
            //};
            var results = new List<TessellatedSolid>();
            foreach (var shell in shellData)
                if (shell.Vertices.Any() && shell.FaceToVertexIndices.Any())
                results.Add(new TessellatedSolid(shell.Name + "_" + shell.Material.materialName, shell.Vertices,
                    shell.FaceToVertexIndices, shell.Colors));
            Message.output("Successfully read in SHELL file called " + getNameFromStream(s) + " in " + (DateTime.Now - now).TotalSeconds + " seconds.", 4);
            return results;
        }


        
        internal static bool TryReadAscii(Stream stream, out List<ShellFileData> shellData)
        {
            var defaultName = getNameFromStream(stream) + "_";
            var solidNum = 0;
            var reader = new StreamReader(stream);
            shellData = new List<ShellFileData>();
            var shellSolid = new ShellFileData();
            var line = ReadLine(reader); // first line is the units
            while (!reader.EndOfStream)
            {
                line = ReadLine(reader);
                switch (line)
                {
                    case "shell":
                        inObject = true;
                        line = ReadLine(reader);
                        shellSolid.ReadMaterial(line);
                        break;
                    case "vertices":
                        startofVertices = true;
                        line = ReadLine(reader);
                        break;
                    case "endvertices":
                        endofVertices = true;
                        break;

                    case "endshell":
                        inObject = false;
                        shellData.Add(shellSolid);
                        shellSolid=new ShellFileData();
                        inObject = startofFacets = startofVertices = endofFacets = endofVertices = false;
                        break;
                    case "facets":
                        startofFacets = true;
                        line = ReadLine(reader);
                        break;
                    case "endfacets":
                        endofFacets = true;
                        break;
                    default:
                        break;
                }
                if (inObject && startofVertices && !endofVertices)                             //read and collect vertices coordinates while we're in the vertices loop of the current object/shell
                {
                   shellSolid.ReadVertices(line);
                }
                if (inObject && startofFacets && !endofFacets)                                       //read and collect trianges numbers while we're in the vertices loop of the current object/shell
                {
                    shellSolid.ReadFaces(line);
                }
            }
            return true;
        }


        //private bool ReadEdges(StreamReader reader)
        //{
            
        //    for (var i = 0; i < NumEdges; i++)
        //        ReadLine(reader);
        //    return true;
        //}

        private  bool ReadFaces(string line)
        {
            double[] numbers;
            if (!TryParseDoubleArray(line, out numbers)) return false;
            var vertIndices = new int[3];
            for (var j = 0; j < 3; j++)
                    vertIndices[j] = (int)Math.Round(numbers[j], 0);
            FaceToVertexIndices.Add(vertIndices);
            return true;
        }

        private  void ReadMaterial(string line)
        {
            float r = 0, g = 0, b = 0;
            string id, values, shellName, colorString;
            double[] shellColor;
            ParseLine(line, out id, out values);
            ParseLine(values, out shellName, out colorString);
            TryParseDoubleArray(colorString, out shellColor);
            r = (float)shellColor[0];
            g = (float)shellColor[1];
            b = (float)shellColor[2];
            var currentColor = new Color(r, g, b);
            Material = new ShellMaterial(shellName, currentColor);
        }

        private  bool ReadVertices(string line)
        {
            double[] point;
            if (TryParseDoubleArray(line, out point))
                Vertices.Add(point);
            else return false;
            return true;
        }

        //private void ReadHeader(StreamReader reader)
        //{
        //    ReadInOrder = new List<ShapeElement>();
        //    ColorDescriptor = new List<ColorElements>();
        //    string line;
        //    do
        //    {
        //        line = ReadLine(reader);
        //        string id, values;
        //        ParseLine(line, out id, out values);
        //        if (id.Equals("comment"))
        //        {
        //            if (Comments == null) Comments = new List<string>();
        //            Comments.Add(values);
        //        }
        //        else if (id.Equals("element"))
        //        {
        //            string numberString;
        //            ParseLine(values, out id, out numberString);
        //            int numberInt;
        //            var successfulParse = int.TryParse(numberString, out numberInt);
        //            if (!successfulParse) continue;
        //            if (id.Equals("vertex"))
        //            {
        //                ReadInOrder.Add(ShapeElement.Vertex);
        //                NumVertices = numberInt;
        //            }
        //            else if (id.Equals("face"))
        //            {
        //                ReadInOrder.Add(ShapeElement.Face);
        //                NumFaces = numberInt;
        //            }
        //            else if (id.Equals("edge"))
        //            {
        //                ReadInOrder.Add(ShapeElement.Edge);
        //                NumEdges = numberInt;
        //            }
        //        }
        //        else if (id.Equals("property") && ReadInOrder.Last() == ShapeElement.Face)
        //        {
        //            string typeString, restString;
        //            ParseLine(values, out typeString, out restString);
        //            // doesn't seem like much point in checking this, it comes in many
        //            // varieties like uint8 int32 vertex_indices, but it'll read in just fine
        //            //if (typeString.Equals("list") && restString.Contains("uchar int vertex_index"))
        //            //    expectingFaceToHaveListOfVertices = true;
        //            if (restString.Equals("red", StringComparison.OrdinalIgnoreCase)
        //                || restString.Equals("r", StringComparison.OrdinalIgnoreCase))
        //                ColorDescriptor.Add(ColorElements.Red);
        //            else if (restString.Equals("blue", StringComparison.OrdinalIgnoreCase)
        //                     || restString.Equals("b", StringComparison.OrdinalIgnoreCase))
        //                ColorDescriptor.Add(ColorElements.Blue);
        //            else if (restString.Equals("green", StringComparison.OrdinalIgnoreCase)
        //                     || restString.Equals("g", StringComparison.OrdinalIgnoreCase))
        //                ColorDescriptor.Add(ColorElements.Green);
        //            else if (restString.Equals("opacity", StringComparison.OrdinalIgnoreCase)
        //                     || restString.StartsWith("transp", StringComparison.OrdinalIgnoreCase)
        //                     || restString.Equals("a", StringComparison.OrdinalIgnoreCase))
        //                ColorDescriptor.Add(ColorElements.Opacity);
        //            else continue;
        //            ColorIsFloat = typeString.StartsWith("float", StringComparison.OrdinalIgnoreCase)
        //                           || typeString.StartsWith("double", StringComparison.OrdinalIgnoreCase);
        //        }
        //    } while (!line.Equals("end_header"));
        //}

        internal static bool Save(Stream stream, IList<TessellatedSolid> solids)
        {
            throw new NotImplementedException();
        }
    }
}