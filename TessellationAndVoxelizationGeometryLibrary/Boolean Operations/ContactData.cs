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

namespace TVGL
{
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
        /// The combined perimeter of the 2D loops defined with the Contact Data.
        /// </summary>
        public readonly double Perimeter;
        /// <summary>
        /// The combined area of the 2D loops defined with the Contact Data
        /// </summary>
        public readonly double Area;
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
                if (ce is ArtificialContactElement)
                    result += "a";
                else if (ce is CoincidentEdgeContactElement)
                    result += "_";
                else if (ce is ThroughFaceContactElement)
                    result += "|";
                else result += ".";
            }
            return result;
        }
    }
}

