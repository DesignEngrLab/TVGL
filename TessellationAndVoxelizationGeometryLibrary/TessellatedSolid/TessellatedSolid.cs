// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using TVGL.Numerics;
using TVGL.TwoDimensional;

namespace TVGL
{
    /// <summary>
    ///     Class TessellatedSolid.
    /// </summary>
    /// <tags>help</tags>
    /// <remarks>
    ///     This is the currently the <strong>main</strong> class within TVGL all filetypes are read in as a TessellatedSolid,
    ///     and
    ///     all interesting operations work on the TessellatedSolid.
    /// </remarks>
    public partial class TessellatedSolid : Solid
    {
        #region Fields and Properties
        /// <summary>
        ///     Gets the faces.
        /// </summary>
        /// <value>The faces.</value>
        [JsonIgnore]
        public PolygonalFace[] Faces { get; private set; }

        /// <summary>
        ///     Gets the edges.
        /// </summary>
        /// <value>The edges.</value>
        [JsonIgnore]
        public Edge[] Edges
        {
            get
            {
                if (_edges == null) MakeEdges();
                return _edges;
            }
        }
        private Edge[] _edges;

        /// <summary>
        ///     Gets the vertices.
        /// </summary>
        /// <value>The vertices.</value>
        [JsonIgnore]
        public Vertex[] Vertices { get; private set; }

        /// <summary>
        ///     Gets the number of faces.
        /// </summary>
        /// <value>The number of faces.</value>
        [JsonIgnore]
        public int NumberOfFaces { get; private set; }

        /// <summary>
        ///     Gets the number of vertices.
        /// </summary>
        /// <value>The number of vertices.</value>
        [JsonIgnore]
        public int NumberOfVertices { get; private set; }

        /// <summary>
        ///     Gets the number of edges.
        /// </summary>
        /// <value>The number of edges.</value>
        [JsonIgnore]
        public int NumberOfEdges { get; private set; }

