// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-14-2023
// ***********************************************************************
// <copyright file="ContactData.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TVGL
{
    /// <summary>
    /// A ContactData that stores all the necessary face information from a slice
    /// to be able to produce solids.
    /// </summary>
    public class ContactData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContactData"/> class.
        /// </summary>
        /// <param name="solidContactData">The solid contact data.</param>
        /// <param name="intersectionGroups">The intersection groups.</param>
        /// <param name="plane">The plane.</param>
        internal ContactData(IEnumerable<SolidContactData> solidContactData, IEnumerable<IntersectionGroup> intersectionGroups, Plane plane)
        {
            SolidContactData = new List<SolidContactData>(solidContactData);
            IntersectionGroups = new List<IntersectionGroup>(intersectionGroups);
            Plane = plane;
        }

        /// <summary>
        /// Gets the intersection loop information. Empty, unless considering finite planes.
        /// </summary>
        public readonly IEnumerable<IntersectionGroup> IntersectionGroups;

        /// <summary>
        /// Gets the list of positive side contact data
        /// </summary>
        /// <value>The positive loops.</value>
        public readonly IEnumerable<SolidContactData> SolidContactData;

        /// <summary>
        /// Gets the list of positive side contact data. If finite plane, this contact data could also be on the negative side.
        /// </summary>
        /// <value>The positive loops.</value>
        public IEnumerable<SolidContactData> PositiveSideContactData => SolidContactData.Where(s => s.OnPositiveSide);

        /// <summary>
        /// Gets the list of negative side contact data. If finite plane, this contact data could also be on the positive side.
        /// </summary>
        /// <value>The positive loops.</value>
        public IEnumerable<SolidContactData> NegativeSideContactData => SolidContactData.Where(s => s.OnNegativeSide);

        /// <summary>
        /// Gets the plane for this contact data
        /// </summary>
        /// <value>The positive loops.</value>
        public readonly Plane Plane;
    }

    /// <summary>
    /// Stores the information
    /// </summary>
    public class SolidContactData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SolidContactData"/> class.
        /// </summary>
        /// <param name="groupsOfLoops">The groups of loops.</param>
        /// <param name="onSideFaces">The on side faces.</param>
        /// <param name="onPlaneFaces">The on plane faces.</param>
        internal SolidContactData(IEnumerable<GroupOfLoops> groupsOfLoops, IEnumerable<TriangleFace> onSideFaces, IEnumerable<TriangleFace> onPlaneFaces)
        {
            OnSideFaces = new List<TriangleFace>(onSideFaces);
            var triangleFaces = onPlaneFaces as TriangleFace[] ?? onPlaneFaces.ToArray();
            OnPlaneFaces = new List<TriangleFace>(triangleFaces);
            var onSideContactFaces = new List<TriangleFace>();
            GroupsOfLoops = new List<GroupOfLoops>(groupsOfLoops);
            var positiveLoops = new List<Loop>();
            var negativeLoops = new List<Loop>();
            foreach (var groupOfLoops in GroupsOfLoops)
            {
                foreach (var loop in groupOfLoops.AllLoops)
                {
                    Area += loop.Area;
                    if (loop.IsPositive) positiveLoops.Add(loop);
                    else negativeLoops.Add(loop);
                    onSideContactFaces.AddRange(loop.OnSideContactFaces);
                    //Note: With a finite plane, it is possible to have loops on both the positive and negative sides (Consider cutting 
                    //vertically through the center of "S" without seperating the middle).
                    if (loop.PositiveSide) OnPositiveSide = true;
                    else OnNegativeSide = true;
                }
            }

            var area2 = triangleFaces.Sum(face => face.Area);
            if (!Area.IsPracticallySame(area2, 0.01 * Area)) Message.output("SolidContactData loop area and face area do not match.", 2);
            //Set Immutable Lists
            OnSideContactFaces = onSideContactFaces;
            PositiveLoops = positiveLoops;
            NegativeLoops = negativeLoops;
            _volume = 0;
        }

        /// <summary>
        /// The groups of loops
        /// </summary>
        public List<GroupOfLoops> GroupsOfLoops;

        //Contact data can be made up of information from the positive side or the negative side with infinite cutting planes
        //Finite cutting planes may have solids with contact data on both sides. 
        /// <summary>
        /// Gets a value indicating whether [on positive side].
        /// </summary>
        /// <value><c>true</c> if [on positive side]; otherwise, <c>false</c>.</value>
        public bool OnPositiveSide { get; }
        /// <summary>
        /// Gets a value indicating whether [on negative side].
        /// </summary>
        /// <value><c>true</c> if [on negative side]; otherwise, <c>false</c>.</value>
        public bool OnNegativeSide { get; }

        /// <summary>
        /// Gets the vertices belonging to this solid
        /// </summary>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>System.Double.</returns>
        public double Volume(double tolerance)
        {
            if (_volume > 0) return _volume;
            TessellatedSolid.CalculateVolumeAndCenter(AllFaces, tolerance, out _volume, out _);
            return _volume;
        }

        /// <summary>
        /// The volume
        /// </summary>
        private double _volume;

        /// <summary>
        /// Gets the positive loops.
        /// </summary>
        /// <value>The positive loops.</value>
        public readonly IEnumerable<Loop> PositiveLoops;

        /// <summary>
        /// Gets the loops of negative area (i.e. holes).
        /// </summary>
        /// <value>The negative loops.</value>
        public readonly IEnumerable<Loop> NegativeLoops;

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
        public readonly IEnumerable<TriangleFace> OnSideFaces;

        /// <summary>
        /// The faces that were formed on-side for all the loops in this solid.
        /// </summary>
        public readonly IEnumerable<TriangleFace> OnSideContactFaces;

        /// <summary>
        /// A list of the on plane faces formed by the triangulation of the loops
        /// </summary>
        public readonly IEnumerable<TriangleFace> OnPlaneFaces;

        /// <summary>
        /// Gets all faces belonging to this solid's contact data (All faces except those that will be triangulated in plane)
        /// </summary>
        /// <value>All loops.</value>
        public List<TriangleFace> AllFaces
        {
            get
            {
                var allFaces = new List<TriangleFace>(OnSideFaces);
                allFaces.AddRange(OnSideContactFaces);
                allFaces.AddRange(OnPlaneFaces);
                return allFaces;
            }
        }
    }

    /// <summary>
    /// The IntersectionLoop class stores information about how the positive and negative side
    /// loops connect together. This includes a 2D intersection and pointers back to the GroupOfLoops
    /// that contributes to it from each side.
    /// </summary>
    public class IntersectionGroup
    {
        /// <summary>
        /// The intersection2 d
        /// </summary>
        public readonly List<Polygon> Intersection2D;
        /// <summary>
        /// The group of loops
        /// </summary>
        public readonly HashSet<GroupOfLoops> GroupOfLoops;
        /// <summary>
        /// Gets the loop indices.
        /// </summary>
        /// <returns>List&lt;System.Int32&gt;.</returns>
        public List<int> GetLoopIndices()
        {
            var loopIndices = new List<int>();
            foreach (var group in GroupOfLoops)
            {
                foreach (var loop in group.AllLoops)
                {
                    loopIndices.Add(loop.Index);
                }
            }
            return loopIndices;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IntersectionGroup"/> class.
        /// </summary>
        /// <param name="posSideGroupOfLoops">The position side group of loops.</param>
        /// <param name="negSideGroupOfLoops">The neg side group of loops.</param>
        /// <param name="intersection2D">The intersection2 d.</param>
        /// <param name="index">The index.</param>
        public IntersectionGroup(GroupOfLoops posSideGroupOfLoops, GroupOfLoops negSideGroupOfLoops,
            List<Polygon> intersection2D, int index)
        {
            GroupOfLoops = new HashSet<GroupOfLoops> { posSideGroupOfLoops, negSideGroupOfLoops };
            Intersection2D = intersection2D;
            Index = index;
        }

        /// <summary>
        /// Gets the index.
        /// </summary>
        /// <value>The index.</value>
        public int Index { get; }
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
        public readonly IEnumerable<Loop> NegativeLoops;

        /// <summary>
        /// Gets all loops in one list (the positive loops are followed by the
        /// negative loops).
        /// </summary>
        /// <value>All loops.</value>
        public List<Loop> AllLoops
        {
            get
            {
                var allLoops = new List<Loop>() { PositiveLoop };
                allLoops.AddRange(NegativeLoops);
                return allLoops;
            }
        }

        /// <summary>
        /// The faces that were formed on-side for all the loops in this group.
        /// </summary>
        public readonly IEnumerable<TriangleFace> OnSideContactFaces;

        /// <summary>
        /// A list of the idices of the faces that were adjacent and onside to the straddle faces
        /// </summary>
        public readonly IEnumerable<int> AdjOnsideFaceIndices;

        /// <summary>
        /// A list of the idices of the straddle faces
        /// </summary>
        public readonly IEnumerable<int> StraddleFaceIndices;

        /// <summary>
        /// A list of the on plane faces formed by the triangulation of the loops
        /// </summary>
        public readonly IEnumerable<TriangleFace> OnPlaneFaces;

        /// <summary>
        /// The straddle edge on side vertices
        /// </summary>
        public readonly HashSet<Vertex> StraddleEdgeOnSideVertices;

        /// <summary>
        /// The cross section2 d
        /// </summary>
        public Polygon CrossSection2D;

        /// <summary>
        /// Sets the cross section2 d.
        /// </summary>
        /// <param name="plane">The plane.</param>
        public void SetCrossSection2D(Plane plane)
        {
            var flattenTransform = MiscFunctions.TransformToXYPlane(plane.Normal, out _);
            var positivePath = PositiveLoop.VertexLoop.ProjectTo2DCoordinates(flattenTransform).ToList();
            if (positivePath.Area() < 0) positivePath.Reverse();
            CrossSection2D = new Polygon(positivePath);
            foreach (var loop in NegativeLoops)
            {
                var negativePath = loop.VertexLoop.ProjectTo2DCoordinates(flattenTransform).ToList();
                if (negativePath.Area() > 0) negativePath.Reverse();
                CrossSection2D.AddInnerPolygon(new Polygon(negativePath));
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GroupOfLoops"/> class.
        /// </summary>
        /// <param name="positiveLoop">The positive loop.</param>
        /// <param name="negativeLoops">The negative loops.</param>
        /// <param name="onPlaneFaces">The on plane faces.</param>
        internal GroupOfLoops(Loop positiveLoop, IEnumerable<Loop> negativeLoops, IEnumerable<TriangleFace> onPlaneFaces)
        {
            var onSideContactFaces = new List<TriangleFace>(positiveLoop.OnSideContactFaces);
            var straddleFaceIndices = new HashSet<int>(positiveLoop.StraddleFaceIndices);
            var adjOnsideFaceIndices = new HashSet<int>(positiveLoop.AdjOnsideFaceIndices);
            OnPlaneFaces = new List<TriangleFace>(onPlaneFaces);
            PositiveLoop = positiveLoop;
            StraddleEdgeOnSideVertices = new HashSet<Vertex>(positiveLoop.StraddleEdgeOnSideVertices);
            NegativeLoops = new List<Loop>(negativeLoops);
            foreach (var negativeLoop in NegativeLoops)
            {
                onSideContactFaces.AddRange(negativeLoop.OnSideContactFaces);
                foreach (var vertex in negativeLoop.StraddleEdgeOnSideVertices) StraddleEdgeOnSideVertices.Add(vertex);

                foreach (var straddleFaceIndex in negativeLoop.StraddleFaceIndices)
                {
                    straddleFaceIndices.Add(straddleFaceIndex);
                }
                foreach (var adjOnsideFaceIndex in negativeLoop.AdjOnsideFaceIndices)
                {
                    adjOnsideFaceIndices.Add(adjOnsideFaceIndex);
                }
            }
            //Set immutable lists
            OnSideContactFaces = onSideContactFaces;
            StraddleFaceIndices = straddleFaceIndices;
            AdjOnsideFaceIndices = adjOnsideFaceIndices;
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
        public Vertex[] VertexLoop;
        /// <summary>
        /// The faces that were formed on-side for this loop. About 2/3 s
        /// of these faces should have one negligible adjacent face.
        /// </summary>
        public readonly IEnumerable<TriangleFace> OnSideContactFaces;

        /// <summary>
        /// The straddle edge on side vertices
        /// </summary>
        public readonly HashSet<Vertex> StraddleEdgeOnSideVertices;

        /// <summary>
        /// Is the loop positive - meaning does it enclose material versus representing a hole
        /// </summary>
        /// <value><c>true</c> if this instance is positive; otherwise, <c>false</c>.</value>
        public bool IsPositive
        {
            get => _isPositive;
            set
            {
                _isPositive = value;
                var positiveArea = !(Area < 0);
                if (_isPositive == positiveArea) return;

                //Else, reverse the loop and the invert the area.
                Area = -Area;
                var temp = VertexLoop.Reverse();
                VertexLoop = temp.ToArray();
            }
        }

        /// <summary>
        /// The is positive
        /// </summary>
        private bool _isPositive;

        /// <summary>
        /// The length of the loop.
        /// </summary>
        public readonly double Perimeter;

        /// <summary>
        /// The area of the loop
        /// </summary>
        public double Area;

        /// <summary>
        /// Is the loop closed?
        /// </summary>
        public readonly bool IsClosed;

        /// <summary>
        /// A list of the idices of the faces that were adjacent and onside to the straddle faces
        /// </summary>
        public readonly IEnumerable<int> AdjOnsideFaceIndices;

        /// <summary>
        /// A list of the idices of the straddle faces
        /// </summary>
        public readonly IEnumerable<int> StraddleFaceIndices;

        /// <summary>
        /// The index
        /// </summary>
        public readonly int Index;

        /// <summary>
        /// The positive side
        /// </summary>
        public readonly bool PositiveSide;

        /// <summary>
        /// Initializes a new instance of the <see cref="Loop" /> class.
        /// </summary>
        /// <param name="vertexLoop">The vertex loop.</param>
        /// <param name="onSideContactFaces">The on side contact faces.</param>
        /// <param name="normal">The normal.</param>
        /// <param name="straddleFaceIndices">The straddle face indices.</param>
        /// <param name="adjOnsideFaceIndices">The adj onside face indices.</param>
        /// <param name="index">The index.</param>
        /// <param name="fromPositiveSide">if set to <c>true</c> [from positive side].</param>
        /// <param name="straddleEdgeOnSideVertices">The straddle edge on side vertices.</param>
        /// <param name="isClosed">is closed.</param>
        internal Loop(IList<Vertex> vertexLoop, IEnumerable<TriangleFace> onSideContactFaces, Vector3 normal,
            IEnumerable<int> straddleFaceIndices, IEnumerable<int> adjOnsideFaceIndices, int index, bool fromPositiveSide,
            IEnumerable<Vertex> straddleEdgeOnSideVertices, bool isClosed = true)
        {
            if (!IsClosed) Message.output("loop not closed!", 3);
            VertexLoop = vertexLoop.ToArray();
            OnSideContactFaces = onSideContactFaces;
            IsClosed = isClosed;
            Area = MiscFunctions.AreaOf3DPolygon(vertexLoop, normal);
            _isPositive = !(Area < 0);
            Perimeter = MiscFunctions.Length(vertexLoop);
            AdjOnsideFaceIndices = new List<int>(adjOnsideFaceIndices);
            StraddleFaceIndices = new List<int>(straddleFaceIndices);
            Index = index;
            PositiveSide = fromPositiveSide;
            StraddleEdgeOnSideVertices = new HashSet<Vertex>(straddleEdgeOnSideVertices);
        }
    }
}

