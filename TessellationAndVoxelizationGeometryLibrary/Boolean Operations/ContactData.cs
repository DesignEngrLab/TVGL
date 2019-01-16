using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using StarMathLib;

namespace TVGL
{
    /// <summary>
    /// A ContactData that stores all the necessary face information from a slice
    /// to be able to produce solids.
    /// </summary>
    public class ContactData
    {
        internal ContactData(IEnumerable<SolidContactData>solidContactData, Flat plane)
        {
            SolidContactData = new List<SolidContactData>(solidContactData);
            Plane = plane;
        }

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
        public readonly Flat Plane;
    }

    /// <summary>
    /// Stores the information 
    /// </summary>
    public class SolidContactData
    {
        internal SolidContactData(IEnumerable<Loop> loops, IEnumerable<PolygonalFace> onSideFaces, IEnumerable<PolygonalFace> onPlaneFaces)
        {
            OnSideFaces = new List<PolygonalFace>(onSideFaces);
            var polygonalFaces = onPlaneFaces as PolygonalFace[] ?? onPlaneFaces.ToArray();
            OnPlaneFaces = new List<PolygonalFace>(polygonalFaces);
            var onSideContactFaces = new List<PolygonalFace>();
            var positiveLoops = new List<Loop>();
            var negativeLoops = new List<Loop>();
            foreach (var loop in loops)
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

            var area2 = polygonalFaces.Sum(face => face.Area);
            if (!Area.IsPracticallySame(area2, 0.01*Area)) Debug.WriteLine("SolidContactData loop area and face area do not match.");
            //Set Immutable Lists
            OnSideContactFaces = onSideContactFaces;
            PositiveLoops = positiveLoops;
            NegativeLoops = negativeLoops;
            _vertices = new List<Vertex>();
            _volume = 0;
        }

        //Contact data can be made up of information from the positive side or the negative side with infinite cutting planes
        //Finite cutting planes may have solids with contact data on both sides. 
        public bool OnPositiveSide { get; }
        public bool OnNegativeSide { get; }

        /// <summary>
        /// Gets the vertices belonging to this solid
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Vertex> AllVertices()
        {
            if (_vertices.Any()) return _vertices;

            //Find all the vertices for this solid.
            var vertexHash = new HashSet<Vertex>();
            var allFaces = new List<PolygonalFace>(OnSideFaces);
            allFaces.AddRange(OnSideContactFaces);
            foreach (var vertex in allFaces.SelectMany(face => face.Vertices.Where(vertex => !vertexHash.Contains(vertex))))
            {
                vertexHash.Add(vertex);
            }
            _vertices = vertexHash;
            return _vertices;
        }

        private IEnumerable<Vertex> _vertices;

        /// <summary>
        /// Gets the vertices belonging to this solid
        /// </summary>
        /// <returns></returns>
        public double Volume()
        {
            if (_volume > 0) return _volume;
            //Else
            double[] center;
            _volume = TessellatedSolid.CalculateVolume(AllFaces, out center);
            return _volume;
        }

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
        public readonly IEnumerable<PolygonalFace> OnSideFaces;

        /// <summary>
        /// The faces that were formed on-side for all the loops in this solid. 
        /// </summary>
        public readonly IEnumerable<PolygonalFace> OnSideContactFaces;

        /// <summary>
        /// A list of the on plane faces formed by the triangulation of the loops
        /// </summary>
        public readonly IEnumerable<PolygonalFace> OnPlaneFaces;

        /// <summary>
        /// Gets all faces belonging to this solid's contact data (All faces except those that will be triangulated in plane)
        /// </summary>
        /// <value>All loops.</value>
        public List<PolygonalFace> AllFaces
        {
            get
            {
                var allFaces = new List<PolygonalFace>(OnSideFaces);
                allFaces.AddRange(OnSideContactFaces);
                allFaces.AddRange(OnPlaneFaces);
                return allFaces;
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
                var allLoops = new List<Loop>() { PositiveLoop};
                allLoops.AddRange(NegativeLoops);
                return allLoops;
            }
        }

