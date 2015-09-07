using System;
using System.Collections.Generic;
using System.Linq;
using StarMathLib;

namespace TVGL.Boolean_Operations
{
    /// <summary>
    /// The Slice class includes static functions for cutting a tessellated solid.
    /// </summary>
    public static class Slice2
    {
        #region Define Contact at a Flat Plane
        /// <summary>
        /// Performs the slicing operation on the prescribed flat plane. This destructively alters
        /// the tessellated solid into one or more solids which are returned in the "out" parameter
        /// lists.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="plane">The plane.</param>
        /// <param name="positiveSideSolids">The solids that are on the positive side of the plane
        /// This means that are on the side that the normal faces.</param>
        /// <param name="negativeSideSolids">The solids on the negative side of the plane.</param>
        public static void OnFlat(TessellatedSolid ts, Flat plane,
            out List<TessellatedSolid> positiveSideSolids, out List<TessellatedSolid> negativeSideSolids)
        {
            positiveSideSolids = new List<TessellatedSolid>();
            negativeSideSolids = new List<TessellatedSolid>();
            List<PolygonalFace> positiveSideFaces;
            List<PolygonalFace> negativeSideFaces;
            List<Vertex> positiveSideLoopVertices;
            List<Vertex> negativeSideLoopVertices;
            //1. Divide up the faces into either negative or positive. OnPlane faces are not used. 
            //Straddle faces are split into 2 or 3 new faces.
            DivideUpFaces(ts, plane, out positiveSideFaces, out negativeSideFaces,
                out positiveSideLoopVertices, out negativeSideLoopVertices);
            //2. Find loops to define the missing space on the plane
            var positiveSideLoops = FindLoops(positiveSideLoopVertices, positiveSideFaces);
            var negativeSideLoops = FindLoops(negativeSideLoopVertices, negativeSideFaces);
            //3. Triangulate that empty space and add to list 
            var triangles = TriangulatePolygon.Run(positiveSideLoops, plane.Normal);
            positiveSideFaces.AddRange(triangles.Select(triangle => new PolygonalFace(triangle, plane.Normal.multiply(-1))));
            triangles = TriangulatePolygon.Run(negativeSideLoops, plane.Normal);
            negativeSideFaces.AddRange(triangles.Select(triangle => new PolygonalFace(triangle, plane.Normal)));
            //4. Create a new tesselated solid. This solid may actually be multiple solids.
            //This step removes all previous relationships and rebuilds them.
            if (positiveSideFaces.Count > 3 && negativeSideFaces.Count > 3)
            {
                var positiveSideSolid = ts.BuildNewFromOld(positiveSideFaces);
                //5. Split the tesselated solid into multiple solids if necessary
                positiveSideSolids = new List<TessellatedSolid>(MiscFunctions.GetMultipleSolids(positiveSideSolid));

                //6. Repeat steps 4-5 for the negative side
                var negativeSideSolid = ts.BuildNewFromOld(negativeSideFaces);
                negativeSideSolids = new List<TessellatedSolid>(MiscFunctions.GetMultipleSolids(negativeSideSolid));
            }
            else //There was no cut made. Return the original tesselated solid.
            {
                if (positiveSideFaces.Count > 3) positiveSideSolids.Add(ts);
                else if (negativeSideFaces.Count > 3) negativeSideSolids.Add(ts);
                else throw new Exception("Error");
            }
        }

