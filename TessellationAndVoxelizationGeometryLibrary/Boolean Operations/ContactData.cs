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
    /// The enumerator, FaceSplitType, is used to describe how a ContactElement within the ContactData
    /// relates to the tessellated solid.
    /// </summary>
    public enum FaceSplitType
    {
        /// <summary>
        /// The contact element corresponds to an existing edge within the solid.
        /// </summary>
        CoincidentEdge,
        /// <summary>
        /// The contact element passes through two edges (or a face without coinciding with
        /// an edge or vertex of the slice).
        /// </summary>
        ThroughFace,
        /// <summary>
        /// The contact element passed through a vertex in the solid.
        /// </summary>
        ThroughVertex,
        /// <summary>
        /// The contact element is created artificially and doesn't correspond to elements
        /// within the real solid.
        /// </summary>
        Artificial
    }
    /// <summary>
    /// A ContactElement describes the atomic class for the collective ContactData class which describes
    /// how a slice affects the solid. A ContactElement is basically an edge that may or may not correspond
    /// to a real edge in the solid. Additionally, it has properties that reference pertinent data in the solid.
    /// </summary>
    public class ContactElement
    {
        /// <summary>
        /// Gets or sets the starting vertex from the original tessellated solid.
        /// </summary>
        /// <value>The reference start.</value>
        internal Vertex ReferenceStart { get; set; }
        /// <summary>
        /// Gets or sets the reference end.
        /// </summary>
        /// <value>The reference end.</value>
        internal Vertex ReferenceEnd { get; set; }
        /// <summary>
        /// Gets the contact point.
        /// </summary>
        /// <value>The contact point.</value>
        internal Vertex ContactPoint { get; set; }
        /// <summary>
        /// Gets the split edge.
        /// </summary>
        /// <value>The split edge.</value>
        internal Edge SplitEdge { get; set; }
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
        /// <summary>
        /// Gets the type of the split.
        /// </summary>
        /// <value>The type of the split.</value>
        public FaceSplitType SplitType { get; internal set; }


        /// <summary>
        /// Makes the in plane edge.
        /// </summary>
        /// <param name="edge">The edge.</param>
        /// <param name="plane">The plane.</param>
        /// <returns>ContactElement.</returns>
        internal static ContactElement MakeInPlaneEdge(Edge edge, Flat plane)
        {
            var avgFaceNormal = edge.OwnedFace.Normal.add(edge.OtherFace.Normal).divide(2);
            var edgeDir = plane.Normal.crossProduct(avgFaceNormal);
            Vertex contactPoint;
            ContactElement contactElt;
            if (edgeDir.dotProduct(edge.Vector) > 0)
            {
                contactPoint = edge.From.Copy();
                contactElt = new ContactElement
                {
                    SplitType = FaceSplitType.CoincidentEdge,
                    ReferenceStart = edge.From,
                    ReferenceEnd = edge.To,
                    ContactEdge = new Edge(contactPoint, edge.To.Copy(), null, null),
                    ContactPoint = contactPoint,
                    SplitFacePositive = edge.OwnedFace, // the owned face is "above the flat plane
                    SplitFaceNegative = edge.OtherFace  // the other face is below it
                };
            }
            else
            {
                contactPoint = edge.To.Copy();
                contactElt = new ContactElement
                {
                    SplitType = FaceSplitType.CoincidentEdge,
                    ReferenceStart = edge.To,
                    ReferenceEnd = edge.From,
                    ContactEdge = new Edge(contactPoint, edge.From.Copy(), null, null),
                    ContactPoint = contactPoint,
                    SplitFacePositive = edge.OtherFace,  // the reverse from above. The other face is above the flat
                    SplitFaceNegative = edge.OwnedFace
                };
            }
            contactElt.ContactEdge.DefineVectorAndLength();
            return contactElt;
        }

        internal static void MakeInPlaneContactElement(Flat plane, Edge edge, HashSet<Edge> edgeHashSet, double[] vertexDistancesToPlane, List<ContactElement> contactElts)
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
                contactElts.Add(MakeInPlaneEdge(edge, plane));
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
                contactElts.Add(MakeInPlaneEdge(outerEdge, plane));
            }
        }

        internal static ContactElement MakeStraddleContactElement(Flat plane, Edge edge, double toDistance)
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
            return new ContactElement
            {
                SplitType = FaceSplitType.ThroughFace,
                SplitEdge = edge,
                SplitFaceNegative = negativeFace,
                SplitFacePositive = positiveFace,
                ContactPoint = GeometryFunctions.PointOnPlaneFromIntersectingLine(
                    plane.Normal, plane.DistanceToOrigin, edge.From, edge.To)
            };
        }

        internal static ContactElement MakeVertexOnPlaneContactElement(Flat plane, Vertex onPlaneVertex, double[] vertexDistancesToPlane)
        {
            PolygonalFace negativeFace = null;
            PolygonalFace positiveFace = null;
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
            if (negativeFace == null || positiveFace == null) return null;
            return new ContactElement
            {
                SplitType = FaceSplitType.ThroughVertex,
                ReferenceStart = onPlaneVertex,
                ReferenceEnd = onPlaneVertex,
                SplitFacePositive = positiveFace,
                SplitFaceNegative = negativeFace,
                ContactPoint = onPlaneVertex.Copy()
            };
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

    /// <summary>
    /// The Loop class is basically a list of ContactElements that form a path. Usually, this path
    /// is closed, hence the name "loop", but it may be used and useful for open paths as well.
    /// </summary>
    public class Loop : List<ContactElement>
    {
        /// <summary>
        /// Is the loop positive - meaning does it enclose material versus representing a hole
        /// </summary>
        public readonly Boolean IsPositive;
        /// <summary>
        /// The length of the loop.
        /// </summary>
        public readonly double Perimeter;
        /// <summary>
        /// The area of the loop
        /// </summary>
        public readonly double Area;
        /// <summary>
        /// Is the loop closed?
        /// </summary>
        public readonly Boolean IsClosed;
        /// <summary>
        /// Was the loop closed by artificial ContactElements
        /// </summary>
        public readonly Boolean ArtificiallyClosed;
        /// <summary>
        /// Initializes a new instance of the <see cref="Loop" /> class.
        /// </summary>
        /// <param name="contactElements">The contact elements.</param>
        /// <param name="normal">The normal.</param>
        /// <param name="isClosed">is closed.</param>
        /// <param name="artificiallyClosed">is artificially closed.</param>
        internal Loop(ICollection<ContactElement> contactElements, double[] normal, Boolean isClosed, Boolean artificiallyClosed)
            : base(contactElements)
        {
            ArtificiallyClosed = artificiallyClosed;
            IsClosed = isClosed;
            if (!IsClosed) Debug.WriteLine("loop not closed!");
            var center = new double[3];
            foreach (var contactElement in contactElements)
            {
                Perimeter += contactElement.ContactEdge.Length;
                center = center.add(contactElement.ContactEdge.From.Position);
            }
            center = center.divide(contactElements.Count);
            foreach (var contactElement in contactElements)
            {
                var radial = contactElement.ContactEdge.From.Position.subtract(center);
                var areaVector = radial.crossProduct(contactElement.ContactEdge.Vector);
                Area += areaVector.dotProduct(normal) / 2.0;
            }
            IsPositive = (Area >= 0);
        }

        internal string MakeDebugContactString()
        {
            var result = "";
            foreach (var ce in this)
            {    
                switch (ce.SplitType)
                {
                                     case FaceSplitType.Artificial:
                        result += "a";
                        break;
                                     case FaceSplitType.CoincidentEdge:
                        result += "-";
                        break;
                                     case FaceSplitType.ThroughFace:
                        result += "|";
                        break;
                                     case FaceSplitType.ThroughVertex:
                        result += ".";
                        break;
                }
            }
            return result;
        }
    }
    /// <summary>
    /// A ContactData object represents a 2D path on the surface of the tesselated solid. 
    /// It is notably comprised of loops (both positive and negative) and loops are comprised
    /// of contact elements).
    /// </summary>
    public class ContactData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContactData" /> class.
        /// </summary>
        /// <param name="loops">The loops.</param>
        /// <param name="additionalEdges">The additional edges.</param>
        internal ContactData(List<Loop> loops, List<Edge> additionalEdges = null)
        {
            PositiveLoops = new List<Loop>();
            NegativeLoops = new List<Loop>();
            foreach (var loop in loops)
            {
                Perimeter += loop.Perimeter;
                Area += loop.Area;
                if (loop.IsPositive) PositiveLoops.Add(loop);
                else NegativeLoops.Add(loop);
            }
            if (additionalEdges != null)
                Perimeter += additionalEdges.Sum(e => e.Length);
        }

        /// <summary>
        /// Gets the positive loops.
        /// </summary>
        /// <value>The positive loops.</value>
        public List<Loop> PositiveLoops { get; internal set; }
        /// <summary>
        /// Gets the loops of negative area (i.e. holes).
        /// </summary>
        /// <value>The negative loops.</value>
        public List<Loop> NegativeLoops { get; internal set; }

        /// <summary>
        /// The combined perimeter of the 2D loops defined with the Contact Data.
        /// </summary>
        public readonly double Perimeter;
        /// <summary>
        /// The combined area of the 2D loops defined with the Contact Data
        /// </summary>
        public readonly double Area;
    }
}