        /// <summary>
        ///     Errors in the tesselated solid
        /// </summary>
        [JsonIgnore]
        public TessellationError Errors { get; internal set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="TessellatedSolid"/> class.
        /// </summary>
        public TessellatedSolid() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TessellatedSolid" /> class. This is the one that
        /// matches with the STL format.
        /// </summary>
        /// <param name="vertsPerFace">The verts per face.</param>
        /// <param name="createFullVersion">if set to <c>true</c> [make edges].</param>
        /// <param name="colors">The colors.</param>
        /// <param name="units">The units.</param>
        /// <param name="name">The name.</param>
        /// <param name="filename">The filename.</param>
        /// <param name="comments">The comments.</param>
        /// <param name="language">The language.</param>
        public TessellatedSolid(IEnumerable<List<Vector3>> vertsPerFace, bool createFullVersion, IList<Color> colors,
            UnitType units = UnitType.unspecified, string name = "", string filename = "", List<string> comments = null,
            string language = "")
            : base(units, name, filename, comments, language)
        {
            var vertsPerFaceList = vertsPerFace as IList<List<Vector3>> ?? vertsPerFace.ToList();
            DefineAxisAlignedBoundingBoxAndTolerance(vertsPerFaceList.SelectMany(v => v));
            var scaleFactor = 1.0;
            if ((Bounds[1] - Bounds[0]).Length() < 0.1)
            {
                if (units == UnitType.unspecified || units == UnitType.meter)
                {
                    units = UnitType.millimeter;
                    scaleFactor = 1000;
                }
                else if (units == UnitType.foot)
                {
                    units = UnitType.inch;
                    scaleFactor = 12;
                }
                DefineAxisAlignedBoundingBoxAndTolerance(vertsPerFaceList.SelectMany(vList => vList.Select(v => scaleFactor * v)));
            }
            MakeVertices(vertsPerFaceList, scaleFactor, out List<int[]> faceToVertexIndices);
            //Complete Construction with Common Functions
            MakeFaces(faceToVertexIndices, colors);
            if (createFullVersion) CompleteInitiation(true);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TessellatedSolid" /> class. This matches with formats
        /// that use indices to the vertices (almost everything except STL).
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="faceToVertexIndices">The face to vertex indices.</param>
        /// <param name="createFullVersion">if set to <c>true</c> [make edges].</param>
        /// <param name="colors">The colors.</param>
        /// <param name="units">The units.</param>
        /// <param name="name">The name.</param>
        /// <param name="filename">The filename.</param>
        /// <param name="comments">The comments.</param>
        /// <param name="language">The language.</param>
        public TessellatedSolid(IList<Vector3> vertices, IList<int[]> faceToVertexIndices, bool createFullVersion,
            IList<Color> colors, UnitType units = UnitType.unspecified, string name = "", string filename = "",
            List<string> comments = null, string language = "") : base(units, name, filename, comments, language)
        {
            DefineAxisAlignedBoundingBoxAndTolerance(vertices);
            MakeVertices(vertices);
            //Complete Construction with Common Functions
            MakeFaces(faceToVertexIndices, colors);
            if (createFullVersion) CompleteInitiation();
        }
        [OnSerializing]
        protected void OnSerializingMethod(StreamingContext context)
        {
            //if (serializationData == null)
            serializationData = new Dictionary<string, JToken>();
            serializationData.Add("ConvexHullVertices",
                JToken.FromObject(ConvexHull.Vertices.Select(v => v.IndexInList)));
            serializationData.Add("ConvexHullFaces",
                JToken.FromObject(ConvexHull.Faces.SelectMany(face => face.Vertices.Select(v => v.IndexInList))));
            serializationData.Add("FaceIndices",
                JToken.FromObject(Faces.SelectMany(face => face.Vertices.Select(v => v.IndexInList)).ToArray()));
            serializationData.Add("VertexCoords",
               JToken.FromObject(Vertices.ConvertTo1DDoublesCollection()));
            serializationData.Add("Colors",
            (HasUniformColor || Faces.All(f => f.Color.Equals(Faces[0].Color)))
            ? SolidColor.ToString()
            : JToken.FromObject(Faces.Select(f => f.Color)));
        }


        [OnDeserialized]
        protected void OnDeserializedMethod(StreamingContext context)
        {
            JArray jArray = (JArray)serializationData["VertexCoords"];
            var vertexArray = jArray.ToObject<double[]>();
            var coords = new Vector3[vertexArray.Length / 3];
            for (int i = 0; i < vertexArray.Length / 3; i++)
                coords[i] = new Vector3(vertexArray[3 * i], vertexArray[3 * i + 1], vertexArray[3 * i + 2]);

            jArray = (JArray)serializationData["FaceIndices"];
            var faceIndicesArray = jArray.ToObject<int[]>();
            var faceIndices = new int[faceIndicesArray.Length / 3][];
            for (int i = 0; i < faceIndicesArray.Length / 3; i++)
                faceIndices[i] = new[] { faceIndicesArray[3 * i], faceIndicesArray[3 * i + 1], faceIndicesArray[3 * i + 2] };

            jArray = serializationData["Colors"] as JArray;
            Color[] colors;
            if (jArray == null)
            { colors = new[] { new Color(serializationData["Colors"].ToString()) }; }
            else
            {
                var colorStringsArray = jArray.ToObject<string[]>();
                colors = new Color[colorStringsArray.Length];
                for (int i = 0; i < colorStringsArray.Length; i++)
                    colors[i] = new Color(colorStringsArray[i]);
            }
            DefineAxisAlignedBoundingBoxAndTolerance(coords);
            MakeVertices(coords);
            MakeFaces(faceIndices, colors);
            MakeEdges();

            if (serializationData.ContainsKey("ConvexHullVertices"))
            {
                jArray = (JArray)serializationData["ConvexHullVertices"];
                var cvxIndices = jArray.ToObject<int[]>();
                var cvxVertices = new Vertex[cvxIndices.Length];
                for (int i = 0; i < cvxIndices.Length; i++)
                    cvxVertices[i] = Vertices[cvxIndices[i]];
                jArray = (JArray)serializationData["ConvexHullFaces"];
                var cvxFaceIndices = jArray.ToObject<int[]>();
                ConvexHull = new TVGLConvexHull(Vertices, cvxVertices, cvxFaceIndices, SameTolerance);
            }
            else
            {
                ConvexHull = new TVGLConvexHull(this);
            }
            foreach (var cvxHullPt in ConvexHull.Vertices)
                cvxHullPt.PartOfConvexHull = true;
            foreach (var face in Faces.Where(face => face.Vertices.All(v => v.PartOfConvexHull)))
            {
                face.PartOfConvexHull = true;
                foreach (var e in face.Edges)
                    if (e != null) e.PartOfConvexHull = true;
            }
            if (Primitives != null && Primitives.Any())
            {
                foreach (var surface in Primitives)
                    surface.CompletePostSerialization(this);
            }

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TessellatedSolid" /> class. This constructor is
        /// for cases in which the faces and vertices are already defined.
        /// </summary>
        /// <param name="faces">The faces.</param>
        /// <param name="createFullVersion">if set to <c>true</c> [make edges].</param>
        /// <param name="copyElements">if set to <c>true</c> [copy elements].</param>
        /// <param name="vertices">The vertices.</param>
        /// <param name="colors">The colors.</param>
        /// <param name="units">The units.</param>
        /// <param name="name">The name.</param>
        /// <param name="filename">The filename.</param>
        /// <param name="comments">The comments.</param>
        /// <param name="language">The language.</param>
        public TessellatedSolid(IEnumerable<PolygonalFace> faces, bool createFullVersion, bool copyElements,
            IEnumerable<Vertex> vertices = null, IList<Color> colors = null, UnitType units = UnitType.unspecified, string name = "", string filename = "",
            List<string> comments = null, string language = "") : base(units, name, filename, comments, language)
        {
            if (colors != null && colors.Count == 1)
            {
                SolidColor = colors[0];
                HasUniformColor = true;
            }
            var manyInputColors = (colors != null && colors.Count > 1);
            if (manyInputColors) HasUniformColor = false;
            Faces = faces.ToArray();
            NumberOfFaces = Faces.Length;
            if (vertices == null)
            {
                vertices = new HashSet<Vertex>();
                foreach (var face in Faces)
                    foreach (var vertex in face.Vertices)
                        if (!vertices.Contains(vertex))
                            ((HashSet<Vertex>)vertices).Add(vertex);
            }
            Vertices = vertices.ToArray();
            NumberOfVertices = Vertices.Length;
            var simpleCompareDict = new Dictionary<Vertex, Vertex>();
            if (copyElements)
            {
                for (var i = 0; i < NumberOfVertices; i++)
                {
                    var origVertex = Vertices[i];
                    var vertex = origVertex.Copy();
                    vertex.IndexInList = i;
                    vertex.PartOfConvexHull = false; //We will find the convex hull vertices during CompleteInitiation
                    Vertices[i] = vertex;
                    simpleCompareDict.Add(origVertex, vertex);
                }
            }
            else
            {
                for (var i = 0; i < NumberOfVertices; i++)
                {
                    var vertex = Vertices[i];
                    vertex.IndexInList = i;
                    vertex.PartOfConvexHull = false; //We will find the convex hull vertices during CompleteInitiation
                }
            }

            if (createFullVersion)
            {
                DefineAxisAlignedBoundingBoxAndTolerance(Vertices.Select(v => v.Coordinates));
                if (copyElements)
                {
                    var i = 0;
                    foreach (var origFace in Faces)
                    {
                        //Keep "CreatedInFunction" to help with debug
                        var face = origFace.Copy();
                        face.PartOfConvexHull = false;
                        face.IndexInList = i;
                        var faceVertices = new List<Vertex>();
                        foreach (var vertex in origFace.Vertices)
                        {
                            var newVertex = simpleCompareDict[vertex];
                            faceVertices.Add(newVertex);
                            newVertex.Faces.Add(face);
                        }
                        face.Vertices = faceVertices;
                        if (HasUniformColor)
                            face.Color = SolidColor;
                        else if (manyInputColors)
                        {
                            var j = i < colors.Count - 1 ? i : colors.Count - 1;
                            face.Color = colors[j];
                            if (!SolidColor.Equals(face.Color)) HasUniformColor = false;
                        }
                        Faces[i] = face;
                        i++;
                    }
                }
                else
                {
                    NumberOfFaces = Faces.Length;
                    for (var i = 0; i < NumberOfFaces; i++)
                    {
                        var face = Faces[i];
                        face.IndexInList = i;
                        face.PartOfConvexHull = false; //We will find the convex hull vertices during CompleteInitiation
                        if (HasUniformColor)
                            face.Color = SolidColor;
                        else if (manyInputColors)
                        {
                            var j = i < colors.Count - 1 ? i : colors.Count - 1;
                            face.Color = colors[j];
                            if (!SolidColor.Equals(face.Color)) HasUniformColor = false;
                        }
                    }
                }
                CompleteInitiation();
            }
            else
            {
                if (copyElements)
                {
                    var i = 0;
                    foreach (var origFace in Faces)
                    {
                        //Keep "CreatedInFunction" to help with debug
                        var face = origFace.Copy();
                        face.PartOfConvexHull = false;
                        face.IndexInList = i;
                        var faceVertices = new List<Vertex>();
                        foreach (var vertex in origFace.Vertices)
                        {
                            var newVertex = simpleCompareDict[vertex];
                            faceVertices.Add(newVertex);
                            newVertex.Faces.Add(face);
                        }
                        face.Vertices = faceVertices;
                        if (HasUniformColor)
                            face.Color = SolidColor;
                        else if (manyInputColors)
                        {
                            var j = i < colors.Count - 1 ? i : colors.Count - 1;
                            face.Color = colors[j];
                            if (!SolidColor.Equals(face.Color)) HasUniformColor = false;
                        }
                        Faces[i] = face;
                        i++;
                    }
                }
                else
                {
                    for (var i = 0; i < NumberOfFaces; i++)
                    {
                        var face = Faces[i];
                        face.IndexInList = i;
                        if (HasUniformColor)
                            face.Color = SolidColor;
                        else if (manyInputColors)
                        {
                            var j = i < colors.Count - 1 ? i : colors.Count - 1;
                            face.Color = colors[j];
                            if (!SolidColor.Equals(face.Color)) HasUniformColor = false;
                        }
                    }

                }
            }
        }

        public void MakeEdgesIfNonExistent()
        {
            if (_edges != null && _edges.Length > 0) return;
            CompleteInitiation();
        }

        internal void CompleteInitiation(bool fromSTL = false)
        {
            MakeEdges(fromSTL);
            CalculateVolume();
            this.CheckModelIntegrity();
            ConvexHull = new TVGLConvexHull(this);
            if (ConvexHull.Vertices != null)
                foreach (var cvxHullPt in ConvexHull.Vertices)
                    cvxHullPt.PartOfConvexHull = true;
            foreach (var face in Faces.Where(face => face.Vertices.All(v => v.PartOfConvexHull)))
            {
                face.PartOfConvexHull = true;
                foreach (var e in face.Edges)
                    if (e != null) e.PartOfConvexHull = true;
            }
        }


        #endregion

        #region Make many elements (called from constructors)

        /// <summary>
        ///     Defines the axis aligned bounding box and tolerance. This is called first in the constructors
        ///     because the tolerance is used in making the vertices.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        private void DefineAxisAlignedBoundingBoxAndTolerance(IEnumerable<Vector3> vertices)
        {
            var xMin = double.PositiveInfinity;
            var yMin = double.PositiveInfinity;
            var zMin = double.PositiveInfinity;
            var xMax = double.NegativeInfinity;
            var yMax = double.NegativeInfinity;
            var zMax = double.NegativeInfinity;
            foreach (var v in vertices)
            {
                if (xMin > v.X) xMin = v.X;
                if (yMin > v.Y) yMin = v.Y;
                if (zMin > v.Z) zMin = v.Z;
                if (xMax < v.X) xMax = v.X;
                if (yMax < v.Y) yMax = v.Y;
                if (zMax < v.Z) zMax = v.Z;
            }
            Bounds = new[] { new Vector3(xMin, yMin, zMin), new Vector3(xMax, yMax, zMax) };
            var averageDimension = 0.333 * ((XMax - XMin) + (YMax - YMin) + (ZMax - ZMin));
            SameTolerance = averageDimension * Constants.BaseTolerance;
        }

        /// <summary>
        /// Makes the faces, avoiding duplicates.
        /// </summary>
        /// <param name="faceToVertexIndices">The face to vertex indices.</param>
        /// <param name="colors">The colors.</param>
        /// <param name="doublyLinkToVertices">if set to <c>true</c> [doubly link to vertices].</param>
        /// 
        internal void MakeFaces(IEnumerable<List<Vector3>> vertsPerFace, IList<Color> colors)
        {
            IList<List<Vector3>> vertexLocations = vertsPerFace as IList<List<Vector3>> ?? vertsPerFace.ToArray();
            HasUniformColor = true;
            if (colors == null || !colors.Any() || colors.All(c => c == null))
                SolidColor = new Color(Constants.DefaultColor);
            else SolidColor = colors[0];
            NumberOfFaces = vertexLocations.Count;
            Faces = new PolygonalFace[NumberOfFaces];
            for (var i = 0; i < NumberOfFaces; i++)
            {
                var color = SolidColor;
                if (colors != null)
                {
                    var j = i < colors.Count - 1 ? i : colors.Count - 1;
                    if (colors[j] != null) color = colors[j];
                    if (!SolidColor.Equals(color)) HasUniformColor = false;
                }
                Faces[i] = new PolygonalFace(vertexLocations[i].Select(v => new Vertex(v)));
                Faces[i].IndexInList = i;
            }
        }


        /// <summary>
        /// Makes the faces, avoiding duplicates.
        /// </summary>
        /// <param name="faceToVertexIndices">The face to vertex indices.</param>
        /// <param name="colors">The colors.</param>
        /// <param name="doublyLinkToVertices">if set to <c>true</c> [doubly link to vertices].</param>
        /// 
        internal void MakeFaces(IList<int[]> faceToVertexIndices, IList<Color> colors,
            bool doublyLinkToVertices = true)
        {
            var duplicateFaceCheck = true;
            HasUniformColor = true;
            if (colors == null || !colors.Any() || colors.All(c => c == null))
                SolidColor = new Color(Constants.DefaultColor);
            else SolidColor = colors[0];
            NumberOfFaces = faceToVertexIndices.Count;
            var listOfFaces = new List<PolygonalFace>(NumberOfFaces);
            var faceChecksums = new HashSet<long>();
            if (NumberOfVertices > Constants.CubeRootOfLongMaxValue)
            {
                Message.output("Repeat Face check is disabled since the number of vertices exceeds "
                               + Constants.CubeRootOfLongMaxValue);
                duplicateFaceCheck = false;
            }
            var checksumMultiplier = duplicateFaceCheck
                ? new List<long> { 1, NumberOfVertices, NumberOfVertices * NumberOfVertices }
                : null;
            for (var i = 0; i < NumberOfFaces; i++)
            {
                var faceToVertexIndexList = faceToVertexIndices[i];
                if (duplicateFaceCheck)
                {
                    // first check to be sure that this is a new face and not a duplicate or a degenerate
                    var orderedIndices =
                        new List<int>(faceToVertexIndexList.Select(index => Vertices[index].IndexInList));
                    orderedIndices.Sort();
                    while (orderedIndices.Count > checksumMultiplier.Count)
                        checksumMultiplier.Add((long)Math.Pow(NumberOfVertices, checksumMultiplier.Count));
                    var checksum = orderedIndices.Select((index, p) => index * checksumMultiplier[p]).Sum();
                    if (faceChecksums.Contains(checksum)) continue; //Duplicate face. Do not create
                    if (orderedIndices.Count < 3 || ContainsDuplicateIndices(orderedIndices)) continue;
                    // if you made it passed these to "continue" conditions, then this is a valid new face
                    faceChecksums.Add(checksum);
                }
                var faceVertices =
                    faceToVertexIndexList.Select(vertexMatchingIndex => Vertices[vertexMatchingIndex]).ToArray();

                var color = SolidColor;
                if (colors != null)
                {
                    var j = i < colors.Count - 1 ? i : colors.Count - 1;
                    if (colors[j] != null) color = colors[j];
                    if (SolidColor == null || !SolidColor.Equals(color)) HasUniformColor = false;
                }
                if (faceVertices.Length == 3)
                {
                    var face = new PolygonalFace(faceVertices, doublyLinkToVertices);
                    if (!HasUniformColor) face.Color = color;
                    listOfFaces.Add(face);
                }
                else
                {
                    var normal = MiscFunctions.DetermineNormalForA3DPolygon(faceVertices, faceVertices.Length, out _, Vector3.Null, out _);
                    var triangulatedList = faceVertices.Triangulate(normal);
                    var listOfFlatFaces = new List<PolygonalFace>();
                    foreach (var vertexSet in triangulatedList)
                    {
                        var face = new PolygonalFace(vertexSet, normal, doublyLinkToVertices);
                        if (!HasUniformColor) face.Color = color;
                        listOfFaces.Add(face);
                        listOfFlatFaces.Add(face);
                    }
                    Primitives ??= new List<PrimitiveSurface>();
                    Primitives.Add(new Plane(listOfFlatFaces));
                }
            }
            Faces = listOfFaces.ToArray();
            NumberOfFaces = Faces.GetLength(0);
            for (var i = 0; i < NumberOfFaces; i++)
                Faces[i].IndexInList = i;
        }

        /// <summary>
        ///     Makes the vertices.
        /// </summary>
        /// <param name="vertsPerFace">The verts per face.</param>
        /// <param name="faceToVertexIndices">The face to vertex indices.</param>
        private void MakeVertices(IEnumerable<List<Vector3>> vertsPerFace, double scaleFactor, out List<int[]> faceToVertexIndices)
        {
            var numDecimalPoints = 0;
            //Gets the number of decimal places
            while (Math.Round(scaleFactor * SameTolerance, numDecimalPoints) == 0.0) numDecimalPoints++;
            /* vertexMatchingIndices will be used to speed up the linking of faces and edges to vertices
             * it  preserves the order of vertsPerFace (as read in from the file), and indicates where
             * you can find each vertex in the new array of vertices. This is essentially what is built in 
             * the remainder of this method. */
            faceToVertexIndices = new List<int[]>();
            var listOfVertices = new List<Vector3>();
            var simpleCompareDict = new Dictionary<Vector3, int>();
            //in order to reduce compare times we use a string comparer and dictionary
            foreach (var t in vertsPerFace)
            {
                var locationIndices = new List<int>(); // this will become a row in faceToVertexIndices
                for (int i = 0; i < t.Count; i++)
                {
                    /* given the low precision in files like STL, this should be a sufficient way to detect identical points. 
                     * I believe comparing these lookupStrings will be quicker than comparing two 3d points.*/
                    //First, round the vertices, then convert to a string. This will catch bidirectional tolerancing (+/-)
                    var coordinates = t[i] = new Vector3(scaleFactor * Math.Round(t[i].X, numDecimalPoints),
                        Math.Round(scaleFactor * t[i].Y, numDecimalPoints), Math.Round(scaleFactor * t[i].Z, numDecimalPoints));
                    if (simpleCompareDict.ContainsKey(coordinates))
                        /* if it's in the dictionary, simply put the location in the locationIndices */
                        locationIndices.Add(simpleCompareDict[coordinates]);
                    else
                    {
                        /* else, add a new vertex to the list, and a new entry to simpleCompareDict. Also, be sure to indicate
                        * the position in the locationIndices. */
                        var newIndex = listOfVertices.Count;
                        listOfVertices.Add(t[i]);
                        simpleCompareDict.Add(coordinates, newIndex);
                        locationIndices.Add(newIndex);
                    }
                }
                faceToVertexIndices.Add(locationIndices.ToArray());
            }
            //Make vertices from the double arrays
            MakeVertices(listOfVertices);
        }

        /// <summary>
        ///     Makes the vertices, and set CheckSum multiplier
        /// </summary>
        /// <param name="listOfVertices">The list of vertices.</param>
        private void MakeVertices(IList<Vector3> listOfVertices)
        {
            NumberOfVertices = listOfVertices.Count;
            Vertices = new Vertex[NumberOfVertices];
            for (var i = 0; i < NumberOfVertices; i++)
                Vertices[i] = new Vertex(listOfVertices[i], i);
            //Set the checksum
        }

        #endregion

        #region Add or Remove Items

        #region Vertices - the important thing about these is updating the IndexInList property of the vertices

        internal void AddVertex(Vertex newVertex)
        {
            var newVertices = new Vertex[NumberOfVertices + 1];
            for (var i = 0; i < NumberOfVertices; i++)
                newVertices[i] = Vertices[i];
            newVertices[NumberOfVertices] = newVertex;
            newVertex.IndexInList = NumberOfVertices;
            Vertices = newVertices;
            NumberOfVertices++;
        }

        internal void AddVertices(IList<Vertex> verticesToAdd)
        {
            var numToAdd = verticesToAdd.Count;
            var newVertices = new Vertex[NumberOfVertices + numToAdd];
            for (var i = 0; i < NumberOfVertices; i++)
                newVertices[i] = Vertices[i];
            for (var i = 0; i < numToAdd; i++)
            {
                var newVertex = verticesToAdd[i];
                newVertices[NumberOfVertices + i] = newVertex;
                newVertex.IndexInList = NumberOfVertices + i;
            }
            Vertices = newVertices;
            NumberOfVertices += numToAdd;
        }

        internal void ReplaceVertex(Vertex removeVertex, Vertex newVertex, bool removeReferecesToVertex = true)
        {
            ReplaceVertex(Vertices.FindIndex(removeVertex), newVertex, removeReferecesToVertex);
        }

        internal void ReplaceVertex(int removeVIndex, Vertex newVertex, bool removeReferecesToVertex = true)
        {
            if (removeReferecesToVertex) RemoveReferencesToVertex(Vertices[removeVIndex]);
            newVertex.IndexInList = removeVIndex;
            Vertices[removeVIndex] = newVertex;
        }

        internal void RemoveVertex(Vertex removeVertex, bool removeReferecesToVertex = true)
        {
            RemoveVertex(Vertices.FindIndex(removeVertex), removeReferecesToVertex);
        }

        internal void RemoveVertex(int removeVIndex, bool removeReferecesToVertex = true)
        {
            if (removeReferecesToVertex) RemoveReferencesToVertex(Vertices[removeVIndex]);
            NumberOfVertices--;
            var newVertices = new Vertex[NumberOfVertices];
            for (var i = 0; i < removeVIndex; i++)
                newVertices[i] = Vertices[i];
            for (var i = removeVIndex; i < NumberOfVertices; i++)
            {
                var v = Vertices[i + 1];
                v.IndexInList--;
                newVertices[i] = v;
            }
            Vertices = newVertices;
            UpdateAllEdgeCheckSums();
        }

        internal void RemoveVertices(IEnumerable<Vertex> removeVertices)
        {
            RemoveVertices(removeVertices.Select(Vertices.FindIndex).ToList());
        }

        internal void RemoveVertices(List<int> removeIndices)
        {
            var numToRemove = removeIndices.Count;
            if (numToRemove == 0) return;
            var offset = 0;
            foreach (var vertexIndex in removeIndices)
            {
                RemoveReferencesToVertex(Vertices[vertexIndex]);
            }
            removeIndices.Sort();
            NumberOfVertices -= numToRemove;
            var newVertices = new Vertex[NumberOfVertices];
            for (var i = 0; i < NumberOfVertices; i++)
            {
                while (offset < numToRemove && i + offset == removeIndices[offset])
                    offset++;
                var v = Vertices[i + offset];
                v.IndexInList = i;
                newVertices[i] = v;
            }
            Vertices = newVertices;
            UpdateAllEdgeCheckSums();
        }

        internal void UpdateAllEdgeCheckSums()
        {
            foreach (var edge in Edges)
                SetAndGetEdgeChecksum(edge);
        }

        #endregion

        #region Faces

        internal void AddFace(PolygonalFace newFace)
        {
            var newFaces = new PolygonalFace[NumberOfFaces + 1];
            for (var i = 0; i < NumberOfFaces; i++)
                newFaces[i] = Faces[i];
            newFaces[NumberOfFaces] = newFace;
            newFace.IndexInList = NumberOfFaces;
            Faces = newFaces;
            NumberOfFaces++;
        }

        internal void AddFaces(IList<PolygonalFace> facesToAdd)
        {
            var numToAdd = facesToAdd.Count;
            var newFaces = new PolygonalFace[NumberOfFaces + numToAdd];
            for (var i = 0; i < NumberOfFaces; i++)
                newFaces[i] = Faces[i];
            for (var i = 0; i < numToAdd; i++)
            {
                newFaces[NumberOfFaces + i] = facesToAdd[i];
                newFaces[NumberOfFaces + i].IndexInList = NumberOfFaces + i;
            }
            Faces = newFaces;
            NumberOfFaces += numToAdd;
        }

        internal void RemoveFace(PolygonalFace removeFace)
        {
            RemoveFace(Faces.FindIndex(removeFace));
        }

        internal void RemoveFace(int removeFaceIndex)
        {
            //First. Remove all the references to each edge and vertex.
            RemoveReferencesToFace(removeFaceIndex);
            NumberOfFaces--;
            var newFaces = new PolygonalFace[NumberOfFaces];
            for (var i = 0; i < removeFaceIndex; i++)
                newFaces[i] = Faces[i];
            for (var i = removeFaceIndex; i < NumberOfFaces; i++)
            {
                newFaces[i] = Faces[i + 1];
                newFaces[i].IndexInList = i;
            }
            Faces = newFaces;
        }

        internal void RemoveFaces(IEnumerable<PolygonalFace> removeFaces)
        {
            RemoveFaces(removeFaces.Select(Faces.FindIndex).ToList());
        }

        internal void RemoveFaces(List<int> removeIndices)
        {
            //First. Remove all the references to each edge and vertex.
            foreach (var faceIndex in removeIndices)
            {
                RemoveReferencesToFace(faceIndex);
            }
            var offset = 0;
            var numToRemove = removeIndices.Count;
            removeIndices.Sort();
            NumberOfFaces -= numToRemove;
            var newFaces = new PolygonalFace[NumberOfFaces];
            for (var i = 0; i < NumberOfFaces; i++)
            {
                while (offset < numToRemove && i + offset == removeIndices[offset])
                    offset++;
                newFaces[i] = Faces[i + offset];
                newFaces[i].IndexInList = i;
            }
            Faces = newFaces;
        }

        private void RemoveReferencesToFace(int removeFaceIndex)
        {
            var face = Faces[removeFaceIndex];
            foreach (var vertex in face.Vertices)
                vertex.Faces.Remove(face);
            foreach (var edge in face.Edges)
            {
                if (edge == null) continue;
                if (face == edge.OwnedFace) edge.OwnedFace = null;
                if (face == edge.OtherFace) edge.OtherFace = null;
            }
            //Face adjacency is a method call, not an object reference. So it updates automatically.
        }

        #endregion

        #region Edges

        internal void AddEdge(Edge newEdge)
        {
            var newEdges = new Edge[NumberOfEdges + 1];
            for (var i = 0; i < NumberOfEdges; i++)
                newEdges[i] = _edges[i];
            newEdges[NumberOfEdges] = newEdge;
            if (newEdge.EdgeReference <= 0) SetAndGetEdgeChecksum(newEdge);
            newEdge.IndexInList = NumberOfEdges;
            _edges = newEdges;
            NumberOfEdges++;
        }

        internal void AddEdges(IList<Edge> edgesToAdd)
        {
            var numToAdd = edgesToAdd.Count;
            var newEdges = new Edge[NumberOfEdges + numToAdd];
            for (var i = 0; i < NumberOfEdges; i++)
                newEdges[i] = Edges[i];
            for (var i = 0; i < numToAdd; i++)
            {
                newEdges[NumberOfEdges + i] = edgesToAdd[i];
                if (newEdges[NumberOfEdges + i].EdgeReference <= 0) SetAndGetEdgeChecksum(newEdges[NumberOfEdges + i]);
                newEdges[NumberOfEdges + i].IndexInList = NumberOfEdges;
            }
            _edges = newEdges;
            NumberOfEdges += numToAdd;
        }

        internal void RemoveEdge(Edge removeEdge)
        {
            RemoveEdge(Edges.FindIndex(removeEdge));
        }

        internal void RemoveEdge(int removeEdgeIndex)
        {
            RemoveReferencesToEdge(removeEdgeIndex);
            NumberOfEdges--;
            var newEdges = new Edge[NumberOfEdges];
            for (var i = 0; i < removeEdgeIndex; i++)
                newEdges[i] = Edges[i];
            for (var i = removeEdgeIndex; i < NumberOfEdges; i++)
            {
                newEdges[i] = Edges[i + 1];
                newEdges[i].IndexInList = i;
            }
            _edges = newEdges;
        }

        internal void RemoveEdges(IEnumerable<Edge> removeEdges)
        {
            RemoveEdges(removeEdges.Select(Edges.FindIndex).ToList());
        }

        internal void RemoveEdges(List<int> removeIndices)
        {
            //First. Remove all the references to each edge and vertex.
            foreach (var edgeIndex in removeIndices)
            {
                RemoveReferencesToEdge(edgeIndex);
            }
            var offset = 0;
            var numToRemove = removeIndices.Count;
            removeIndices.Sort();
            NumberOfEdges -= numToRemove;
            var newEdges = new Edge[NumberOfEdges];
            for (var i = 0; i < NumberOfEdges; i++)
            {
                while (offset < numToRemove && i + offset == removeIndices[offset])
                    offset++;
                newEdges[i] = Edges[i + offset];
                newEdges[i].IndexInList = i;
            }
            _edges = newEdges;
        }

        private void RemoveReferencesToEdge(int removeEdgeIndex)
        {
            var edge = Edges[removeEdgeIndex];
            int index;
            if (edge.To != null)
            {
                index = edge.To.Edges.IndexOf(edge);
                if (index >= 0) edge.To.Edges.RemoveAt(index);
            }
            if (edge.From != null)
            {
                index = edge.From.Edges.IndexOf(edge);
                if (index >= 0) edge.From.Edges.RemoveAt(index);
            }
            if (edge.OwnedFace != null)
            {
                index = edge.OwnedFace.Edges.IndexOf(edge);
                if (index >= 0) edge.OwnedFace.Edges.RemoveAt(index);
            }
            if (edge.OtherFace != null)
            {
                index = edge.OtherFace.Edges.IndexOf(edge);
                if (index >= 0) edge.OtherFace.Edges.RemoveAt(index);
            }
        }

        #endregion

        /// <summary>
        ///     Adds the primitive.
        /// </summary>
        /// <param name="p">The p.</param>
        public void AddPrimitive(PrimitiveSurface p)
        {
            Primitives ??= new List<PrimitiveSurface>();
            Primitives.Add(p);
            foreach (var face in p.Faces)
                face.BelongsToPrimitive = p;
        }

        #endregion

        #region Copy Function

        /// <summary>
        ///     Copies this instance.
        /// </summary>
        /// <returns>TessellatedSolid.</returns>
        public TessellatedSolid Copy()
        {
            var copy = new TessellatedSolid(Faces, Edges != null, true, Vertices, Faces.Select(p => p.Color).ToList(), Units, Name + "_Copy",
                FileName, Comments, Language);
            if (Primitives != null && Primitives.Any())
            {
                foreach (var surface in Primitives)
                {
                    var surfType = surface.GetType();
                    var surfConstructor = surfType.GetConstructor(new[] { surfType, typeof(TessellatedSolid) });
                    copy.AddPrimitive((PrimitiveSurface)surfConstructor.Invoke(new object[] { surface, copy }));
                }
            }
            return copy;
        }

        #endregion

        #region Reset Color Function
        public void ResetDefaultColor()
        {
            var defaultColor = new Color(KnownColors.LightGray);
            foreach (var face in Faces) face.Color = defaultColor;
        }
        #endregion

        #region Transform
        /// <summary>
        /// Translates and Squares Tesselated Solid based on its oriented bounding box. 
        /// The resulting Solid should be located at the origin, and only in the positive X, Y, Z octant.
        /// </summary>
        /// <returns></returns>
        public TessellatedSolid SetToOriginAndSquareToNewSolid(out BoundingBox originalBoundingBox)
        {
            originalBoundingBox = this.OrientedBoundingBox();
            Matrix4x4.Invert(originalBoundingBox.Transform, out var transform);
            return (TessellatedSolid)TransformToNewSolid(transform);
        }
        /// <summary>
        /// Translates and Squares Tesselated Solid based on its oriented bounding box. 
        /// The resulting Solid should be located at the origin, and only in the positive X, Y, Z octant.
        /// </summary>
        /// <returns></returns>
        public void SetToOriginAndSquare(out BoundingBox originalBoundingBox)
        {
            originalBoundingBox = this.OrientedBoundingBox();
            Matrix4x4.Invert(originalBoundingBox.Transform, out var transform);
            Transform(transform);
        }


        /// <summary>
        /// Transforms the specified transform matrix.
        /// </summary>
        /// <param name="transformMatrix">The transform matrix.</param>
        public override void Transform(Matrix4x4 transformMatrix)
        {
            var xMin = double.PositiveInfinity;
            var yMin = double.PositiveInfinity;
            var zMin = double.PositiveInfinity;
            var xMax = double.NegativeInfinity;
            var yMax = double.NegativeInfinity;
            var zMax = double.NegativeInfinity;
            foreach (var v in Vertices)
            {
                v.Coordinates = v.Coordinates.Transform(transformMatrix);
                if (xMin > v.Coordinates.X) xMin = v.Coordinates.X;
                if (yMin > v.Coordinates.Y) yMin = v.Coordinates.Y;
                if (zMin > v.Coordinates.Z) zMin = v.Coordinates.Z;
                if (xMax < v.Coordinates.X) xMax = v.Coordinates.X;
                if (yMax < v.Coordinates.Y) yMax = v.Coordinates.Y;
                if (zMax < v.Coordinates.Z) zMax = v.Coordinates.Z;
            }
            Bounds = new[] { new Vector3(xMin, yMin, zMin), new Vector3(xMax, yMax, zMax) };


            //Update the faces
            foreach (var face in Faces)
            {
                face.Update();
            }
            //Update the edges
            foreach (var edge in Edges)
            {
                edge.Update(true);
            }
            _center = _center.Transform(transformMatrix);
            // I'm not sure this is right, but I'm just using the 3x3 rotational submatrix to rotate the inertia tensor
            var rotMatrix = new Matrix3x3(transformMatrix.M11, transformMatrix.M12, transformMatrix.M13,
                    transformMatrix.M21, transformMatrix.M22, transformMatrix.M23,
                    transformMatrix.M31, transformMatrix.M32, transformMatrix.M33);
            _inertiaTensor *= rotMatrix;
            if (Primitives != null)
                foreach (var primitive in Primitives)
                    primitive.Transform(transformMatrix);
        }
        /// <summary>
        /// Gets a new solid by transforming its vertices.
        /// </summary>
        /// <param name="transformationMatrix"></param>
        /// <returns></returns>
        public override Solid TransformToNewSolid(Matrix4x4 transformationMatrix)
        {
            var copy = this.Copy();
            copy.Transform(transformationMatrix);
            return copy;
        }


        internal bool TurnModelInsideOut()
        {
            _volume = -1 * _volume;
            _inertiaTensor = Matrix3x3.Null;
            foreach (var face in Faces) face.Invert();

            if (_edges != null)
                foreach (var edge in Edges) edge.Invert();
            return true;
        }

        protected override void CalculateCenter()
        {
            CalculateVolumeAndCenter(Faces, SameTolerance, out _volume, out _center);
        }

        protected override void CalculateVolume()
        {
            CalculateVolumeAndCenter(Faces, SameTolerance, out _volume, out _center);
        }

        public static void CalculateVolumeAndCenter(IEnumerable<PolygonalFace> faces, double tolerance, out double volume, out Vector3 center)
        {
            center = new Vector3();
            volume = 0.0;
            double oldVolume;
            var iterations = 0;
            Vector3 oldCenter1 = center;
            var facesList = faces as IList<PolygonalFace> ?? faces.ToList();
            do
            {
                oldVolume = volume;
                var oldCenter2 = oldCenter1;
                oldCenter1 = center;
                volume = 0;
                center = Vector3.Zero;
                foreach (var face in facesList)
                {
                    if (face.Area.IsNegligible(tolerance)) continue; //Ignore faces with zero area, since their Normals are not set.
                    var tetrahedronVolume = face.Area * face.Normal.Dot(face.Vertices[0].Coordinates - oldCenter1) / 3;
                    // this is the volume of a tetrahedron from defined by the face and the origin {0,0,0}. The origin would be part of the second term
                    // in the dotproduct, "face.Normal.Dot(face.Vertices[0].Position - ORIGIN))", but clearly there is no need to subtract
                    // {0,0,0}. Note that the volume of the tetrahedron could be negative. This is fine as it ensures that the origin has no influence
                    // on the volume.
                    volume += tetrahedronVolume;
                    center += new Vector3(
                        (oldCenter1[0] + face.Vertices[0].X + face.Vertices[1].X + face.Vertices[2].X) * tetrahedronVolume / 4,
                        (oldCenter1[1] + face.Vertices[0].Y + face.Vertices[1].Y + face.Vertices[2].Y) * tetrahedronVolume / 4,
                        (oldCenter1[2] + face.Vertices[0].Z + face.Vertices[1].Z + face.Vertices[2].Z) * tetrahedronVolume / 4);
                    // center is found by a weighted sum of the centers of each tetrahedron. The weighted sum coordinate are collected here.
                }
                if (iterations > 10 || volume < 0) center = 0.5 * (oldCenter1 + oldCenter2);
                else center = center / volume;
                iterations++;
            } while (Math.Abs(oldVolume - volume) > tolerance && iterations <= 20);
        }

        protected override void CalculateSurfaceArea()
        {
            _surfaceArea = Faces.Sum(face => face.Area);
        }

        const double oneSixtieth = 1.0 / 60.0;

        protected override void CalculateInertiaTensor()
        {
            //var matrixA = new double[3, 3];
            var matrixCtotal = new Matrix3x3();
            var canonicalMatrix = new Matrix3x3(oneSixtieth, 0.5 * oneSixtieth, 0.5 * oneSixtieth,
                0.5 * oneSixtieth, oneSixtieth, 0.5 * oneSixtieth,
                0.5 * oneSixtieth, 0.5 * oneSixtieth, oneSixtieth);
            foreach (var face in Faces)
            {
                var matrixA = new Matrix3x3(
                   face.Vertices[0].Coordinates[0] - Center[0],
                   face.Vertices[0].Coordinates[1] - Center[1],
                   face.Vertices[0].Coordinates[2] - Center[2],

                   face.Vertices[1].Coordinates[0] - Center[0],
                   face.Vertices[1].Coordinates[1] - Center[1],
                   face.Vertices[1].Coordinates[2] - Center[2],

                   face.Vertices[2].Coordinates[0] - Center[0],
                   face.Vertices[2].Coordinates[1] - Center[1],
                   face.Vertices[2].Coordinates[2] - Center[2]);

                var matrixC = matrixA.Transpose() * canonicalMatrix;
                matrixC = matrixC * matrixA * matrixA.GetDeterminant();
                matrixCtotal = matrixCtotal + matrixC;
            }
            // todo fix this calculation
            //var translateMatrix = new double[,] { { 0 }, { 0 }, { 0 } };
            ////what is this crazy equations?
            //var matrixCprime =
            //    (translateMatrix * -1)
            //         * (translateMatrix.Transpose())
            //         + (translateMatrix * ((translateMatrix * -1).transpose()))
            //         + ((translateMatrix * -1) * ((translateMatrix * -1).transpose())
            //         * Volume);
            //matrixCprime = matrixCprime + matrixCtotal;
            //var result = Matrix4x4.Identity * (matrixCprime[0, 0] + matrixCprime[1, 1] + matrixCprime[2, 2]);
            //return result.Subtract(matrixCprime);
            throw new NotImplementedException();
        }
        #endregion
    }
}