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
using System.Threading.Tasks;
using MIConvexHull;
using StarMathLib;
using TVGL.Boolean_Operations;
using TVGL.Miscellaneous_Functions.TriangulatePolygon;

namespace TVGL.Tessellation
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
        #endregion

        #region Constructors
        /// <summary>
        ///     Prevents a default instance of the <see cref="TessellatedSolid" /> class from being created.
        /// </summary>
        private TessellatedSolid()
        {
        }


        /// <summary>
        ///     Initializes a new instance of the <see cref="TessellatedSolid" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="normals">The normals.</param>
        /// <param name="vertsPerFace">The verts per face.</param>
        /// <param name="colors">The colors.</param>
        /// <param name="performInParallel">The perform in parallel.</param>
        public TessellatedSolid(string name, List<double[]> normals, List<List<double[]>> vertsPerFace,
            List<Color> colors, bool performInParallel = false)
        {
            List<int> indicesOfDuplicates = null;
            var now = DateTime.Now;
            Name = name;
            List<List<int>> faceToVertexIndices = new List<List<int>>();
            #region Parallel Approach

            if (performInParallel)
            {
                /*** Tasks 1 & 2 ***/
                /* Task 1 makes the faces and adds in the normals. */
                var task1 = Task.Factory.StartNew(() => MakeFaces(normals));
                /* Task 2 makes the Vertices, avoids duplicates and creates the FaceToVertexMatcher */
                var task2 = Task.Factory.StartNew(() => MakeVertices(vertsPerFace, faceToVertexIndices, out indicesOfDuplicates));
                Task.WaitAll(task1, task2);

                /*** Tasks 3, 4 & 5 ***/
                /* Task 3 Make the convex hull. Requires: 2 */
                var task3 = Task.Factory.StartNew(CreateConvexHull);
                /* Task 4 Remove any duplicate faces or faces that connect to the same vertex more than 
                 * once, and then link the face to its vertices. Requires: 1, 2 */
                var task4 = Task.Factory.StartNew(() => RemoveDuplicateFacesAndLinkToVertices(faceToVertexIndices));
                /* Task 5 defines the bounding box and the center of the solid (by averaging the vertices). Requires 2 */
                var task5 = Task.Factory.StartNew(DefineBoundingBoxAndCenter);
                Task.WaitAll(task4, task5);

                /*** Tasks 6, & 7 ***/
                /* Task 6 now remove the duplicates found above. Requires 2 and 5 (well, 5 has to be done before we do this, but
                 * 6 is not dependent on 5. */
                var task6 = Task.Factory.StartNew(() => RemoveVertices(indicesOfDuplicates));
                /* Task 7 makes the edges. Requires 4 */
                var task7 = Task.Factory.StartNew(MakeEdges);
                /* Task 8 averages the vertices of the face to define the center. Requires 4 */
                var task8 = Task.Factory.StartNew(() => DefineFaceCentersAndColors(colors));
                Task.WaitAll(task3, task6, task7, task8);

                /*** Tasks 8 & 9 ***/
                /* Task 9 find the areas of faces and the volume of the solid. Requires 9 */
                var task9 = Task.Factory.StartNew(DefineVolumeAndAreas);
                /* Task 10 relates the Convex Hull results back to the objects. Requires 3, 6 */
                var task10 = Task.Factory.StartNew(ConnectConvexHullToObjects);
                Task.WaitAll(task10, task9);


                /* Task 13 goes through the faces and, by examining the edge angles determines it curvature. 
                 * Requires 11 */
                var task13 = Task.Factory.StartNew(DefineFaceCurvature);
                /* Task 14 goes through the vertices and  and, by examining the edge angles determines it curvature. 
                 * Requires 11 */
                var task14 = Task.Factory.StartNew(DefineVertexCurvature);
                Task.WaitAll(task14, task13);
            }
            #endregion
            #region Series Approach

            else
            {
                MakeFaces(normals);
                MakeVertices(vertsPerFace, faceToVertexIndices, out indicesOfDuplicates);
                CreateConvexHull();
                RemoveDuplicateFacesAndLinkToVertices(faceToVertexIndices);
                RemoveVertices(indicesOfDuplicates);
                DefineBoundingBoxAndCenter();
                MakeEdges();
                DefineFaceCentersAndColors(colors);
                ConnectConvexHullToObjects();
                DefineVolumeAndAreas();
                DefineFaceCurvature();
                DefineVertexCurvature();
            }

            #endregion

            Debug.WriteLine("File opened in: " + (DateTime.Now - now).ToString());
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="TessellatedSolid" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="vertices">The vertices.</param>
        /// <param name="faceToVertexIndices">The face to vertex indices.</param>
        /// <param name="colors">The colors.</param>
        /// <param name="performInParallel">if set to <c>true</c> [perform in parallel].</param>
        public TessellatedSolid(string name, List<double[]> vertices, List<List<int>> faceToVertexIndices,
            List<Color> colors, bool performInParallel)
        {
            List<int> indicesOfDuplicates = null;
            var now = DateTime.Now;
            Name = name;

            if (performInParallel)
            {
                /*** Task 1: make vertices ***/
                MakeVertices(vertices);
                /*** Tasks 2, 3, & 4 ***/
                /* Task 3 Find all duplicate vertices. Requires: 1 */
                var task2 = Task.Factory.StartNew(() => FindDuplicateVertices(out indicesOfDuplicates));
                /* Task 3 Make the convex hull. Requires: 1 */
                var task3 = Task.Factory.StartNew(CreateConvexHull);
                /* Task 4 defines the bounding box and the center of the solid (by averaging the vertices). Requires 1 */
                var task4 = Task.Factory.StartNew(DefineBoundingBoxAndCenter);
                Task.WaitAll(task2);
                /* Task 5  link the face to its vertices. Requires: 1, 2 */
                var task5 = Task.Factory.StartNew(() => MakeFaces(faceToVertexIndices));
                Task.WaitAll(task5);

                /*** Tasks 6, & 7 ***/
                /* Task 6 now remove the duplicates found above. Requires 2 and 5 (well, 5 has to be done before we do this, but
                 * 6 is not dependent on 5. */
                var task6 = Task.Factory.StartNew(() => RemoveVertices(indicesOfDuplicates));
                /* Task 7 makes the edges. Requires 4 */
                var task7 = Task.Factory.StartNew(MakeEdges);
                /* Task 8 averages the vertices of the face to define the center. Requires 4 */
                var task8 = Task.Factory.StartNew(() => DefineFaceCentersAndColors(colors));
                Task.WaitAll(task3, task4, task6, task7);

                /*** Tasks 9 & 10 ***/
                /* Task 9 find the areas of faces and the volume of the solid. Requires 8 */
                var task9 = Task.Factory.StartNew(DefineVolumeAndAreas);
                /* Task 10 relates the Convex Hull results back to the objects. Requires 3, 6 */
                var task10 = Task.Factory.StartNew(ConnectConvexHullToObjects);
                Task.WaitAll(task8, task9);


                /* Task 13 goes through the faces and, by examining the edge angles determines it curvature. 
                 * Requires 11 */
                var task13 = Task.Factory.StartNew(DefineFaceCurvature);
                /* Task 13 goes through the vertices and  and, by examining the edge angles determines it curvature. 
                 * Requires 11 */
                var task14 = Task.Factory.StartNew(DefineVertexCurvature);
                Task.WaitAll(task13, task14, task10);
            }
            else
            {
                //1
                MakeVertices(vertices);
                //2
                FindDuplicateVertices(out indicesOfDuplicates);
                CreateConvexHull();
                DefineBoundingBoxAndCenter();
                //3
                MakeFaces(faceToVertexIndices);
                //4
                RemoveVertices(indicesOfDuplicates);
                MakeEdges();
                DefineFaceCentersAndColors(colors);
                //5
                ConnectConvexHullToObjects();
                DefineVolumeAndAreas();
                DefineFaceCurvature();
                DefineVertexCurvature();
            }
            Debug.WriteLine("File opened in: " + (DateTime.Now - now).ToString());
        }

        internal TessellatedSolid(List<PolygonalFace> facesList, Vertex[] subSolidVertices, Vertex[][] newEdgeVertices,
            double[] normal, Boolean[] loopIsPositive)
        {
            Faces = facesList.ToArray();
            NumberOfFaces = facesList.Count;
            Vertices = new Vertex[0];
            AddVertices(subSolidVertices);
            NumberOfVertices = Vertices.GetLength(0);
            var numloops = newEdgeVertices.GetLength(0);
            var points2D = new Point[numloops][];
            for (int i = 0; i < numloops; i++)
                points2D[i] = MinimumEnclosure.Get2DProjectionPoints(newEdgeVertices[i], normal);
            var patchTriangles = TriangulatePolygon.Run(points2D.ToList(), loopIsPositive);
            var patchFaces = new List<PolygonalFace>();
            foreach (var triangle in patchTriangles)
                patchFaces.Add(new PolygonalFace(triangle, normal));
            AddFaces(patchFaces);
            foreach (var face in Faces)
                face.Edges.Clear();
            foreach (var vertex in Vertices)
                vertex.Edges.Clear();
            MakeEdges(Faces);
            CreateConvexHull();
            DefineBoundingBoxAndCenter();
            for (int i = 0; i < Faces.Length; i++)
            {
                var face = Faces[i];
                var centerX = face.Vertices.Average(v => v.X);
                var centerY = face.Vertices.Average(v => v.Y);
                var centerZ = face.Vertices.Average(v => v.Z);
                face.Center = new[] { centerX, centerY, centerZ };
            }
            ConnectConvexHullToObjects();
            DefineVolumeAndAreas();
            DefineFaceCurvature();
            DefineVertexCurvature();
        }



        #endregion

        #region Make many elements (called from constructors)
        /// <summary>
        /// Makes the faces.
        /// </summary>
        /// <param name="normals">The normals.</param>
        private void MakeFaces(IList<double[]> normals)
        {
            NumberOfFaces = normals.Count;
            Faces = new PolygonalFace[NumberOfFaces];
            for (var i = 0; i < NumberOfFaces; i++)
            {
                /* the normal vector read in the from the file should already be a unit vector,
                 * but just to be certain, and to increase the precision since most numbers in an
                 * STL or similar file are only 10 characters or so (and that often includes E-001) */
                var normal = normals[i].normalize();
                if (normal.Any(double.IsNaN)) normal = new double[3];
                Faces[i] = new PolygonalFace(normal);
            }
        }

        private void MakeFaces(List<List<int>> faceToVertexIndices)
        {
            NumberOfFaces = faceToVertexIndices.Count;
            var listOfFaces = new List<PolygonalFace>(NumberOfFaces);
            for (var i = 0; i < NumberOfFaces; i++)
            {
                var badFace = false;
                var vertexIndices = faceToVertexIndices[i];
                var numVertices = vertexIndices.Count;
                var vertices = new List<Vertex>();
                for (int j = 0; j < numVertices; j++)
                {
                    if (vertices.Contains(Vertices[vertexIndices[j]]))
                        badFace = true;
                    vertices.Add(Vertices[vertexIndices[j]]);
                }
                if (!badFace)
                    listOfFaces.Add(new PolygonalFace(vertices));
            }
            Faces = listOfFaces.ToArray();
            NumberOfFaces = Faces.GetLength(0);
        }

        private void MakeEdges()
        {
            Edges = MakeEdges(Faces);
        }

        private Edge[] MakeEdges(PolygonalFace[] localFaces)
        {
            NumberOfEdges = 3 * NumberOfFaces / 2;
            var alreadyDefinedEdges = new Dictionary<int, Edge>();
            for (var i = 0; i < NumberOfFaces; i++)
            {
                var face = localFaces[i];
                var lastIndex = face.Vertices.Count - 1;
                for (var j = 0; j <= lastIndex; j++)
                {
                    var fromVertex = face.Vertices[j];
                    var toVertex = face.Vertices[(j == lastIndex) ? 0 : j + 1];
                    var checksum = EdgeChecksum(fromVertex, toVertex);
                    if (alreadyDefinedEdges.ContainsKey(checksum))
                    {
                        var edge = alreadyDefinedEdges[checksum];
                        edge.OtherFace = face;
                        face.Edges.Add(edge);
                    }
                    else
                    {
                        var edge = new Edge(fromVertex, toVertex, face, null);
                        alreadyDefinedEdges.Add(checksum, edge);
                    }
                }
            }
            var badEdges = new List<Edge>();
            foreach (var edge in alreadyDefinedEdges.Values)
                if (edge.OwnedFace == null || edge.OtherFace == null)
                {
                    badEdges.Add(edge);
                    edge.OwnedFace = edge.OtherFace = edge.OwnedFace ?? edge.OtherFace;
                    Debug.WriteLine("Edge found with only face (face normal = " +
                                    edge.OwnedFace.Normal.MakePrintString()
                                    + ", between vertices " + edge.From.Position.MakePrintString() + " & " +
                                    edge.To.Position.MakePrintString());
                }
            var localEdges = alreadyDefinedEdges.Values.ToArray();
            NumberOfEdges = localEdges.GetLength(0);
            return localEdges;
        }

        private int EdgeChecksum(Vertex from, Vertex to)
        {
            var fromIndex = from.IndexInList;
            var toIndex = to.IndexInList;

            if (fromIndex == toIndex) throw new Exception("edge to same vertices.");
            return (fromIndex < toIndex)
                 ? fromIndex + (NumberOfVertices * toIndex)
                 : toIndex + (NumberOfVertices * fromIndex);
        }

        /// <summary>
        /// Makes the vertices.
        /// </summary>
        /// <param name="vertsPerFace">The verts per face.</param>
        /// <param name="faceToVertexIndices">The face to vertex indices.</param>
        /// <param name="indicesToRemove">The indices to remove.</param>
        private void MakeVertices(ICollection<List<double[]>> vertsPerFace,
            List<List<int>> faceToVertexIndices, out List<int> indicesToRemove)
        {
            /* if all the faces are triangles and nothing was gone wrong,
             * then we know the number of vertices is 2 more than half the number
             * of faces. This comes from the Euler operator equation V - E + F = 2 */
            var expectedNumberOfVertices = 2 + vertsPerFace.Count / 2;
            /* vertexMatchingIndices will be used to speed up the linking of faces and edges to vertices
             * it  preserves the order of vertsPerFace (as read in from the file), and indicates where
             * you can find each vertex in the new array of vertices. This is essentially what is built in 
             * the remainder of this method. */
            var listOfVertices = new List<double[]>(expectedNumberOfVertices);
            // we are not confident that NumberOfVertices will be correct,
            //  so we start with a list and convert it to an array later.
            var simpleCompareDict = new Dictionary<string, int>();
            //in order to reduce compare times we use a string comparer and dictionary
            foreach (var t in vertsPerFace)
            {
                var locationIndices = new List<int>(); // this will become a row in faceToVertexIndices
                foreach (var vertex in t)
                {
                    /* given the low precision in files like STL, this should be a sufficient way to detect identical points. 
                     * I believe comparing these lookupStrings will be quicker than comparing two 3d points.*/
                    var lookupString = vertex[0].ToString(Constants.LookUpStringFormat) + "|"
                                       + vertex[1].ToString(Constants.LookUpStringFormat) + "|"
                                       + vertex[2].ToString(Constants.LookUpStringFormat) + "|";
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
            MakeVertices(listOfVertices);
            if (listOfVertices.Count > expectedNumberOfVertices)
                indicesToRemove = RemoveNClosestVertices(listOfVertices.Count - expectedNumberOfVertices, simpleCompareDict);
            else if (listOfVertices.Count < expectedNumberOfVertices)
            {
                Debug.WriteLine("expected number of vertices = " + expectedNumberOfVertices + "; actual = " + listOfVertices.Count);
                indicesToRemove = new List<int>();
            }
            else indicesToRemove = new List<int>();
        }


        /// <summary>
        ///     Makes the vertices.
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
        private void DefineFaceCentersAndColors(List<Color> colors)
        {
            HasUniformColor = true;
            if (colors == null) SolidColor = new Color(Constants.DefaultColor);
            else if (colors.Count == 1) SolidColor = new Color(Constants.DefaultColor);
            else HasUniformColor = false;
            for (int i = 0; i < Faces.Length; i++)
            {
                var face = Faces[i];
                var centerX = face.Vertices.Average(v => v.X);
                var centerY = face.Vertices.Average(v => v.Y);
                var centerZ = face.Vertices.Average(v => v.Z);
                face.Center = new[] { centerX, centerY, centerZ };
                if (!HasUniformColor)
                {
                    var j = (i < colors.Count - 1) ? i : colors.Count - 1;
                    face.color = colors[j];
                }
            }
        }


        /// <summary>
        ///     Defines the volume and areas.
        /// </summary>

        /// <summary>
        ///     Defines the volume and areas.
        /// </summary>    
        private void DefineVolumeAndAreas()
        {
            Volume = 0;
            SurfaceArea = 0;
            double tempProductX = 0;
            double tempProductY = 0;
            double tempProductZ = 0;
            foreach (var face in Faces)
            {
                // assuming triangular faces: the area is half the magnitude of the cross product of two of the edges
                face.Area = face.Edges[0].Vector.crossProduct(face.Edges[1].Vector).norm2() / 2;
                SurfaceArea += face.Area;   // accumulate areas into surface area
                /* the Center is not correct! It's merely the center of the bounding box, but it doesn't need to be the true center for
                 * the calculation of the volume. Each tetrahedron is added up - even if they are negative - to form the correct value for
                 * the volume. The dot-product to the center gives the height, and 1/3 of the height times the area gives the volume.
                 * While, we're working on it, we  average the centers of the tetrahedrons and do a weighted sum to find the
                 * true center of mass.*/
                var tetrahedronVolume = face.Area * (face.Normal.dotProduct(face.Vertices[0].Position.subtract(Center))) / 3;
                tempProductX += (face.Vertices[0].Position[0] + face.Vertices[1].Position[0] + face.Vertices[2].Position[0] + Center[0]) * tetrahedronVolume / 4;
                tempProductY += (face.Vertices[0].Position[1] + face.Vertices[1].Position[1] + face.Vertices[2].Position[1] + Center[1]) * tetrahedronVolume / 4;
                tempProductZ += (face.Vertices[0].Position[2] + face.Vertices[1].Position[2] + face.Vertices[2].Position[2] + Center[2]) * tetrahedronVolume / 4;
                Volume += tetrahedronVolume;
            }
            Center = new[] { tempProductX / Volume, tempProductY / Volume, tempProductZ / Volume };
        }

        /// <summary>
        ///     Defines the bounding box and center.
        /// </summary>
        private void DefineBoundingBoxAndCenter()
        {
            XMax = double.NegativeInfinity;
            YMax = double.NegativeInfinity;
            ZMax = double.NegativeInfinity;
            XMin = double.PositiveInfinity;
            YMin = double.PositiveInfinity;
            ZMin = double.PositiveInfinity;
            double xSum = 0;
            double ySum = 0;
            double zSum = 0;
            foreach (var v in Vertices)
            {
                xSum += v.Position[0];
                ySum += v.Position[1];
                zSum += v.Position[2];
                if (XMax < v.Position[0]) XMax = v.Position[0];
                if (YMax < v.Position[1]) YMax = v.Position[1];
                if (ZMax < v.Position[2]) ZMax = v.Position[2];
                if (XMin > v.Position[0]) XMin = v.Position[0];
                if (YMin > v.Position[1]) YMin = v.Position[1];
                if (ZMin > v.Position[2]) ZMin = v.Position[2];
            }
            Center = new[] { xSum / NumberOfVertices, ySum / NumberOfVertices, zSum / NumberOfVertices };
        }
        #endregion

        #region Remove Bad or Duplicate elements
        /// <summary>
        ///     Removes the duplicate faces and link to vertices.
        /// </summary>
        private void RemoveDuplicateFacesAndLinkToVertices(List<List<int>> faceToVertexIndices)
        {
            var faceChecksum = new HashSet<long>();
            var duplicates = new List<int>();
            var numberOfDegenerate = 0;
            var checksumMultiplier = new long[Constants.MaxNumberEdgesPerFace];
            for (var j = 0; j < Constants.MaxNumberEdgesPerFace; j++)
                checksumMultiplier[j] = (long)(Math.Pow(NumberOfVertices, j));
            for (var i = 0; i < NumberOfFaces; i++)
            {
                long checksum = 0;
                var orderedIndices = new List<int>(faceToVertexIndices[i].Select(index => Vertices[index].IndexInList));
                orderedIndices.Sort();
                if (orderedIndices.Count != Constants.MaxNumberEdgesPerFace
                    || ContainsDuplicateIndices(orderedIndices))
                {
                    duplicates.Add(i);
                    numberOfDegenerate++;
                    continue;
                }
                for (var j = 0; j < orderedIndices.Count; j++)
                    checksum += orderedIndices[j] * checksumMultiplier[j];
                if (faceChecksum.Contains(checksum)) duplicates.Add(i);
                else
                {
                    faceChecksum.Add(checksum);
                    foreach (var vertexMatchingIndex in faceToVertexIndices[i])
                    {
                        var v = Vertices[vertexMatchingIndex];
                        Faces[i].Vertices.Add(v);
                        v.Faces.Add(Faces[i]);
                    }
                }
            }
            if (duplicates.Count == 0) return;
            Debug.WriteLine("Removing {0} duplicate faces and {1} degenerate faces.",
                duplicates.Count - numberOfDegenerate, numberOfDegenerate);
            var facesList = Faces.ToList();
            for (var i = duplicates.Count - 1; i >= 0; i--)
                facesList.RemoveAt(duplicates[i]);
            Faces = facesList.ToArray();
            NumberOfFaces = Faces.Count();
        }

        /// <summary>
        ///     Duplicates the vertices.
        /// </summary>
        /// <param name="orderedIndices">The ordered indices.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private static bool ContainsDuplicateIndices(List<int> orderedIndices)
        {
            for (var i = 0; i < orderedIndices.Count - 1; i++)
                if (orderedIndices[i] == orderedIndices[i + 1]) return true;
            return false;
        }

        private List<int> RemoveNClosestVertices(int n, Dictionary<string, int> simpleCompareDict)
        {
            var sortedVerts = new SortedList<string, int>(simpleCompareDict);
            var removeList = new SortedList<double, Tuple<int, int>>(new NoEqualSort());
            var largestError = double.PositiveInfinity;
            var numVerts = Vertices.GetLength(0);
            for (int i = 1; i < numVerts; i++)
            {
                var pIndex0 = sortedVerts.Values[i - 1];
                var pIndex1 = sortedVerts.Values[i];
                var distance = Vertices[pIndex0].Position.norm2(Vertices[pIndex1].Position, true);
                if (distance < largestError)
                {
                    removeList.Add(distance, new Tuple<int, int>(pIndex0, pIndex1));
                    if (removeList.Count == n + 1)
                    {
                        removeList.RemoveAt(n);
                        largestError = removeList.Keys[n - 1];
                    }
                }
            }
            foreach (var replace in removeList.Values)
                Vertices[replace.Item1] = Vertices[replace.Item2];
            return removeList.Values.Select(tuple => tuple.Item1).ToList();
        }

        private void FindDuplicateVertices(out List<int> indicesOfDuplicates)
        {
            indicesOfDuplicates = new List<int>();
            /* this is similar to the STL reader, but it is important. Sometimes these
             * other (better) formats still duplicate vertices. We don't want that. So,
             * a similar dictionary-hashstring approach is used. */
            // we are not confident that NumberOfVertices will be correct,
            //  so we start with a list and convert it to an array later.
            var simpleCompareDict = new Dictionary<string, int>();
            //in order to reduce compare times we use a string comparer and dictionary
            for (int i = 0; i < Vertices.Length; i++)
            {
                var coord = Vertices[i].Position;
                var lookupString = coord[0].ToString(Constants.LookUpStringFormat) + "|"
                                   + coord[1].ToString(Constants.LookUpStringFormat) + "|"
                                   + coord[2].ToString(Constants.LookUpStringFormat) + "|";
                if (simpleCompareDict.ContainsKey(lookupString))
                {
                    Vertices[i] = Vertices[simpleCompareDict[lookupString]];
                    indicesOfDuplicates.Add(i);
                }
                else
                    simpleCompareDict.Add(lookupString, i);
            }
            if (indicesOfDuplicates.Any())
                Debug.WriteLine("expected number of vertices = " + NumberOfVertices + "; actual = " +
                                    (NumberOfVertices - indicesOfDuplicates.Count));
        }

        #endregion

        #region the Copy Function

        /// <summary>
        ///     Copies this instance.
        /// </summary>
        /// <returns>TessellatedSolid.</returns>
        public TessellatedSolid Copy()
        {
            var copyOfFaces = new PolygonalFace[NumberOfFaces];
            for (var i = 0; i < NumberOfFaces; i++)
                copyOfFaces[i] = Faces[i].Copy();
            var copyOfVertices = new Vertex[NumberOfVertices];
            for (var i = 0; i < NumberOfVertices; i++)
                copyOfVertices[i] = Vertices[i].Copy();
            for (var fIndex = 0; fIndex < NumberOfFaces; fIndex++)
            {
                var thisFace = copyOfFaces[fIndex];
                var oldFace = Faces[fIndex];
                var vertexIndices = new List<int>();
                foreach (var oldVertex in oldFace.Vertices)
                {
                    var vIndex = oldVertex.IndexInList;
                    vertexIndices.Add(vIndex);
                    var thisVertex = copyOfVertices[vIndex];
                    thisFace.Vertices.Add(thisVertex);
                    thisVertex.Faces.Add(thisFace);
                }
            }
            Edge[] copyOfEdges = MakeEdges(copyOfFaces);
            var copy = new TessellatedSolid
           {
               SurfaceArea = SurfaceArea,
               Center = (double[])Center.Clone(),
               Faces = copyOfFaces,
               Vertices = copyOfVertices,
               Edges = copyOfEdges,
               Name = Name,
               NumberOfFaces = NumberOfFaces,
               NumberOfVertices = NumberOfVertices,
               Volume = Volume,
               XMax = XMax,
               XMin = XMin,
               YMax = YMax,
               YMin = YMin,
               ZMax = ZMax,
               ZMin = ZMin
           };
            copy.CreateConvexHull();
            return copy;
        }

        #endregion

        #region Convex hull and curvatures

        /// <summary>
        ///     Defines the vertex curvature.
        /// </summary>
        private void DefineVertexCurvature()
        {
            foreach (var v in Vertices)
            {
                if (v.Edges.Any(e => e.Curvature == CurvatureType.Undefined))
                    v.EdgeCurvature = CurvatureType.Undefined;
                else if (v.Edges.All(e => e.Curvature == CurvatureType.SaddleOrFlat))
                    v.EdgeCurvature = CurvatureType.SaddleOrFlat;
                else if (v.Edges.Any(e => e.Curvature != CurvatureType.Convex))
                    v.EdgeCurvature = CurvatureType.Concave;
                else if (v.Edges.Any(e => e.Curvature != CurvatureType.Concave))
                    v.EdgeCurvature = CurvatureType.Convex;
                else v.EdgeCurvature = CurvatureType.SaddleOrFlat;
            }
        }

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
            var convexHull = ConvexHull.Create<Vertex, DefaultConvexFace<Vertex>>(Vertices);
            ConvexHullVertices = convexHull.Points.ToArray();
            var numCvxFaces = convexHull.Faces.Count();
            ConvexHullFaces = new PolygonalFace[numCvxFaces];
            ConvexHullEdges = new Edge[3 * numCvxFaces / 2];
            var faceIndex = 0;
            var edgeIndex = 0;
            foreach (var cvxFace in convexHull.Faces)
            {
                var newFace = new PolygonalFace(cvxFace.Normal) { Vertices = cvxFace.Vertices.ToList() };
                //foreach (var v in newFace.Vertices)
                //    v.Faces.Add(newFace);
                ConvexHullFaces[faceIndex++] = newFace;
            }
            faceIndex = 0;
            foreach (var cvxFace in convexHull.Faces)
            {
                var newFace = ConvexHullFaces[faceIndex++];
                for (var j = 0; j < cvxFace.Adjacency.GetLength(0); j++)
                {
                    var adjacentOldFace = cvxFace.Adjacency[j];
                    if (newFace.Edges.Count <= j || newFace.Edges[j] == null)
                    {
                        var adjFaceIndex = convexHull.Faces.FindIndex(adjacentOldFace);
                        var adjFace = ConvexHullFaces[adjFaceIndex];
                        var sharedVerts = newFace.Vertices.Intersect(adjacentOldFace.Vertices).ToList();
                        var newEdge = new Edge(sharedVerts[0], sharedVerts[1], newFace, adjFace, false);
                        while (newFace.Edges.Count <= j) newFace.Edges.Add(null);
                        newFace.Edges[j] = newEdge;
                        var k = adjacentOldFace.Adjacency.FindIndex(cvxFace);
                        while (adjFace.Edges.Count <= k) adjFace.Edges.Add(null);
                        adjFace.Edges[k] = newEdge;
                        ConvexHullEdges[edgeIndex++] = newEdge;
                    }
                }
            }
        }


        /// <summary>
        ///     Defines the face curvature. Depends on DefineEdgeAngle
        /// </summary>
        private void DefineFaceCurvature()
        {
            foreach (var face in Faces)
            {
                if (face.Edges.Any(e => e.Curvature == CurvatureType.Undefined))
                    face.Curvature = CurvatureType.Undefined;
                else if (face.Edges.All(e => e.Curvature != CurvatureType.Concave))
                    face.Curvature = CurvatureType.Convex;
                else if (face.Edges.All(e => e.Curvature != CurvatureType.Convex))
                    face.Curvature = CurvatureType.Concave;
                else face.Curvature = CurvatureType.SaddleOrFlat;
            }
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

        internal void RemoveVertex(Vertex removeVertex)
        {
            RemoveVertex(Vertices.FindIndex(removeVertex));
        }

        internal void RemoveVertex(int removeVIndex)
        {
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
        }

        private void RemoveVertices(List<Vertex> removeVertices)
        {
            RemoveVertices(removeVertices.Select(Vertices.FindIndex).ToList());
        }

        private void RemoveVertices(List<int> removeIndices)
        {
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
            NumberOfFaces--;
            var newFaces = new PolygonalFace[NumberOfFaces];
            for (int i = 0; i < removeFaceIndex; i++)
                newFaces[i] = Faces[i];
            for (int i = removeFaceIndex; i < NumberOfFaces; i++)
                newFaces[i] = Faces[i + 1];
            Faces = newFaces;
        }
        internal void RemoveFaces(List<PolygonalFace> removeFaces)
        {
            RemoveFaces(removeFaces.Select(Faces.FindIndex).ToList());
        }

        internal void RemoveFaces(List<int> removeIndices)
        {
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
            NumberOfEdges--;
            var newEdges = new Edge[NumberOfEdges];
            for (int i = 0; i < removeEdgeIndex; i++)
                newEdges[i] = Edges[i];
            for (int i = removeEdgeIndex; i < NumberOfEdges; i++)
                newEdges[i] = Edges[i + 1];
            Edges = newEdges;
        }
        internal void RemoveEdges(List<Edge> removeEdges)
        {
            RemoveEdges(removeEdges.Select(Edges.FindIndex).ToList());
        }

        internal void RemoveEdges(List<int> removeIndices)
        {
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
        #endregion
        #endregion

    }
}