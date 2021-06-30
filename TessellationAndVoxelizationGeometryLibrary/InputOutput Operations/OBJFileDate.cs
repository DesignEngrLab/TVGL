// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TVGL.Numerics;

namespace TVGL.IOFunctions
{
    internal class OBJFileData : IO
    {
        #region Constructor

        /// <summary>
        ///     Initializes a new instance of the <see cref="OBJFileData" /> class.
        /// </summary>
        internal OBJFileData()
        {
            GeometrySets = new List<GeometrySet>();
        }

        #endregion

        #region Fields and Properties

        internal class GeometrySet
        {
            internal List<Vector3> Vertices { get; }
            internal List<int[]> GeometryMap { get; }
            internal OBJGeometryType GeometryType { get; }
            internal GeometrySet()
            {
                Vertices = new List<Vector3>();
                GeometryMap = new List<int[]>();
            }
        }

        internal enum OBJGeometryType
        {
            face,
            line
        }

        /// <summary>
        ///     Gets or sets the Vertices.
        /// </summary>
        /// <value>The vertices.</value>
        List<GeometrySet> GeometrySets { get; }

        GeometrySet CurrentGeometrySet { get; set; }


        /// <summary>
        ///     Gets the vertices.
        /// </summary>
        /// <value>The face to vertex indices.</value>
        internal List<Vector3> Vertices { get; }

        /// <summary>
        ///     Gets the face to vertex indices.
        /// </summary>
        /// <value>The face to vertex indices.</value>
        private List<int[]> FaceToVertexIndices { get; }

        /// <summary>
        ///     Gets or sets the surface edges.
        /// </summary>
        /// <value>The normals.</value>
        private List<int[]> SurfaceEdges { get; }

        #endregion

        #region Open Solids

        /// <summary>
        /// Opens the specified s.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <param name="filename">The filename.</param>
        /// <returns>List&lt;TessellatedSolid&gt;.</returns>
        internal static TessellatedSolid[] OpenSolids(Stream s, string filename)
        {
            var typeString = "OBJ";
            var now = DateTime.Now;
            // Try to read in BINARY format
            if (!TryRead(s, filename, out var objData))
                Message.output("Unable to read in OBJ file called {0}", filename, 1);
            var results = new TessellatedSolid[objData.Count];
            for (int i = 0; i < objData.Count; i++)
            {
                var objFileData = objData[i];
                results[i] = new TessellatedSolid(objFileData.Vertices, objFileData.FaceToVertexIndices, true, null,
                                   objFileData.Units, objFileData.Name, filename, objFileData.Comments, objFileData.Language);
            }
            Message.output(
                "Successfully read in " + typeString + " file called " + filename + " in " +
                (DateTime.Now - now).TotalSeconds + " seconds.", 4);
            return results;
        }


        /// <summary>
        /// Reads the model in ASCII format from the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="filename">The filename.</param>
        /// <param name="stlData">The STL data.</param>
        /// <returns>True if the model was loaded successfully.</returns>
        internal static bool TryRead(Stream stream, string filename, out List<OBJFileData> objData)
        {
            char[] split = new char[] { ' ' };
            var defaultName = Path.GetFileNameWithoutExtension(filename) + "_";
            var solidNum = 0;
            var reader = new StreamReader(stream);
            objData = new List<OBJFileData>();
            var objSolid = new OBJFileData { FileName = filename, Units = UnitType.unspecified };
            var comments = new List<string>();
            var priorWasVertex = false;
            while (!reader.EndOfStream)
            {
                var line = ReadLine(reader);
                comments.Add(line);
                ParseLine(line, out var id, out var values);
                //Start a new geometry set every time we get to a new vertex group
                if (id != "v" && priorWasVertex)
                {
                    objSolid.CurrentGeometrySet = new GeometrySet();
                    objSolid.GeometrySets.Add(objSolid.CurrentGeometrySet);
                    priorWasVertex = false;
                }
                switch (id)
                {
                    case "#":
                        objSolid.Comments.Add(values);
                        break;
                    case "mtllib":
                        //ToDo: Read the materials file if needed.
                        break;
                    case "usemtl":
                        //  The material is everything after the first space.
                        //objSolid.Material.Add(values);
                        break;
                    case "v"://vertex
                        priorWasVertex = true;
                        objSolid.ReadVertex(line.Substring(2).Split(split, StringSplitOptions.RemoveEmptyEntries));
                        break;
                    case "vt"://texture coordinate (ignore?)
                        break;
                    case "vn"://Vertex normal (ignore?)
                        break;
                    case "f"://Face - multiple formats possible
                        objSolid.ReadFace(line.Substring(2).Split(split, StringSplitOptions.RemoveEmptyEntries));
                        break;
                    case "l"://Line 
                        objSolid.ReadSurfaceLine(line.Substring(2).Split(split, StringSplitOptions.RemoveEmptyEntries));
                        break;
                }
            }
            return true;
        }


        private void ReadVertex(string[] values)
        {    
            //  Parse vertex coordinates.
            float x = float.Parse(values[0], CultureInfo.InvariantCulture);
            float y = float.Parse(values[1], CultureInfo.InvariantCulture);
            float z = float.Parse(values[2], CultureInfo.InvariantCulture);

            //   Add the vertices.
            CurrentGeometrySet.Vertices.Add(new Vector3(x, y, z));
        }

        private void ReadFace(string[] values)
        {
            //  Add each index.
            var trim = new char[] { '-' };
            var face = new int[3];
            foreach (var index in values)
            {
                //  Split the parts.
                string[] parts = index.Split(new char[] { '/' }, StringSplitOptions.None);

                //We just need the vertex references, which are the first index of each part.
                //  Add each part.
                face[0] = int.Parse(parts[0].Trim(trim), CultureInfo.InvariantCulture);
                face[1] = int.Parse(parts[1].Trim(trim), CultureInfo.InvariantCulture);
                face[2] = int.Parse(parts[2].Trim(trim), CultureInfo.InvariantCulture);
            }

      
            CurrentGeometrySet.GeometryMap.Add(face);
        }

        private void ReadSurfaceLine(string[] values)
        {
            var trim = new char[] { '-' };
            var line = new int[values.Length];
            for(var i = 0; i < values.Length; i++)
            {
                line[i] = int.Parse(values[i].Trim(trim), CultureInfo.InvariantCulture);
            }
        
            CurrentGeometrySet.GeometryMap.Add(line);
        }
        #endregion
    }
}