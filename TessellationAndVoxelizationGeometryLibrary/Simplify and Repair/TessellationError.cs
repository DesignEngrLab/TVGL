using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using StarMathLib;

namespace TVGL
{
    /// <summary>
    /// Stores errors in the tesselated solid
    /// </summary>
    public class TessellationError
    {
        private double _edgeFaceRatio = double.NaN;

        #region Puplic Properties
        /// <summary>
        /// Edges that are used by more than two faces
        /// </summary>
        public List<Tuple<Edge, List<PolygonalFace>>> OverusedEdges { get; private set; }
        /// <summary>
        /// Edges that only have one face
        /// </summary>
        public List<Tuple <Edge,bool>> SingledSidedEdges { get; private set; }
        /// <summary>
        /// Faces with errors
        /// </summary>
        public List<int[]> DegenerateFaces { get; private set; }
        /// <summary>
        /// Duplicate Faces
        /// </summary>
        public List<int[]> DuplicateFaces { get; private set; }
        /// <summary>
        /// Faces with more that three vertices or 3 edges
        /// </summary>
        public List<PolygonalFace> NonTriangularFaces { get; private set; }
        /// <summary>
        /// Faces with only one vertex
        /// </summary>
        public List<PolygonalFace> FacesWithOneVertex { get; private set; }
        /// <summary>
        /// Faces with only one edge
        /// </summary>
        public List<PolygonalFace> FacesWithOneEdge { get; private set; }
        /// <summary>
        /// Faces with only two vertices
        /// </summary>
        public List<PolygonalFace> FacesWithTwoVertices { get; private set; }
        /// <summary>
        /// Faces with only two edges
        /// </summary>
        public List<PolygonalFace> FacesWithTwoEdges { get; private set; }
        /// <summary>
        /// Faces with negligible area (which is not necessarily an error)
        /// </summary>
        public List<PolygonalFace> FacesWithNegligibleArea { get; private set; }
        /// <summary>
        /// Edges that do not link back to faces that link to them
        /// </summary>
        public List<Tuple<PolygonalFace, Edge>> EdgesThatDoNotLinkBackToFace { get; private set; }
        /// <summary>
        /// Edges that do not link back to vertices that link to them
        /// </summary>
        public List<Tuple<Vertex, Edge>> EdgesThatDoNotLinkBackToVertex { get; private set; }
        /// <summary>
        /// Vertices that do not link back to faces that link to them
        /// </summary>
        public List<Tuple<PolygonalFace, Vertex>> VertsThatDoNotLinkBackToFace { get; private set; }
        /// <summary>
        /// Vertices that do not link back to edges that link to them
        /// </summary>
        public List<Tuple<Edge, Vertex>> VertsThatDoNotLinkBackToEdge { get; private set; }
        /// <summary>
        /// Faces that do not link back to edges that link to them
        /// </summary>
        public List<Tuple<Edge, PolygonalFace>> FacesThatDoNotLinkBackToEdge { get; private set; }
        /// <summary>
        /// Faces that do not link back to vertices that link to them
        /// </summary>
        public List<Tuple<Vertex, PolygonalFace>> FacesThatDoNotLinkBackToVertex { get; private set; }
        /// <summary>
        /// Faces that are missing adjacency to other faces
        /// </summary>
        public List<PolygonalFace> FacesWithMissingAdjacency { get; private set; }
        /// <summary>
        /// Edges with bad angles
        /// </summary>
        public List<Edge> EdgesWithBadAngle { get; private set; }
        /// <summary>
        /// Edges to face ratio 
        /// </summary>
        // ReSharper disable once ConvertToAutoProperty
        public double EdgeFaceRatio
        {
            get { return _edgeFaceRatio; }
            private set { _edgeFaceRatio = value; }
        }
        /// <summary>
        /// Whether ts.Errors contains any errors that need to be resolved
        /// </summary>
        public bool NoErrors { get; private set; }

