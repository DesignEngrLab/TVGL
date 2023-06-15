// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="SHELLFileData.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace TVGL
{
    /// <summary>
    /// Class ShellFileData.
    /// Implements the <see cref="TVGL.IO" />
    /// </summary>
    /// <seealso cref="TVGL.IO" />
    internal class ShellFileData : IO
    {
        #region Constructor

        /// <summary>
        /// Prevents a default instance of the <see cref="ShellFileData"/> class from being created.
        /// </summary>
        private ShellFileData()
        {
            Vertices = new List<Vector3>();
            FaceToVertexIndices = new List<int[]>();
        }

        #endregion

        #region Properties and Fields
        /// <summary>
        /// The in object
        /// </summary>
        private static bool inObject;
        /// <summary>
        /// The startof vertices
        /// </summary>
        private static bool startofVertices;
        /// <summary>
        /// The endof vertices
        /// </summary>
        private static bool endofVertices;
        /// <summary>
        /// The startof facets
        /// </summary>
        private static bool startofFacets;
        /// <summary>
        /// The endof facets
        /// </summary>
        private static bool endofFacets;
        /// <summary>
        /// Gets or sets the colors.
        /// </summary>
        /// <value>The colors.</value>
        private List<Color> Colors { get; set; }
        /// <summary>
        /// Gets the vertices.
        /// </summary>
        /// <value>The vertices.</value>
        private List<Vector3> Vertices { get; }
        /// <summary>
        /// Gets the face to vertex indices.
        /// </summary>
        /// <value>The face to vertex indices.</value>
        private List<int[]> FaceToVertexIndices { get; }
        /// <summary>
        /// Gets or sets the material.
        /// </summary>
        /// <value>The material.</value>
        private ShellMaterial Material { get; set; }

        /// <summary>
        /// Struct ShellMaterial
        /// </summary>
        private struct ShellMaterial
        {
            /// <summary>
            /// The material name
            /// </summary>
            internal readonly string materialName;
            /// <summary>
            /// The material color
            /// </summary>
            internal Color materialColor;

            /// <summary>
            /// Initializes a new instance of the <see cref="ShellMaterial"/> struct.
            /// </summary>
            /// <param name="name">The name.</param>
            /// <param name="color">The color.</param>
            internal ShellMaterial(string name, Color color)
            {
                materialName = name;
                materialColor = color;
            }
        }

        #endregion

        #region Open Solids

        /// <summary>
        /// Opens the solids.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <param name="filename">The filename.</param>
        /// <returns>TessellatedSolid[].</returns>
        internal static TessellatedSolid[] OpenSolids(Stream s, string filename, TessellatedSolidBuildOptions tsBuildOptions)
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
                        results[i] = new TessellatedSolid(shell.Vertices, shell.FaceToVertexIndices,
                           shell.Colors, tsBuildOptions, unit, shell.Name + "_" + shell.Material.materialName,
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

        /// <summary>
        /// Reads the faces.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private bool ReadFaces(string line)
        {
            if (!TryParseDoubleArray(line, out var numbers)) return false;
            var vertIndices = new int[3];
            for (var j = 0; j < 3; j++)
                vertIndices[j] = (int)Math.Round(numbers[j], 0);
            FaceToVertexIndices.Add(vertIndices);
            return true;
        }

        /// <summary>
        /// Reads the material.
        /// </summary>
        /// <param name="line">The line.</param>
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

        /// <summary>
        /// Reads the vertices.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private bool ReadVertices(string line)
        {
            if (TryParseDoubleArray(line, out var point))
                Vertices.Add(new Vector3(point));
            else return false;
            return true;
        }

        #endregion

        #region Save Solids

        /// <summary>
        /// Saves the solids.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="solids">The solids.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        private static bool SaveSolids(Stream stream, IList<TessellatedSolid> solids)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}