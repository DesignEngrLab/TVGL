// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Matt Campbell
// Created          : 03-05-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 03-18-2015
// ***********************************************************************
// <copyright file="ContactData.cs" company="">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using StarMathLib;
using TVGL.Tessellation;

namespace TVGL.Boolean_Operations
{
    /// <summary>
    /// The contact element corresponds to an existing edge within the solid.
    /// </summary>
    public class CoincidentEdgeContactElement : ContactElement
    {
        internal CoincidentEdgeContactElement(Edge edge, Flat plane)
        {
            var avgFaceNormal = edge.OwnedFace.Normal.add(edge.OtherFace.Normal).divide(2);
            var edgeDir = plane.Normal.crossProduct(avgFaceNormal);
            if (edgeDir.dotProduct(edge.Vector) > 0)
            {
                StartVertex = edge.From;
                EndVertex = edge.To;
                SplitFacePositive = edge.OwnedFace; // the owned face is "above the flat plane
                SplitFaceNegative = edge.OtherFace; // the other face is below it
            }
            else
            {
                StartVertex = edge.To;
                EndVertex = edge.From;
                SplitFacePositive = edge.OtherFace; // the reverse from above. The other face is above the flat
                SplitFaceNegative = edge.OwnedFace;
                ReverseDirection = true;

            }
            ContactEdge = edge;
            // ContactEdge = new Edge(ReferenceStart, ReferenceEnd, null, null);
        }


        /// <summary>
        /// Gets or sets the reference vertex at the other end (use in conjunction
        /// with ReferenceVertex).
        /// </summary>
        /// <value>The reference end.</value>
        internal Vertex EndVertex { get; set; }

    }

    /// <summary>
    /// The contact element passes through two edges (or a face without coinciding with
    /// an edge or vertex of the slice).
    public class ThroughFaceContactElement : ContactElement
    {

        internal ThroughFaceContactElement(Flat plane, Edge edge, double toDistance)
        {
            PolygonalFace negativeFace, positiveFace;
            if (toDistance > 0)
            {
                positiveFace = edge.OtherFace;
                negativeFace = edge.OwnedFace;
            }
            else
            {
                negativeFace = edge.OtherFace;
                positiveFace = edge.OwnedFace;
            }
            SplitEdge = edge;
            SplitFaceNegative = negativeFace;
            SplitFacePositive = positiveFace;
            StartVertex = GeometryFunctions.PointOnPlaneFromIntersectingLine(
                plane.Normal, plane.DistanceToOrigin, edge.From, edge.To);
        }
        /// <summary>
        /// Gets the split edge.
        /// </summary>
        /// <value>The split edge.</value>
        internal Edge SplitEdge { get; set; }

    }

    /// <summary>
    /// The contact element passed through a vertex in the solid.
    /// </summary>
    public class ThroughVertexContactElement : ContactElement
    {
        internal ThroughVertexContactElement(Vertex onPlaneVertex, PolygonalFace negativeFace, PolygonalFace positiveFace)
        {
            StartVertex = onPlaneVertex;
            SplitFacePositive = positiveFace;
            SplitFaceNegative = negativeFace;
        }
        internal static Boolean FindNegativeAndPositiveFaces(Flat plane, Vertex onPlaneVertex, double[] vertexDistancesToPlane, out PolygonalFace negativeFace, out PolygonalFace positiveFace)
        {
            negativeFace = null;
            positiveFace = null;
            foreach (var face in onPlaneVertex.Faces)
            {
                var otherEdge = face.OtherEdge(onPlaneVertex);
                var toDistance = vertexDistancesToPlane[otherEdge.To.IndexInList];
                var fromDistance = vertexDistancesToPlane[otherEdge.From.IndexInList];
                if ((toDistance > 0 && fromDistance < 0) || (toDistance < 0 && fromDistance > 0))
                    if ((toDistance > 0) == (face == otherEdge.OwnedFace))
                        positiveFace = face;
                    else negativeFace = face;
            }
            return (negativeFace != null && positiveFace != null);
        }
    }

    /// <summary>
    /// The contact element is created artificially and doesn't correspond to elements
    /// within the real solid.
    /// </summary>
    public class ArtificialContactElement : ContactElement
    {

    }