        #endregion

        #region Check Model Integrity
        /// <summary>
        /// Checks the model integrity.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="repairAutomatically">The repair automatically.</param>
        public static void CheckModelIntegrity(TessellatedSolid ts, bool repairAutomatically = true)
        {
            if (ts.Errors == null) ts.Errors = new TessellationError();
            ts.Errors.NoErrors = true;
            Message.output("Model Integrity Check...", 3);
            if (ts.MostPolygonSides > 3) StoreHigherThanTriFaces(ts);
            var edgeFaceRatio = ts.NumberOfEdges / (double)ts.NumberOfFaces;
            if (ts.MostPolygonSides == 3 && !edgeFaceRatio.IsPracticallySame(1.5)) StoreEdgeFaceRatio(ts, edgeFaceRatio);
            //Check if each face has cyclic references with each edge, vertex, and adjacent faces.
            foreach (var face in ts.Faces)
            {
                if (face.Vertices.Count == 1) StoreFaceWithOneVertex(ts, face);
                if (face.Vertices.Count == 2) StoreFaceWithTwoVertices(ts, face);
                if (face.Edges.Count == 1) StoreFaceWithOneEdge(ts, face);
                if (face.Edges.Count == 2) StoreFaceWithOTwoEdges(ts, face);
                if (face.Area.IsNegligible(ts.SameTolerance)) StoreFaceWithNegligibleArea(ts, face);
                foreach (var edge in face.Edges.Where(edge => edge.OwnedFace != face && edge.OtherFace != face))
                    StoreEdgeDoesNotLinkBackToFace(ts, face, edge);
                foreach (var vertex in face.Vertices.Where(vertex => !vertex.Faces.Contains(face)))
                    StoreVertexDoesNotLinkBackToFace(ts, face, vertex);
                if (face.AdjacentFaces.Any(adjacentFace => adjacentFace == null))
                    StoreFaceWithMissingAdjacency(ts, face);    
            }
            foreach (var face in ts.Errors.FacesWithMissingAdjacency)
            {
                var adjFaces = face.AdjacentFaces;
                for (int i = 0; i < adjFaces.Count; i++)
                    if (adjFaces[i] == null) StoreSingleSidedEdges(ts, face.Edges[i], (face.Edges[i].OwnedFace == null));
            }
            //Check if each edge has cyclic references with each vertex and each face.
            foreach (var edge in ts.Edges)
            {
                if (!edge.OwnedFace.Edges.Contains(edge)) StoreFaceDoesNotLinkBackToEdge(ts, edge, edge.OwnedFace);
                if (!edge.OtherFace.Edges.Contains(edge)) StoreFaceDoesNotLinkBackToEdge(ts, edge, edge.OtherFace);
                if (!edge.To.Edges.Contains(edge)) StoreVertDoesNotLinkBackToEdge(ts, edge, edge.To);
                if (!edge.From.Edges.Contains(edge)) StoreVertDoesNotLinkBackToEdge(ts, edge, edge.From);
                if (double.IsNaN(edge.InternalAngle) || edge.InternalAngle < 0 || edge.InternalAngle > 2*Math.PI)  
                    StoreEdgeHasBadAngle(ts, edge);
            }
            //Check if each vertex has cyclic references with each edge and each face.
            foreach (var vertex in ts.Vertices)
            {
                foreach (var edge in vertex.Edges.Where(edge => edge.To != vertex && edge.From != vertex))
                    StoreEdgeDoesNotLinkBackToVertex(ts, vertex, edge);
                foreach (var face in vertex.Faces.Where(face => !face.Vertices.Contains(vertex)))
                    StoreFaceDoesNotLinkBackToVertex(ts, vertex, face);
            }
            if (ts.Errors.NoErrors)
            {
                Message.output("** Model contains no errors.",3);
                return;
            }
            if (repairAutomatically)
            {
                Message.output("Some errors found. Attempting to Repair...",2);
                var success = ts.Errors.Repair(ts);
                if (success)
                {
                    ts.Errors = null;
                    Message.output("Repair successfully fixed the model.",2);
                }
                else Message.output("Repair did not successfully fix all the problems.",1);
                CheckModelIntegrity(ts, false);
                return;
            }
            ts.Errors.Report();
        }

