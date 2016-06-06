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
        /// Loop directionality must be set prior to initiallizing a new instance.
        /// </summary>
        /// <param name="loops">The loops.</param>
        /// <param name="onSideFaces"></param>
        internal ContactData(List<Loop> loops, List<PolygonalFace> onSideFaces)
        {
            OnSideFaces = onSideFaces;
            OnSideContactFaces = new List<PolygonalFace>();
            PositiveLoops = new List<Loop>();
            NegativeLoops = new List<Loop>();
            foreach (var loop in loops)
            {
                Area += loop.Area;
                if (loop.IsPositive) PositiveLoops.Add(loop);
                else NegativeLoops.Add(loop);
                OnSideContactFaces.AddRange(loop.OnSideContactFaces);
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
        /// The combined area of the 2D loops defined with the Contact Data
        /// </summary>
        public readonly double Area;

        /// <summary>
        /// List of pre-existing faces on this side of the cutting plane
        /// </summary>
        public readonly List<PolygonalFace> OnSideFaces;

        /// <summary>
        /// The faces that were formed on-side for all the loops in this solid. 
        /// </summary>
        public readonly List<PolygonalFace> OnSideContactFaces;

        /// <summary>
        /// Gets all faces belonging to this solid's contact data (All faces except those that will be triangulated in plane)
        /// </summary>
        /// <value>All loops.</value>
        public List<PolygonalFace> AllOnSideFaces
        {
            get
            {
                var allOnSideFaces = new List<PolygonalFace>(OnSideFaces);
                allOnSideFaces.AddRange(OnSideContactFaces);
                return allOnSideFaces;
            }
        }
    }

    /// <summary>
    /// The GroupOfLoops class is a list of dependent loops and their associated information.
    /// This difference from ContactData, since it only every has one positive loop.
    /// </summary>
    public class GroupOfLoops
    {
        /// <summary>
        /// Gets the positive loop.
        /// </summary>
        /// <value>The positive loops.</value>
        public readonly Loop PositiveLoop;

        /// <summary>
        /// Gets the loops of negative area (i.e. holes).
        /// </summary>
        /// <value>The negative loops.</value>                    
        public readonly List<Loop> NegativeLoops;

        /// <summary>
        /// Gets all loops in one list (the positive loops are followed by the
        /// negative loops).
        /// </summary>
        /// <value>All loops.</value>
        public List<Loop> AllLoops
        {
            get
            {
                var allLoops = new List<Loop>() { PositiveLoop};
                allLoops.AddRange(NegativeLoops);
                return allLoops;
            }
        }

        /// <summary>
        /// The faces that were formed on-side for all the loops in this group. 
        /// </summary>
        public readonly List<PolygonalFace> OnSideContactFaces;

        /// <summary>
        /// A list of the idices of the faces that were adjacent and onside to the straddle faces
        /// </summary>
        public readonly HashSet<int> AdjOnsideFaceIndices;

        /// <summary>
        /// A list of the idices of the straddle faces 
        /// </summary>
        public readonly HashSet<int> StraddleFaceIndices;

        internal GroupOfLoops(Loop positiveLoop, List<Loop> negativeLoops = null)
        {
            OnSideContactFaces = positiveLoop.OnSideContactFaces;
            StraddleFaceIndices = positiveLoop.StraddleFaceIndices;
            AdjOnsideFaceIndices = positiveLoop.AdjOnsideFaceIndices;
            PositiveLoop = positiveLoop;
            if (negativeLoops == null) return;
            NegativeLoops = negativeLoops;
            foreach (var negativeLoop in NegativeLoops)
            {
                foreach (var onSideContactFace in negativeLoop.OnSideContactFaces)
                {
                    OnSideContactFaces.Add(onSideContactFace);
                }
                foreach (var straddleFaceIndex in negativeLoop.StraddleFaceIndices)
                {
                    StraddleFaceIndices.Add(straddleFaceIndex);
                }
                foreach (var adjOnsideFaceIndex in negativeLoop.AdjOnsideFaceIndices)
                {
                    AdjOnsideFaceIndices.Add(adjOnsideFaceIndex);
                }
            }
        }
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
        /// A list of the idices of the faces that were adjacent and onside to the straddle faces
        /// </summary>
        public readonly HashSet<int> AdjOnsideFaceIndices;

        /// <summary>
        /// A list of the idices of the straddle faces 
        /// </summary>
        public readonly HashSet<int> StraddleFaceIndices;

        /// <summary>
        /// Initializes a new instance of the <see cref="Loop" /> class.
        /// </summary>
        /// <param name="vertexLoop"></param>
        /// <param name="onSideContactFaces"></param>
        /// <param name="normal">The normal.</param>
        /// <param name="straddleFaceIndices"></param>
        /// <param name="adjOnsideFaceIndices"></param>
        /// <param name="isClosed">is closed.</param>
        internal Loop(List<Vertex> vertexLoop, List<PolygonalFace> onSideContactFaces, double[] normal, HashSet<int> straddleFaceIndices, HashSet<int> adjOnsideFaceIndices, bool isClosed = true)
        {
            if (!IsClosed) Message.output("loop not closed!",3);
            VertexLoop = vertexLoop;
            OnSideContactFaces = onSideContactFaces;
            IsClosed = isClosed;
            Area = MiscFunctions.AreaOf3DPolygon(vertexLoop, normal);
            Perimeter = MiscFunctions.Perimeter(vertexLoop);
            AdjOnsideFaceIndices = adjOnsideFaceIndices;
            StraddleFaceIndices = straddleFaceIndices;
            foreach (var index in straddleFaceIndices)
            {
                if(adjOnsideFaceIndices.Contains(index)) throw new Exception("Face cannot be both a straddle face and an adjacent onside face.");
            }
        }

        
    }
}