        /// <summary>
        /// The faces that were formed on-side for all the loops in this group. 
        /// </summary>
        public readonly IEnumerable<PolygonalFace> OnSideContactFaces;

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
        public readonly IEnumerable<PolygonalFace> OnPlaneFaces;

        public readonly HashSet<Vertex> StraddleEdgeOnSideVertices;

        internal GroupOfLoops(Loop positiveLoop, IEnumerable<Loop> negativeLoops, IEnumerable<PolygonalFace> onPlaneFaces)
        {
            var onSideContactFaces = new List<PolygonalFace>(positiveLoop.OnSideContactFaces);
            var straddleFaceIndices = new HashSet<int>(positiveLoop.StraddleFaceIndices);
            var adjOnsideFaceIndices = new HashSet<int>(positiveLoop.AdjOnsideFaceIndices);
            OnPlaneFaces = new List<PolygonalFace>(onPlaneFaces);
            PositiveLoop = positiveLoop;
            StraddleEdgeOnSideVertices = new HashSet<Vertex>(positiveLoop.StraddleEdgeOnSideVertices);
            NegativeLoops = new List<Loop>(negativeLoops);
            foreach (var negativeLoop in NegativeLoops)
            {
                onSideContactFaces.AddRange(negativeLoop.OnSideContactFaces);
                foreach(var vertex in negativeLoop.StraddleEdgeOnSideVertices) StraddleEdgeOnSideVertices.Add(vertex);

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
        public IList<Vertex> VertexLoop;
        /// <summary>
        /// The faces that were formed on-side for this loop. About 2/3 s
        /// of these faces should have one negligible adjacent face. 
        /// </summary>
        public readonly IEnumerable<PolygonalFace> OnSideContactFaces;

        public readonly HashSet<Vertex> StraddleEdgeOnSideVertices;

        /// <summary>
        /// Is the loop positive - meaning does it enclose material versus representing a hole
        /// </summary>
        public bool IsPositive
        {
            get { return _isPositive; }
            set
            {
                _isPositive = value;
                var positiveArea = !(Area < 0);
                if (_isPositive == positiveArea) return;

                //Else, reverse the loop and the invert the area.
                Area = -Area;
                var temp = VertexLoop.Reverse();
                VertexLoop = new List<Vertex>(temp);
            }
        }

        private bool _isPositive;

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

        public readonly int Index;

        public readonly bool PositiveSide;

        /// <summary>
        /// Initializes a new instance of the <see cref="Loop" /> class.
        /// </summary>
        /// <param name="vertexLoop"></param>
        /// <param name="onSideContactFaces"></param>
        /// <param name="normal">The normal.</param>
        /// <param name="straddleFaceIndices"></param>
        /// <param name="adjOnsideFaceIndices"></param>
        /// <param name="index"></param>
        /// <param name="fromPositiveSide"></param>
        /// <param name="straddleEdgeOnSideVertices"></param>
        /// <param name="isClosed">is closed.</param>
        internal Loop(ICollection<Vertex> vertexLoop, IEnumerable<PolygonalFace> onSideContactFaces, double[] normal, 
            IEnumerable<int> straddleFaceIndices, IEnumerable<int> adjOnsideFaceIndices, int index, bool fromPositiveSide,
            IEnumerable<Vertex> straddleEdgeOnSideVertices, bool isClosed = true)
        {
            if (!IsClosed) Message.output("loop not closed!",3);
            VertexLoop = new List<Vertex>(vertexLoop);
            OnSideContactFaces = onSideContactFaces;
            IsClosed = isClosed;
            Area = MiscFunctions.AreaOf3DPolygon(vertexLoop, normal);
            _isPositive = !(Area < 0);
            Perimeter = MiscFunctions.Perimeter(vertexLoop);
            AdjOnsideFaceIndices = new List<int>(adjOnsideFaceIndices);
            StraddleFaceIndices = new List<int>(straddleFaceIndices);
            Index = index;
            PositiveSide = fromPositiveSide;
            StraddleEdgeOnSideVertices = new HashSet<Vertex>(straddleEdgeOnSideVertices);
        }
    }
}
  