        /// <summary>
        /// Report out any errors
        /// </summary>
        public void Report()
        {
            if (3 > (int)Message.Verbosity) return;
            Message.output("Errors found in model:");
            Message.output("======================");
            if (NonTriangularFaces != null)
                Message.output("==> " + NonTriangularFaces.Count + " faces are polygons with more than 3 sides.");
            if (!double.IsNaN(EdgeFaceRatio))
                Message.output("==> Edges / Faces = " + EdgeFaceRatio + ", but it should be 1.5.");
            if (OverusedEdges != null)
            {
                Message.output("==> " + OverusedEdges.Count + " overused edges.");
                Message.output("    The number of faces per overused edge: " +
                                OverusedEdges.Select(p => p.Item2.Count).MakePrintString());
            }
            if (SingledSidedEdges != null) Message.output("==> " + SingledSidedEdges.Count + " single-sided edges.");
            if (DegenerateFaces != null) Message.output("==> " + DegenerateFaces.Count + " degenerate faces in file.");
            if (DuplicateFaces != null) Message.output("==> " + DuplicateFaces.Count + " duplicate faces in file.");
            if (FacesWithOneVertex != null)
                Message.output("==> " + FacesWithOneVertex.Count + " faces with only one vertex.");
            if (FacesWithOneEdge != null) Message.output("==> " + FacesWithOneEdge.Count + " faces with only one edge.");
            if (FacesWithTwoVertices != null) Message.output("==> " + FacesWithTwoVertices.Count + "  faces with only two vertices.");
            if (FacesWithTwoEdges != null) Message.output("==> " + FacesWithTwoEdges.Count + " faces with only two edges.");
            if (FacesWithMissingAdjacency != null) Message.output("==> " + FacesWithMissingAdjacency.Count + " faces with missing adjacency.");
            if (FacesWithNegligibleArea != null) Message.output("==> " + FacesWithNegligibleArea.Count + " faces with negligible area.");
            if (EdgesWithBadAngle != null) Message.output("==> " + EdgesWithBadAngle.Count + " edges with bad angles.");
            if (EdgesThatDoNotLinkBackToFace != null)
                Message.output("==> " + EdgesThatDoNotLinkBackToFace.Count + " edges that do not link back to faces that link to them.");
            if (EdgesThatDoNotLinkBackToVertex != null)
                Message.output("==> " + EdgesThatDoNotLinkBackToVertex.Count + " edges that do not link back to vertices that link to them.");
            if (VertsThatDoNotLinkBackToFace != null)
                Message.output("==> " + VertsThatDoNotLinkBackToFace.Count + " vertices that do not link back to faces that link to them.");
            if (VertsThatDoNotLinkBackToEdge != null)
                Message.output("==> " + VertsThatDoNotLinkBackToEdge.Count + " vertices that do not link back to edges that link to them.");
            if (FacesThatDoNotLinkBackToEdge != null)
                Message.output("==> " + FacesThatDoNotLinkBackToEdge.Count + " faces that do not link back to edges that link to them.");
            if (FacesThatDoNotLinkBackToVertex != null)
                Message.output("==> " + FacesThatDoNotLinkBackToVertex.Count + " faces that do not link back to vertices that link to them.");
        }
        #endregion

