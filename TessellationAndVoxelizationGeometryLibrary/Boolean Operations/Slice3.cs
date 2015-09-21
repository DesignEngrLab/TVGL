using System;
using System.Collections.Generic;
using System.Linq;
using StarMathLib;

namespace TVGL.Boolean_Operations
{
    /// <summary>
    /// The Slice class includes static functions for cutting a tessellated solid.
    /// This slice function makes a seperate cut for the positive and negative side,
    /// at a specified offset in both directions. It rebuilds straddle triangles, 
    /// but only uses one of the two straddle edge intersection vertices to prevent
    /// tiny triangles from being created.
    /// </summary>
    public static class Slice3
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
            out List<TessellatedSolid> positiveSideSolids, out List<TessellatedSolid> negativeSideSolids, 
            double tolerance = Constants.Error)
        {
            positiveSideSolids = new List<TessellatedSolid>();
            negativeSideSolids = new List<TessellatedSolid>();
            List<PolygonalFace> positiveSideFaces;
            List<PolygonalFace> negativeSideFaces;
            List<Vertex> positiveSideLoopVertices;
            List<Vertex> negativeSideLoopVertices;
            //MiscFunctions.IsSolidBroken(ts);
            //1. Offset positive and get the positive faces.
            //Straddle faces are split into 2 or 3 new faces.
            DivideUpFaces(ts, plane, out positiveSideFaces, out positiveSideLoopVertices, 1, tolerance);
            DivideUpFaces(ts, plane, out negativeSideFaces, out negativeSideLoopVertices, -1, tolerance);
            //2. Find loops to define the missing space on the plane
            var positiveSideLoops = FindLoops(positiveSideLoopVertices);
            var negativeSideLoops = FindLoops(negativeSideLoopVertices);
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

        private static void DivideUpFaces(TessellatedSolid ts, Flat plane, out List<PolygonalFace> onSideFaces, out List<Vertex> loopVertices,
            int isPositiveSide, double tolerance = Constants.Error)
        {
            onSideFaces = new List<PolygonalFace>();
            //Set the distance of every vertex in the solid to the plane
            var distancesToPlane = new List<double>();
            var pointOnPlane = new double[3];
            var looserTolerance = Math.Sqrt(Math.Sqrt(tolerance)); //A looser tolerance is necessary to determine straddle edges
            //Because of the way distance to origin is found in relation to the normal, always add a positive offset to move further 
            //along direction of normal, and add a negative offset to move backward along normal.
            var offset = Math.Sqrt(looserTolerance) + looserTolerance;
            plane.DistanceToOrigin = plane.DistanceToOrigin + offset * isPositiveSide;
            var successfull = false;
            var originalDistanceToOrigin = plane.DistanceToOrigin;
            while (!successfull)
            {
                distancesToPlane = new List<double>();
                plane.DistanceToOrigin = plane.DistanceToOrigin + looserTolerance*isPositiveSide;
                pointOnPlane = plane.Normal.multiply(plane.DistanceToOrigin);
                for (int i = 0; i < ts.NumberOfVertices; i++)
                {
                    var distance = ts.Vertices[i].Position.subtract(pointOnPlane).dotProduct(plane.Normal);
                    if (Math.Abs(distance) < looserTolerance)
                    {
                        successfull = false;
                        break;
                    }
                    distancesToPlane.Add(distance);
                }
                if (distancesToPlane.Count == ts.NumberOfVertices) successfull = true;
            }

            //Find all the straddle edges and add the new intersect vertices to both the pos and neg loops.
            var straddleEdges = new List<StraddleEdge>();
            loopVertices = new List<Vertex>();
            foreach (var edge in ts.Edges)
            {
                //Reset the checksum value, if needed.
                if (edge.EdgeReference == 0) edge.EdgeReference = GetCheckSum(edge.From, edge.To);
                var toDistance = distancesToPlane[edge.To.IndexInList];
                var fromDistance = distancesToPlane[edge.From.IndexInList];
                //Check for a straddle edge
                if (((toDistance > looserTolerance) && (fromDistance < -looserTolerance)) ||
                    ((toDistance < -looserTolerance) && (fromDistance > looserTolerance)))
                {
                    //If it is a straddle edge
                    var straddleEdge = new StraddleEdge(edge, plane);
                    straddleEdges.Add(straddleEdge);
                    loopVertices.Add(straddleEdge.IntersectVertex);
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
                    //if (vertex.IndexInList == 2323) vertex.IndexInList = 2323; //DEBUG Line for finding a particular vertex
                    var distance = distancesToPlane[vertex.IndexInList];
                    if (Math.Abs(distance) < looserTolerance) onPlaneVerticesFromFace.Add(vertex);
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
                    if (numberOfNewVertices < 2)
                    {
                        throw new Exception("There should be NO vertices on the plane");
                    }
                    //Two vertices are on the positive side, and one is on the negative side.
                    else if (numberOfNewVertices == 2 && positiveSideVertices.Count == 2)
                    {
                        //Find the straddle edge that contains both the positive[0] and negative[0] vertex
                        var checksum1 = GetCheckSum(positiveSideVertices[0], negativeSideVertices[0]);
                        var checksum2 = GetCheckSum(positiveSideVertices[1], negativeSideVertices[0]);
                        var newVertex1 = straddleEdges.First(s => s.EdgeReference == checksum1).IntersectVertex;
                        var newVertex2 = straddleEdges.First(s => s.EdgeReference == checksum2).IntersectVertex;
                        if (newVertex1 == null || newVertex2 == null) throw new Exception();
                        newOnPlaneEdges.Add(new Edge(newVertex1, newVertex2, true));
                        if(isPositiveSide == 1)
                        {
                            onSideFaces.Add(new PolygonalFace(new[] { positiveSideVertices[0], positiveSideVertices[1], newVertex1 }, face.Normal));
                            onSideFaces.Add(new PolygonalFace(new[] { positiveSideVertices[1], newVertex1, newVertex2 }, face.Normal));
                        }
                        else 
                        {
                            onSideFaces.Add(new PolygonalFace(new[] { newVertex1, newVertex2, negativeSideVertices[0] }, face.Normal));
                        }
                    }
                    //Two vertices are on the negative side, and one is on the positive side.
                    else if (numberOfNewVertices == 2 && negativeSideVertices.Count == 2)
                    {
                        var checksum1 = GetCheckSum(positiveSideVertices[0], negativeSideVertices[0]);
                        var checksum2 = GetCheckSum(positiveSideVertices[0], negativeSideVertices[1]);
                        var newVertex1 = straddleEdges.First(s => s.EdgeReference == checksum1).IntersectVertex;
                        var newVertex2 = straddleEdges.First(s => s.EdgeReference == checksum2).IntersectVertex;
                        if (newVertex1 == null || newVertex2 == null) throw new Exception();
                        newOnPlaneEdges.Add(new Edge(newVertex1, newVertex2, true));
                        if (isPositiveSide == 1) 
                        {
                            onSideFaces.Add(new PolygonalFace(new[] { newVertex1, newVertex2, positiveSideVertices[0] }, face.Normal));
                        }
                        else
                        {
                             onSideFaces.Add(new PolygonalFace(new[] { negativeSideVertices[0], negativeSideVertices[1], newVertex1 }, face.Normal));
                             onSideFaces.Add(new PolygonalFace(new[] { negativeSideVertices[1], newVertex1, newVertex2 }, face.Normal));
                        }
                    }
                    else throw new Exception("Error: one of the two options above must be true.");
                }
                else if (isPositiveSide == 1 && positiveSideVertices.Count > 0)
                {
                    onSideFaces.Add(face);
                }
                else if (isPositiveSide == -1 && negativeSideVertices.Count > 0)
                {
                    onSideFaces.Add(face);
                }
                //Else: Do nothing with On-Plane faces.
            }
            //Reset back to original
             plane.DistanceToOrigin = originalDistanceToOrigin;
        }

        internal static int GetCheckSum(Vertex vertex1, Vertex vertex2)
        {
            var checkSumMultiplier = TessellatedSolid.CheckSumMultiplier;
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
                if (IntersectVertex == null) throw new Exception("Cannot Be Null");
            }
        }

        private static List<List<Vertex>> FindLoops(IList<Vertex> onPlaneVertices)
        {
            //Every vertex that is on Plane is on a loop.
            var loops = new List<List<Vertex>>();
            var vertices = new List<Vertex>(onPlaneVertices);
            while (vertices.Any())
            {
                var attempts = 0;
                var loop = new List<Vertex>();
                var removedVertices = new List<Vertex>();
                var startVertex = vertices[0];
                var currentEdge = startVertex.Edges[0];
                var newStartVertex = currentEdge.OtherVertex(startVertex);
                //Update the lists for newStartVertex.
                loop.Add(newStartVertex);
                vertices.Remove(newStartVertex);
                do
                {
                    Edge nextEdge = null;
                    if (newStartVertex.Edges.Count == 2)
                    {
                        foreach (var edge in newStartVertex.Edges)
                        {
                            if (edge == currentEdge) continue;
                            nextEdge = edge;
                            break;
                        }
                    }
                    else throw new Exception("This should always happen with Slice3");
                    newStartVertex = nextEdge.OtherVertex(newStartVertex);
                    loop.Add(newStartVertex);
                    vertices.Remove(newStartVertex);
                    currentEdge = nextEdge;
                    attempts++;
                }
                while (newStartVertex != startVertex && attempts < onPlaneVertices.Count());
                loops.Add(loop); 
            }
            return loops;
        }
        #endregion
    }
}