        private static void DivideUpFaces(TessellatedSolid ts, Flat plane, out List<PolygonalFace> positiveSideFaces,
            out List<PolygonalFace> negativeSideFaces, out List<Vertex> positiveSideLoopVertices, 
            out List<Vertex> negativeSideLoopVertices)  
        {
            const double tolerance = 0.00001;
            positiveSideFaces = new List<PolygonalFace>();
            negativeSideFaces = new List<PolygonalFace>();
            var checkSumMultiplier = (int)Math.Pow(10, (int)Math.Floor(Math.Log10(ts.Faces.Count() * 2 + 2)) + 1);
            //Set the distance of every vertex in the solid to the plane, and reset the index in list
            var distancesToPlane = new List<double>();
            var pointOnPlane = plane.Normal.multiply(plane.DistanceToOrigin);
            for (int i = 0; i < ts.NumberOfVertices; i++)
            {
                var distance = ts.Vertices[i].Position.subtract(pointOnPlane).dotProduct(plane.Normal);
                distancesToPlane.Add(distance);
                ts.Vertices[i].IndexInList = i;
            }

            //Find all the straddle edges and add the new intersect vertices to both the pos and nef loops.
            var straddleEdges = new List<StraddleEdge>();
            positiveSideLoopVertices = new List<Vertex>(); 
            negativeSideLoopVertices = new List<Vertex>(); 
            foreach (var edge in ts.Edges)
            {
                //Reset the checksum values, just in case.
                edge.EdgeReference = GetCheckSum(edge.From, edge.To, checkSumMultiplier);
                var toDistance = distancesToPlane[edge.To.IndexInList];
                var fromDistance = distancesToPlane[edge.From.IndexInList];
                //Check for a straddle edge
                if ((toDistance > tolerance && fromDistance < -tolerance) || (toDistance < -tolerance && fromDistance > tolerance))
                {
                    var straddleEdge = new StraddleEdge(edge, plane);
                    straddleEdges.Add(straddleEdge);
                    positiveSideLoopVertices.Add(straddleEdge.IntersectVertex);
                    negativeSideLoopVertices.Add(straddleEdge.IntersectVertex);
                }
            }
            
            //Categorize all the faces in the solid
            var newOnPlaneEdges = new List<Edge>(); //Place holder for debugging.
            foreach (var face in ts.Faces)
            {
                
                if (face.Vertices.Count() != 3) throw new Exception("This was written with triangles in mind, ONLY");
                if (face.Edges.Count() != 3) throw new Exception("This was written with triangles in mind, ONLY");
                //Categorize the vertices of the face
                var positiveSideVertices = new List<Vertex>();
                var negativeSideVertices = new List<Vertex>();
                var onPlaneVerticesFromFace = new List<Vertex>();
                foreach (var vertex in face.Vertices)
                {
                    if (vertex.IndexInList == 4) vertex.IndexInList = 4;
                    var distance = distancesToPlane[vertex.IndexInList];
                    if (Math.Abs(distance) < tolerance) onPlaneVerticesFromFace.Add(vertex);
                    else if (Math.Sign(distance) > 0) positiveSideVertices.Add(vertex);
                    else if (Math.Sign(distance) < 0) negativeSideVertices.Add(vertex);
                    else throw new Exception("Error: one of the three options above must be true.");
                }
                //If a straddle face, split that face.  
                //Note that this will create many duplicate vertices
                if (positiveSideVertices.Count > 0 && negativeSideVertices.Count > 0)
                {
                    var numberOfNewVertices = positiveSideVertices.Count + negativeSideVertices.Count - 1;
                    //One vertex is on the positive side, one on the negative side, and one on the plane.
                    if (numberOfNewVertices == 1)
                    {
                        var checksum = GetCheckSum(positiveSideVertices[0], negativeSideVertices[0], checkSumMultiplier);
                        var newVertex = straddleEdges.First(s => s.EdgeReference == checksum).IntersectVertex;
                        if (newVertex == null) throw new Exception();
                        newOnPlaneEdges.Add(new Edge(onPlaneVerticesFromFace[0], newVertex, true));
                        positiveSideFaces.Add(new PolygonalFace(new [] {onPlaneVerticesFromFace[0], newVertex, positiveSideVertices[0] }, face.Normal));
                        negativeSideFaces.Add(new PolygonalFace(new [] {onPlaneVerticesFromFace[0], newVertex, negativeSideVertices[0] }, face.Normal));
                    } 
                    //Two vertices are on the positive side, and one is on the negative side.
                    else if (numberOfNewVertices == 2 && positiveSideVertices.Count == 2 ) 
                    {
                        //Find the straddle edge that contains both the positive[0] and negative[0] vertex
                        var checksum1 = GetCheckSum(positiveSideVertices[0], negativeSideVertices[0], checkSumMultiplier);
                        var checksum2 = GetCheckSum(positiveSideVertices[1], negativeSideVertices[0], checkSumMultiplier);
                        var newVertex1 = straddleEdges.First(s => s.EdgeReference == checksum1).IntersectVertex;
                        var newVertex2 = straddleEdges.First(s => s.EdgeReference == checksum2).IntersectVertex;
                        if (newVertex1 == null || newVertex2 == null) throw new Exception();
                        newOnPlaneEdges.Add(new Edge(newVertex1, newVertex2, true));
                        positiveSideFaces.Add(new PolygonalFace(new []{positiveSideVertices[0], positiveSideVertices[1], newVertex1},face.Normal));
                        positiveSideFaces.Add(new PolygonalFace(new []{positiveSideVertices[1], newVertex1, newVertex2},face.Normal));
                        negativeSideFaces.Add(new PolygonalFace(new []{newVertex1, newVertex2, negativeSideVertices[0]},face.Normal));
                    }
                    //Two vertices are on the negative side, and one is on the positive side.
                    else if (numberOfNewVertices == 2 && negativeSideVertices.Count == 2 ) 
                    {
                        var checksum1 = GetCheckSum(positiveSideVertices[0], negativeSideVertices[0], checkSumMultiplier);
                        var checksum2 = GetCheckSum(positiveSideVertices[0], negativeSideVertices[1], checkSumMultiplier);
                        var newVertex1 = straddleEdges.First(s => s.EdgeReference == checksum1).IntersectVertex;
                        var newVertex2 = straddleEdges.First(s => s.EdgeReference == checksum2).IntersectVertex;
                        if (newVertex1 == null || newVertex2 == null) throw new Exception();
                        newOnPlaneEdges.Add(new Edge(newVertex1, newVertex2, true));
                        positiveSideFaces.Add(new PolygonalFace(new []{newVertex1, newVertex2, positiveSideVertices[0]},face.Normal));
                        negativeSideFaces.Add(new PolygonalFace(new []{negativeSideVertices[0], negativeSideVertices[1], newVertex1},face.Normal));
                        negativeSideFaces.Add(new PolygonalFace(new []{negativeSideVertices[1], newVertex1, newVertex2},face.Normal));
                    }
                    else throw new Exception("Error: one of the two options above must be true.");
                }
                else if (positiveSideVertices.Count > 0)
                {
                    positiveSideFaces.Add(face);
                    foreach (var vertex in onPlaneVerticesFromFace)
                    {
                        if(!positiveSideLoopVertices.Contains(vertex)) positiveSideLoopVertices.Add(vertex);
                    }
                }
                else if (negativeSideVertices.Count > 0)
                {
                    negativeSideFaces.Add(face);
                    foreach (var vertex in onPlaneVerticesFromFace)
                    {
                        if (!negativeSideLoopVertices.Contains(vertex)) negativeSideLoopVertices.Add(vertex);
                    }
                }
                //Else: Do nothing with On-Plane faces.
            }
        }