        #region Error Storing
        private static void StoreHigherThanTriFaces(TessellatedSolid ts)
        {
            ts.Errors.NoErrors = false;
            if (ts.Errors.NonTriangularFaces == null)
                ts.Errors.NonTriangularFaces = new List<PolygonalFace>();
            foreach (var face in ts.Faces.Where(face => face.Vertices.Count > 3))
                ts.Errors.NonTriangularFaces.Add(face);
        }
        private static void StoreEdgeFaceRatio(TessellatedSolid ts, double edgeFaceRatio)
        {
            ts.Errors.NoErrors = false;
            ts.Errors.EdgeFaceRatio = edgeFaceRatio;
        }

        private static void StoreFaceDoesNotLinkBackToVertex(TessellatedSolid ts, Vertex vertex, PolygonalFace face)
        {
            ts.Errors.NoErrors = false;
            if (ts.Errors.FacesThatDoNotLinkBackToVertex == null)
                ts.Errors.FacesThatDoNotLinkBackToVertex
                    = new List<Tuple<Vertex, PolygonalFace>> { new Tuple<Vertex, PolygonalFace>(vertex, face) };
            else ts.Errors.FacesThatDoNotLinkBackToVertex.Add(new Tuple<Vertex, PolygonalFace>(vertex, face));
        }

        private static void StoreEdgeDoesNotLinkBackToVertex(TessellatedSolid ts, Vertex vertex, Edge edge)
        {
            ts.Errors.NoErrors = false;
            if (ts.Errors.EdgesThatDoNotLinkBackToVertex == null)
                ts.Errors.EdgesThatDoNotLinkBackToVertex
                    = new List<Tuple<Vertex, Edge>> { new Tuple<Vertex, Edge>(vertex, edge) };
            else ts.Errors.EdgesThatDoNotLinkBackToVertex.Add(new Tuple<Vertex, Edge>(vertex, edge));
        }

        private static void StoreEdgeHasBadAngle(TessellatedSolid ts, Edge edge)
        {
            ts.Errors.NoErrors = false;
            if (ts.Errors.EdgesWithBadAngle == null) ts.Errors.EdgesWithBadAngle = new List<Edge> { edge };
            else if (!ts.Errors.EdgesWithBadAngle.Contains(edge)) ts.Errors.EdgesWithBadAngle.Add(edge);
        }

        private static void StoreVertDoesNotLinkBackToEdge(TessellatedSolid ts, Edge edge, Vertex vert)
        {
            ts.Errors.NoErrors = false;
            if (ts.Errors.VertsThatDoNotLinkBackToEdge == null)
                ts.Errors.VertsThatDoNotLinkBackToEdge
                    = new List<Tuple<Edge, Vertex>> { new Tuple<Edge, Vertex>(edge, vert) };
            else ts.Errors.VertsThatDoNotLinkBackToEdge.Add(new Tuple<Edge, Vertex>(edge, vert));
        }

        private static void StoreFaceDoesNotLinkBackToEdge(TessellatedSolid ts, Edge edge, PolygonalFace face)
        {
            ts.Errors.NoErrors = false;
            if (ts.Errors.FacesThatDoNotLinkBackToEdge == null)
                ts.Errors.FacesThatDoNotLinkBackToEdge
                    = new List<Tuple<Edge, PolygonalFace>> { new Tuple<Edge, PolygonalFace>(edge, face) };
            else ts.Errors.FacesThatDoNotLinkBackToEdge.Add(new Tuple<Edge, PolygonalFace>(edge, face));
        }

        private static void StoreFaceWithNegligibleArea(TessellatedSolid ts, PolygonalFace face)
        {
            //This is not truly an error, to don't change the NoErrors boolean.
            if (ts.Errors.FacesWithNegligibleArea == null)
                ts.Errors.FacesWithNegligibleArea = new List<PolygonalFace> {face};
            else if(!ts.Errors.FacesWithNegligibleArea.Contains(face)) ts.Errors.FacesWithNegligibleArea.Add(face);
        }

