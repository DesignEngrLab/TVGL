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
    /// It is notably comprised of loops (both positive and negative). Each subvolume
    /// created from a slice has its own contact data.
    /// </summary>
    public class ContactData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContactData" /> class.
        /// </summary>
        /// <param name="loops">The loops.</param>
        /// <param name="onSideFaces"></param>
        internal ContactData(IEnumerable<Loop> loops, List<PolygonalFace> onSideFaces)
        {
            OnSideFaces = onSideFaces;
            PositiveLoops = new List<Loop>();
            NegativeLoops = new List<Loop>();
            foreach (var loop in loops)
            {
                //Perimeter += loop.Perimeter;
                //Area += loop.Area;
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
        /// The combined perimeter of the 2D loops defined with the Contact Data.
        /// </summary>
        //public readonly double Perimeter;
        /// <summary>
        /// The combined area of the 2D loops defined with the Contact Data
        /// </summary>
        //public readonly double Area;

        /// <summary>
        /// List of In Plane Faces
        /// </summary>
        public readonly List<PolygonalFace> OnSideFaces;
    }
    
    /// <summary>
    /// The Loop class is basically a list of ContactElements that form a path. Usually, this path
    /// is closed, hence the name "loop", but it may be used and useful for open paths as well.
    /// </summary>
    public class Loop
    {
        /// <summary>
        /// The vertices making up this loop
        /// </summary>
        public readonly List<Vertex> VertexLoop;
        /// <summary>
        /// The faces that were formed on-side for this loop. About 2/3 s
        /// of these faces should have one negligible adjacent face. 
        /// </summary>
        public readonly List<PolygonalFace> OnSideContactFaces;
        /// <summary>
        /// Is the loop positive - meaning does it enclose material versus representing a hole
        /// </summary>
        public bool IsPositive;
        /// <summary>
        /// Negative loops must always be inside positive loops. This is a place to store all
        /// the pos/neg loop dependency.
        /// </summary>
        public List<Loop> DependentLoops;
        /// <summary>
        /// The length of the loop.
        /// </summary>
        //public readonly double Perimeter;
        /// <summary>
        /// The area of the loop
        /// </summary>
        //public readonly double Area;
        /// <summary>
        /// Is the loop closed?
        /// </summary>
        public readonly bool IsClosed;

        /// <summary>
        /// Initializes a new instance of the <see cref="Loop" /> class.
        /// </summary>
        /// <param name="normal">The normal.</param>
        /// <param name="isClosed">is closed.</param>
        internal Loop(List<Vertex> vertexLoop, List<PolygonalFace> onSideContactFaces, IList<double> normal, bool isClosed = true)
        {
            if (!IsClosed) Message.output("loop not closed!",3);
            VertexLoop = vertexLoop;
            OnSideContactFaces = onSideContactFaces;
            IsClosed = isClosed;    
        }

        //ToDo: Add functions to determine Perimeter and Area if necessary
    }
}

