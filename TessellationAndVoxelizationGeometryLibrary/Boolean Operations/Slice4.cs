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
    public static class Slice4
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
            var looserTolerance = Math.Sqrt(tolerance); //A looser tolerance is necessary to determine straddle edges
            //Because of the way distance to origin is found in relation to the normal, always add a positive offset to move further 
            //along direction of normal, and add a negative offset to move backward along normal.
            var normal = plane.Normal.multiply(isPositiveSide);
            var distanceToOrigin = (plane.DistanceToOrigin + tolerance) * isPositiveSide;
            var successfull = false;
            while (!successfull)
            {
                distancesToPlane = new List<double>();
                distanceToOrigin = distanceToOrigin + looserTolerance * isPositiveSide;
                pointOnPlane = normal.multiply(distanceToOrigin);
                for (int i = 0; i < ts.NumberOfVertices; i++)
                {
                    var distance = ts.Vertices[i].Position.subtract(pointOnPlane).dotProduct(normal);
                    if (Math.Abs(distance) < looserTolerance)
                    {
                        successfull = false;
                        break;
                    }
                    distancesToPlane.Add(distance);
                }
                if (distancesToPlane.Count == ts.NumberOfVertices) successfull = true;
            }

            //Find all the straddle edges
            var straddleEdges = new List<StraddleEdge>();
            loopVertices = new List<Vertex>();
            foreach (var edge in ts.Edges)
            {
                //Reset the checksum value, if needed.
                if (edge.EdgeReference == 0) edge.EdgeReference = GetCheckSum(edge.From, edge.To);
                var toDistance = distancesToPlane[edge.To.IndexInList];
                var fromDistance = distancesToPlane[edge.From.IndexInList];
                //Check for a straddle edge
                if (toDistance > looserTolerance && fromDistance < -looserTolerance)
                {
                    //If it is a straddle edge, add it to the list. The To vertex is onSide.
                    var straddleEdge = new StraddleEdge(edge, plane, edge.To);
                    straddleEdges.Add(straddleEdge);
                }
                else if (toDistance < -looserTolerance && fromDistance > looserTolerance)
                {
                    //If it is a straddle edge, add it to the list. The From vertex is onSide.
                    var straddleEdge = new StraddleEdge(edge, plane, edge.From);
                    straddleEdges.Add(straddleEdge);
                }
            }

            //Categorize all the faces in the solid
            var newOnPlaneEdges = new List<Edge>(); //Place holder for debugging.
            var straddleFaces = new List<StraddleFace>();
            foreach (var face in ts.Faces)
            {
                if (face.Vertices.Count() != 3) throw new Exception("This was written with triangles in mind, ONLY");
                if (face.Edges.Count() != 3) throw new Exception("This was written with triangles in mind, ONLY");
                //Categorize the vertices of the face
                var onSideVertices = new List<Vertex>();
                var offSideVertices = new List<Vertex>();
                foreach (var vertex in face.Vertices)
                {
                    //if (vertex.IndexInList == 2323) vertex.IndexInList = 2323; //DEBUG Line for finding a particular vertex
                    var distance = distancesToPlane[vertex.IndexInList];
                    if (Math.Abs(distance) < looserTolerance) throw new Exception("There should be NO vertices on the plane");
                    else if (Math.Sign(distance) > 0) onSideVertices.Add(vertex);
                    else if (Math.Sign(distance) < 0) offSideVertices.Add(vertex);
                    else throw new Exception("Error: one of the three options above must be true.");
                }
                if (onSideVertices.Count > 0 && offSideVertices.Count > 0)
                {
                    var checksum1 = -1;
                    var checksum2 = -1;
                    //Two vertices are on the positive side, and one is on the negative side.
                    if (onSideVertices.Count == 2 && offSideVertices.Count == 1)
                    {
                        //Find the straddle edges that are on this face
                        checksum1 = GetCheckSum(onSideVertices[0], offSideVertices[0]);
                        checksum2 = GetCheckSum(onSideVertices[1], offSideVertices[0]);
                    }
                    //Two vertices are on the negative side, and one is on the positive side.
                    else if (offSideVertices.Count == 2 && onSideVertices.Count == 1)
                    {
                        //Find the straddle edges that are on this face
                        checksum1 = GetCheckSum(onSideVertices[0], offSideVertices[0]);
                        checksum2 = GetCheckSum(onSideVertices[0], offSideVertices[1]);
                    }
                    else throw new Exception("Error: one of the two options above must be true.");
                    //Create a new straddle face from the straddle edges.
                    var straddleEdge1 = straddleEdges.First(s => s.EdgeReference == checksum1);
                    var straddleEdge2 = straddleEdges.First(s => s.EdgeReference == checksum2);
                    straddleFaces.Add(new StraddleFace(face, new List<StraddleEdge> { straddleEdge1, straddleEdge2 }));
                }
                else if (onSideVertices.Count > 0)
                {
                    onSideFaces.Add(face);
                }
                //Else: Do nothing with On-Plane faces.
            }

            //Now, we need to create new triangles to replace the straddle faces.
            for (var i = 0; i < straddleFaces.Count; i++)
            {
                //First, find all the straddle faces that form a flat.
                var straddleFace = straddleFaces[i];
                straddleFaces.Remove(straddleFace);
                //For each straddle edge
                var vertexLoops = new List<List<Vertex>>();
                var vertexLoop = new List<Vertex>();
                for (var j = 0; j < 2; j++)
                {
                    var straddleEdge = straddleFace.StraddleEdges[j];
                    var startFace = straddleFace.Face;
                    var currentFace = straddleFace.Face;
                    vertexLoop = new List<Vertex>();
                    vertexLoop.Add(straddleEdge.OnSideVertex);
                    PolygonalFace nextFace;
                    var flat = true;
                    do
                    {
                        if (currentFace == straddleEdge.Edge.OwnedFace) nextFace = straddleEdge.Edge.OtherFace;
                        else nextFace = straddleEdge.Edge.OwnedFace;
                        var dot = Math.Abs(Math.Abs(nextFace.Normal.dotProduct(startFace.Normal)) - 1);
                        var currentStraddleFace = straddleFaces.FirstOrDefault(f => f.Face == nextFace);
                        if (dot < Constants.ErrorForFaceInSurface && currentStraddleFace != null)
                        {
                            straddleEdge = currentStraddleFace.OtherStraddleEdge(straddleEdge);
                            currentFace = nextFace;
                            straddleFaces.Remove(currentStraddleFace);
                            if (!vertexLoop.Contains(straddleEdge.OnSideVertex)) vertexLoop.Add(straddleEdge.OnSideVertex);
                        }
                        else
                        {
                            //Add the intersection vertex from the final straddle edge in this direction
                            vertexLoop.Add(straddleEdge.IntersectVertex);
                            flat = false;
                        }
                    } while (flat);
                    vertexLoops.Add(vertexLoop);
                }
                //Make one loop from the two loops above.
                vertexLoop = new List<Vertex>(vertexLoops[0]);
                //If they start at the same vertex, remove that vertex from the list
                if (vertexLoops[0][0] == vertexLoops[1][0]) vertexLoops[1].RemoveAt(0);
                vertexLoops[1].Reverse();
                vertexLoop.AddRange(vertexLoops[1]);
                var triangles = TriangulatePolygon.Run(new List<List<Vertex>> { vertexLoop }, straddleFace.Normal);

                //Create new faces from the triangles
                foreach (var triangle in triangles)
                {
                    onSideFaces.Add(new PolygonalFace(triangle, normal, true, true));
                }
            }
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

            public Vertex OnSideVertex;

            public Edge Edge;

            internal StraddleEdge(Edge edge, Flat plane, Vertex onSideVertex)
            {
                Edge = edge;
                if (edge.EdgeReference == 0) throw new Exception("Edge reference has not been set");
                EdgeReference = edge.EdgeReference;
                OnSideVertex = onSideVertex;
                IntersectVertex = MiscFunctions.PointOnPlaneFromIntersectingLine(plane.Normal, plane.DistanceToOrigin, edge.To, edge.From);
                if (IntersectVertex == null) throw new Exception("Cannot Be Null");
            }
        }

        public class StraddleFace
        {
            public List<StraddleEdge> StraddleEdges;
            public double[] Normal;
            public PolygonalFace Face;

            internal StraddleFace(PolygonalFace face, List<StraddleEdge> straddleEdges)
            {
                Face = face;
                Normal = face.Normal;
                StraddleEdges = straddleEdges;
            }

            public StraddleEdge OtherStraddleEdge(StraddleEdge straddleEdge)
            {
                if (StraddleEdges[0] == null || StraddleEdges[1] == null) return null;
                if (straddleEdge == StraddleEdges[0]) return StraddleEdges[1];
                else if (straddleEdge == StraddleEdges[1]) return StraddleEdges[0];
                else throw new Exception();
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