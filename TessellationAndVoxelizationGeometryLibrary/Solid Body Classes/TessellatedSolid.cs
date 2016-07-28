// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 02-27-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 03-07-2015
// ***********************************************************************
// <copyright file="TessellatedSolid.cs" company="Design Engineering Lab">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MIConvexHull;
using StarMathLib;
using TVGL.IOFunctions;

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
    public partial class TessellatedSolid
    {
        #region Fields and Properties

        /// <summary>
        ///     Gets the center.
        /// </summary>
        /// <value>The center.</value>
        public double[] Center { get; private set; }

        /// <summary>
        ///     Gets the z maximum.
        /// </summary>
        /// <value>The z maximum.</value>
        public double ZMax { get; private set; }

        /// <summary>
        ///     Gets the y maximum.
        /// </summary>
        /// <value>The y maximum.</value>
        public double YMax { get; private set; }

        /// <summary>
        ///     Gets the x maximum.
        /// </summary>
        /// <value>The x maximum.</value>
        public double XMax { get; private set; }

        /// <summary>
        ///     Gets the z minimum.
        /// </summary>
        /// <value>The z minimum.</value>
        public double ZMin { get; private set; }

        /// <summary>
        ///     Gets the y minimum.
        /// </summary>
        /// <value>The y minimum.</value>
        public double YMin { get; private set; }

        /// <summary>
        ///     Gets the x minimum.
        /// </summary>
        /// <value>The x minimum.</value>
        public double XMin { get; private set; }

        /// <summary>
        ///     Gets the bounds.
        /// </summary>
        /// <value>The bounds.</value>
        public double[][] Bounds
        {
            get
            {
                var _bounds = new double[2][];
                _bounds[0] = new[] { XMin, YMin, ZMin };
                _bounds[1] = new[] { XMax, YMax, ZMax };
                return _bounds;
            }
        }

        /// <summary>
        ///     Gets the volume.
        /// </summary>
        /// <value>The volume.</value>
        public double Volume { get; internal set; }

        /// <summary>
        ///     Gets and sets the mass.
        /// </summary>
        /// <value>The mass.</value>
        public double Mass { get; set; }

        /// <summary>
        ///     Gets the surface area.
        /// </summary>
        /// <value>The surface area.</value>
        public double SurfaceArea { get; private set; }

        /// <summary>
        ///     The name of solid
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// The comments
        /// </summary>
        public readonly List<string> Comments;

        /// <summary>
        /// The file name
        /// </summary>
        public readonly string FileName;

        /// <summary>
        /// Gets or sets the units.
        /// </summary>
        /// <value>The units.</value>
        public UnitType Units;

        /// <summary>
        /// The language
        /// </summary>
        public readonly string Language;

        /// <summary>
        ///     Gets the faces.
        /// </summary>
        /// <value>The faces.</value>
        public PolygonalFace[] Faces { get; private set; }

        /// <summary>
        ///     Gets the edges.
        /// </summary>
        /// <value>The edges.</value>
        public Edge[] Edges { get; private set; }


        public Edge[] BorderEdges { get; private set; }

        /// <summary>
        ///     Gets the vertices.
        /// </summary>
        /// <value>The vertices.</value>
        public Vertex[] Vertices { get; private set; }

        /// <summary>
        ///     Gets the number of faces.
        /// </summary>
        /// <value>The number of faces.</value>
        public int NumberOfFaces { get; private set; }

        /// <summary>
        ///     Gets the number of vertices.
        /// </summary>
        /// <value>The number of vertices.</value>
        public int NumberOfVertices { get; private set; }

        /// <summary>
        ///     Gets the number of edges.
        /// </summary>
        /// <value>The number of edges.</value>
        public int NumberOfEdges { get; private set; }

        /// <summary>
        ///     Gets the convex hull.
        /// </summary>
        /// <value>The convex hull.</value>
        public TVGLConvexHull ConvexHull { get; private set; }

        /// <summary>
        ///     The has uniform color
        /// </summary>
        public bool HasUniformColor = true;

        /// <summary>
        ///     The has uniform color
        /// </summary>
        public double[,] InertiaTensor
        {
            get
            {
                if (_inertiaTensor == null)
                    _inertiaTensor = DefineInertiaTensor(Faces, Center, Volume);
                return _inertiaTensor;
            }
        }

        internal double[,] _inertiaTensor;

        /// <summary>
        ///     The solid color
        /// </summary>
        public Color SolidColor = new Color(Constants.DefaultColor);

        /// <summary>
        ///     The tolerance is set during the initiation (constructor phase). This is based on the maximum
        ///     length of the axis-aligned bounding box times Constants.
        /// </summary>
        /// <value>The same tolerance.</value>
        internal double SameTolerance { private set; get; }

        /// <summary>
        ///     Errors in the tesselated solid
        /// </summary>
        public TessellationError Errors { get; set; }

        /// <summary>
        ///     Gets or sets the primitive objects that make up the solid
        /// </summary>
        public List<PrimitiveSurface> Primitives { get; private set; }

        #endregion

        #region Constructors

        private TessellatedSolid(UnitType units = UnitType.unspecified, string name = "", string filename = "",
            List<string> comments = null, string language = "")
        {
            //Begin Construction 
            Name = name;
            FileName = filename;
            Comments = new List<string>();
            if (comments != null)
                Comments.AddRange(comments);
            Language = language;
            Units = units;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TessellatedSolid" /> class. This is the one that
        /// matches with the STL format.
        /// </summary>
        /// <param name="normals">The normals.</param>
        /// <param name="vertsPerFace">The verts per face.</param>
        /// <param name="colors">The colors.</param>
        /// <param name="units">The units.</param>
        /// <param name="name">The name.</param>
        /// <param name="filename">The filename.</param>
        /// <param name="comments">The comments.</param>
        /// <param name="language">The language.</param>
        public TessellatedSolid(IList<double[]> normals, IList<List<double[]>> vertsPerFace,
            IList<Color> colors, UnitType units = UnitType.unspecified, string name = "", string filename = "",
            List<string> comments = null, string language = "") : this(units, name, filename, comments, language)
        {
            List<int[]> faceToVertexIndices;
            DefineAxisAlignedBoundingBoxAndTolerance(vertsPerFace.SelectMany(v => v));
            MakeVertices(vertsPerFace, out faceToVertexIndices);
            //Complete Construction with Common Functions
            MakeFaces(faceToVertexIndices, colors, normals);
            CompleteInitiation();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TessellatedSolid" /> class. This matches with formats
        /// that use indices to the vertices (almost everything except STL).
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="faceToVertexIndices">The face to vertex indices.</param>
        /// <param name="colors">The colors.</param>
        /// <param name="units">The units.</param>
        /// <param name="name">The name.</param>
        /// <param name="filename">The filename.</param>
        /// <param name="comments">The comments.</param>
        /// <param name="language">The language.</param>
        public TessellatedSolid(IList<double[]> vertices, IList<int[]> faceToVertexIndices,
            IList<Color> colors, UnitType units = UnitType.unspecified, string name = "", string filename = "",
            List<string> comments = null, string language = "") : this(units, name, filename, comments, language)
        {
            DefineAxisAlignedBoundingBoxAndTolerance(vertices);
            MakeVertices(vertices, ref faceToVertexIndices);
            //Complete Construction with Common Functions
            MakeFaces(faceToVertexIndices, colors);
            CompleteInitiation();
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="TessellatedSolid" /> class. This constructor is
        /// for cases in which the faces and vertices are already defined.
        /// </summary>
        /// <param name="faces">The faces.</param>
        /// <param name="vertices">The vertices.</param>
        /// <param name="colors">The colors.</param>
        /// <param name="units">The units.</param>
        /// <param name="name">The name.</param>
        /// <param name="filename">The filename.</param>
        /// <param name="comments">The comments.</param>
        /// <param name="language">The language.</param>
        public TessellatedSolid(IList<PolygonalFace> faces, IList<Vertex> vertices = null,
            IList<Color> colors = null, UnitType units = UnitType.unspecified, string name = "", string filename = "",
            List<string> comments = null, string language = "") : this(units, name, filename, comments, language)
        {
            if (vertices == null)
                vertices = faces.SelectMany(face => face.Vertices).Distinct().ToList();
            DefineAxisAlignedBoundingBoxAndTolerance(vertices.Select(v => v.Position));
            //Create a copy of the vertex and face (This is NON-Destructive!)

            var newVertices = new List<Vertex>();
            var simpleCompareDict = new Dictionary<Vertex, Vertex>();
            for (var i = 0; i < vertices.Count; i++)
            {
                var vertex = vertices[i].Copy();
                vertex.ReferenceIndex = 0;
                vertex.IndexInList = i;
                newVertices.Add(vertex);
                simpleCompareDict.Add(vertices[i], vertex);
            }
            Vertices = newVertices.ToArray();
            NumberOfVertices = Vertices.Length;

            HasUniformColor = true;
            if (colors == null || !colors.Any())
                SolidColor = new Color(Constants.DefaultColor);
            else SolidColor = colors[0];
            var newFaces = new List<PolygonalFace>();
            for (var i = 0; i < faces.Count; i++)
            {
                //Keep "CreatedInFunction" to help with debug
                var face = faces[i].Copy();
                face.PartofConvexHull = false;
                face.IndexInList = i;
                var faceVertices = new List<Vertex>();
                foreach (var vertex in faces[i].Vertices)
                {
                    var newVertex = simpleCompareDict[vertex];
                    faceVertices.Add(newVertex);
                    newVertex.Faces.Add(face);
                }
                face.Vertices = faceVertices;
                face.Color = SolidColor;
                if (colors != null)
                {
                    var j = i < colors.Count - 1 ? i : colors.Count - 1;
                    face.Color = colors[j];
                    if (!SolidColor.Equals(face.Color)) HasUniformColor = false;
                }
                newFaces.Add(face);
            }
            Faces = newFaces.ToArray();
            NumberOfFaces = Faces.Length;
            CompleteInitiation();
        }

        private void CompleteInitiation()
        {
            List<PolygonalFace> newFaces;
            List<Vertex> removedVertices;
            MakeEdges(out newFaces, out removedVertices);
            AddFaces(newFaces);
            RemoveVertices(removedVertices);
            double[] center;
            double volume;
            double surfaceArea;
            DefineCenterVolumeAndSurfaceArea(Faces, out center, out volume, out surfaceArea);
            Center = center;
            Volume = volume;
            SurfaceArea = surfaceArea;
            foreach (var face in Faces)
                face.DefineFaceCurvature();
            foreach (var v in Vertices)
                v.DefineVertexCurvature();
            TessellationError.CheckModelIntegrity(this);

            //Create convex hull last. After the volume for the solid has found and errors corrected.
            CreateConvexHull(Volume);
        }


        #endregion

        #region Make many elements (called from constructors)

        /// <summary>
        ///     Defines the axis aligned bounding box and tolerance. This is called first in the constructors
        ///     because the tolerance is used in making the vertices.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        private void DefineAxisAlignedBoundingBoxAndTolerance(IEnumerable<double[]> vertices)
        {
            XMin = vertices.Min(v => v[0]);
            XMax = vertices.Max(v => v[0]);
            YMin = vertices.Min(v => v[1]);
            YMax = vertices.Max(v => v[1]);
            ZMin = vertices.Min(v => v[2]);
            ZMax = vertices.Max(v => v[2]);
            var shortestDimension = Math.Min(XMax - XMin, Math.Min(YMax - YMin, ZMax - ZMin));
            SameTolerance = shortestDimension * Constants.BaseTolerance;
        }

        /// <summary>
        /// Makes the faces, avoiding duplicates.
        /// </summary>
        /// <param name="faceToVertexIndices">The face to vertex indices.</param>
        /// <param name="colors">The colors.</param>
        /// <param name="normals">The normals.</param>
        /// <param name="doublyLinkToVertices">if set to <c>true</c> [doubly link to vertices].</param>
        private void MakeFaces(IList<int[]> faceToVertexIndices, IList<Color> colors,
            IList<double[]> normals = null, bool doublyLinkToVertices = true)
        {
            var duplicateFaceCheck = true;
            HasUniformColor = true;
            if (colors == null || !colors.Any())
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
                    faceToVertexIndexList.Select(vertexMatchingIndex => Vertices[vertexMatchingIndex]).ToList();
                bool reverseVertexOrder;
                var normal = PolygonalFace.DetermineNormal(faceVertices, out reverseVertexOrder,
                    normals != null ? normals[i] : null);
                if (reverseVertexOrder) faceVertices.Reverse();

                var color = SolidColor;
                if (colors != null)
                {
                    var j = i < colors.Count - 1 ? i : colors.Count - 1;
                    color = colors[j];
                    if (!SolidColor.Equals(color)) HasUniformColor = false;
                }
                if (faceVertices.Count == 3)
                    listOfFaces.Add(new PolygonalFace(faceVertices, normal, doublyLinkToVertices) { Color = color });
                else
                {
                    List<List<Vertex[]>> triangleFaceList = TriangulatePolygon.Run(new List<List<Vertex>> { faceVertices }, normal);
                    var triangles = triangleFaceList.SelectMany(tl => tl).ToList();
                    foreach (var triangle in triangles)
                    {
                        var v1 = triangle[1].Position.subtract(triangle[0].Position);
                        var v2 = triangle[2].Position.subtract(triangle[0].Position);
                        listOfFaces.Add(v1.crossProduct(v2).dotProduct(normal) < 0
                            ? new PolygonalFace(triangle.Reverse(), normal, doublyLinkToVertices) { Color = color }
                            : new PolygonalFace(triangle, normal, doublyLinkToVertices) { Color = color });
                    }
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
        private void MakeVertices(IEnumerable<List<double[]>> vertsPerFace, out List<int[]> faceToVertexIndices)
        {
            var numDecimalPoints = 0;
            //Gets the number of decimal places, with the maximum being the StarMath Equality (1E-15)
            while (Math.Round(SameTolerance, numDecimalPoints).IsPracticallySame(0.0)) numDecimalPoints++;
            /* vertexMatchingIndices will be used to speed up the linking of faces and edges to vertices
             * it  preserves the order of vertsPerFace (as read in from the file), and indicates where
             * you can find each vertex in the new array of vertices. This is essentially what is built in 
             * the remainder of this method. */
            faceToVertexIndices = new List<int[]>();
            var listOfVertices = new List<double[]>();
            var simpleCompareDict = new Dictionary<string, int>();
            var stringformat = "F" + numDecimalPoints;
            //in order to reduce compare times we use a string comparer and dictionary
            foreach (var t in vertsPerFace)
            {
                var locationIndices = new List<int>(); // this will become a row in faceToVertexIndices
                foreach (var vertex in t)
                {
                    /* given the low precision in files like STL, this should be a sufficient way to detect identical points. 
                     * I believe comparing these lookupStrings will be quicker than comparing two 3d points.*/
                    //First, round the vertices, then convert to a string. This will catch bidirectional tolerancing (+/-)
                    vertex[0] = Math.Round(vertex[0], numDecimalPoints);
                    vertex[1] = Math.Round(vertex[1], numDecimalPoints);
                    vertex[2] = Math.Round(vertex[2], numDecimalPoints);
                    var lookupString = vertex[0].ToString(stringformat) + "|"
                                       + vertex[1].ToString(stringformat) + "|"
                                       + vertex[2].ToString(stringformat) + "|";
                    if (simpleCompareDict.ContainsKey(lookupString))
                        /* if it's in the dictionary, simply put the location in the locationIndices */
                        locationIndices.Add(simpleCompareDict[lookupString]);
                    else
                    {
                        /* else, add a new vertex to the list, and a new entry to simpleCompareDict. Also, be sure to indicate
                        * the position in the locationIndices. */
                        var newIndex = listOfVertices.Count;
                        listOfVertices.Add(vertex);
                        simpleCompareDict.Add(lookupString, newIndex);
                        locationIndices.Add(newIndex);
                    }
                }
                faceToVertexIndices.Add(locationIndices.ToArray());
            }
            //Make vertices from the double arrays
            MakeVertices(listOfVertices);
        }

        /// <summary>
        ///     Makes the vertices.
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="faceToVertexIndices">The face to vertex indices.</param>
        private void MakeVertices(IList<double[]> vertices, ref IList<int[]> faceToVertexIndices)
        {
            var numDecimalPoints = 0;
            //Gets the number og decimal places, with the maximum being the StarMath Equality (1E-15)
            while (Math.Round(SameTolerance, numDecimalPoints).IsPracticallySame(0.0)) numDecimalPoints++;
            var listOfVertices = new List<double[]>();
            var simpleCompareDict = new Dictionary<string, int>();
            var stringformat = "F" + numDecimalPoints;
            //in order to reduce compare times we use a string comparer and dictionary
            foreach (var faceToVertexIndex in faceToVertexIndices)
            {
                for (var i = 0; i < faceToVertexIndex.Length; i++)
                {
                    //Get vertex from un-updated list of vertices
                    var vertex = vertices[faceToVertexIndex[i]];
                    /* given the low precision in files like STL, this should be a sufficient way to detect identical points. 
                     * I believe comparing these lookupStrings will be quicker than comparing two 3d points.*/
                    //First, round the vertices, then convert to a string. This will catch bidirectional tolerancing (+/-)
                    vertex[0] = Math.Round(vertex[0], numDecimalPoints);
                    vertex[1] = Math.Round(vertex[1], numDecimalPoints);
                    vertex[2] = Math.Round(vertex[2], numDecimalPoints);
                    var lookupString = vertex[0].ToString(stringformat) + "|"
                                       + vertex[1].ToString(stringformat) + "|"
                                       + vertex[2].ToString(stringformat) + "|";
                    if (simpleCompareDict.ContainsKey(lookupString))
                    {
                        // if it's in the dictionary, update the faceToVertexIndex
                        faceToVertexIndex[i] = simpleCompareDict[lookupString];
                    }
                    else
                    {
                        /* else, add a new vertex to the list, and a new entry to simpleCompareDict. Also, be sure to indicate
                        * the position in the locationIndices. */
                        var newIndex = listOfVertices.Count;
                        listOfVertices.Add(vertex);
                        simpleCompareDict.Add(lookupString, newIndex);
                        faceToVertexIndex[i] = newIndex;
                    }
                }
            }
            //Make vertices from the double arrays
            MakeVertices(listOfVertices);
        }

        /// <summary>
        ///     Makes the vertices, and set CheckSum multiplier
        /// </summary>
        /// <param name="listOfVertices">The list of vertices.</param>
        private void MakeVertices(IList<double[]> listOfVertices)
        {
            NumberOfVertices = listOfVertices.Count;
            Vertices = new Vertex[NumberOfVertices];
            for (var i = 0; i < NumberOfVertices; i++)
                Vertices[i] = new Vertex(listOfVertices[i], i);
            //Set the checksum
        }

        #endregion


        #region Convex Hull

        /// <summary>
        ///     Creates the convex hull. 
        /// </summary>
        private void CreateConvexHull(double solidVolume)
        {
            if (solidVolume < 0)
                Debug.WriteLine("Correct the face normals (currently inside-out) before calling this function");
            //Take the absolute value of volume, incase the solid is inside out, which will be corrected later.
            ConvexHull = new TVGLConvexHull(Vertices, solidVolume);
            foreach (var cvxHullPt in ConvexHull.Vertices)
                cvxHullPt.PartofConvexHull = true;
            foreach (var face in Faces.Where(face => face.Vertices.All(v => v.PartofConvexHull)))
            {
                face.PartofConvexHull = true;
                foreach (var e in face.Edges)
                    if (e != null) e.PartofConvexHull = true;
            }
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
            foreach (var vertexIndex in removeIndices)
            {
                RemoveReferencesToVertex(Vertices[vertexIndex]);
            }
            var offset = 0;
            var numToRemove = removeIndices.Count;
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
                newEdges[i] = Edges[i];
            newEdges[NumberOfEdges] = newEdge;
            if (newEdge.EdgeReference <= 0) SetAndGetEdgeChecksum(newEdge);
            newEdge.IndexInList = NumberOfEdges;
            Edges = newEdges;
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
            Edges = newEdges;
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
            Edges = newEdges;
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
            Edges = newEdges;
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
            if (Primitives == null) Primitives = new List<PrimitiveSurface>();
            Primitives.Add(p);
        }

        #endregion

        #region Copy Function

        /// <summary>
        ///     Copies this instance.
        /// </summary>
        /// <returns>TessellatedSolid.</returns>
        public TessellatedSolid Copy()
        {
            return new TessellatedSolid(Vertices.Select(vertex => (double[])vertex.Position.Clone()).ToList(),
                Faces.Select(f => f.Vertices.Select(vertex => vertex.IndexInList).ToArray()).ToList(),
                Faces.Select(f => f.Color).ToList(), this.Units, Name + "_Copy",
                FileName, Comments, Language);
        }

        #endregion

        /// <summary>
        ///     Repairs this instance.
        /// </summary>
        /// <returns><c>true</c> if this solid is now free of errors, <c>false</c> if errors remain.</returns>
        public bool Repair()
        {
            if (Errors == null)
            {
                Message.output("No errors to fix!", 4);
                return true;
            }
            var success = Errors.Repair(this);
            if (success) Errors = null;
            return success;
        }


        /// <summary>
        /// Transforms the specified transform matrix.
        /// </summary>
        /// <param name="transformMatrix">The transform matrix.</param>
        public void Transform(double[,] transformMatrix)
        {
            double[] tempCoord;
            foreach (var vert in Vertices)
            {
                tempCoord = transformMatrix.multiply(new[] { vert.X, vert.Y, vert.Z, 1 });
                vert.Position[0] = tempCoord[0];
                vert.Position[1] = tempCoord[1];
                vert.Position[2] = tempCoord[2];
            }
            tempCoord = transformMatrix.multiply(new[] { XMin, YMin, ZMin, 1 });
            XMin = tempCoord[0];
            YMin = tempCoord[1];
            ZMin = tempCoord[2];

            tempCoord = transformMatrix.multiply(new[] { XMax, YMax, ZMax, 1 });
            XMax = tempCoord[0];
            YMax = tempCoord[1];
            ZMax = tempCoord[2];
            Center = transformMatrix.multiply(new[] { Center[0], Center[1], Center[2], 1 });
            // I'm not sure this is right, but I'm just using the 3x3 rotational submatrix to rotate the inertia tensor
            if (_inertiaTensor != null)
            {
                var rotMatrix = new double[3, 3];
                for (int i = 0; i < 3; i++)
                    for (int j = 0; j < 3; j++)
                        rotMatrix[i, j] = transformMatrix[i, j];
                _inertiaTensor = rotMatrix.multiply(_inertiaTensor);
            }
            if (Primitives != null)
                foreach (var primitive in Primitives)
                    primitive.Transform(transformMatrix);
        }
    }
}