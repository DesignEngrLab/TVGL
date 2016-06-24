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
using System.Linq;

namespace TVGL.IOFunctions
{
    internal class ShellFileData : IO
    {
        #region Constructor

        private ShellFileData()
        {
            Vertices = new List<double[]>();
            FaceToVertexIndices = new List<int[]>();
        }

        #endregion

        #region Properties and Fields
        private static bool inObject;
        private static bool startofVertices;
        private static bool endofVertices;
        private static bool startofFacets;
        private static bool endofFacets;
        private List<Color> Colors { get; set; }
        private List<double[]> Vertices { get; }
        private List<int[]> FaceToVertexIndices { get; }
        private ShellMaterial Material { get; set; }

        private struct ShellMaterial
        {
            internal readonly string materialName;
            internal Color materialColor;

            internal ShellMaterial(string name, Color color)
            {
                materialName = name;
                materialColor = color;
            }
        }

        #endregion

        #region Open Solids

        internal static List<TessellatedSolid> OpenSolids(Stream s, string filename)
        {
            var now = DateTime.Now;
            try
            {
                var reader = new StreamReader(s);
                var shellData = new List<ShellFileData>();
                var shellSolid = new ShellFileData();
                string id, unitstring;
                ParseLine(ReadLine(reader), out id, out unitstring);
                UnitType unit;
                TryParseUnits(unitstring, out unit);
                while (!reader.EndOfStream)
                {
                    var line = ReadLine(reader);
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
                            shellSolid = new ShellFileData();
                            inObject = startofFacets = startofVertices = endofFacets = endofVertices = false;
                            break;
                        case "facets":
                            startofFacets = true;
                            line = ReadLine(reader);
                            break;
                        case "endfacets":
                            endofFacets = true;
                            break;
                    }
                    if (inObject && startofVertices && !endofVertices)
                    //read and collect vertices coordinates while we're in the vertices loop of the current object/shell
                    {
                        shellSolid.ReadVertices(line);
                    }
                    if (inObject && startofFacets && !endofFacets)
                    //read and collect trianges numbers while we're in the vertices loop of the current object/shell
                    {
                        shellSolid.ReadFaces(line);
                    }
                }
                var results = new List<TessellatedSolid>();
                foreach (var shell in shellData)
                    if (shell.Vertices.Any() && shell.FaceToVertexIndices.Any())
                        results.Add(new TessellatedSolid(shell.Vertices,
                            shell.FaceToVertexIndices, shell.Colors, unit, shell.Name + "_" + shell.Material.materialName,
                            filename, shell.Comments, shell.Language));
                Message.output(
                    "Successfully read in SHELL file called " + filename + " in " + (DateTime.Now - now).TotalSeconds +
                    " seconds.", 4);
                return results;
            }
            catch
            {
                Message.output("Unable to read in SHELL file.", 1);
                return null;
            }
        }

        private bool ReadFaces(string line)
        {
            double[] numbers;
            if (!TryParseDoubleArray(line, out numbers)) return false;
            var vertIndices = new int[3];
            for (var j = 0; j < 3; j++)
                vertIndices[j] = (int)Math.Round(numbers[j], 0);
            FaceToVertexIndices.Add(vertIndices);
            return true;
        }

        private void ReadMaterial(string line)
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

        private bool ReadVertices(string line)
        {
            double[] point;
            if (TryParseDoubleArray(line, out point))
                Vertices.Add(point);
            else return false;
            return true;
        }

        #endregion

        #region Save Solids

        private static bool SaveSolids(Stream stream, IList<TessellatedSolid> solids)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}