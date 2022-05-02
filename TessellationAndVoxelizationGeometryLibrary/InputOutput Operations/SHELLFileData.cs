// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace TVGL
{
    internal class ShellFileData : IO
    {
        #region Constructor

        private ShellFileData()
        {
            Vertices = new List<Vector3>();
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
        private List<Vector3> Vertices { get; }
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

        internal static TessellatedSolid[] OpenSolids(Stream s, string filename)
        {
            var now = DateTime.Now;
            try
            {
                var reader = new StreamReader(s);
                var shellData = new List<ShellFileData>();
                var shellSolid = new ShellFileData();
                ParseLine(ReadLine(reader), out _, out var unitstring);
                TryParseUnits(unitstring, out var unit);
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
                var results = new TessellatedSolid[shellData.Count];
                for (int i = 0; i < shellData.Count; i++)
                {
                    var shell = shellData[i];
                    if (shell.Vertices.Any() && shell.FaceToVertexIndices.Any())
                        results[i] = new TessellatedSolid(shell.Vertices, shell.FaceToVertexIndices, true,
                           shell.Colors, unit, shell.Name + "_" + shell.Material.materialName,
                            filename, shell.Comments, shell.Language);
                }

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
            if (!TryParseDoubleArray(line, out var numbers)) return false;
            var vertIndices = new int[3];
            for (var j = 0; j < 3; j++)
                vertIndices[j] = (int)Math.Round(numbers[j], 0);
            FaceToVertexIndices.Add(vertIndices);
            return true;
        }

        private void ReadMaterial(string line)
        {
            ParseLine(line, out _, out var values);
            ParseLine(values, out var shellName, out var colorString);
            TryParseDoubleArray(colorString, out var shellColor);
            var r = (float)shellColor[0];
            var g = (float)shellColor[1];
            var b = (float)shellColor[2];
            var currentColor = new Color(r, g, b);
            Material = new ShellMaterial(shellName, currentColor);
        }

        private bool ReadVertices(string line)
        {
            if (TryParseDoubleArray(line, out var point))
                Vertices.Add(new Vector3(point));
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