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
using System.Linq;
using StarMathLib;
using TVGL.Tessellation;

namespace TVGL
{
    internal enum ContactTypes
    {
        ThroughFace,
        ThroughVertex,
        AlongEdge,
        Artificial
    }


    /// <summary>
    /// A ContactElement describes the atomic class for the collective ContactData class which describes
    /// how a slice affects the solid. A ContactElement is basically an edge that may or may not correspond
    /// to a real edge in the solid. Additionally, it has properties that reference pertinent data in the solid.
    /// </summary>
    public class ContactElement
    {
        internal ContactElement(Edge edge, bool ownedFaceIsPositive)
        {
            ContactType = ContactTypes.AlongEdge;
            ContactEdge = edge;
            if (ownedFaceIsPositive)
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
        }

        internal ContactElement(Vertex startVertex, Edge startEdge, Vertex endVertex, Edge endEdge, PolygonalFace face, ContactTypes contactType)
        {
            StartVertex = startVertex;
            StartEdge = startEdge;
            EndVertex = endVertex;
            EndEdge = endEdge;
            ContactEdge = new Edge(StartVertex, EndVertex, false);
            SplitFacePositive = face;
            ContactType = contactType;
        }


        internal readonly Vertex StartVertex;
        internal readonly Vertex EndVertex;
        internal readonly Edge StartEdge;
        internal readonly Edge EndEdge;

        /// <summary>
        /// Gets the edge corresponds to the surface of the solid within the designated slice.
        /// </summary>
        /// <value>The contact edge.</value>
        public readonly Edge ContactEdge;
        /// <summary>
        /// Gets the split face.
        /// </summary>
        /// <value>The split face.</value>                
        internal PolygonalFace SplitFacePositive { get; set; }
        internal PolygonalFace SplitFaceNegative { get; set; }

        /// <summary>
        /// Is the edge in the reverse direction needed to describe the loop in the proper
        /// right-hand rule approach? Since all new edges are created in the proper direction
        /// this is only set to true for edges that are part of the loop but existed in the
        /// model previously.
        /// </summary>
        public readonly Boolean ReverseDirection;


        internal ContactTypes ContactType { get; set; }

        internal double[] Vector
        {
            get
            {
                return new[]
                {
                    EndVertex.X - StartVertex.X,
                    EndVertex.Y - StartVertex.Y,
                    EndVertex.Z - StartVertex.Z
                };
            }
        }
    }
}