    /// <summary>
    /// A ContactElement describes the atomic class for the collective ContactData class which describes
    /// how a slice affects the solid. A ContactElement is basically an edge that may or may not correspond
    /// to a real edge in the solid. Additionally, it has properties that reference pertinent data in the solid.
    /// </summary>
    public abstract class ContactElement
    {
        internal Vertex StartVertex { get; set; }
        /// <summary>
        /// Gets the edge corresponds to the surface of the solid within the designated slice.
        /// </summary>
        /// <value>The contact edge.</value>
        public Edge ContactEdge { get; internal set; }
        /// <summary>
        /// Gets the split face.
        /// </summary>
        /// <value>The split face.</value>                
        internal PolygonalFace SplitFacePositive { get; set; }
        internal PolygonalFace SplitFaceNegative { get; set; }

        public  Boolean ReverseDirection { get; protected set; }

        internal static void MakeInPlaneContactElement(Flat plane, Edge edge, HashSet<Edge> edgeHashSet, double[] vertexDistancesToPlane, List<CoincidentEdgeContactElement> contactElts)
        {
            // the edge is in the plane. Let's look at details on the two faces that meet at that edge.
            // ownedFaceOtherVertex is the vertex of the face of the in-plane edge that is NOT on the plane...
            // it's the "other" vertex
            var ownedFaceOtherVertex = edge.OwnedFace.OtherVertex(edge);
            // the distance to the flat plane for the other vertex.
            var distPlaneOwned = vertexDistancesToPlane[ownedFaceOtherVertex.IndexInList];
            // repeat for the other face of the in-plane edge
            var otherFaceOtherVertex = edge.OtherFace.OtherVertex(edge);
            var distPlaneOther = vertexDistancesToPlane[otherFaceOtherVertex.IndexInList];

            // here, we check to see if the faces are on either side of the flat plane, if they are
            // then we make a 
            if ((distPlaneOwned > 0 && distPlaneOther < 0) || (distPlaneOwned < 0 && distPlaneOther > 0))
            {
                RemoveNeighboringEdgesToo(edgeHashSet, edge);
                //remove the edges of the faces from the hash to speed things along
                contactElts.Add(new CoincidentEdgeContactElement(edge, plane));
                return;
            }
            // if faces are not on either side, check to see if one of the faces is
            // in the same plane as flat. If so, then do a depth first search with "GetInPlaneFlat"
            // to build up that section of faces. 
            Flat inPlaneFlat;
            if (StarMath.IsNegligible(distPlaneOwned)) inPlaneFlat = GetInPlaneFlat(edge.OwnedFace);
            else if (StarMath.IsNegligible(distPlaneOther)) inPlaneFlat = GetInPlaneFlat(edge.OtherFace);
            else //if neither face is in the plane then you have an edge which is a knife on the plane
            {
                // either on the positive side or the negative side. Doesn't matter which one - we don't want it (i.e. continue).
                RemoveNeighboringEdgesToo(edgeHashSet, edge);
                //remove the edges of the faces from the hash to speed things along
                return;
            }
            foreach (var innerEdge in inPlaneFlat.InnerEdges) edgeHashSet.Remove(innerEdge);
            foreach (var outerEdge in inPlaneFlat.OuterEdges)
            {
                edgeHashSet.Remove(outerEdge);
                contactElts.Add(new CoincidentEdgeContactElement(outerEdge, plane));
            }
        }


        private static void RemoveNeighboringEdgesToo(HashSet<Edge> edgeHashSet, Edge edge)
        {
            foreach (var e in edge.OwnedFace.Edges)
                if (e != edge) edgeHashSet.Remove(e);
            foreach (var e in edge.OtherFace.Edges)
                if (e != edge) edgeHashSet.Remove(e);
        }


        /// <summary>
        /// Gets the in plane flat.
        /// </summary>
        /// <param name="startFace">The start face.</param>
        /// <returns>Flat.</returns>
        private static Flat GetInPlaneFlat(PolygonalFace startFace)
        {
            var flat = new Flat(new List<PolygonalFace>(new[] { startFace }));
            var visitedFaces = new HashSet<PolygonalFace>(startFace.AdjacentFaces);
            visitedFaces.Add(startFace);
            var stack = new Stack<PolygonalFace>(visitedFaces);
            while (stack.Any())
            {
                var face = stack.Pop();
                if (!flat.IsNewMemberOf(face)) continue;
                flat.UpdateWith(face);
                foreach (var adjacentFace in face.AdjacentFaces)
                    if (!visitedFaces.Contains(adjacentFace))
                    {
                        visitedFaces.Add(adjacentFace);
                        stack.Push(adjacentFace);
                    }
            }
            return flat;
        }
    }
}