        private static void StoreVertexDoesNotLinkBackToFace(TessellatedSolid ts, PolygonalFace face, Vertex vertex)
        {
            ts.Errors.NoErrors = false;
            if (ts.Errors.VertsThatDoNotLinkBackToFace == null)
                ts.Errors.VertsThatDoNotLinkBackToFace
                    = new List<Tuple<PolygonalFace, Vertex>> { new Tuple<PolygonalFace, Vertex>(face, vertex) };
            else ts.Errors.VertsThatDoNotLinkBackToFace.Add(new Tuple<PolygonalFace, Vertex>(face, vertex));
        }

        private static void StoreEdgeDoesNotLinkBackToFace(TessellatedSolid ts, PolygonalFace face, Edge edge)
        {
            ts.Errors.NoErrors = false;
            if (ts.Errors.EdgesThatDoNotLinkBackToFace == null)
                ts.Errors.EdgesThatDoNotLinkBackToFace
                    = new List<Tuple<PolygonalFace, Edge>> { new Tuple<PolygonalFace, Edge>(face, edge) };
            else ts.Errors.EdgesThatDoNotLinkBackToFace.Add(new Tuple<PolygonalFace, Edge>(face, edge));
        }

        private static void StoreFaceWithOTwoEdges(TessellatedSolid ts, PolygonalFace face)
        {
            ts.Errors.NoErrors = false;
            if (ts.Errors.FacesWithTwoEdges == null) ts.Errors.FacesWithTwoEdges = new List<PolygonalFace> { face };
            else ts.Errors.FacesWithTwoEdges.Add(face);
        }

        private static void StoreFaceWithTwoVertices(TessellatedSolid ts, PolygonalFace face)
        {
            ts.Errors.NoErrors = false;
            if (ts.Errors.FacesWithTwoVertices == null) ts.Errors.FacesWithTwoVertices = new List<PolygonalFace> { face };
            else ts.Errors.FacesWithTwoVertices.Add(face);
        }

        private static void StoreFaceWithOneEdge(TessellatedSolid ts, PolygonalFace face)
        {
            ts.Errors.NoErrors = false;
            if (ts.Errors.FacesWithOneEdge == null) ts.Errors.FacesWithOneEdge = new List<PolygonalFace> { face };
            else ts.Errors.FacesWithOneEdge.Add(face);
        }

        private static void StoreFaceWithOneVertex(TessellatedSolid ts, PolygonalFace face)
        {
            ts.Errors.NoErrors = false;
            if (ts.Errors.FacesWithOneVertex == null) ts.Errors.FacesWithOneVertex = new List<PolygonalFace> { face };
            else ts.Errors.FacesWithOneVertex.Add(face);
        }

        private static void StoreFaceWithMissingAdjacency(TessellatedSolid ts, PolygonalFace face)
        {
            ts.Errors.NoErrors = false;
            if (ts.Errors.FacesWithMissingAdjacency == null) ts.Errors.FacesWithMissingAdjacency = new List<PolygonalFace> { face };
            else ts.Errors.FacesWithMissingAdjacency.Add(face);
        }

        internal static void StoreOverusedEdges(TessellatedSolid ts, IEnumerable<Tuple<Edge, List<PolygonalFace>>> edgeFaceTuples)
        {
            ts.Errors.NoErrors = false;
            ts.Errors.OverusedEdges = edgeFaceTuples.ToList();
        }
        internal static void StoreSingleSidedEdges(TessellatedSolid ts, Edge singledSidedEdge, bool ownedIsMissing)
        {
            ts.Errors.NoErrors = false;
            if (ts.Errors.SingledSidedEdges == null) ts.Errors.SingledSidedEdges = new List<Tuple<Edge, bool>> ();
            ts.Errors.SingledSidedEdges.Add(new Tuple<Edge, bool>(singledSidedEdge,ownedIsMissing));
        }


