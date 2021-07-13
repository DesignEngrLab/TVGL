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
            FaceGroups = new List<int[]>();
            FaceToVertexIndices = new List<int[]>();
            SurfaceEdges = new List<int[]>();
            Vertices = new Dictionary<Vector3, int>();
            VerticesByLine = new List<Vector3>();
        }

        #endregion

        #region Fields and Properties

        List<int[]> FaceGroups { get; }


        /// <summary>
        ///     Gets the face to vertex indices.
        /// </summary>
        /// <value>The face to vertex indices.</value>
        private List<int[]> FaceToVertexIndices { get; }

        /// <summary>
        ///     Gets or sets the surface edges.
        /// </summary>
        private List<int[]> SurfaceEdges { get; }

        /// <summary>
        ///     Gets the vertices as ordered by lines in the code. The same Vector3 may be referenced by multiple keys
        ///     if it appears in multiple lines of code.
        /// </summary>
        /// <value>The face to vertex indices.</value>
        private List<Vector3> VerticesByLine { get; }


        /// <summary>
        ///     Gets the integer location of the Vector within the list of vertices of the solid.
        /// </summary>
        /// <value>The face to vertex indices.</value>
        private Dictionary<Vector3, int> Vertices { get; }

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
                var vertices = objFileData.Vertices.Keys.ToList();
                var ts = new TessellatedSolid(vertices, objFileData.FaceToVertexIndices, true, null,
                               InferUnitsFromComments(objFileData.Comments), objFileData.Name, filename, objFileData.Comments,
                               objFileData.Language);
                CreateRegionsFromPolylineAndFaceGroups(objFileData, ts);
                results[i] = ts;
            }
            Message.output(
                "Successfully read in " + typeString + " file called " + filename + " in " +
                (DateTime.Now - now).TotalSeconds + " seconds.", 4);
            return results;
        }

        private static void CreateRegionsFromPolylineAndFaceGroups(OBJFileData objFileData, TessellatedSolid ts)
        {
            var showPatches = true;
            ts.Primitives = new List<PrimitiveSurface>();
            var significantEdges = new HashSet<Edge>();
            var remainingFaces = new HashSet<PolygonalFace>(ts.Faces);
            foreach (var faceIndices in objFileData.FaceGroups)
            {
                var primitive = new UnknownRegion(faceIndices.Select(index => ts.Faces[index]));
                ts.Primitives.Add(primitive);
                foreach (var face in primitive.Faces)
                    remainingFaces.Remove(face);
                primitive.DefineBorders();
                foreach (var border in primitive.Borders)
                    foreach (var edge in border.Edges.EdgeList)
                        if (!significantEdges.Contains(edge))
                            significantEdges.Add(edge);
            }
            foreach (var borderIndices in objFileData.SurfaceEdges)
            {
                for (int k = 1, j = 0; k < borderIndices.Length; j = k++) //clever loop to have j always one step behind k
                {
                    var vertexJ = ts.Vertices[j];
                    var vertexK = ts.Vertices[k];
                    Edge connectingEdge = null;
                    foreach (var edge in vertexJ.Edges)
                    {
                        if (edge.OtherVertex(vertexJ) == vertexK)
                        {
                            connectingEdge = edge;
                            break;
                        }
                    }
                    if (connectingEdge == null)
                        continue;
                    //throw new Exception("No edge in tessellated solid that matches polyline segment");
                    if (!significantEdges.Contains(connectingEdge))
                        significantEdges.Add(connectingEdge);
                }
            }
            var patches = SurfaceBorder.GetFacePatchesBetweenSignificantEdges(significantEdges, remainingFaces);
            foreach (var patch in patches)
            {
                var primitive = new UnknownRegion(patch);
                ts.Primitives.Add(primitive);
            }
#if PRESENT
            if (showPatches)
            {
                ts.ResetDefaultColor();
                ts.HasUniformColor = false;
                var colorsEnumerator = Constants.GetRandomColor().GetEnumerator();
                foreach (var primitive in ts.Primitives)
                {
                    colorsEnumerator.MoveNext();
                    var color = colorsEnumerator.Current;
                    foreach (var face in primitive.Faces)
                    {
                        face.Color = color;
                    }
                }
                Presenter.ShowVertexPathsWithSolid(significantEdges.Select(edge => new[] { edge.From.Coordinates, edge.To.Coordinates }),
                    new[] { ts }, false);
            }
#endif
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
            var reader = new StreamReader(stream);
            var numDecimalPoints = 8;

            char[] split = new char[] { ' ' };
            var defaultName = Path.GetFileNameWithoutExtension(filename) + "_";
            var solidNum = 0;
            objData = new List<OBJFileData>();
            var objSolid = new OBJFileData { FileName = filename, Name = defaultName + solidNum, Units = UnitType.unspecified };
            var readingFaces = false;
            var faceGroup = new List<int>();
            var i = 0;
            while (!reader.EndOfStream)
            {
                if (!readingFaces && faceGroup.Any())
                {
                    objSolid.FaceGroups.Add(faceGroup.ToArray());
                    faceGroup = new List<int>();
                }
                var line = ReadLine(reader);
                ParseLine(line, out var id, out var values);

                readingFaces = false; //instead of putting this in every case below, let's just re-set it to false. if we are reading faces
                switch (id)
                {
                    case "#":
                        objSolid.Comments.Add(values);
                        break;
                    case "mtllib":
                        //ToDo: Read the materials file if needed.
                        break;
                    case "g":
                        if (objSolid.FaceToVertexIndices.Count == 0)
                        {   // often, the solid is not defined until one gets to the faces. So, if encountering the "g" before any faces
                            // have been defined, then this simply defines the name for the sub-solid
                            if (!string.IsNullOrWhiteSpace(values)) objSolid.Name = values;
                            // also use this as the opportunity to add the solid to the collection
                            objData.Add(objSolid);
                        }
                        else // then that's the end of the prior solid. Time to start a new one.
                        {
                            if (faceGroup.Any()) // but before we do that, better be sure to capture any file GeometrySets
                            {
                                objSolid.FaceGroups.Add(faceGroup.ToArray());
                                faceGroup = new List<int>();
                            }
                            solidNum++;
                            objSolid = new OBJFileData { FileName = filename, Name = defaultName + solidNum, Units = UnitType.unspecified };
                        }
                        break;
                    case "usemtl":
                        //  The material is everything after the first space.
                        //objSolid.Material.Add(values);
                        break;
                    case "v"://vertex
                        var v = ReadVertex(values.Split(split, StringSplitOptions.RemoveEmptyEntries));
                        var coordinates = new Vector3(Math.Round(v.X, numDecimalPoints),
                            Math.Round(v.Y, numDecimalPoints), Math.Round(v.Z, numDecimalPoints));
                        //If the vertex already exists, store the 
                        if (!objSolid.Vertices.ContainsKey(coordinates))
                            objSolid.Vertices.Add(coordinates, i++);
                        //Store the vertex by line so that it can be referenced by geometry (i.e., face or line)
                        objSolid.VerticesByLine.Add(coordinates);
                        break;
                    case "vt"://texture coordinate (ignore?)
                        break;
                    case "vn"://Vertex normal (ignore?)
                        break;
                    case "f"://Face - multiple formats possible
                        faceGroup.Add(objSolid.FaceToVertexIndices.Count);
                        objSolid.FaceToVertexIndices.Add(objSolid.ReadFace(values.Split(split, StringSplitOptions.RemoveEmptyEntries)));
                        readingFaces = true;
                        break;
                    case "l"://Line 
                        objSolid.SurfaceEdges.Add(objSolid.ReadSurfaceLine(values.Split(split, StringSplitOptions.RemoveEmptyEntries)));
                        break;
                }
            }
            if (!objData.Any() || objData[^1] != objSolid) 
                objData.Add(objSolid);
            return true;
        }


        private static Vector3 ReadVertex(string[] values)
        {
            var x = double.Parse(values[0], CultureInfo.InvariantCulture);
            var y = double.Parse(values[1], CultureInfo.InvariantCulture);
            var z = double.Parse(values[2], CultureInfo.InvariantCulture);
            if (values.Length < 4)
                return new Vector3(x, y, z);
            var w = double.Parse(values[2], CultureInfo.InvariantCulture);
            return new Vector3(x / w, y / w, z / w);
        }

        private int[] ReadFace(string[] values)
        {
            var face = new int[3];
            for (int i = 0; i < values.Length; i++)
            {
                //  Split the parts.
                string[] parts = values[i].Split(new char[] { '/' }, StringSplitOptions.None);
                face[i] = GetFirstVertexIndex(parts[0]);
                //face[1] = GetFirstVertexIndex(parts[1]); // this is the vt or vertex texture index
                //face[2] = GetFirstVertexIndex(parts[2]); // this is the vt or vertex normal index
            }
            return face;
        }

        private int[] ReadSurfaceLine(string[] values)
        {
            var line = new int[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                line[i] = GetFirstVertexIndex(values[i]);
            }
            return line;
        }

        /// <summary>
        /// Gets the correct vertex reference. If a negative is before the integer, 
        /// then count backward in the list of vertices by line.
        /// </summary>
        /// <param name="vertexIndex"></param>
        /// <returns></returns>
        private int GetFirstVertexIndex(string vertexIndex)
        {
            var a = int.Parse(vertexIndex, CultureInfo.InvariantCulture);
            Index i = a < 0 ? ^-a : a - 1;
            return Vertices[VerticesByLine[i]];
        }

        #endregion

        #region Save Solids
        internal static bool SaveSolids(Stream stream, TessellatedSolid[] tessellatedSolids)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}