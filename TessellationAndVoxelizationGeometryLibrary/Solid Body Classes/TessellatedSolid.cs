// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Matt Campbell
// Created          : 02-27-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 03-07-2015
// ***********************************************************************
// <copyright file="TessellatedSolid.cs" company="">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using MIConvexHull;
using StarMathLib;

namespace TVGL
{
    /// <tags>help</tags>             
    /// <summary>
    ///     Class TessellatedSolid.
    /// </summary>
    /// <remarks>This is the currently the <strong>main</strong> class within TVGL all filetypes are read in as a TessellatedSolid, and
    /// all interesting operations work on the TessellatedSolid.</remarks>
    public class TessellatedSolid
    {
        #region Fields and Properties

        /// <summary>
        ///     The _bounds
        /// </summary>
        private double[][] _bounds;

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
                if (_bounds == null)
                {
                    _bounds = new double[2][];
                    _bounds[0] = new[] { XMin, YMin, ZMin };
                    _bounds[1] = new[] { XMax, YMax, ZMax };
                }
                return
                    _bounds;
            }
        }

        /// <summary>
        ///     Gets the volume.
        /// </summary>
        /// <value>The volume.</value>
        public double Volume { get; private set; }

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
        ///     Gets the faces.
        /// </summary>
        /// <value>The faces.</value>
        public PolygonalFace[] Faces { get; private set; }

        /// <summary>
        ///     Gets the edges.
        /// </summary>
        /// <value>The edges.</value>
        public Edge[] Edges { get; private set; }

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
        ///     Gets the checksum multiplier to be used for face and edge references. Set at end of "Make Vertices".
        /// </summary>
        /// <value>The number of vertices.</value>
        public static int VertexCheckSumMultiplier { get; private set; }

        /// <summary>
        ///     Gets the number of edges.
        /// </summary>
        /// <value>The number of edges.</value>
        public int NumberOfEdges { get; private set; }

        /// <summary>
        ///     Gets the convex hull faces.
        /// </summary>
        /// <value>The convex hull faces.</value>
        public PolygonalFace[] ConvexHullFaces { get; private set; }

        /// <summary>
        ///     Gets whether the convex hull creation was successful.
        /// </summary>
        /// <value>The convex hull faces.</value>
        public bool ConvexHullSuceeded { get; private set; }

        /// <summary>
        ///     Gets the convex hull edges.
        /// </summary>
        /// <value>The convex hull edges.</value>
        public Edge[] ConvexHullEdges { get; private set; }

        /// <summary>
        ///     Gets the convex hull vertices.
        /// </summary>
        /// <value>The convex hull vertices.</value>
        public Vertex[] ConvexHullVertices { get; private set; }

        /// <summary>
        /// The has uniform color
        /// </summary>
        public Boolean HasUniformColor = true;

        /// <summary>
        /// The solid color
        /// </summary>
        public Color SolidColor = new Color(Constants.DefaultColor);

        /// <summary>
        /// The tolerance is set during the initiation (constructor phase). This is based on the maximum
        /// length of the axis-aligned bounding box times Constants.
        /// </summary>
        /// <value>The same tolerance.</value>
        internal double sameTolerance { private set; get; }

        #endregion

        #region Constructors
        /// <summary>
        ///     Prevents a default instance of the <see cref="TessellatedSolid" /> class from being created.
        /// </summary>
        private TessellatedSolid()
        {
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="TessellatedSolid" /> class. This is the one that
        /// matches with the STL format.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="normals">The normals.</param>
        /// <param name="vertsPerFace">The verts per face.</param>
        /// <param name="colors">The colors.</param>
        public TessellatedSolid(string name, List<double[]> normals, List<List<double[]>> vertsPerFace,
            List<Color> colors)
        {
            var now = DateTime.Now;
            //Begin Construction 
            Name = name;
            List<List<int>> faceToVertexIndices;
            DefineAxisAlignedBoundingBoxAndTolerance(vertsPerFace.SelectMany(v => v));
            MakeVertices(vertsPerFace, out faceToVertexIndices);
            //Complete Construction with Common Functions
            MakeFaces(faceToVertexIndices, normals);
            DefineFaceColors(colors);
            CompleteInitiation();

            Debug.WriteLine("File opened in: " + (DateTime.Now - now).ToString());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TessellatedSolid" /> class. This matches with formats
        /// that use indices to the vertices (almost everything except STL).
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="vertices">The vertices.</param>
        /// <param name="faceToVertexIndices">The face to vertex indices.</param>
        /// <param name="colors">The colors.</param>
        public TessellatedSolid(string name, List<double[]> vertices, List<List<int>> faceToVertexIndices,
            List<Color> colors)
        {
            var now = DateTime.Now;
            //Begin Construction 
            Name = name;
            DefineAxisAlignedBoundingBoxAndTolerance(vertices);
            MakeVertices(vertices, ref faceToVertexIndices);
            //Complete Construction with Common Functions
            MakeFaces(faceToVertexIndices);
            DefineFaceColors(colors);
            CompleteInitiation();

            Debug.WriteLine("File opened in: " + (DateTime.Now - now).ToString());
        }


        internal TessellatedSolid(IList<PolygonalFace> faces, IList<Vertex> vertices)
        {
            DefineAxisAlignedBoundingBoxAndTolerance(vertices.Select(v => v.Position));
            Faces = faces.ToArray();
            NumberOfFaces = Faces.Count();
            Vertices = new Vertex[0];
            AddVertices(vertices);
            NumberOfVertices = Vertices.GetLength(0);
            foreach (var face in Faces)
                face.Edges.Clear();
            foreach (var vertex in Vertices)
                vertex.Edges.Clear();
            DefineFaceColors();

            Edges = MakeEdges(Faces);
            CreateConvexHull();
            CompleteInitiation();
        }

        private void CompleteInitiation()
        {
            //1
            CreateConvexHull();
            MakeEdges();
            //2
            //RepairFaces();
            //3
            DefineCenterVolumeAndSurfaceArea();
            //DefineInertiaTensor();
            ConnectConvexHullToObjects();
            DefineFaceCurvature();
            DefineVertexCurvature();
            //3
            CheckModelIntegrity();
        }

        #endregion

        #region Build New from Portions of Old Solid and the Copy Function
        public TessellatedSolid BuildNewFromOld(IList<PolygonalFace> polyFaces)
        {
            var vertices = new HashSet<Vertex>();
            var listDoubles = new List<double[]>();
            var index = 0;
            //Get a list of all the vertices in the new tesselated solid
            foreach (var polyFace in polyFaces)
            {
                foreach (var vertex in polyFace.Vertices)
                {
                    if (!vertices.Contains(vertex))
                    {
                        vertices.Add(vertex);
                        vertex.IndexInList = index;
                        listDoubles.Add(vertex.Position);
                        //listDoubles.Add((double[])vertex.Position.Clone());
                        index++;
                    }
                }
            }
            var faces = new List<List<int>>();
            for (var i = 0; i < polyFaces.Count(); i++)
            {
                var face = new List<int>();
                foreach (var vertex in polyFaces[i].Vertices)
                {
                    face.Add(vertex.IndexInList);
                }
                faces.Add(face);
            }
            return new TessellatedSolid(Name + "_Copy", listDoubles, faces, new List<Color> { SolidColor });
        }

        /// <summary>
        ///     Copies this instance.
        /// </summary>
        /// <returns>TessellatedSolid.</returns>
        public TessellatedSolid Copy()
        {
            var listDoubles = new List<double[]>();
            for (var i = 0; i < Vertices.Count(); i++)
            {
                Vertices[i].IndexInList = i;  //todo: is this line necessary? is it possible that the vertices got out of order?
                listDoubles.Add(Vertices[i].Position);
                //listDoubles.Add((double[])Vertices[i].Position.Clone());
            }
            var faces = new List<List<int>>();
            for (var i = 0; i < Faces.Count(); i++)
            {
                var face = new List<int>();
                foreach (var vertex in Faces[i].Vertices)
                {
                    face.Add(vertex.IndexInList);
                }
                faces.Add(face);
            }
            return new TessellatedSolid(Name + "_Copy", listDoubles, faces, new List<Color> { SolidColor });
        }
        #endregion

        #region Make many elements (called from constructors)

        /// <summary>
        /// Defines the axis aligned bounding box and tolerance. This is called first in the constructors
        /// because the tolerance is used in making the vertices.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        private void DefineAxisAlignedBoundingBoxAndTolerance(IEnumerable<double[]> vertices)
        {
            var taskXMin = Task.Factory.StartNew(() => XMin = vertices.Min(v => v[0]));
            var taskXMax = Task.Factory.StartNew(() => XMax = vertices.Max(v => v[0]));
            var taskYMin = Task.Factory.StartNew(() => YMin = vertices.Min(v => v[1]));
            var taskYMax = Task.Factory.StartNew(() => YMax = vertices.Max(v => v[1]));
            var taskZMin = Task.Factory.StartNew(() => ZMin = vertices.Min(v => v[2]));
            var taskZMax = Task.Factory.StartNew(() => ZMax = vertices.Max(v => v[2]));
            Task.WaitAll(taskXMin, taskXMax, taskYMin, taskYMax, taskZMin, taskZMax);
            var shortestDimension = Math.Min(XMax - XMin, Math.Min(YMax - YMin, ZMax - ZMin));
            sameTolerance = shortestDimension * Constants.BaseTolerance;
        }
        /// <summary>
        /// Makes the faces, avoiding duplicates.
        /// </summary>
        /// <param name="normals">The normals.</param>
        private void MakeFaces(List<List<int>> faceToVertexIndices,
            IList<double[]> normals = null, bool doublyLinkToVertices = true)
        {
            NumberOfFaces = faceToVertexIndices.Count;
            var listOfFaces = new List<PolygonalFace>(NumberOfFaces);
            var faceChecksums = new HashSet<long>();
            //var duplicates = new List<int>();
            var numberOfDegenerate = 0;
            var checksumMultiplier = new long[Constants.MaxNumberEdgesPerFace];
            for (var i = 0; i < Constants.MaxNumberEdgesPerFace; i++)
                checksumMultiplier[i] = (long)Math.Pow(NumberOfVertices, i);
            for (var i = 0; i < NumberOfFaces; i++)
            {
                double[] normal = null;
                var orderedIndices = new List<int>(faceToVertexIndices[i].Select(index => Vertices[index].IndexInList));
                orderedIndices.Sort();
                long checksum = orderedIndices.Select((index, j) => index * checksumMultiplier[j]).Sum();
                if (faceChecksums.Contains(checksum) || orderedIndices.Count < 3
                    || ContainsDuplicateIndices(orderedIndices))
                {
                    //duplicates.Add(i);
                    numberOfDegenerate++;
                    continue;
                }
                //Get the actual vertices to create a new face. 
                faceChecksums.Add(checksum);
                var faceVertices = new List<Vertex>();
                foreach (var vertexMatchingIndex in faceToVertexIndices[i])
                {
                    faceVertices.Add(Vertices[vertexMatchingIndex]);
                }
                //Get the normal, if it was given.
                if (normals != null) normal = normals[i];
                listOfFaces.Add(new PolygonalFace(faceVertices, normal, doublyLinkToVertices));
            }
            Faces = listOfFaces.ToArray();
            NumberOfFaces = Faces.GetLength(0);
            if (numberOfDegenerate > 0)
                Debug.WriteLine("{0} number of degenerate or duplicate faces found in the the file.", numberOfDegenerate);
        }

        internal static bool ContainsDuplicateIndices(List<int> orderedIndices)
        {
            for (var i = 0; i < orderedIndices.Count - 1; i++)
                if (orderedIndices[i] == orderedIndices[i + 1]) return true;
            return false;
        }

        private void MakeEdges()
        {
            Edges = MakeEdges(Faces);
        }

        private Edge[] MakeEdges(PolygonalFace[] localFaces)
        {
            NumberOfEdges = 3 * NumberOfFaces / 2;
            var localEdges = MakeEdges(localFaces, true);
            NumberOfEdges = localEdges.GetLength(0);
            return localEdges;
        }

        private Edge[] MakeEdges(IList<PolygonalFace> faces, bool doublyLinkToVertices)
        {
            var partlyDefinedEdges = new Dictionary<int, Edge>();
            var alreadyDefinedEdges = new Dictionary<int, Edge>();
            var overUsedEdges = new Dictionary<int, Tuple<Edge, List<PolygonalFace>>>();
            foreach (var face in faces)
            {
                var lastIndex = face.Vertices.Count - 1;
                for (var j = 0; j <= lastIndex; j++)
                {
                    #region get the edge CheckSum value
                    var fromVertex = face.Vertices[j];
                    var toVertex = face.Vertices[(j == lastIndex) ? 0 : j + 1];
                    var fromIndex = fromVertex.IndexInList;
                    var toIndex = toVertex.IndexInList;
                    if (fromIndex == toIndex) throw new Exception("edge to same vertices.");
                    var checksum = (fromIndex < toIndex)
                            ? fromIndex + (NumberOfVertices * toIndex)
                            : toIndex + (NumberOfVertices * fromIndex);
                    #endregion

                    if (overUsedEdges.ContainsKey(checksum))
                    {
                        overUsedEdges[checksum].Item2.Add(face);
                    }
                    else if (alreadyDefinedEdges.ContainsKey(checksum))
                    {
                        var edge = alreadyDefinedEdges[checksum];
                        var facesConnectedToEdge = new List<PolygonalFace> { edge.OwnedFace, edge.OtherFace, face };
                        overUsedEdges.Add(checksum, new Tuple<Edge, List<PolygonalFace>>(edge, facesConnectedToEdge));
                        alreadyDefinedEdges.Remove(checksum);
                    }
                    else if (partlyDefinedEdges.ContainsKey(checksum))
                    {
                        //Finish creating edge.
                        var edge = partlyDefinedEdges[checksum];
                        if (face.Area.IsNegligible(sameTolerance))
                            face.Normal = (double[])edge.OwnedFace.Normal.Clone();
                        if (edge.OwnedFace.Area.IsNegligible(sameTolerance))
                            edge.OwnedFace.Normal = (double[])face.Normal.Clone();
                        edge.OtherFace = face;
                        face.Edges.Add(edge);
                        alreadyDefinedEdges.Add(checksum, edge);
                        partlyDefinedEdges.Remove(checksum);
                    }
                    else // this edge doesn't already exist.
                    {
                        var edge = new Edge(fromVertex, toVertex, face, null, doublyLinkToVertices, checksum);
                        partlyDefinedEdges.Add(checksum, edge);
                    }
                }
            }
            if (overUsedEdges.Count > 0)
            //foreach (var overUsedEdgeTuple in overUsedEdges)
            {
                //overUsedEdgeTuple.Value.Item1;
                Debug.WriteLine("{0} overused edges found. This is the number of faces per: ", overUsedEdges.Count);
                Debug.WriteLine(overUsedEdges.Values.Select(p => p.Item2.Count).MakePrintString());
            }
            if (partlyDefinedEdges.Count > 0)
            {
                //There is a chance, one or more faces is just missing. This can be repaired if the bad edges
                //form a loop around the missing section, if the missing section if reletively flat.
                RepairMissingFacesFromEdges(ref partlyDefinedEdges, ref alreadyDefinedEdges, doublyLinkToVertices);
                if (partlyDefinedEdges.Count > 0)
                {
                    foreach (var badEdge in partlyDefinedEdges.Values)
                    {
                        Debug.WriteLine("Edge found with only face. Edge Reference: " + badEdge.EdgeReference);
                    }
                    // throw new Exception();
                }
            }
            return alreadyDefinedEdges.Values.ToArray();
        }

        /// <summary>
        /// Makes the vertices.
        /// </summary>
        /// <param name="vertsPerFace">The verts per face.</param>
        /// <param name="faceToVertexIndices">The face to vertex indices.</param>
        /// <param name="indicesToRemove">The indices to remove.</param>
        private void MakeVertices(ICollection<List<double[]>> vertsPerFace, out List<List<int>> faceToVertexIndices)
        {
            var numDecimalPoints = 0;
            while (Math.Round(sameTolerance, numDecimalPoints) == 0.0) numDecimalPoints++;
            numDecimalPoints++;
            /* vertexMatchingIndices will be used to speed up the linking of faces and edges to vertices
             * it  preserves the order of vertsPerFace (as read in from the file), and indicates where
             * you can find each vertex in the new array of vertices. This is essentially what is built in 
             * the remainder of this method. */
            faceToVertexIndices = new List<List<int>>();
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
                faceToVertexIndices.Add(locationIndices);
            }
            //Make vertices from the double arrays
            MakeVertices(listOfVertices);
        }

        /// <summary>
        /// Makes the vertices.
        /// </summary>
        /// <param name="vertsPerFace">The verts per face.</param>
        /// <param name="faceToVertexIndices">The face to vertex indices.</param>
        /// <param name="indicesToRemove">The indices to remove.</param>
        private void MakeVertices(IList<double[]> vertices, ref List<List<int>> faceToVertexIndices)
        {
            var numDecimalPoints = 0;
            while (Math.Round(sameTolerance, numDecimalPoints) == 0.0) numDecimalPoints++;
            numDecimalPoints++;
            var listOfVertices = new List<double[]>();
            var simpleCompareDict = new Dictionary<string, int>();
            var stringformat = "F" + numDecimalPoints;
            //in order to reduce compare times we use a string comparer and dictionary
            foreach (var faceToVertexIndex in faceToVertexIndices)
            {
                for (var i = 0; i < faceToVertexIndex.Count; i++)
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
        }
        #endregion

        #region Define Additional Characteristics of Faces, Edges and Vertices
        /// <summary>
        ///     Defines the face centers.
        /// </summary>
        private void DefineFaceColors(List<Color> colors = null)
        {
            HasUniformColor = true;
            if (colors == null) SolidColor = new Color(Constants.DefaultColor);
            else if (colors.Count == 1) SolidColor = colors[0];
            else HasUniformColor = false;
            for (int i = 0; i < Faces.Length; i++)
            {
                var face = Faces[i];
                if (face.color != null && !face.color.Equals(SolidColor)) HasUniformColor = false;
                else if (!HasUniformColor)
                {
                    if (colors == null || !colors.Any()) face.color = SolidColor;
                    else
                    {
                        var j = (i < colors.Count - 1) ? i : colors.Count - 1;
                        face.color = colors[j];
                    }
                }
            }
        }

        /// <summary>
        /// Defines the center, the volume and the surface area.
        /// </summary>
        private void DefineCenterVolumeAndSurfaceArea()
        {
            Volume = 0;
            SurfaceArea = 0;
            double centerX = 0;
            double centerY = 0;
            double centerZ = 0;
            foreach (var face in Faces)
            {
                // assuming triangular faces: the area is half the magnitude of the cross product of two of the edges
                if (face.Area.IsNegligible()) face.Area = face.DetermineArea(); //the area of the face was also determined in 
                // one of the PolygonalFace constructors. In case it is zero, we will recalculate it here.
                SurfaceArea += face.Area;   // accumulate areas into surface area
                var tetrahedronVolume = face.Area * (face.Normal.dotProduct(face.Vertices[0].Position)) / 3;
                // this is the volume of a tetrahedron from defined by the face and the origin {0,0,0}. The origin would be part of the second term
                // in the dotproduct, "face.Normal.dotProduct(face.Vertices[0].Position.subtract(ORIGIN))", but clearly there is no need to subtract
                // {0,0,0}. Note that the volume of the tetrahedron could be negative. This is fine as it ensures that the origin has no influence
                // on the volume.
                Volume += tetrahedronVolume;
                centerX += (face.Vertices[0].Position[0] + face.Vertices[1].Position[0] + face.Vertices[2].Position[0]) * tetrahedronVolume / 4;
                centerY += (face.Vertices[0].Position[1] + face.Vertices[1].Position[1] + face.Vertices[2].Position[1]) * tetrahedronVolume / 4;
                centerZ += (face.Vertices[0].Position[2] + face.Vertices[1].Position[2] + face.Vertices[2].Position[2]) * tetrahedronVolume / 4;
                // center is found by a weighted sum of the centers of each tetrahedron. The weighted sum coordinate are collected here.
            }
            Center = new[] { centerX / Volume, centerY / Volume, centerZ / Volume };

            // theoretically, we are done, but practically there appears to be cases in which the volume can be off by a bit. This is happening as a result
            // of long tetrahedra. In order to combat this the volume is done again about the newly found center and averaged with the volume found above.
            foreach (var face in Faces)
            {
                var tetrahedronVolume = face.Area * (face.Normal.dotProduct(face.Vertices[0].Position.subtract(Center))) / 3;
                Volume += tetrahedronVolume;
            }
            Volume /= 2.0;
        }


        private void DefineInertiaTensor()
        {
            double tempProductX = 0;
            double tempProductY = 0;
            double tempProductZ = 0;
            Center = StarMath.makeZeroVector(3);
            double[,] inertiaTensor = StarMath.makeZero(3, 3);
            double[,] translateMatrix = new double[3, 1];
            double[,] matrixA = StarMath.makeZero(3, 3);
            double[,] matrixC = StarMath.makeZero(3, 3);
            double[,] matrixCtotal = StarMath.makeZero(3, 3);
            double[,] matrixCprime = StarMath.makeZero(3, 3);
            double[,] canonicalMatrix = new double[,] { { 0.0166, 0.0083, 0.0083 }, { 0.0083, 0.0166, 0.0083 }, { 0.0083, 0.0083, 0.0166 } };
            foreach (var face in Faces)
            {

                matrixA.SetRow(0, new[] { face.Vertices[0].Position[0] - Center[0], face.Vertices[0].Position[1] - Center[1], face.Vertices[0].Position[2] - Center[2] });
                matrixA.SetRow(1, new[] { face.Vertices[1].Position[0] - Center[0], face.Vertices[1].Position[1] - Center[1], face.Vertices[1].Position[2] - Center[2] });
                matrixA.SetRow(2, new[] { face.Vertices[2].Position[0] - Center[0], face.Vertices[2].Position[1] - Center[1], face.Vertices[2].Position[2] - Center[2] });

                matrixC = StarMath.multiply(matrixA, canonicalMatrix);
                matrixC = StarMath.multiply(matrixC, matrixA.transpose()).multiply(matrixA.determinant());
                matrixCtotal = matrixCtotal.add(matrixC);

            }

            translateMatrix = new double[,] { { this.Center[0] }, { this.Center[1] }, { this.Center[2] } };
            matrixCprime = (StarMath.multiply(translateMatrix, translateMatrix.transpose())).multiply(Volume).multiply(3).add(matrixCtotal);
            inertiaTensor = StarMath.makeIdentity(3).multiply(matrixCprime[0, 0] + matrixCprime[1, 1] + matrixCprime[2, 2]).subtract(matrixCprime);

        }
        #endregion      

        #region Curvatures
        /// <summary>
        ///     Defines the vertex curvature.
        /// </summary>
        private void DefineVertexCurvature()
        {
            foreach (var v in Vertices)
            {
                v.DefineVertexCurvature();
            }
        }

        /// <summary>
        ///     Defines the face curvature. Depends on DefineEdgeAngle
        /// </summary>
        private void DefineFaceCurvature()
        {
            foreach (var face in Faces)
            {
                face.DefineFaceCurvature();
            }
        }
        #endregion

        #region Convex Hull
        /// <summary>
        ///     Connects the convex hull to objects.
        /// </summary>
        private void ConnectConvexHullToObjects()
        {
            foreach (var cvxHullPt in ConvexHullVertices)
                cvxHullPt.PartofConvexHull = true;
            foreach (var face in Faces.Where(face => face.Vertices.All(v => v.PartofConvexHull)))
            {
                face.PartofConvexHull = true;
                foreach (var e in face.Edges)
                    e.PartofConvexHull = true;
            }
        }

        /// <summary>
        ///     Creates the convex hull.
        /// </summary>
        private void CreateConvexHull()
        {
            var config = new ConvexHullComputationConfig
            {
                PointTranslationType = PointTranslationType.TranslateInternal,
                PlaneDistanceTolerance = 0.000001,
                // the translation radius should be lower than PlaneDistanceTolerance / 2
                PointTranslationGenerator = ConvexHullComputationConfig.RandomShiftByRadius(0.0000001, 0)
            };
            var convexHull = ConvexHull.Create(Vertices, config);
            //var convexHull = ConvexHull.Create<Vertex, DefaultConvexFace<Vertex>>(Vertices);
            ConvexHullVertices = convexHull.Points.ToArray();
            var numCvxFaces = convexHull.Faces.Count();
            if (numCvxFaces < 3)
            {
                ConvexHullSuceeded = false;
                return;
            }
            var convexHullFaceList = new List<PolygonalFace>();
            var checkSumMultipliers = new long[Constants.MaxNumberEdgesPerFace];
            for (var i = 0; i < Constants.MaxNumberEdgesPerFace; i++)
                checkSumMultipliers[i] = (long)Math.Pow(NumberOfVertices, i);
            var alreadyCreatedFaces = new HashSet<long>();
            foreach (var cvxFace in convexHull.Faces)
            {
                var vertices = cvxFace.Vertices;
                var orderedIndices = vertices.Select(v => v.IndexInList).ToList();
                orderedIndices.Sort();
                long checksum = orderedIndices.Select((t, j) => t * checkSumMultipliers[j]).Sum();
                if (alreadyCreatedFaces.Contains(checksum)) continue;
                alreadyCreatedFaces.Add(checksum);
                convexHullFaceList.Add(new PolygonalFace(vertices, cvxFace.Normal, false));
            }
            ConvexHullFaces = convexHullFaceList.ToArray(); //Now, convert to an array.
            ConvexHullEdges = MakeEdges(ConvexHullFaces, false);
            ConvexHullSuceeded = true;
        }
        #endregion

        #region Add or Remove Items
        #region Vertices - the important thing about these is updating the IndexInList property of the vertices
        internal void AddVertex(Vertex newVertex)
        {
            var newVertices = new Vertex[NumberOfVertices + 1];
            for (int i = 0; i < NumberOfVertices; i++)
                newVertices[i] = Vertices[i];
            newVertices[NumberOfVertices] = newVertex;
            newVertex.IndexInList = NumberOfVertices;
            Vertices = newVertices;
            NumberOfVertices++;
        }
        internal void AddVertices(IList<Vertex> verticesToAdd)
        {
            var numToAdd = verticesToAdd.Count();
            var newVertices = new Vertex[NumberOfVertices + numToAdd];
            for (int i = 0; i < NumberOfVertices; i++)
                newVertices[i] = Vertices[i];
            for (int i = 0; i < numToAdd; i++)
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
            for (int i = 0; i < removeVIndex; i++)
                newVertices[i] = Vertices[i];
            for (int i = removeVIndex; i < NumberOfVertices; i++)
            {
                var v = Vertices[i + 1];
                v.IndexInList--;
                newVertices[i] = v;
            }
            Vertices = newVertices;
            UpdateAllFaceAndEdgeReferenceValues();
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
            for (int i = 0; i < NumberOfVertices; i++)
            {
                while (offset < numToRemove && (i + offset) == removeIndices[offset])
                    offset++;
                var v = Vertices[i + offset];
                v.IndexInList = i;
                newVertices[i] = v;
            }
            Vertices = newVertices;
            UpdateAllFaceAndEdgeReferenceValues();
        }

        private void RemoveReferencesToVertex(Vertex vertex)
        {
            foreach (var face in vertex.Faces)
            {
                var index = face.Vertices.IndexOf(vertex);
                if (index >= 0) face.Vertices.RemoveAt(index);
            }
            foreach (var edge in vertex.Edges)
            {
                if (vertex == edge.To) edge.To = null;
                if (vertex == edge.From) edge.From = null;
            }
        }

        private void UpdateAllFaceAndEdgeReferenceValues()
        {
            foreach (var edge in Edges)
            {
                edge.SetEdgeReference();
            }
        }
        #endregion
        #region Faces
        internal void AddFace(PolygonalFace newFace)
        {
            var newFaces = new PolygonalFace[NumberOfFaces + 1];
            for (int i = 0; i < NumberOfFaces; i++)
                newFaces[i] = Faces[i];
            newFaces[NumberOfFaces] = newFace;
            Faces = newFaces;
            NumberOfFaces++;
        }
        internal void AddFaces(IList<PolygonalFace> facesToAdd)
        {
            var numToAdd = facesToAdd.Count();
            var newFaces = new PolygonalFace[NumberOfFaces + numToAdd];
            for (int i = 0; i < NumberOfFaces; i++)
                newFaces[i] = Faces[i];
            for (int i = 0; i < numToAdd; i++)
                newFaces[NumberOfFaces + i] = facesToAdd[i];
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
            for (int i = 0; i < removeFaceIndex; i++)
                newFaces[i] = Faces[i];
            for (int i = removeFaceIndex; i < NumberOfFaces; i++)
                newFaces[i] = Faces[i + 1];
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
            for (int i = 0; i < NumberOfFaces; i++)
            {
                while (offset < numToRemove && (i + offset) == removeIndices[offset])
                    offset++;
                newFaces[i] = Faces[i + offset];
            }
            Faces = newFaces;
        }

        private void RemoveReferencesToFace(int removeFaceIndex)
        {
            var face = Faces[removeFaceIndex];
            foreach (var vertex in face.Vertices)
            {
                var index = vertex.Faces.IndexOf(face);
                if (index >= 0) vertex.Faces.RemoveAt(index);
            }
            foreach (var edge in face.Edges)
            {
                if (face == edge.OwnedFace) edge.OwnedFace = null;
                if (face == edge.OtherFace) edge.OwnedFace = null;
            }
            //Face adjacency is a method call, not an object reference. So it updates automatically.
        }
        #endregion
        #region Edges
        internal void AddEdge(Edge newEdge)
        {
            var newEdges = new Edge[NumberOfEdges + 1];
            for (int i = 0; i < NumberOfEdges; i++)
                newEdges[i] = Edges[i];
            newEdges[NumberOfEdges] = newEdge;
            Edges = newEdges;
            NumberOfEdges++;
        }
        internal void AddEdges(IList<Edge> edgesToAdd)
        {
            var numToAdd = edgesToAdd.Count();
            var newEdges = new Edge[NumberOfEdges + numToAdd];
            for (int i = 0; i < NumberOfEdges; i++)
                newEdges[i] = Edges[i];
            for (int i = 0; i < numToAdd; i++)
                newEdges[NumberOfEdges + i] = edgesToAdd[i];
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
            for (int i = 0; i < removeEdgeIndex; i++)
                newEdges[i] = Edges[i];
            for (int i = removeEdgeIndex; i < NumberOfEdges; i++)
                newEdges[i] = Edges[i + 1];
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
            for (int i = 0; i < NumberOfEdges; i++)
            {
                while (offset < numToRemove && (i + offset) == removeIndices[offset])
                    offset++;
                newEdges[i] = Edges[i + offset];
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
        #endregion

        #region Check References
        public void CheckModelIntegrity()
        {
            Debug.WriteLine("=============Model Integrity Check=============");
            //Check if each face has cyclic references with each edge, vertex, and adjacent faces.
            var edgeDoesNotLinkBackToFace = 0;
            var faceHasTwoEdges = 0;
            var faceHasOneEdge = 0;
            var faceHasTwoVerts = 0;
            var faceHasOneVert = 0;
            var vertDoesNotLinkBackToFace = 0;
            var faceAreaNegligble = 0;
            foreach (var face in Faces)
            {
                if (face.Edges.Count == 1) faceHasOneEdge++;
                if (face.Edges.Count == 2) faceHasTwoEdges++;
                if (face.Vertices.Count == 1) faceHasOneVert++;
                if (face.Vertices.Count == 2) faceHasTwoVerts++;
                if (face.Area.IsNegligible(sameTolerance)) faceAreaNegligble++;
                edgeDoesNotLinkBackToFace += face.Edges.Count(edge => edge.OwnedFace != face && edge.OtherFace != face);
                vertDoesNotLinkBackToFace += face.Vertices.Count(vertex => !vertex.Faces.Contains(face));
            }
            //Check if each edge has cyclic references with each vertex and each face.
            var faceDoesNotLinkBackToEdge = 0;
            var vertDoesNotLinkBackToEdge = 0;
            var edgeBadAngle = 0;
            foreach (var edge in Edges)
            {

                if (!edge.OwnedFace.Edges.Contains(edge)) faceDoesNotLinkBackToEdge++;
                if (!edge.OtherFace.Edges.Contains(edge)) faceDoesNotLinkBackToEdge++;
                if (!edge.To.Edges.Contains(edge)) vertDoesNotLinkBackToEdge++;
                if (!edge.From.Edges.Contains(edge)) vertDoesNotLinkBackToEdge++;
                if (double.IsNaN(edge.InternalAngle) || edge.InternalAngle.IsNegligible(sameTolerance)
                    || edge.InternalAngle > 2*Math.PI) edgeBadAngle++;
            }
            //Check if each vertex has cyclic references with each edge and each face.
            var faceDoesNotLinkBackToVertex = 0;
            var edgeDoesNotLinkBackToVertex = 0;

            foreach (var vertex in Vertices)
            {
                edgeDoesNotLinkBackToVertex += vertex.Edges.Count(edge => edge.To != vertex && edge.From != vertex);
                faceDoesNotLinkBackToVertex += vertex.Faces.Count(face => !face.Vertices.Contains(vertex));
            }
            if (edgeDoesNotLinkBackToFace > 0) Debug.WriteLine("==> {0} edges do not link back to face.", edgeDoesNotLinkBackToFace);
            if (vertDoesNotLinkBackToFace > 0) Debug.WriteLine("==> {0} vertices do not link back to face.", vertDoesNotLinkBackToFace);
            if (faceAreaNegligble>0) Debug.WriteLine("==> {0} faces have negligible area.", faceAreaNegligble);
            if (faceHasOneVert > 0) Debug.WriteLine("==> {0} faces only have one vertex.", faceHasOneVert);
            if (faceHasTwoVerts > 0) Debug.WriteLine("==> {0} faces only have two vertices.", faceHasTwoVerts);
            if (faceHasOneEdge > 0) Debug.WriteLine("==> {0} faces only have one edge.", faceHasOneEdge);
            if (faceHasTwoEdges > 0) Debug.WriteLine("==> {0} faces only have two edges.", faceHasTwoEdges);
            if (edgeBadAngle > 0) Debug.WriteLine("==> {0} edges have a bad angle (0,2pi or Nan).", edgeBadAngle);
            if (faceDoesNotLinkBackToEdge > 0) Debug.WriteLine("==> {0} faces do not link back to edge.", faceDoesNotLinkBackToEdge);
            if (vertDoesNotLinkBackToEdge > 0) Debug.WriteLine("==> {0} vertices do not link back to edge.", vertDoesNotLinkBackToEdge);
            if (faceDoesNotLinkBackToVertex > 0) Debug.WriteLine("==> {0} faces do not link back to vertex.", faceDoesNotLinkBackToVertex);
            if (edgeDoesNotLinkBackToVertex > 0) Debug.WriteLine("==> {0} vertices do not link back to vertex.", edgeDoesNotLinkBackToVertex);
            Debug.WriteLine("==> complete.=============");
        }
        #endregion

        #region Repair Functions
        //This function repairs all the negligible area faces in the solid. 
        //For each negligible triangle, the longest edge and smallest edge are found.
        //The triangle is then collapsed to the vertex that both those edges share.
        //Note that this removes 2 triangles, 3 edges, and 1 vertex from the model.
        //The new triangles, will either be flattened (if they had different normals)
        //OR, they will simply be merged. 
        public void RepairFaces()
        {
            // var negligible = 0.00000001;
            bool completed;
            var badFaceCount = 0;
            do
            {
                completed = true;
                var negligibleFace = Faces.FirstOrDefault(f => f.Area.IsNegligible(sameTolerance));
                if (negligibleFace != null)
                {
                    var splittingEdge = negligibleFace.Edges.OrderByDescending(item => item.Length).First();
                    completed = false;
                    badFaceCount++;

                    //Get the vertex opposite the splitting edge
                    var otherVertex = negligibleFace.OtherVertex(splittingEdge);
                    //Get the faces to be removed
                    var removeTheseFaces = new List<PolygonalFace>(otherVertex.Faces);
                    //Get the edges to be removed.
                    var removeTheseEdges = new List<Edge>(otherVertex.Edges);

                    //Use the same vertex which already exists, rather that creating a new one.
                    //This method will simplify the model by 2 triangles.
                    Vertex collapseToVertex = null;
                    var newEdges = new List<Edge>();
                    var newFaces = new List<PolygonalFace>();
                    var negligibleEdge = negligibleFace.Edges.OrderBy(item => item.Length).First(); //Get the smallest edge of the negligible face.
                    if (negligibleEdge.To == otherVertex) collapseToVertex = negligibleEdge.From;
                    else collapseToVertex = negligibleEdge.To;


                    //Get the outer edges and faces
                    var allAffectedEdges = new List<Edge>();
                    var allAffectedFaces = new List<PolygonalFace>();
                    foreach (var face in removeTheseFaces)
                    {
                        foreach (var edge in face.Edges)
                        {
                            if (removeTheseEdges.Contains(edge)) continue; //don't need to do anything, since it will be removed.
                            allAffectedEdges.Add(edge); //Add this outer edge
                            if (edge.OwnedFace == face) allAffectedFaces.Add(edge.OtherFace); //Add the other face (its edges will need to be updated)
                            else allAffectedFaces.Add(edge.OwnedFace); //Add the owned face (its edges will need to be updated)
                        }
                    }

                    //Find the second face to be collapsed and the third vertices from the two faces to be collapsed
                    PolygonalFace secondFace = null;
                    foreach (var face in removeTheseFaces)
                    {
                        if (face == negligibleFace) continue; //This also meets the criteria below, but we don't want this face.
                        if (!face.Vertices.Contains(collapseToVertex)) continue;
                        //If it already contains the new vertex, then it must be the secondFace we are going to collapse
                        if (secondFace != null) throw new Exception("this condition can only happen once");
                        secondFace = face;
                    }
                    if (secondFace == null) throw new Exception("this face must be set");
                    var thirdVertexOfSecondFace = negligibleFace.OtherVertex(negligibleEdge);
                    var thirdVertexOfNegligibleFace = secondFace.OtherVertex(negligibleEdge);

                    //Create new faces to replace the ones being removed.
                    foreach (var face in removeTheseFaces)
                    {
                        if (face == negligibleFace || face == secondFace) continue; //We don't need to do anything with these faces for now.
                        //Replace the vertices from this face. Keep the normal, or add guess bool. normalIsGuess.
                        var newFaceVertexList = new List<Vertex>(face.Vertices);
                        newFaceVertexList.Remove(otherVertex);
                        newFaceVertexList.Add(collapseToVertex);
                        PolygonalFace newFace;
                        newFace = new PolygonalFace(newFaceVertexList, face.Normal);
                        newFaces.Add(newFace);
                    }
                    allAffectedFaces.AddRange(newFaces);

                    //Create new edges to replace the ones being removed. 
                    foreach (var edge in removeTheseEdges)
                    {
                        if (edge == negligibleEdge) continue;  //We don't need this edge
                        if ((edge.To == otherVertex && edge.From == thirdVertexOfNegligibleFace || edge.From == thirdVertexOfSecondFace)) continue; //We don't need this edge
                        if ((edge.From == otherVertex && edge.To == thirdVertexOfNegligibleFace || edge.To == thirdVertexOfSecondFace)) continue; //We don't need this edge
                        if (otherVertex == edge.From) newEdges.Add(new Edge(collapseToVertex, edge.OtherVertex(otherVertex), true));
                        else newEdges.Add(new Edge(edge.OtherVertex(otherVertex), collapseToVertex, true));
                    }
                    allAffectedEdges.AddRange(newEdges);

                    //Set the owned and other faces of the affected edges. All new edges's faces are in newFaces
                    foreach (var edge in allAffectedEdges)
                    {
                        //Reset the owned and other.
                        edge.OwnedFace = null;
                        edge.OtherFace = null;
                        foreach (var face in allAffectedFaces)
                        {
                            if (face.Vertices.Contains(edge.To) && face.Vertices.Contains(edge.From))
                            {
                                if (edge.OwnedFace == null) edge.OwnedFace = face;
                                else if (edge.OtherFace == null) edge.OtherFace = face;
                                else if (edge.OwnedFace == edge.OtherFace) edge.OtherFace = face; //A mistake was made. Fix it.
                                else throw new Exception();
                            }
                        }
                        //Update the edge list for all the faces     
                        if (!edge.OwnedFace.Edges.Contains(edge)) edge.OwnedFace.Edges.Add(edge);
                        if (!edge.OtherFace.Edges.Contains(edge)) edge.OtherFace.Edges.Add(edge);
                    }

                    //Brief check to make sure everything was set properly
                    foreach (var edge in allAffectedEdges)
                    {
                        if (edge.OwnedFace == null || edge.OtherFace == null) throw new Exception();
                        if (edge.OwnedFace == edge.OtherFace) throw new Exception();
                    }
                    foreach (var face in newFaces)
                    {
                        if (face.Edges.Count() != 3 || face.Vertices.Count() != 3) throw new Exception();
                    }

                    //Remove and then add all the new faces and edges.
                    //The remove functions also remove any circular reference back to the face or edge.
                    RemoveFaces(removeTheseFaces);
                    RemoveEdges(removeTheseEdges);
                    AddEdges(newEdges);
                    AddFaces(newFaces);

                    //Check the normals of the new faces.
                    foreach (var face in newFaces)
                    {
                        foreach (var adjacentFace in face.AdjacentFaces)
                        {
                            if (!adjacentFace.AdjacentFaces.Contains(face)) throw new Exception();
                            if (face.Normal.dotProduct(adjacentFace.Normal).IsPracticallySame(-1.0)) throw new Exception();
                        }
                    }

                    //lastly, remove the unused vertex and re-update all the edge and face references,
                    //since we removed a vertex at the start of this function.
                    RemoveVertex(otherVertex, true);
                }
            }
            //badFaceCount is used because this function may be unstable if many bad faces exist.
            while (completed == false && badFaceCount < 100);
            if (badFaceCount == 100) throw new Exception();
            if (badFaceCount == 1) Debug.WriteLine("1 negligible area face has been fixed.");
            else if (badFaceCount > 1) Debug.WriteLine(badFaceCount + " negligible area faces have been fixed.");
        }

        private void RepairMissingFacesFromEdges(ref Dictionary<int, Edge> partlyDefinedEdges, ref Dictionary<int, Edge> alreadyDefinedEdges, bool doublyLinkToVertices)
        {
            var newFaces = new List<PolygonalFace>();
            var loops = new List<List<Vertex>>();
            var loopNormals = new List<double[]>();
            var attempts = 0;
            var remainingEdges = partlyDefinedEdges.Values.ToList();
            while (remainingEdges.Count > 0 && attempts < remainingEdges.Count)
            {
                var loop = new List<Vertex>();
                var successfull = true;
                var removedEdges = new List<Edge>();
                var remainingEdge = remainingEdges[0];
                var startVertex = remainingEdge.From;
                var newStartVertex = remainingEdge.To;
                var normal = remainingEdge.OwnedFace.Normal;
                loop.Add(newStartVertex);
                removedEdges.Add(remainingEdge);
                remainingEdges.RemoveAt(0);
                do
                {
                    var possibleNextEdges = remainingEdges.Where(e => e.To == newStartVertex || e.From == newStartVertex).ToList();
                    if (possibleNextEdges.Count() != 1) successfull = false;
                    else
                    {
                        var currentEdge = possibleNextEdges[0];
                        normal = normal.multiply(loop.Count).add(currentEdge.OwnedFace.Normal).divide(loop.Count + 1);
                        normal.normalizeInPlace();
                        newStartVertex = currentEdge.OtherVertex(newStartVertex);
                        loop.Add(newStartVertex);
                        removedEdges.Add(currentEdge);
                        remainingEdges.Remove(currentEdge);
                    }
                }
                while (newStartVertex != startVertex && successfull);
                if (successfull)
                {
                    //Average the normals from all the owned faces.
                    loopNormals.Add(normal);
                    loops.Add(loop);
                    attempts = 0;
                }
                else
                {
                    remainingEdges.AddRange(removedEdges);
                    attempts++;
                }
            }

            for (var i = 0; i < loops.Count; i++)
            {
                //if a simple triangle, create a new face from vertices
                if (loops[i].Count == 3)
                {
                    var newFace = new PolygonalFace(loops[i], loopNormals[i], doublyLinkToVertices);
                    newFaces.Add(newFace);
                }
                //Else, use the triangulate function
                else if (loops[i].Count > 3)
                {
                    //First, get an average normal from all vertices, assuming CCW order.
                    var triangles = TriangulatePolygon.Run(new List<List<Vertex>> { loops[i] }, loopNormals[i]);
                    foreach (var triangle in triangles)
                    {
                        var newFace = new PolygonalFace(triangle, loopNormals[i], doublyLinkToVertices);
                        newFaces.Add(newFace);
                    }
                }
            }
            AddFaces(newFaces);
            if (newFaces.Count == 1) Debug.WriteLine("1 missing face was fixed");
            if (newFaces.Count > 1) Debug.WriteLine(newFaces.Count + " missing faces were fixed");

            foreach (var face in newFaces)
            {

                for (var j = 0; j < 3; j++)
                {
                    #region get the edge CheckSum value
                    var fromVertex = face.Vertices[j];
                    var toVertex = face.Vertices[(j == 2) ? 0 : j + 1];
                    var fromIndex = fromVertex.IndexInList;
                    var toIndex = toVertex.IndexInList;
                    if (fromIndex == toIndex) throw new Exception("edge to same vertices.");
                    var checksum = (fromIndex < toIndex)
                            ? fromIndex + (NumberOfVertices * toIndex)
                            : toIndex + (NumberOfVertices * fromIndex);
                    #endregion
                    if (alreadyDefinedEdges.ContainsKey(checksum)) throw new Exception("Edge has already been created.");
                    else if (partlyDefinedEdges.ContainsKey(checksum))
                    {
                        //Finish creating edge.
                        var edge = partlyDefinedEdges[checksum];
                        edge.EdgeReference = checksum;
                        edge.OtherFace = face;
                        face.Edges.Add(edge);
                        alreadyDefinedEdges.Add(checksum, edge);
                        partlyDefinedEdges.Remove(checksum);
                    }
                    else
                    {
                        var edge = new Edge(fromVertex, toVertex, face, null, doublyLinkToVertices, checksum);
                        partlyDefinedEdges.Add(checksum, edge);
                    }
                }
            }
        }
        #endregion
    }
}