        internal static int GetCheckSum(Vertex vertex1, Vertex vertex2, int checkSumMultiplier)
        {
            if (vertex1.IndexInList == vertex2.IndexInList) throw new Exception("edge to same vertices.");
            //Multiply larger value by checksum in case lower value == 0;
            var checksum = (vertex1.IndexInList < vertex2.IndexInList)
                ? vertex1.IndexInList + (checkSumMultiplier * (vertex2.IndexInList))
                : vertex2.IndexInList + (checkSumMultiplier * (vertex1.IndexInList));
            return checksum;
        }

        /// <summary>
        /// Straddle edge references original edge and an intersection vertex.
        /// </summary>
        public class StraddleEdge
        {
            /// <summary>
            /// Point of edge / plane intersection
            /// </summary>
            public Vertex IntersectVertex;
            /// <summary>
            /// Original edge checksum reference
            /// </summary>
            public int EdgeReference;

            internal StraddleEdge(Edge edge, Flat plane)
            {
                if (edge.EdgeReference == 0) throw new Exception("Edge reference has not been set");
                EdgeReference = edge.EdgeReference;
                IntersectVertex = MiscFunctions.PointOnPlaneFromIntersectingLine(plane.Normal, plane.DistanceToOrigin, edge.To, edge.From);
            }
        }

        private static List<List<Vertex>> FindLoops(IList<Vertex> onPlaneVertices, IEnumerable<PolygonalFace> onSideFaces)
        {
            //Set a new reference index for each vertex, because some of the vertices are new (from straddle edges)
            var onPlaneEdges = new List<Edge>();
            var onPlaneEdgeHash = new HashSet<Edge>();
            var hashFaces = new HashSet<PolygonalFace>(onSideFaces);
            var onPlaneVerticesHash = new HashSet<Vertex>(onPlaneVertices);
            for (var i = 0; i < onPlaneVertices.Count(); i++ )
            {
                var vertex1 = onPlaneVertices[i];
                vertex1.IndexInList = i;
                foreach(var edge in vertex1.Edges)
                {
                    var vertex2 = edge.OtherVertex(vertex1);
                    if (!onPlaneVerticesHash.Contains(vertex2)) continue;
                    if (onPlaneEdgeHash.Contains(edge))
                    {
                        //keep any new edges (they will have a null reference)
                        if (edge.OwnedFace == null || edge.OtherFace == null)
                        {
                            onPlaneEdges.Add(edge);
                        }
                        //Else, keep any edge that has at least one face on this side of plane
                        else if(hashFaces.Contains(edge.OwnedFace) || hashFaces.Contains(edge.OtherFace))
                        {
                            onPlaneEdges.Add(edge);
                        }
                        onPlaneEdgeHash.Remove(edge);
                    }
                    else onPlaneEdgeHash.Add(edge);
                }
            }
            if (onPlaneEdgeHash.Count != 0) throw new Exception("tempHash should be empty, since the edge should have come up twice");

            //Every vertex that is on Plane is on a loop.
            var loops = new List<List<Vertex>>();
            var remainingEdges = new List<Edge>(onPlaneEdges);
            while(remainingEdges.Count > 0)
            {
                var loop = new List<Vertex>();
                var startVertex = remainingEdges[0].From;
                var newStartVertex = remainingEdges[0].To;
                loop.Add(newStartVertex);
                remainingEdges.RemoveAt(0);
                do
                {
                    var possibleNextEdges = remainingEdges.Where(e => e.To == newStartVertex || e.From == newStartVertex).ToList();
                    if (possibleNextEdges.Count() != 1) throw new Exception("Should always be == to 1");
                    newStartVertex = possibleNextEdges[0].OtherVertex(newStartVertex);
                    loop.Add(newStartVertex);
                    remainingEdges.Remove(possibleNextEdges[0]);
                }
                while (newStartVertex != startVertex);
                loops.Add(loop);
            }
            return loops;
        }
        #endregion
    }
}