        internal static void StoreDegenerateFace(TessellatedSolid ts, int[] faceVertexIndices)
        {
            ts.Errors.NoErrors = false;
            if (ts.Errors.DegenerateFaces == null) ts.Errors.DegenerateFaces = new List<int[]> { faceVertexIndices };
            else ts.Errors.DegenerateFaces.Add(faceVertexIndices);
        }

        internal static void StoreDuplicateFace(TessellatedSolid ts, int[] faceVertexIndices)
        {
            ts.Errors.NoErrors = false;
            if (ts.Errors.DuplicateFaces == null) ts.Errors.DuplicateFaces = new List<int[]> { faceVertexIndices };
            else ts.Errors.DuplicateFaces.Add(faceVertexIndices);
        }
        #endregion

        #region Repair Functions

        internal bool Repair(TessellatedSolid ts)
        {
            var completelyRepaired = true;
            if (EdgesWithBadAngle != null)
                completelyRepaired = FlipFacesBasedOnBadAngles(ts);
            if (NonTriangularFaces != null)
                completelyRepaired = completelyRepaired && DivideUpNonTriangularFaces(ts);
            if (SingledSidedEdges != null) //what about faces with only one or two edges?
                completelyRepaired = completelyRepaired && RepairMissingFacesFromEdges(ts);
            //Note that negligible faces are not truly errors, so they are not repaired
            return completelyRepaired;
        }

        private bool FlipFacesBasedOnBadAngles(TessellatedSolid ts)
        {
            var edgesWithBadAngles = new HashSet<Edge>(ts.Errors.EdgesWithBadAngle);
            var facesToConsider = new HashSet<PolygonalFace>(
                edgesWithBadAngles.SelectMany(e => new[] { e.OwnedFace, e.OtherFace }).Distinct());
            var allEdgesToUpdate = new HashSet<Edge>();
            foreach (var face in facesToConsider)
            {
                var edgesToUpdate = new List<Edge>();
                foreach (var edge in face.Edges)
                {
                    if (edgesWithBadAngles.Contains(edge)) edgesToUpdate.Add(edge);
                    else if (facesToConsider.Contains((edge.OwnedFace == face) ? edge.OtherFace : edge.OwnedFace))
                        edgesToUpdate.Add(edge);
                    else break;
                }
                if (edgesToUpdate.Count < face.Edges.Count) continue;
                face.Normal = face.Normal.multiply(-1);
                face.Edges.Reverse();
                face.Vertices.Reverse();
                foreach (var edge in edgesToUpdate)
                    if (!allEdgesToUpdate.Contains(edge)) allEdgesToUpdate.Add(edge);
            }
            foreach (var edge in allEdgesToUpdate)
            {
                edge.Update();
                ts.Errors.EdgesWithBadAngle.Remove(edge);
            }
            if (ts.Errors.EdgesWithBadAngle.Any()) return false;
            ts.Errors.EdgesWithBadAngle = null;
            return true;
        }


        private bool DivideUpNonTriangularFaces(TessellatedSolid ts)
        {
            var allNewFaces = new List<PolygonalFace>();
            var singleSidedEdges = new HashSet<Edge>();
            foreach (var nonTriangularFace in ts.Errors.NonTriangularFaces)
            {
                var newFaces = new List<PolygonalFace>();
                foreach (var edge in nonTriangularFace.Edges)
                {
                    if (!singleSidedEdges.Contains(edge)) singleSidedEdges.Add(edge);
                }
                //Using Triangulate Polygon guarantees that even if the face has concave edges, it will triangulate properly.
                List<List<Vertex[]>> triangleFaceList;
                var triangles = TriangulatePolygon.Run(new List<List<Vertex>> { nonTriangularFace.Vertices }, nonTriangularFace.Normal, out triangleFaceList);
                foreach (var triangle in triangles)
                {
                    var newFace = new PolygonalFace(triangle, nonTriangularFace.Normal) { Color = nonTriangularFace.Color };
                    newFaces.Add(newFace);
                }
                ts.AddPrimitive(new Flat(newFaces));
                allNewFaces.AddRange(newFaces);
            }
            ts.RemoveFaces(ts.Errors.NonTriangularFaces);
            ts.Errors.NonTriangularFaces = null;
            ts.MostPolygonSides = 3;
            // todo: whoa! why make the singledsidedEdges here?! why not just repair
            ts.Errors.SingledSidedEdges = singleSidedEdges.ToList();
            return LinkUpNewFaces(allNewFaces, ts);
        }

