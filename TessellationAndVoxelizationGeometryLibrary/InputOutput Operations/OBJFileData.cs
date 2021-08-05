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
            VertexNormals = new Dictionary<Vector3, int>();
            VertexNormalsByLine = new List<Vector3>();
            FaceToNormalIndices = new List<int[]>();
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

        /// <summary>
        ///     Just like vertices, the vertex normals are stored. However, in TVGL - this is primarily used to find edges
        ///     that represent C1 discontinuities which are important in delineating the primtive surfaces.
        /// </summary>
        /// <value>The face to vertex indices.</value>
        private List<Vector3> VertexNormalsByLine { get; }


        /// <summary>
        ///     Gets the integer location of the vertex normal within the list of normals of the solid.
        /// </summary>
        /// <value>The face to vertex indices.</value>
        private Dictionary<Vector3, int> VertexNormals { get; }
        /// <summary>
        ///     Gets the face to normal indices.
        /// </summary>
        /// <value>The face to vertex indices.</value>
        private List<int[]> FaceToNormalIndices { get; }

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
            ts.NonsmoothEdges = new HashSet<Edge>();
            var remainingFaces = new HashSet<PolygonalFace>(ts.Faces);
            if(objFileData.FaceGroups.Count > 1 && !objFileData.SurfaceEdges.Any())
            {
                foreach (var faceIndices in objFileData.FaceGroups)
                {
                    MiscFunctions.DefineInnerOuterEdges(faceIndices.Select(index => ts.Faces[index]), out _, out var outerEdges);
                    foreach (var discontinuousEdge in outerEdges)
                        ts.NonsmoothEdges.Add(discontinuousEdge);
                }
            }       
            foreach (var borderIndices in objFileData.SurfaceEdges)
            {
                for (int k = 1, j = 0; k < borderIndices.Length; j = k++) //clever loop to have j always one step behind k
                {
                    var vertexJ = ts.Vertices[borderIndices[j]];
                    var vertexK = ts.Vertices[borderIndices[k]];
                    Edge discontinuousEdge = null;
                    foreach (var edge in vertexJ.Edges)
                    {
                        if (edge.OtherVertex(vertexJ) == vertexK)
                        {
                            discontinuousEdge = edge;
                            break;
                        }
                    }
                    if (discontinuousEdge == null) continue; //The edge may have been part of a duplicate face or otherwise removed
                    ts.NonsmoothEdges.Add(discontinuousEdge);
                }
            }
            if (!ts.NonsmoothEdges.Any())
            {
                //Try setting the edges. This is less accurate tha
                var dotMinSmoothAngle = Math.Abs(Math.Cos(Constants.TwoPi - Constants.MinSmoothAngle));
                var vertexNormals = objFileData.VertexNormals.Keys.Select(n => n.Normalize()).ToList();
                foreach (var possibleEdge in ts.Edges)
                {
                    if (ts.NonsmoothEdges.Contains(possibleEdge)) continue;
                    var index = possibleEdge.OwnedFace.IndexInList;
                    var ownedFace = ts.Faces[index];
                    if (index >= objFileData.FaceToNormalIndices.Count) continue;
                    var ownedNormalIndices = objFileData.FaceToNormalIndices[index];
                    index = possibleEdge.OtherFace.IndexInList;
                    var otherFace = ts.Faces[index];
                    if (index >= objFileData.FaceToNormalIndices.Count) continue;
                    var otherNormalIndices = objFileData.FaceToNormalIndices[index];
                    index = ownedFace.Vertices.IndexOf(possibleEdge.From);
                    var fromOwnedIndex = ownedNormalIndices[index];
                    var fromOwnedNormal = vertexNormals[fromOwnedIndex];
                    index = otherFace.Vertices.IndexOf(possibleEdge.From);
                    var fromOtherIndex = ownedNormalIndices[index];
                    var fromOtherNormal = vertexNormals[fromOtherIndex];
                    if (fromOwnedNormal.Dot(fromOtherNormal) < dotMinSmoothAngle)
                        ts.NonsmoothEdges.Add(possibleEdge);
                    else
                    {
                        index = ownedFace.Vertices.IndexOf(possibleEdge.To);
                        var toOwnedIndex = ownedNormalIndices[index];
                        var toOwnedNormal = vertexNormals[toOwnedIndex];
                        index = otherFace.Vertices.IndexOf(possibleEdge.To);
                        var toOtherIndex = ownedNormalIndices[index];
                        var toOtherNormal = vertexNormals[toOtherIndex];
                        if (toOwnedNormal.Dot(toOtherNormal) < dotMinSmoothAngle)
                            ts.NonsmoothEdges.Add(possibleEdge);
                    }
                }
            }   
        }


        /// <summary>
        /// Reads the model in ASCII format from the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="filename">The filename.</param>
        /// <param name="stlData">The STL data.</param>
        /// <returns>True if the model was loaded successfully.</returns>  
        private static bool TryRead(Stream stream, string filename, out List<OBJFileData> objData)
        {
            var reader = new StreamReader(stream);
            var numDecimalPointsCoords = 8;
            var numDecimalPointsNormals = 3;

            char[] split = new char[] { ' ' };
            var defaultName = Path.GetFileNameWithoutExtension(filename) + "_";
            var solidNum = 0;
            objData = new List<OBJFileData>();
            var objSolid = new OBJFileData { FileName = filename, Name = defaultName + solidNum, Units = UnitType.unspecified };
            var readingFaces = false;
            var faceGroup = new List<int>();
            var indexFace = 0;
            var indexNormal = 0;
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
                        var v = ReadVector3(values.Split(split, StringSplitOptions.RemoveEmptyEntries));
                        var coordinates = new Vector3(Math.Round(v.X, numDecimalPointsCoords),
                            Math.Round(v.Y, numDecimalPointsCoords), Math.Round(v.Z, numDecimalPointsCoords));
                        //If the vertex doesn't already exists, store it in the list 
                        if (!objSolid.Vertices.ContainsKey(coordinates))
                            objSolid.Vertices.Add(coordinates, indexFace++);
                        //Store the vertex by line so that it can be referenced by geometry (i.e., face or line)
                        objSolid.VerticesByLine.Add(coordinates);
                        break;
                    case "vt"://texture coordinate (ignore?)
                        break;
                    case "vn"://Vertex normal
                        var vn = ReadVector3(values.Split(split, StringSplitOptions.RemoveEmptyEntries));
                        var normal = new Vector3(Math.Round(vn.X, numDecimalPointsNormals),
                           Math.Round(vn.Y, numDecimalPointsNormals), Math.Round(vn.Z, numDecimalPointsNormals));
                        //If the normal already exists, store the 
                        if (!objSolid.VertexNormals.ContainsKey(normal))
                            objSolid.VertexNormals.Add(normal, indexNormal++);
                        //Store the vertex by line so that it can be referenced by geometry (i.e., face or line)
                        objSolid.VertexNormalsByLine.Add(normal);
                        break;
                    case "f"://Face - multiple formats possible
                        faceGroup.Add(objSolid.FaceToVertexIndices.Count);
                        var perVertexValues = values.Split(split, StringSplitOptions.RemoveEmptyEntries);
                        objSolid.FaceToVertexIndices.Add(objSolid.ReadFaceVertices(perVertexValues));
                        objSolid.FaceToNormalIndices.Add(objSolid.ReadFaceNormals(perVertexValues));
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


        private static Vector3 ReadVector3(string[] values)
        {
            var x = double.Parse(values[0], CultureInfo.InvariantCulture);
            var y = double.Parse(values[1], CultureInfo.InvariantCulture);
            var z = double.Parse(values[2], CultureInfo.InvariantCulture);
            if (values.Length < 4)
                return new Vector3(x, y, z);
            var w = double.Parse(values[2], CultureInfo.InvariantCulture);
            return new Vector3(x / w, y / w, z / w);
        }

        private int[] ReadFaceVertices(IEnumerable<string> values)
        {
            var face = new int[3];
            var i = 0;
            foreach (var value in values)
            {
                string[] parts = value.Split(new char[] { '/' }, StringSplitOptions.None);
                face[i] = GetFirstVertexIndex(parts[0]);
                i++;
            }
            return face;
        }
        private int[] ReadFaceNormals(IEnumerable<string> values)
        {
            var face = new int[3];
            var i = 0;
            var success = false;
            foreach (var value in values)
            {
                string[] parts = value.Split(new char[] { '/' }, StringSplitOptions.None);
                if (parts.Length < 3) face[i] = -1;
                else if (int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var a))
                {
                    Index index = a < 0 ? ^-a : a - 1;
                    face[i] = VertexNormals[VertexNormalsByLine[index]];
                    success = true;
                }
                else face[i] = -1;
                i++;
            }
            if (success) return face;
            return null;
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