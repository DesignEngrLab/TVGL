// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 03-05-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 03-18-2016
// ***********************************************************************
// <copyright file="ContactData.cs" company="Design Engineering Lab">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

namespace TVGL
{
    /// <summary>
    ///     Enum ContactTypes
    /// </summary>
    internal enum ContactTypes
    {
        /// <summary>
        ///     The through face
        /// </summary>
        ThroughFace,

        /// <summary>
        ///     The through vertex
        /// </summary>
        ThroughVertex,

        /// <summary>
        ///     The along edge
        /// </summary>
        AlongEdge,

        /// <summary>
        ///     The artificial
        /// </summary>
        Artificial
    }


    /// <summary>
    ///     A ContactElement describes the atomic class for the collective ContactData class which describes
    ///     how a slice affects the solid. A ContactElement is basically an edge that may or may not correspond
    ///     to a real edge in the solid. Additionally, it has properties that reference pertinent data in the solid.
    /// </summary>
    public class ContactElement
    {
        /// <summary>
        ///     Gets the edge corresponds to the surface of the solid within the designated slice.
        /// </summary>
        /// <value>The contact edge.</value>
        public readonly Edge ContactEdge;

        /// <summary>
        ///     The end vertex
        /// </summary>
        internal readonly Vertex EndVertex;

        /// <summary>
        ///     Is the edge in the reverse direction needed to describe the loop in the proper
        ///     right-hand rule approach? Since all new edges are created in the proper direction
        ///     this is only set to true for edges that are part of the loop but existed in the
        ///     model previously.
        /// </summary>
        public readonly bool ReverseDirection;

        /// <summary>
        ///     The start edge
        /// </summary>
        internal readonly Edge StartEdge;

        // way to do the slicing 
        /// <summary>
        ///     The start vertex
        /// </summary>
        internal readonly Vertex StartVertex;


        /// <summary>
        ///     The duplicate vertex
        /// </summary>
        internal Vertex DuplicateVertex; //I can't say I'm proud of this being here, but it was the simplest

        /// <summary>
        ///     Initializes a new instance of the <see cref="ContactElement" /> class.
        /// </summary>
        /// <param name="edge">The edge.</param>
        /// <param name="ownedFaceIsPositive">if set to <c>true</c> [owned face is positive].</param>
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

        /// <summary>
        ///     Initializes a new instance of the <see cref="ContactElement" /> class.
        /// </summary>
        /// <param name="startVertex">The start vertex.</param>
        /// <param name="startEdge">The start edge.</param>
        /// <param name="endVertex">The end vertex.</param>
        /// <param name="endEdge">The end edge.</param>
        /// <param name="face">The face.</param>
        /// <param name="contactType">Type of the contact.</param>
        internal ContactElement(Vertex startVertex, Edge startEdge, Vertex endVertex, Edge endEdge, PolygonalFace face,
            ContactTypes contactType)
        {
            StartVertex = startVertex;
            StartEdge = startEdge;
            EndVertex = endVertex;
            ContactEdge = new Edge(StartVertex, EndVertex, false);
            SplitFacePositive = face;
            ContactType = contactType;
        }

        /// <summary>
        ///     Gets the split face.
        /// </summary>
        /// <value>The split face.</value>
        internal PolygonalFace SplitFacePositive { get; set; }

        /// <summary>
        ///     Gets or sets the split face negative.
        /// </summary>
        /// <value>The split face negative.</value>
        internal PolygonalFace SplitFaceNegative { get; set; }


        /// <summary>
        ///     Gets or sets the type of the contact.
        /// </summary>
        /// <value>The type of the contact.</value>
        internal ContactTypes ContactType { get; set; }

        /// <summary>
        ///     Gets the vector.
        /// </summary>
        /// <value>The vector.</value>
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