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
using StarMathLib;

namespace TVGL
{
    /// <summary>
    /// A ContactData object represents a 2D path on the surface of the tessellated solid. 
    /// It is notably comprised of loops (both positive and negative) and loops are comprised
    /// of contact elements).
    /// </summary>
    public class ContactData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContactData" /> class.
        /// </summary>
        /// <param name="loops">The loops.</param>
        /// <param name="inPlaneFaces"></param>
        internal ContactData(IEnumerable<Loop> loops, List<PolygonalFace> inPlaneFaces)
        {
            InPlaneFaces = inPlaneFaces;
            PositiveLoops = new List<Loop>();
            NegativeLoops = new List<Loop>();
            foreach (var loop in loops)
            {
                Perimeter += loop.Perimeter;
                Area += loop.Area;
                if (loop.IsPositive) PositiveLoops.Add(loop);
                else NegativeLoops.Add(loop);
            }
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
        /// Gets all loops in one list (the positive loops are followed by the
        /// negative loops).
        /// </summary>
        /// <value>All loops.</value>
        public List<Loop> AllLoops
        {
            get
            {
                var allLoops = new List<Loop>(PositiveLoops);
                allLoops.AddRange(NegativeLoops);
                return allLoops;
            }
        }

        /// <summary>
        /// The number of new vertices
        /// </summary>
        public readonly int NumberOfNewVertices;
        /// <summary>
        /// The combined perimeter of the 2D loops defined with the Contact Data.
        /// </summary>
        public readonly double Perimeter;
        /// <summary>
        /// The combined area of the 2D loops defined with the Contact Data
        /// </summary>
        public readonly double Area;

        /// <summary>
        /// List of In Plane Faces
        /// </summary>
        public readonly List<PolygonalFace> InPlaneFaces;
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
        public readonly bool IsPositive;
        /// <summary>
        /// Negative loops must always be inside positive loops. This is a place to store all
        /// the pos/neg loop dependency.
        /// </summary>
        public List<Loop> DependentLoops;
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
        public readonly bool IsClosed;
        /// <summary>
        /// Was the loop closed by artificial ContactElements
        /// </summary>
        public readonly bool ArtificiallyClosed;
        /// <summary>
        /// Does the loop enclose a bunch of faces that lie in the plane?
        /// </summary>
        public readonly Boolean EnclosesInPlaneFace;
        /// <summary>
        /// Does the loop belong to the positive solids?
        /// </summary>
        public readonly Boolean OnPositiveSolids;
        /// <summary>
        /// Does the loop belong to the negative solids?
        /// </summary>
        public readonly Boolean OnNegativeSolids;

        /// <summary>
        /// Initializes a new instance of the <see cref="Loop" /> class.
        /// </summary>
        /// <param name="contactElements">The contact elements.</param>
        /// <param name="normal">The normal.</param>
        /// <param name="isClosed">is closed.</param>
        /// <param name="artificiallyClosed">is artificially closed.</param>
        /// <param name="enclosesInPlaneFace"></param>
        /// <param name="onNegativeSolids"></param>
        /// <param name="onPositiveSolids"></param>
        internal Loop(ICollection<ContactElement> contactElements, IList<double> normal, bool isClosed, bool artificiallyClosed, 
            bool enclosesInPlaneFace, bool onNegativeSolids = true, bool onPositiveSolids = true)
            : base(contactElements)
        {
            ArtificiallyClosed = artificiallyClosed;
            IsClosed = isClosed;
            EnclosesInPlaneFace = enclosesInPlaneFace;
            OnPositiveSolids = onPositiveSolids;
            OnNegativeSolids = onNegativeSolids;
            if (!IsClosed) Message.output("loop not closed!",3);
            var center = new double[3];
            foreach (var contactElement in contactElements)
            {
                Perimeter += contactElement.ContactEdge.Length;
                if (contactElement.ReverseDirection)
                    center = center.add(contactElement.ContactEdge.To.Position);
                else center = center.add(contactElement.ContactEdge.From.Position);
            }
            center = center.divide(contactElements.Count);
            foreach (var contactElement in contactElements)
            {
                var radial = (contactElement.ReverseDirection)
                    ? contactElement.ContactEdge.To.Position.subtract(center)
                    : contactElement.ContactEdge.From.Position.subtract(center);
                var areaVector = radial.crossProduct(contactElement.ContactEdge.Vector);
                if (contactElement.ReverseDirection) areaVector = areaVector.multiply(-1);
                Area += areaVector.dotProduct(normal) / 2.0;
            }
            IsPositive = (Area >= 0);
        }

        internal string MakeDebugContactString()
        {
            var result = "";
            foreach (var ce in this)
            {
                switch (ce.ContactType)
                {
                    case ContactTypes.Artificial:
                        result += "a";
                        break;
                    case ContactTypes.ThroughFace:
                        result += "|";
                        break;
                    case ContactTypes.ThroughVertex:
                        result += ".";
                        break;
                    case ContactTypes.AlongEdge:
                        result += "_";
                        break;
                }
            }
            return result;
        }


    }
}

