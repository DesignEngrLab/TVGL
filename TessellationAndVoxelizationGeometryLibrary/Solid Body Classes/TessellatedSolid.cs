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
        ///     Gets the number of vertices.
        /// </summary>
        /// <value>The number of vertices.</value>
        public int CheckSumMultiplier { get; private set; }

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
            List<Color> colors, bool inParallel = true)
        {
            var now = DateTime.Now;

            //Begin Construction 
            Name = name;
            var faceToVertexIndices = new List<List<int>>();
            MakeVertices(vertsPerFace, out faceToVertexIndices);
            MakeFaces(faceToVertexIndices, normals);
            DefineFaceColors(colors);
            //Complete Construction with Common Functions
            this.CompleteInitiation();

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
            List<Color> colors, bool inParallel = true)
        {
            var now = DateTime.Now;

            //Begin Construction 
            Name = name;
            MakeVertices(vertices, ref faceToVertexIndices);
            MakeFaces(faceToVertexIndices);
            DefineFaceColors(colors);
            //Complete Construction with Common Functions
            this.CompleteInitiation();
            
            Debug.WriteLine("File opened in: " + (DateTime.Now - now).ToString());
        }

        internal void CompleteInitiation()
        {
            //1
            CreateConvexHull();
            DefineBoundingBoxAndCenter();
            MakeEdges();
            //2
            RepairFaces();
            //3
            DefineVolumeAndSurfaceArea();
            ConnectConvexHullToObjects();
            DefineFaceCurvature();
            DefineVertexCurvature();
            //3
            CheckReferences();
        }

        internal TessellatedSolid(IList<PolygonalFace> faces, IList<Vertex> vertices)
        {
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
            DefineBoundingBoxAndCenter();
            this.CompleteInitiation();
        }
        #endregion

        #region Build New from Portions of Old Solid, and the Copy and Duplicate Functions (one needs to be removed)
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
                    if(!vertices.Contains(vertex))
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
            return new TessellatedSolid(Name + "_Copy", listDoubles, faces, new List<Color> { SolidColor }, false);
        }
        
        /// <summary>
        ///     Copies this instance.
        /// </summary>
        /// <returns>TessellatedSolid.</returns>
        public TessellatedSolid Copy()
        {
            var listDoubles = new List<double[]>();
            for(var i = 0; i < Vertices.Count(); i++)
            {
                Vertices[i].IndexInList = i;
                listDoubles.Add(Vertices[i].Position);
                //listDoubles.Add((double[])Vertices[i].Position.Clone());
            }
            var faces = new List<List<int>>();
            for(var i = 0; i < Faces.Count(); i++)
            {
                var face = new List<int>();
                foreach (var vertex in Faces[i].Vertices)
                {
                    face.Add(vertex.IndexInList);
                }
                faces.Add(face);
            }
            return new TessellatedSolid(Name + "_Copy", listDoubles, faces, new List<Color> { SolidColor }, false);
        }
        #endregion

        #region Make many elements (called from constructors)
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
            var duplicates = new List<int>();
            var numberOfDegenerate = 0;
            var checksumMultiplier = new long[Constants.MaxNumberEdgesPerFace];
            for (var i = 0; i < Constants.MaxNumberEdgesPerFace; i++)
                checksumMultiplier[i] = (long)Math.Pow(CheckSumMultiplier, i);
            for (var i = 0; i < NumberOfFaces; i++)
            {
                double[] normal = null;
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
                if (faceChecksums.Contains(checksum)) continue;
                //Get the normal, if it was given.
                if (normals != null)
                {
                    normal = normals[i].normalize();
                    if (normal.Any(double.IsNaN)) normal = new double[3];
                }
                //Get the actual vertices to create a new face. 
                //Creating a face this way doubly links the vertices and face.
                faceChecksums.Add(checksum);
                var faceVertices = new List<Vertex>();
                
                foreach (var vertexMatchingIndex in faceToVertexIndices[i])
                {
                    faceVertices.Add(Vertices[vertexMatchingIndex]);
                }
                if (normal == null) listOfFaces.Add(new PolygonalFace(faceVertices, doublyLinkToVertices, CheckSumMultiplier, checksum));
                else listOfFaces.Add(new PolygonalFace(faceVertices, normal, doublyLinkToVertices, CheckSumMultiplier, checksum));
            }
            Faces = listOfFaces.ToArray();
            NumberOfFaces = Faces.GetLength(0);
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
                            ? fromIndex + (CheckSumMultiplier * toIndex)
                            : toIndex + (CheckSumMultiplier * fromIndex);
                    #endregion
                    if (partlyDefinedEdges.ContainsKey(checksum))
                    {
                        if(alreadyDefinedEdges.ContainsKey(checksum)) throw new Exception("Edge has already been created.");
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
                        var edge = new Edge(fromVertex, toVertex, face, null, doublyLinkToVertices, CheckSumMultiplier, checksum);
                        partlyDefinedEdges.Add(checksum, edge);
                    }
                }
            }
            var definedEdges = alreadyDefinedEdges.Values.ToList();
            if (partlyDefinedEdges.Count > 2)
            {
                //There is a chance, one or more faces is just missing. This can be repaired if the bad edges
                //form a loop around the missing section, if the missing section if reletively flat.
                RepairMissingFacesFromEdges(ref partlyDefinedEdges, ref definedEdges, doublyLinkToVertices);
            }
            if (partlyDefinedEdges.Count > 0)
            {
                foreach (var badEdge in partlyDefinedEdges.Values)
                {
                    Debug.WriteLine("Edge found with only face. Edge Reference: " + badEdge.EdgeReference);
                }
                throw new Exception();
            }
            return definedEdges.ToArray();
        }

        /// <summary>
        /// Makes the vertices.
        /// </summary>
        /// <param name="vertsPerFace">The verts per face.</param>
        /// <param name="faceToVertexIndices">The face to vertex indices.</param>
        /// <param name="indicesToRemove">The indices to remove.</param>
        private void MakeVertices(ICollection<List<double[]>> vertsPerFace, out List<List<int>> faceToVertexIndices)
        {
            /* vertexMatchingIndices will be used to speed up the linking of faces and edges to vertices
             * it  preserves the order of vertsPerFace (as read in from the file), and indicates where
             * you can find each vertex in the new array of vertices. This is essentially what is built in 
             * the remainder of this method. */
            faceToVertexIndices = new List<List<int>>();
            var listOfVertices = new List<double[]>();
            var simpleCompareDict = new Dictionary<string, int>();
            //in order to reduce compare times we use a string comparer and dictionary
            foreach (var t in vertsPerFace)
            {
                var locationIndices = new List<int>(); // this will become a row in faceToVertexIndices
                foreach (var vertex in t)
                {
                    /* given the low precision in files like STL, this should be a sufficient way to detect identical points. 
                     * I believe comparing these lookupStrings will be quicker than comparing two 3d points.*/
                    //First, round the vertices, then convert to a string. This will catch bidirectional tolerancing (+/-)
                    vertex[0] = Math.Round(vertex[0], Constants.DecimalPlaceError);
                    vertex[1] = Math.Round(vertex[1], Constants.DecimalPlaceError);
                    vertex[2] = Math.Round(vertex[2], Constants.DecimalPlaceError);
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
            var listOfVertices = new List<double[]>();
            var simpleCompareDict = new Dictionary<string, int>();
            //in order to reduce compare times we use a string comparer and dictionary
            foreach (var faceToVertexIndex in faceToVertexIndices)
            {
                for(var i = 0; i < faceToVertexIndex.Count; i ++)
                {
                    //Get vertex from un-updated list of vertices
                    var vertex = vertices[faceToVertexIndex[i]];
                    /* given the low precision in files like STL, this should be a sufficient way to detect identical points. 
                     * I believe comparing these lookupStrings will be quicker than comparing two 3d points.*/
                    //First, round the vertices, then convert to a string. This will catch bidirectional tolerancing (+/-)
                    vertex[0] = Math.Round(vertex[0], Constants.DecimalPlaceError);
                    vertex[1] = Math.Round(vertex[1], Constants.DecimalPlaceError);
                    vertex[2] = Math.Round(vertex[2], Constants.DecimalPlaceError);
                    var lookupString = vertex[0].ToString(Constants.LookUpStringFormat) + "|"
                                       + vertex[1].ToString(Constants.LookUpStringFormat) + "|"
                                       + vertex[2].ToString(Constants.LookUpStringFormat) + "|";
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
            CheckSumMultiplier = (int)Math.Pow(10, (int)Math.Floor(Math.Log10(Vertices.Count())) + 1);
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
        ///     Defines the volume and areas.
        /// </summary>

        /// <summary>
        ///     Defines the volume and areas.
        /// </summary>    
        private void DefineVolumeAndSurfaceArea()
        {
            Volume = 0;
            SurfaceArea = 0;
            double tempProductX = 0;
            double tempProductY = 0;
            double tempProductZ = 0;
            foreach (var face in Faces)
            {
                // assuming triangular faces: the area is half the magnitude of the cross product of two of the edges
                face.SetArea();
                SurfaceArea += face.Area;   // accumulate areas into surface area
                /* the Center is not correct! It's merely the center of the bounding box, but it doesn't need to be the true center for
                 * the calculation of the volume. Each tetrahedron is added up - even if they are negative - to form the correct value for
                 * the volume. The dot-product to the center gives the height, and 1/3 of the height times the area gives the volume.
                 * While, we're working on it, we  average the centers of the tetrahedrons and do a weighted sum to find the
                 * true center of mass.*/
                var tetrahedronVolume = Math.Abs(face.Area * (face.Normal.dotProduct(face.Vertices[0].Position.subtract(Center)))) / 3;
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
            var convexHull = ConvexHull.Create<Vertex, DefaultConvexFace<Vertex>>(Vertices);
            ConvexHullVertices = convexHull.Points.ToArray();
            var numCvxFaces = convexHull.Faces.Count();
            if (numCvxFaces < 3)
            {
                ConvexHullSuceeded = false;
                return;
            }
            ConvexHullFaces = new PolygonalFace[numCvxFaces];
            ConvexHullEdges = new Edge[3 * numCvxFaces / 2];
            var faceIndex = 0;
            foreach (var cvxFace in convexHull.Faces)
            {
                var newFace = new PolygonalFace(cvxFace.Vertices.ToList(), cvxFace.Normal, false);
                ConvexHullFaces[faceIndex++] = newFace;
            }
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

        internal void ReplaceVertex(Vertex removeVertex, Vertex newVertex)
        {
            ReplaceVertex(Vertices.FindIndex(removeVertex), newVertex);
        }
        internal void ReplaceVertex(int removeVIndex, Vertex newVertex)
        {
            newVertex.IndexInList = removeVIndex;
            var newVertices = new Vertex[NumberOfVertices];
            newVertices[removeVIndex] = newVertex;
            Vertices = newVertices;
        }

        internal void RemoveVertices(List<Vertex> removeVertices)
        {
            RemoveVertices(removeVertices.Select(Vertices.FindIndex).ToList());
        }
        internal void RemoveVertices(List<int> removeIndices)
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
            //First. Remove all the references to each edge and vertex.
            foreach (var vertex in removeFace.Vertices)
            {
                var index = vertex.Faces.IndexOf(removeFace);
                if (index >= 0) vertex.Faces.RemoveAt(index);
            }
            foreach (var edge in removeFace.Edges)
            {
                if (removeFace == edge.OwnedFace) edge.OwnedFace = null;
                if (removeFace == edge.OtherFace) edge.OwnedFace = null;
            }
            //Face adjacency is a method call, not an object reference. So it updates automatically.
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
            //First. Remove all the references to each edge and vertex.
            foreach (var face in removeFaces)
            {
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
            //First. Remove all the references to each edge and vertex.
            foreach (var edge in removeEdges)
            {
                var index = edge.To.Edges.IndexOf(edge);
                if (index >= 0) edge.To.Edges.RemoveAt(index);
                index = edge.From.Edges.IndexOf(edge);
                if (index >= 0) edge.From.Edges.RemoveAt(index);
                index = edge.OwnedFace.Edges.IndexOf(edge);
                if (index >= 0) edge.OwnedFace.Edges.RemoveAt(index);
                index = edge.OtherFace.Edges.IndexOf(edge);
                if (index >= 0) edge.OtherFace.Edges.RemoveAt(index);
            }
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

        #region Check References
        public void CheckReferences()
        {
            //Check if each face has cyclic references with each edge, vertex, and adjacent faces.
            foreach (var face in Faces)
            {
                foreach (var edge in face.Edges)
                {
                    if (edge.OwnedFace != face && edge.OtherFace != face) throw new Exception();
                }
                foreach (var vertex in face.Vertices)
                {
                    if(!vertex.Faces.Contains(face)) throw new Exception();
                }
                foreach (var adjacentFace in face.AdjacentFaces)
                {
                    if (!adjacentFace.AdjacentFaces.Contains(face)) throw new Exception();
                    if (face.Normal.dotProduct(adjacentFace.Normal).IsPracticallySame(-1.0)) throw new Exception();
                }
            }
            //Check if each edge has cyclic references with each vertex and each face.
            foreach (var edge in Edges)
            {
                if (!edge.OwnedFace.Edges.Contains(edge)) throw new Exception();
                if (!edge.OtherFace.Edges.Contains(edge)) throw new Exception();
                if (!edge.To.Edges.Contains(edge)) throw new Exception();
                if (!edge.From.Edges.Contains(edge)) throw new Exception();
            }
            //Check if each vertex has cyclic references with each edge and each face.
            foreach (var vertex in Vertices)
            {
                foreach (var edge in vertex.Edges)
                {
                    if (edge.To != vertex && edge.From != vertex) throw new Exception();
                }
                foreach (var face in vertex.Faces)
                {
                    if (!face.Vertices.Contains(vertex)) throw new Exception();
                }
            }
        } 
        #endregion

        #region Repair Functions
        public void RepairFaces()
        {
            //First, get all the negligible area faces.
            var negligibleFaces = new List<PolygonalFace>();
            foreach (var face in Faces)
            {
                if (face.Area < 0.0000001) negligibleFaces.Add(face);
            }
            //Then, collapse each of them. Special care is taken if they are adjacent.
            for (var i = 0; i < negligibleFaces.Count; i++ )
            {
                var negligibleGroup = new List<PolygonalFace> { negligibleFaces[i] };
                var largestEdges = new List<Edge>{negligibleFaces[i].Edges.OrderByDescending(item => item.Length).First()};
                foreach (var adjacentFace in negligibleFaces[i].AdjacentFaces)
                {
                    if (negligibleFaces.Contains(adjacentFace))
                    {
                        negligibleGroup.Add(adjacentFace);
                        largestEdges.Add(adjacentFace.Edges.OrderByDescending(item => item.Length).First());
                    }
                }
                for (var j = 0; j < negligibleGroup.Count; j++)
                {
                    var firstFace = negligibleGroup[i];
                    var splitEdge = largestEdges[0];
                    largestEdges.RemoveAt(0);
                    //If the largest edge is shared, No new triangles are created. Just collapse both.
                    if(largestEdges.Contains(splitEdge))
                    {
                        throw new Exception("Not Implemented");
                    }
                    else// collapse to middle of largest edge, and split the triangle sharing that edge.
                    {
                        #region Collapse negligible Triangle, by splitting its largest edge. 
                        //FIRST: update vertex list
                        //This has to be done now, so that the new vertex has an index to use for the faceReference and edgeReference properties.
                        var otherVertex = firstFace.OtherVertex(splitEdge); //This vertex will be removed.
                        var midPoint = splitEdge.From.Position.add(splitEdge.To.Position).divide(2.0);
                        var midPointVertex = new Vertex(midPoint); //this vertex will be added.
                        ReplaceVertex(otherVertex, midPointVertex);

                        //Create all the new faces.
                        var removeTheseFaces = new List<PolygonalFace>(firstFace.AdjacentFaces); //This gets all the faces to be removed (firstFace is added later)
                        var newFaces = new List<PolygonalFace>();
                        PolygonalFace toSideSplitFace = null;
                        PolygonalFace fromSideSplitFace = null;
                        foreach(var face in removeTheseFaces)
                        {
                            if(face.Edges.Contains(splitEdge))
                            {
                                //Split this face into two
                                var thirdVertex = face.OtherVertex(splitEdge);
                                toSideSplitFace = new PolygonalFace(new List<Vertex> { thirdVertex, midPointVertex, splitEdge.To}, 
                                    face.Normal, true, this.CheckSumMultiplier);
                                newFaces.Add(toSideSplitFace);

                                fromSideSplitFace = new PolygonalFace(new List<Vertex>{thirdVertex, midPointVertex, splitEdge.From}, 
                                    face.Normal, true, this.CheckSumMultiplier);
                                newFaces.Add(fromSideSplitFace);
                            }
                            else
                            {
                                //Replace the vertices from this face. Keep the normal, or add guess bool. normalIsGuess.
                                var newFaceVertexList = new List<Vertex>(face.Vertices);
                                newFaceVertexList.Remove(otherVertex);
                                newFaceVertexList.Add(midPointVertex);
                                newFaces.Add(new PolygonalFace(newFaceVertexList, face.Normal, true, this.CheckSumMultiplier));
                            }
                        }
                        removeTheseFaces.Add(firstFace); //Also remove the first face.

                        //Create all the new edges.
                        var removeTheseEdges = otherVertex.Edges; //All the edges connected to this vertex will be removed (splitEdge is added later)
                        var newEdges = new List<Edge>(); //This is a list of all the new edges.
                        foreach(var edge in removeTheseEdges)
                        {
                            var ownedFaceReference = edge.OwnedFace.FaceReference;
                            var otherFaceReference = edge.OtherFace.FaceReference;
                            Edge newEdge;
                            if(otherVertex == edge.From) newEdge = new Edge(midPointVertex, edge.OtherVertex(otherVertex), true, this.CheckSumMultiplier);
                            else newEdge = new Edge(edge.OtherVertex(otherVertex), midPointVertex, true, this.CheckSumMultiplier);
                            //Owned and other are arbitrary for now. They are set properly, when both are attached to the edge.
                            if (edge.OtherVertex(otherVertex) == splitEdge.To)
                            {
                                newEdge.OwnedFace = toSideSplitFace;
                                if(edge.OwnedFace == firstFace) newEdge.OtherFace = newFaces.FirstOrDefault(face => face.FaceReference == otherFaceReference);
                                else newEdge.OtherFace = newFaces.FirstOrDefault(face => face.FaceReference == ownedFaceReference);
                            }
                            else if (edge.OtherVertex(otherVertex) == splitEdge.From)
                            {
                                newEdge.OwnedFace = fromSideSplitFace;
                                if(edge.OwnedFace == firstFace) newEdge.OtherFace = newFaces.FirstOrDefault(face => face.FaceReference == otherFaceReference);
                                else newEdge.OtherFace = newFaces.FirstOrDefault(face => face.FaceReference == ownedFaceReference);
                            }
                            else
                            {
                                //else, the owned and other faces for the edges in this foreach loop have the same FaceReference 
                                //as the edge being removed (based on checksum), because we REPLACED the vertex.
                                newEdge.OwnedFace = newFaces.FirstOrDefault(face => face.FaceReference == ownedFaceReference);
                                newEdge.OtherFace = newFaces.FirstOrDefault(face => face.FaceReference == otherFaceReference);  
                            }
                            if(newEdge != null) newEdges.Add(newEdge);
                        }
                        removeTheseEdges.Add(splitEdge); //Also remove the first edge.
                        PolygonalFace splittingFace;
                        if(firstFace == splitEdge.OtherFace) splittingFace = splitEdge.OwnedFace;
                        else splittingFace = splitEdge.OtherFace;
                        //Add the final new edge. It references the split faces.
                        newEdges.Add(new Edge(splittingFace.OtherVertex(splitEdge), midPointVertex, 
                            toSideSplitFace, fromSideSplitFace, true, this.CheckSumMultiplier));
                        
                        //Update all the edges that were on the outside of this reconstruction
                        foreach (var face in removeTheseFaces)
                        {
                            if (face.Edges.Count != 3) throw new Exception();
                            foreach (var edge in face.Edges)
                            {
                                if (removeTheseEdges.Contains(edge)) continue; //don't need to do anything, since it will be removed.
                                if (face == edge.OwnedFace) edge.OwnedFace = newFaces.FirstOrDefault(f => f.FaceReference == face.FaceReference);
                                else if (face == edge.OtherFace) edge.OtherFace = newFaces.FirstOrDefault(f => f.FaceReference == face.FaceReference);
                                else throw new Exception();
                            }
                        }
                        //Remove and then add all the new faces and edges.
                        //The remove functions also remove any circular reference back to the face or edge.
                        RemoveFaces(removeTheseFaces);
                        RemoveEdges(removeTheseEdges);
                        AddEdges(newEdges);
                        AddFaces(newFaces);
                        #endregion
                    }
                }
                foreach (var face in negligibleGroup)
                {
                    negligibleFaces.Remove(face);
                }
            }
        }

        public void RepairMissingFacesFromEdges(ref Dictionary<int, Edge> partlyDefinedEdges, ref List<Edge> definedEdges, bool doublyLinkToVertices)
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

            for(var i = 0; i < loops.Count ; i++)
            {
                //if a simple triangle, create a new face from vertices
                if (loops[i].Count == 3)
                {
                    var newFace = new PolygonalFace(loops[i], loopNormals[i], doublyLinkToVertices);
                    newFaces.Add(newFace);
                }
                //Else, use the triangulate function
                else if(loops[i].Count > 3)
                {
                    //First, get an average normal from all vertices, assuming CCW order.
                    var triangles = TriangulatePolygon.Run(new List<List<Vertex>>{loops[i]}, loopNormals[i]);
                    foreach(var triangle in triangles)
                    {
                        var newFace = new PolygonalFace(triangle, loopNormals[i], doublyLinkToVertices);
                        newFaces.Add(newFace);
                    }
                }
            }
            AddFaces(newFaces);

            //Find which edges need to be added and add those
            var edgeChecksums = new HashSet<int>();
            foreach (var edge in definedEdges)
            {
                edgeChecksums.Add(edge.EdgeReference);
            }
            var alreadyDefinedEdges = new Dictionary<int, Edge>();
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
                            ? fromIndex + (CheckSumMultiplier * toIndex)
                            : toIndex + (CheckSumMultiplier * fromIndex);
                    #endregion
                    if (edgeChecksums.Contains(checksum)) continue;
                    if (partlyDefinedEdges.ContainsKey(checksum))
                    {
                        if(alreadyDefinedEdges.ContainsKey(checksum)) throw new Exception("Edge has already been created.");
                        //Finish creating edge.
                        var edge = partlyDefinedEdges[checksum];
                        edge.EdgeReference = checksum;
                        edge.OtherFace = face;
                        face.Edges.Add(edge);
                        alreadyDefinedEdges.Add(checksum, edge);
                        partlyDefinedEdges.Remove(checksum);
                        edgeChecksums.Add(checksum);
                    }
                    else
                    {
                        var edge = new Edge(fromVertex, toVertex, face, null, doublyLinkToVertices, CheckSumMultiplier, checksum);
                        partlyDefinedEdges.Add(checksum, edge);
                    }
                }              
            }
            var badEdges = partlyDefinedEdges.Values.ToList();
            if (badEdges.Count > 0) throw new Exception("There should be no bad edges in this function, which is fixing bad edges");
            definedEdges.AddRange(alreadyDefinedEdges.Values.ToList());
        }
        #endregion
    }
}