        private bool RepairMissingFacesFromEdges(TessellatedSolid ts)
        {
            var newFaces = new List<PolygonalFace>();
            var loops = new List<List<Vertex>>();
            var loopNormals = new List<double[]>();
            var attempts = 0;
            var remainingEdges = new List<Edge>(ts.Errors.SingledSidedEdges);
            while (remainingEdges.Count > 0 && attempts < remainingEdges.Count)
            {
                var loop = new List<Vertex>();
                var successful = true;
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
                    var possibleNextEdges =
                        remainingEdges.Where(e => e.To == newStartVertex || e.From == newStartVertex).ToList();
                    if (possibleNextEdges.Count() != 1) successful = false;
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
                } while (newStartVertex != startVertex && successful);
                if (successful)
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
                    var newFace = new PolygonalFace(loops[i], loopNormals[i]);
                    newFaces.Add(newFace);
                }
                //Else, use the triangulate function
                else if (loops[i].Count > 3)
                {
                    //First, get an average normal from all vertices, assuming CCW order.
                    List<List<Vertex[]>> triangleFaceList;
                    var triangles = TriangulatePolygon.Run(new List<List<Vertex>> { loops[i] }, loopNormals[i], out triangleFaceList);
                    foreach (var triangle in triangles)
                    {
                        var newFace = new PolygonalFace(triangle, loopNormals[i]);
                        newFaces.Add(newFace);
                    }
                }
            }
            if (newFaces.Count == 1) Message.output("1 missing face was fixed",3);
            if (newFaces.Count > 1) Message.output(newFaces.Count + " missing faces were fixed",3);
            return LinkUpNewFaces(newFaces, ts);
        }

        private bool LinkUpNewFaces(List<PolygonalFace> newFaces, TessellatedSolid ts)
        {
            ts.AddFaces(newFaces);
            //var completedEdges = new List<Edge>();
            var newEdges = new List<Edge>();
            var partlyDefinedEdges = SingledSidedEdges.ToDictionary(ts.SetEdgeChecksum);
            ts.UpdateAllEdgeCheckSums();

            foreach (var face in newFaces)
            {
                for (var j = 0; j < 3; j++)
                {
                    var fromVertex = face.Vertices[j];
                    var toVertex = face.Vertices[(j == 2) ? 0 : j + 1];
                    var checksum = ts.SetEdgeChecksum(fromVertex, toVertex);

                    if (partlyDefinedEdges.ContainsKey(checksum))
                    {
                        //Finish creating edge.
                        var edge = partlyDefinedEdges[checksum];
                        if (edge.OwnedFace == null) edge.OwnedFace = face;
                        else if (edge.OtherFace == null) edge.OtherFace = face;
                        face.Edges.Add(edge);
                        //completedEdges.Add(edge);
                        //partlyDefinedEdges.Remove(checksum);
                    }
                    else
                    {
                        var edge = new Edge(fromVertex, toVertex, face, null, true, checksum);
                        newEdges.Add(edge);
                        partlyDefinedEdges.Add(checksum, edge);
                    }
                }
            }
            ts.AddEdges(newEdges);
            foreach (var edge in partlyDefinedEdges.Values)
                ts.Errors.SingledSidedEdges.Remove(edge);
            if (!ts.Errors.SingledSidedEdges.Any()) ts.Errors.SingledSidedEdges = null;
            return true;
        }
       
        #endregion
    }
}
