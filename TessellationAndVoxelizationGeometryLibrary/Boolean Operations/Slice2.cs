using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using StarMathLib;
using TVGL;

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
            //1. Divide up the faces into either negative or positive. OnPlane faces are not used. Straddle faces are split into 2 or 3 new faces.
            DivideUpFaces(ts, plane, out negativeSideFaces, out positiveSideFaces, out positiveSideLoopVertices, out negativeSideLoopVertices);
            //2. Find loops to define the missing space on the plane
            var positiveSideLoops = FindLoops(positiveSideLoopVertices);
            //3. Triangulate that empty space and add to list 
            var triangles = TriangulatePolygon.Run(positiveSideLoops, plane.Normal);
            foreach (var triangle in triangles)
            {
                var newFace = new PolygonalFace(triangle, plane.Normal);
                positiveSideFaces.Add(newFace);
            }
            //4. Create a new tesselated solid. This solid may actually be multiple solids.
            //This step removes all previous relationships and rebuilds them.
            var positiveSideSolid = new TessellatedSolid(positiveSideFaces);
            //5. Split the tesselated solid into multiple solids if necessary

            //6. Repeat steps 2-5 for the negative side
            var negativeSideLoops = FindLoops(negativeSideLoopVertices);
            triangles = TriangulatePolygon.Run(negativeSideLoops, plane.Normal);
            foreach (var triangle in triangles)
            {
                var newFace = new PolygonalFace(triangle, plane.Normal);
                negativeSideFaces.Add(newFace);
            }
            var negativeSideSolid = new TessellatedSolid(negativeSideFaces);
        }

        private static void DivideUpFaces(TessellatedSolid ts, Flat plane, out List<PolygonalFace> positiveSideFaces,
            out List<PolygonalFace> negativeSideFaces, out List<Vertex> positiveSideLoopVertices, out List<Vertex> negativeSideLoopVertices)  
        {
            var tolerance = StarMath.EqualityTolerance;
            positiveSideFaces = new List<PolygonalFace>();
            negativeSideFaces = new List<PolygonalFace>();
            var onPlaneVertices = new List<Vertex>();
            var checkSumMultiplier = (int)Math.Pow(10, (int)Math.Floor(Math.Log10(ts.Faces.Count() * 2 + 2)) + 1);
            //Set the distance of every vertex in the solid to the plane
            var distancesToPlane = new List<double>();
            var pointOnPlane = plane.Normal.multiply(plane.DistanceToOrigin);
            for (int i = 0; i < ts.NumberOfVertices; i++)
            {
                var distance = ts.Vertices[i].Position.subtract(pointOnPlane).dotProduct(plane.Normal);
                distancesToPlane.Add(distance);
                ts.Vertices[i].ReferenceIndex = i;
                if (Math.Abs(distance) < tolerance) onPlaneVertices.Add(ts.Vertices[i]);
            }

            //Find all the straddle edges and add their intersect vertices to onPlaneVertices
            var straddleEdges = new List<StraddleEdge>();
            positiveSideLoopVertices = new List<Vertex>();
            negativeSideLoopVertices = new List<Vertex>();
            foreach (var edge in ts.Edges)
            {
                var toDistance = distancesToPlane[edge.To.IndexInList];
                var fromDistance = distancesToPlane[edge.From.IndexInList];
                if ((toDistance > tolerance && fromDistance < -tolerance) || (toDistance < -tolerance && fromDistance > tolerance))
                {
                    var straddleEdge = new StraddleEdge(edge, plane);
                    straddleEdges.Add(straddleEdge);
                    onPlaneVertices.Add(straddleEdge.IntersectVertex);
                    positiveSideLoopVertices.Add(straddleEdge.IntersectVertex);
                    negativeSideLoopVertices.Add(straddleEdge.IntersectVertex);
                }
            }
            
            //Categorize all the faces in the solid
            var straddleFaces = new List<PolygonalFace>();
            var onPlaneFaces = new List<PolygonalFace>();
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
                    var distance = distancesToPlane[vertex.ReferenceIndex];
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
                        positiveSideFaces.Add(new PolygonalFace(new Vertex[] {onPlaneVerticesFromFace[0], newVertex, positiveSideVertices[0] }, face.Normal));
                        negativeSideFaces.Add(new PolygonalFace(new Vertex[] {onPlaneVerticesFromFace[0], newVertex, negativeSideVertices[0] }, face.Normal));
                    } 
                    //Two vertices are on the positive side, and one is on the negative side.
                    else if (numberOfNewVertices == 2 && positiveSideVertices.Count == 2 ) 
                    {
                        //Find the straddle edge that contains both the positive[0] and negative[0] vertex
                        var checksum1 = GetCheckSum(positiveSideVertices[0], negativeSideVertices[0], checkSumMultiplier);
                        var checksum2 = GetCheckSum(positiveSideVertices[1], negativeSideVertices[0], checkSumMultiplier);
                        var newVertex1 = straddleEdges.First(s => s.EdgeReference == checksum1).IntersectVertex;
                        var newVertex2 = straddleEdges.First(s => s.EdgeReference == checksum2).IntersectVertex;
                        positiveSideFaces.Add(new PolygonalFace(new Vertex[]{positiveSideVertices[0], positiveSideVertices[1], newVertex1},face.Normal));
                        positiveSideFaces.Add(new PolygonalFace(new Vertex[]{positiveSideVertices[1], newVertex1, newVertex2},face.Normal));
                        negativeSideFaces.Add(new PolygonalFace(new Vertex[]{newVertex1, newVertex2, negativeSideVertices[0]},face.Normal));
                    }
                    //Two vertices are on the negative side, and one is on the positive side.
                    else if (numberOfNewVertices == 2 && negativeSideVertices.Count == 2 ) 
                    {
                        var checksum1 = GetCheckSum(positiveSideVertices[0], negativeSideVertices[0], checkSumMultiplier);
                        var checksum2 = GetCheckSum(positiveSideVertices[0], negativeSideVertices[1], checkSumMultiplier);
                        var newVertex1 = straddleEdges.First(s => s.EdgeReference == checksum1).IntersectVertex;
                        var newVertex2 = straddleEdges.First(s => s.EdgeReference == checksum2).IntersectVertex;
                        positiveSideFaces.Add(new PolygonalFace(new Vertex[]{newVertex1, newVertex2, positiveSideVertices[0]},face.Normal));
                        negativeSideFaces.Add(new PolygonalFace(new Vertex[]{negativeSideVertices[0], negativeSideVertices[1], newVertex1},face.Normal));
                        negativeSideFaces.Add(new PolygonalFace(new Vertex[]{negativeSideVertices[1], newVertex1, newVertex2},face.Normal));
                    }
                    else throw new Exception("Error: one of the two options above must be true.");
                }
                else if (positiveSideVertices.Count > 0)
                {
                    positiveSideFaces.Add(face);
                    foreach (var vertex in onPlaneVerticesFromFace)
                    {
                        if(positiveSideLoopVertices.Contains(vertex)) continue;
                        positiveSideLoopVertices.Add(vertex);
                    }
                }
                else if (negativeSideVertices.Count > 0)
                {
                    negativeSideFaces.Add(face);
                    foreach (var vertex in onPlaneVerticesFromFace)
                    {
                        if (negativeSideLoopVertices.Contains(vertex)) continue;
                        negativeSideLoopVertices.Add(vertex);
                    }
                }
                else onPlaneFaces.Add(face);
            }
        }

        internal static int GetCheckSum(Vertex vertex1, Vertex vertex2, int checkSumMultiplier)
        {
            if (vertex1.IndexInList == vertex2.IndexInList) throw new Exception("edge to same vertices.");
            var checksum = (vertex1.IndexInList < vertex2.IndexInList)
                ? vertex1.IndexInList + (checkSumMultiplier * vertex2.IndexInList)
                : vertex2.IndexInList + (checkSumMultiplier * vertex1.IndexInList);
            return checksum;
        }

        public class StraddleEdge
        {
            public Vertex IntersectVertex;
            public int EdgeReference;

            internal StraddleEdge(Edge edge, Flat plane)
            {
                if (edge.EdgeReference == 0) throw new Exception("Edge reference has not been set");
                EdgeReference = edge.EdgeReference;
                IntersectVertex = MiscFunctions.PointOnPlaneFromIntersectingLine(plane.Normal, plane.DistanceToOrigin, edge.To, edge.From);
            }
        }

        private static List<List<Vertex>> FindLoops(List<Vertex> onPlaneVertices)
        {
            //Set a new reference index for each vertex, because some of the vertices are new (from straddle edges)
            var onPlaneEdges = new List<Edge>();
            var tempHash = new HashSet<Edge>();
            var onPlaneVerticesHash = new HashSet<Vertex>();
            for (var i = 0; i < onPlaneVertices.Count(); i++ )
            {
                var vertex1 = onPlaneVertices[i];
                vertex1.ReferenceIndex = i;
                foreach(var edge in vertex1.Edges)
                {
                    var vertex2 = edge.OtherVertex(vertex1);
                    if (onPlaneVerticesHash.Contains(vertex2))
                    {
                        if(tempHash.Contains(edge)) onPlaneEdges.Add(edge);
                        else tempHash.Add(edge);
                    }
                }
            }
            if (tempHash.Count > 0) throw new Exception("tempHash should be empty, since the edge should have come up twice");

            //Every vertex that is on Plane is on a loop.
            var loops = new List<List<Vertex>>();
            var remainingEdges = new List<Edge>(onPlaneEdges);
            while(remainingEdges.Count > 0)
            {
                var loop = new List<Vertex>();
                var startVertex = remainingEdges[0].From;
                var newStartVertex = remainingEdges[0].To;
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

        private static List<List<PolygonalFace>> FindSolids(List<PolygonalFace> faces, List<List<Vertex>> loops)
        {
            //Creates brand new solids from a list of normals and a list of vertices. Like ts.Duplicate.
            //Ignores all previous references and creates NEW ones.
            var solids = new List<List<PolygonalFace>>();
            return solids;
        }

        
        #endregion
    }
}