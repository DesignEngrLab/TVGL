/*******************************************************************************
*                                                                              *
* Author    :  Angus Johnson                                                   *
* Version   :  6.2.1                                                           *
* Date      :  31 October 2014                                                 *
* Website   :  http://www.angusj.com                                           *
* Copyright :  Angus Johnson 2010-2014                                         *
*                                                                              *
* License:                                                                     *
* Use, modification & distribution is subject to Boost Software License Ver 1. *
* http://www.boost.org/LICENSE_1_0.txt                                         *
*                                                                              *
* Attributions:                                                                *
* The code in this library is an extension of Bala Vatti's clipping algorithm: *
* "A generic solution to polygon clipping"                                     *
* Communications of the ACM, Vol 35, Issue 7 (July 1992) pp 56-63.             *
* http://portal.acm.org/citation.cfm?id=129906                                 *
*                                                                              *
* Computer graphics and geometric modeling: implementation and algorithms      *
* By Max K. Agoston                                                            *
* Springer; 1 edition (January 4, 2005)                                        *
* http://books.google.com/books?q=vatti+clipping+agoston                       *
*                                                                              *
* See also:                                                                    *
* "Polygon Offsetting by Computing Winding Numbers"                            *
* Paper no. DETC2005-85513 pp. 565-575                                         *
* ASME 2005 International Design Engineering Technical Conferences             *
* and Computers and Information in Engineering Conference (IDETC/CIE2005)      *
* September 24-28, 2005 , Long Beach, California, USA                          *
* http://www.me.berkeley.edu/~mcmains/pubs/DAC05OffsetPolygon.pdf              *
*                                                                              *
*******************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using StarMathLib;


namespace TVGL.Clipper
{
    using Path = List<Point>;
    using Paths = List<List<Point>>;

    #region DoublePoint Class
    internal struct DoublePoint
    {
        internal double X;
        internal double Y;

        internal DoublePoint(double x = 0, double y = 0)
        {
            X = x; Y = y;
        }
        internal DoublePoint(DoublePoint dp)
        {
            X = dp.X; Y = dp.Y;
        }
        internal DoublePoint(Point ip)
        {
            X = ip.X; Y = ip.Y;
        }
    }
    #endregion

    #region PolyTree & PolyNode classes
    internal class PolyTree : PolyNode
    {
        internal List<PolyNode> MAllPolys = new List<PolyNode>();

        ~PolyTree()
        {
            Clear();
        }

        internal void Clear()
        {
            for (var i = 0; i < MAllPolys.Count; i++)
                MAllPolys[i] = null;
            MAllPolys.Clear();
            MChilds.Clear();
        }

        internal PolyNode GetFirst()
        {
            if (MChilds.Count > 0)
                return MChilds[0];
            return null;
        }

        internal int Total
        {
            get
            {
                var result = MAllPolys.Count;
                //with negative offsets, ignore the hidden outer polygon ...
                if (result > 0 && MChilds[0] != MAllPolys[0]) result--;
                return result;
            }
        }
    }

    internal class PolyNode
    {
        internal PolyNode MParent;
        internal Path MPolygon = new Path();
        internal int MIndex;
        internal JoinType MJointype;
        internal EndType MEndtype;
        internal List<PolyNode> MChilds = new List<PolyNode>();

        internal bool IsHoleNode()
        {
            var result = true;
            var node = MParent;
            while (node != null)
            {
                result = !result;
                node = node.MParent;
            }
            return result;
        }

        internal int ChildCount => MChilds.Count;

        internal Path Contour => MPolygon;

        internal void AddChild(PolyNode child)
        {
            var cnt = MChilds.Count;
            MChilds.Add(child);
            child.MParent = this;
            child.MIndex = cnt;
        }

        internal PolyNode GetNext()
        {
            return MChilds.Count > 0 ? MChilds[0] : GetNextSiblingUp();
        }

        internal PolyNode GetNextSiblingUp()
        {
            if (MParent == null)
                return null;
            if (MIndex == MParent.MChilds.Count - 1)
                return MParent.GetNextSiblingUp();
            return MParent.MChilds[MIndex + 1];
        }

        internal List<PolyNode> Childs => MChilds;

        internal PolyNode Parent => MParent;

        internal bool IsHole => IsHoleNode();

        internal bool IsOpen { get; set; }
    }
    #endregion

    #region Integer Rectangle Class
    internal struct IntRect
    {
        internal double Left;
        internal double Top;
        internal double Right;
        internal double Bottom;

        internal IntRect(double l, double t, double r, double b)
        {
            Left = l; Top = t;
            Right = r; Bottom = b;
        }
        internal IntRect(IntRect ir)
        {
            Left = ir.Left; Top = ir.Top;
            Right = ir.Right; Bottom = ir.Bottom;
        }
    }
    #endregion

    #region Internal Enum Values
    internal enum ClipType { Intersection, Union, Difference, Xor };
    internal enum PolyType { Subject, Clip };

    //By far the most widely used winding rules for polygon filling are
    //EvenOdd & NonZero (GDI, GDI+, XLib, OpenGL, Cairo, AGG, Quartz, SVG, Gr32)
    //Others rules include Positive, Negative and ABS_GTR_EQ_TWO (only in OpenGL)
    //see http://glprogramming.com/red/chapter11.html
    internal enum PolyFillType { EvenOdd, NonZero, Positive, Negative };

    internal enum JoinType { Square, Round, Miter };
    internal enum EndType { ClosedPolygon, ClosedLine, OpenButt, OpenSquare, OpenRound };

    internal enum EdgeSide { Left, Right };
    internal enum Direction { RightToLeft, LeftToRight };
    #endregion

    #region T Edge Class
    internal class ClipperEdge
    {
        internal Point Bot;
        internal Point Curr;
        internal Point Top;
        internal Point Delta;
        internal double Dx; //Note: For some unknown reason, the author decided to use an inverted slope, where Dx = run/rise, so Dx = inf is horizontal and Dx = 0 is vertical.
        internal PolyType PolyTyp;
        internal EdgeSide Side;
        internal int WindDelta; //1 or -1 depending on winding direction
        internal int WindCnt;
        internal int WindCnt2; //winding count of the opposite polytype
        internal int OutIdx;
        internal ClipperEdge Next;
        internal ClipperEdge Prev;
        internal ClipperEdge NextInLml;
        internal ClipperEdge NextInAEL;
        internal ClipperEdge PrevInAEL;
        internal ClipperEdge NextInSel;
        internal ClipperEdge PrevInSel;
        internal ClipperEdge()
        {
            Bot = new Point(0, 0);
            Curr = new Point(0, 0);
            Top = new Point(0, 0);
            Delta = new Point(0, 0);
        }
    }
    #endregion

    #region IntersectZ Node Class
    internal class IntersectNode
    {
        internal ClipperEdge Edge1;
        internal ClipperEdge Edge2;
        internal Point Pt;
    }
    #endregion

    #region Other Internal Classes
    internal class MyIntersectNodeSort : IComparer<IntersectNode>
    {
        public int Compare(IntersectNode node1, IntersectNode node2)
        {
            var i = node2.Pt.Y - node1.Pt.Y;
            if (i > 0) return 1;
            if (i < 0) return -1;
            return 0;
        }
    }

    internal class LocalMinima
    {
        internal double Y;
        internal ClipperEdge LeftBound;
        internal ClipperEdge RightBound;
        internal LocalMinima Next;
    }

    internal class Scanbeam
    {
        internal double Y;
        internal Scanbeam Next;
    }

    internal class OutRec
    {
        internal int Idx;
        internal bool IsHole;
        internal bool IsOpen;
        internal OutRec FirstLeft; //see comments in clipper.pas
        internal OutPt Pts;
        internal OutPt BottomPt;
        internal PolyNode PolyNode;
    }

    internal class OutPt
    {
        internal int Idx;
        internal Point Pt;
        internal OutPt Next;
        internal OutPt Prev;
    }

    internal class Join
    {
        internal OutPt OutPt1;
        internal OutPt OutPt2;
        internal Point OffPt;
    }
    #endregion

    #region ClipperBase Class
    internal class ClipperBase
    {
        protected const double Horizontal = -3.4E+38; //Note: For some unknown reason, the author decided to use an inverted slope, where Dx = run/rise, so Dx = inf is horizontal and Dx = 0 is vertical.
        protected const int Skip = -2;
        protected const int Unassigned = -1;
        protected static readonly double Tolerance = StarMath.EqualityTolerance;
        internal static bool near_zero(double val) { return (val > -Tolerance) && (val < Tolerance); }
        internal LocalMinima MMinimaList;
        internal LocalMinima MCurrentLm;
        internal List<List<ClipperEdge>> MEdges = new List<List<ClipperEdge>>();
        internal bool MUseFullRange;
        internal bool MHasOpenPaths;

        internal bool PreserveCollinear
        {
            get;
            set;
        }

        internal static bool IsHorizontal(ClipperEdge e)
        {
            return e.Delta.Y.IsNegligible();
        }

        internal bool PointIsVertex(Point pt, OutPt pp)
        {
            var pp2 = pp;
            do
            {
                if (pp2.Pt == pt) return true;
                pp2 = pp2.Next;
            }
            while (pp2 != pp);
            return false;
        }

        internal bool PointOnLineSegment(Point pt,
            Point linePt1, Point linePt2)
        {
            return ((pt.X.IsPracticallySame(linePt1.X)) && (pt.Y.IsPracticallySame(linePt1.Y))) ||
                   ((pt.X.IsPracticallySame(linePt2.X)) && (pt.Y.IsPracticallySame(linePt2.Y))) ||
                   (((pt.X > linePt1.X) == (pt.X < linePt2.X)) &&
                    ((pt.Y > linePt1.Y) == (pt.Y < linePt2.Y)) &&
                    (((pt.X - linePt1.X) * (linePt2.Y - linePt1.Y)).IsPracticallySame((linePt2.X - linePt1.X) * (pt.Y - linePt1.Y))));
        }

        //------------------------------------------------------------------------------

        internal bool PointOnPolygon(Point pt, OutPt pp)
        {
            var pp2 = pp;
            while (true)
            {
                if (PointOnLineSegment(pt, pp2.Pt, pp2.Next.Pt))
                    return true;
                pp2 = pp2.Next;
                if (pp2 == pp) break;
            }
            return false;
        }
        //------------------------------------------------------------------------------

        internal static bool SlopesEqual(ClipperEdge e1, ClipperEdge e2)
        {
            return (e1.Delta.Y * e2.Delta.X).IsPracticallySame(e1.Delta.X * e2.Delta.Y);
        }
        //------------------------------------------------------------------------------

        protected static bool SlopesEqual(Point pt1, Point pt2,
            Point pt3)
        {
            return ((pt1.Y - pt2.Y) * (pt2.X - pt3.X) - (pt1.X - pt2.X) * (pt2.Y - pt3.Y)).IsNegligible();
        }
        //------------------------------------------------------------------------------

        protected static bool SlopesEqual(Point pt1, Point pt2,
            Point pt3, Point pt4)
        {
            return ((pt1.Y - pt2.Y) * (pt3.X - pt4.X) - (pt1.X - pt2.X) * (pt3.Y - pt4.Y)).IsNegligible();
        }

        internal ClipperBase() //constructor (nb: no external instantiation)
        {
            MMinimaList = null;
            MCurrentLm = null;
            MUseFullRange = false;
            MHasOpenPaths = false;
        }

        internal virtual void Clear()
        {
            DisposeLocalMinimaList();
            foreach (var t in MEdges)
            {
                for (var j = 0; j < t.Count; ++j) t[j] = null;
                t.Clear();
            }
            MEdges.Clear();
            MUseFullRange = false;
            MHasOpenPaths = false;
        }

        private void DisposeLocalMinimaList()
        {
            while (MMinimaList != null)
            {
                var tmpLm = MMinimaList.Next;
                MMinimaList = null;
                MMinimaList = tmpLm;
            }
            MCurrentLm = null;
        }

        private static void InitEdge(ClipperEdge e, ClipperEdge eNext,
        ClipperEdge ePrev, Point pt)
        {
            e.Next = eNext;
            e.Prev = ePrev;
            e.Curr = pt;
            e.OutIdx = Unassigned;
        }

        private static void InitEdge2(ClipperEdge e, PolyType polyType)
        {
            if (e.Curr.Y.IsPracticallySame(e.Next.Curr.Y) || e.Curr.Y > e.Next.Curr.Y)
            {
                e.Bot = e.Curr;
                e.Top = e.Next.Curr;
            }
            else
            {
                e.Top = e.Curr;
                e.Bot = e.Next.Curr;
            }
            SetDx(e);
            e.PolyTyp = polyType;
        }

        private static ClipperEdge FindNextLocMin(ClipperEdge e)
        {
            for (;;)
            {
                while (e.Bot != e.Prev.Bot || e.Curr == e.Top) e = e.Next;
                if (!e.Dx.IsPracticallySame(Horizontal) && !e.Prev.Dx.IsPracticallySame(Horizontal)) break;
                while (e.Prev.Dx.IsPracticallySame(Horizontal)) e = e.Prev;
                var e2 = e;
                while (e.Dx.IsPracticallySame(Horizontal)) e = e.Next;
                if (e.Top.Y.IsPracticallySame(e.Prev.Bot.Y)) continue; //ie just an intermediate horz.
                if (e2.Prev.Bot.X < e.Bot.X) e = e2;
                break;
            }
            return e;
        }

        private ClipperEdge ProcessBound(ClipperEdge e, bool leftBoundIsForward)
        {
            ClipperEdge eStart, result = e;
            ClipperEdge horz;

            if (result.OutIdx == Skip)
            {
                //check if there are edges beyond the skip edge in the bound and if so
                //create another LocMin and calling ProcessBound once more ...
                e = result;
                if (leftBoundIsForward)
                {
                    while (e.Top.Y.IsPracticallySame(e.Next.Bot.Y)) e = e.Next;
                    while (e != result && e.Dx.IsPracticallySame(Horizontal)) e = e.Prev;
                }
                else
                {
                    while (e.Top.Y.IsPracticallySame(e.Prev.Bot.Y)) e = e.Prev;
                    while (e != result && e.Dx.IsPracticallySame(Horizontal)) e = e.Next;
                }
                if (e == result)
                {
                    result = leftBoundIsForward ? e.Next : e.Prev;
                }
                else
                {
                    //there are more edges in the bound beyond result starting with E
                    e = leftBoundIsForward ? result.Next : result.Prev;
                    var locMin = new LocalMinima
                    {
                        Next = null,
                        Y = e.Bot.Y,
                        LeftBound = null,
                        RightBound = e
                    };
                    e.WindDelta = 0;
                    result = ProcessBound(e, leftBoundIsForward);
                    InsertLocalMinima(locMin);
                }
                return result;
            }

            if (e.Dx.IsPracticallySame(Horizontal))
            {
                //We need to be careful with open paths because this may not be a
                //true local minima (ie E may be following a skip edge).
                //Also, consecutive horz. edges may start heading left before going right.
                eStart = leftBoundIsForward ? e.Prev : e.Next;
                if (eStart.OutIdx != Skip)
                {
                    if (eStart.Dx.IsPracticallySame(Horizontal)) //ie an adjoining horizontal skip edge
                    {
                        if (!eStart.Bot.X.IsPracticallySame(e.Bot.X) && !eStart.Top.X.IsPracticallySame(e.Bot.X))
                            ReverseHorizontal(e);
                    }
                    else if (!eStart.Bot.X.IsPracticallySame(e.Bot.X))
                        ReverseHorizontal(e);
                }
            }

            eStart = e;
            if (leftBoundIsForward)
            {
                while (result.Top.Y.IsPracticallySame(result.Next.Bot.Y) && result.Next.OutIdx != Skip)
                    result = result.Next;
                if (result.Dx.IsPracticallySame(Horizontal) && result.Next.OutIdx != Skip)
                {
                    //nb: at the top of a bound, horizontals are added to the bound
                    //only when the preceding edge attaches to the horizontal's left vertex
                    //unless a Skip edge is encountered when that becomes the top divide
                    horz = result;
                    while (horz.Prev.Dx.IsPracticallySame(Horizontal)) horz = horz.Prev;
                    if (horz.Prev.Top.X.IsPracticallySame(result.Next.Top.X))
                    {
                    }
                    else if (horz.Prev.Top.X > result.Next.Top.X) result = horz.Prev;
                }
                while (e != result)
                {
                    e.NextInLml = e.Next;
                    if (e.Dx.IsPracticallySame(Horizontal) && e != eStart && !e.Bot.X.IsPracticallySame(e.Prev.Top.X))
                        ReverseHorizontal(e);
                    e = e.Next;
                }
                if (e.Dx.IsPracticallySame(Horizontal) && e != eStart && !e.Bot.X.IsPracticallySame(e.Prev.Top.X))
                    ReverseHorizontal(e);
                result = result.Next; //move to the edge just beyond current bound
            }
            else
            {
                while (result.Top.Y.IsPracticallySame(result.Prev.Bot.Y) && result.Prev.OutIdx != Skip)
                    result = result.Prev;
                if (result.Dx.IsPracticallySame(Horizontal) && result.Prev.OutIdx != Skip)
                {
                    horz = result;
                    while (horz.Next.Dx.IsPracticallySame(Horizontal)) horz = horz.Next;
                    if (horz.Next.Top.X.IsPracticallySame(result.Prev.Top.X))
                    {
                        result = horz.Next;
                    }
                    else if (horz.Next.Top.X > result.Prev.Top.X) result = horz.Next;
                }

                while (e != result)
                {
                    e.NextInLml = e.Prev;
                    if (e.Dx.IsPracticallySame(Horizontal) && e != eStart && !e.Bot.X.IsPracticallySame(e.Next.Top.X))
                        ReverseHorizontal(e);
                    e = e.Prev;
                }
                if (e.Dx.IsPracticallySame(Horizontal) && e != eStart && !e.Bot.X.IsPracticallySame(e.Next.Top.X))
                    ReverseHorizontal(e);
                result = result.Prev; //move to the edge just beyond current bound
            }
            return result;
        }
        //------------------------------------------------------------------------------


        internal bool AddPath(Path pg, PolyType polyType, bool closed)
        {
            if (!closed && polyType == PolyType.Clip)
                throw new ClipperException("AddPath: Open paths must be subject.");

            var highI = pg.Count - 1;
            if (closed) while (highI > 0 && (pg[highI] == pg[0])) --highI;
            while (highI > 0 && (pg[highI] == pg[highI - 1])) --highI;
            if ((closed && highI < 2) || (!closed && highI < 1)) return false;

            //create a new edge array ...
            var edges = new List<ClipperEdge>(highI + 1);
            for (var i = 0; i <= highI; i++) edges.Add(new ClipperEdge());

            var isFlat = true;

            //1. Basic (first) edge initialization ...
            edges[1].Curr = pg[1];
            InitEdge(edges[0], edges[1], edges[highI], pg[0]);
            InitEdge(edges[highI], edges[0], edges[highI - 1], pg[highI]);
            for (int i = highI - 1; i >= 1; --i)
            {
                InitEdge(edges[i], edges[i + 1], edges[i - 1], pg[i]);
            }
            var eStart = edges[0];

            //2. Remove duplicate vertices, and (when closed) collinear edges ...
            ClipperEdge edge = eStart, eLoopStop = eStart;
            for (;;)
            {
                //nb: allows matching start and end points when not Closed ...
                if (edge.Curr == edge.Next.Curr && (closed || edge.Next != eStart))
                {
                    if (edge == edge.Next) break;
                    if (edge == eStart) eStart = edge.Next;
                    edge = RemoveEdge(edge);
                    eLoopStop = edge;
                    continue;
                }
                if (edge.Prev == edge.Next)
                    break; //only two vertices
                else if (closed &&
                  SlopesEqual(edge.Prev.Curr, edge.Curr, edge.Next.Curr) &&
                  (!PreserveCollinear ||
                  !Pt2IsBetweenPt1AndPt3(edge.Prev.Curr, edge.Curr, edge.Next.Curr)))
                {
                    //Collinear edges are allowed for open paths but in closed paths
                    //the default is to merge adjacent collinear edges into a single edge.
                    //However, if the PreserveCollinear property is enabled, only overlapping
                    //collinear edges (ie spikes) will be removed from closed paths.
                    if (edge == eStart) eStart = edge.Next;
                    edge = RemoveEdge(edge);
                    edge = edge.Prev;
                    eLoopStop = edge;
                    continue;
                }
                edge = edge.Next;
                if ((edge == eLoopStop) || (!closed && edge.Next == eStart)) break;
            }

            if ((!closed && (edge == edge.Next)) || (closed && (edge.Prev == edge.Next)))
                return false;

            if (!closed)
            {
                MHasOpenPaths = true;
                eStart.Prev.OutIdx = Skip;
            }

            //3. Do second stage of edge initialization ...
            edge = eStart;
            do
            {
                InitEdge2(edge, polyType);
                edge = edge.Next;
                if (isFlat && !edge.Curr.Y.IsPracticallySame(eStart.Curr.Y)) isFlat = false;
            }
            while (edge != eStart);

            //4. Finally, add edge bounds to LocalMinima list ...

            //Totally flat paths must be handled differently when adding them
            //to LocalMinima list to avoid endless loops etc ...
            if (isFlat)
            {
                if (closed) return false;
                edge.Prev.OutIdx = Skip;
                if (edge.Prev.Bot.X < edge.Prev.Top.X) ReverseHorizontal(edge.Prev);
                var locMin = new LocalMinima
                {
                    Next = null,
                    Y = edge.Bot.Y,
                    LeftBound = null,
                    RightBound = edge
                };
                locMin.RightBound.Side = EdgeSide.Right;
                locMin.RightBound.WindDelta = 0;
                while (edge.Next.OutIdx != Skip)
                {
                    edge.NextInLml = edge.Next;
                    if (!edge.Bot.X.IsPracticallySame(edge.Prev.Top.X)) ReverseHorizontal(edge);
                    edge = edge.Next;
                }
                InsertLocalMinima(locMin);
                MEdges.Add(edges);
                return true;
            }

            MEdges.Add(edges);
            ClipperEdge eMin = null;
            ClipperEdge previousEdge = null;
            var stallCounter = 0;

            //workaround to avoid an endless loop in the while loop below when
            //open paths have matching start and end points ...
            if (edge.Prev.Bot == edge.Prev.Top) edge = edge.Next;
            var successful = false;

            //ToDo: There is a memory leak in this function.
            while (!successful)
            {
                //Find the next local minima, from the current edge
                edge = FindNextLocMin(edge);
                //If the next local minima is the first local minima we found, then exit.
                if (edge == eMin)
                {
                    successful = true;
                    continue;
                }
                //Set the first local minima
                if (eMin == null) eMin = edge;

                //Check if the function is caught in an infinite loop. Fix if possible.
                if (edge == previousEdge) stallCounter++;
                if(stallCounter > 10) throw new Exception("Caught in infinite loop.");
                previousEdge = edge;
                
                //E and E.Prev now share a local minima (left aligned if horizontal).
                //Compare their slopes to find which starts which bound ...
                var locMin = new LocalMinima
                {
                    Next = null,
                    Y = edge.Bot.Y
                };
                bool leftBoundIsForward;

                if (edge.Dx < edge.Prev.Dx)
                {
                    locMin.LeftBound = edge.Prev;
                    locMin.RightBound = edge;
                    leftBoundIsForward = false; //Q.nextInLML = Q.prev
                }
                else
                {
                    locMin.LeftBound = edge;
                    locMin.RightBound = edge.Prev;
                    leftBoundIsForward = true; //Q.nextInLML = Q.next
                }
                locMin.LeftBound.Side = EdgeSide.Left;
                locMin.RightBound.Side = EdgeSide.Right;

                if (!closed) locMin.LeftBound.WindDelta = 0;
                else if (locMin.LeftBound.Next == locMin.RightBound)
                    locMin.LeftBound.WindDelta = -1;
                else locMin.LeftBound.WindDelta = 1;
                locMin.RightBound.WindDelta = -locMin.LeftBound.WindDelta;

                edge = ProcessBound(locMin.LeftBound, leftBoundIsForward);
                if (edge.OutIdx == Skip) edge = ProcessBound(edge, leftBoundIsForward);

                var edge2 = ProcessBound(locMin.RightBound, !leftBoundIsForward);
                if (edge2.OutIdx == Skip) edge2 = ProcessBound(edge2, !leftBoundIsForward);

                if (locMin.LeftBound.OutIdx == Skip)
                    locMin.LeftBound = null;
                else if (locMin.RightBound.OutIdx == Skip)
                    locMin.RightBound = null;
                InsertLocalMinima(locMin);
                if (!leftBoundIsForward) edge = edge2;
            }
            return true;
        }
        //------------------------------------------------------------------------------

        internal bool AddPaths(Paths ppg, PolyType polyType, bool closed)
        {
            var result = false;
            for (var i = 0; i < ppg.Count; ++i)
                if (AddPath(ppg[i], polyType, closed)) result = true;
            return result;
        }
        //------------------------------------------------------------------------------

        internal bool Pt2IsBetweenPt1AndPt3(Point pt1, Point pt2, Point pt3)
        {
            if ((pt1 == pt3) || (pt1 == pt2) || (pt3 == pt2)) return false;
            if (!pt1.X.IsPracticallySame(pt3.X)) return (pt2.X > pt1.X) == (pt2.X < pt3.X);
            return (pt2.Y > pt1.Y) == (pt2.Y < pt3.Y);
        }
        //------------------------------------------------------------------------------

        static ClipperEdge RemoveEdge(ClipperEdge e)
        {
            //removes e from double_linked_list (but without removing from memory)
            e.Prev.Next = e.Next;
            e.Next.Prev = e.Prev;
            var result = e.Next;
            e.Prev = null; //flag as removed (see ClipperBase.Clear)
            return result;
        }
        //------------------------------------------------------------------------------

        private static void SetDx(ClipperEdge e)
        {
            var deltaX = (e.Top.X - e.Bot.X);
            var deltaY = (e.Top.Y - e.Bot.Y);
            e.Delta = new Point(deltaX, deltaY);
            //ToDo: The value used in this IsNegligible is critical. Better to handle as Horizontal than be very close to horizontal.
            if (e.Delta.Y.IsNegligible()) e.Dx = Horizontal;
            else e.Dx = (e.Delta.X) / (e.Delta.Y);
            if(double.IsNaN(e.Dx)) throw new Exception("Must be a number");
        }
        //---------------------------------------------------------------------------

        private void InsertLocalMinima(LocalMinima newLm)
        {
            if (MMinimaList == null)
            {
                MMinimaList = newLm;
            }
            else if (!newLm.Y.IsLessThanNonNegligible(MMinimaList.Y))
            {
                newLm.Next = MMinimaList;
                MMinimaList = newLm;
            }
            else
            {
                var tmpLm = MMinimaList;
                while (tmpLm.Next != null && newLm.Y < tmpLm.Next.Y)
                    tmpLm = tmpLm.Next;
                newLm.Next = tmpLm.Next;
                tmpLm.Next = newLm;
            }
        }
        //------------------------------------------------------------------------------

        protected void PopLocalMinima()
        {
            MCurrentLm = MCurrentLm?.Next;
        }
        //------------------------------------------------------------------------------

        private static void ReverseHorizontal(ClipperEdge edge)
        {
            //swap horizontal edges' top and bottom x's so they follow the natural
            //progression of the bounds - ie so their xbots will align with the
            //adjoining lower edge. [Helpful in the ProcessHorizontal() method.]
            var temp = edge.Top.X;
            edge.Top = new Point(edge.Bot.X, edge.Top.Y);
            edge.Bot = new Point(temp, edge.Bot.Y);
        }
        //------------------------------------------------------------------------------

        protected virtual void Reset()
        {
            MCurrentLm = MMinimaList;
            if (MCurrentLm == null) return; //ie nothing to process

            //reset all edges ...
            var localMinima = MMinimaList;
            while (localMinima != null)
            {
                var edge = localMinima.LeftBound;
                if (edge != null)
                {
                    edge.Curr = edge.Bot;
                    edge.Side = EdgeSide.Left;
                    edge.OutIdx = Unassigned;
                }
                edge = localMinima.RightBound;
                if (edge != null)
                {
                    edge.Curr = edge.Bot;
                    edge.Side = EdgeSide.Right;
                    edge.OutIdx = Unassigned;
                }
                localMinima = localMinima.Next;
            }
        }
        //------------------------------------------------------------------------------

        internal static IntRect GetBounds(Paths paths)
        {
            int i = 0, cnt = paths.Count;
            while (i < cnt && paths[i].Count == 0) i++;
            if (i == cnt) return new IntRect(0, 0, 0, 0);
            var result = new IntRect { Left = paths[i][0].X };
            result.Right = result.Left;
            result.Top = paths[i][0].Y;
            result.Bottom = result.Top;
            for (; i < cnt; i++)
                for (int j = 0; j < paths[i].Count; j++)
                {
                    if (paths[i][j].X < result.Left) result.Left = paths[i][j].X;
                    else if (paths[i][j].X > result.Right) result.Right = paths[i][j].X;
                    if (paths[i][j].Y < result.Top) result.Top = paths[i][j].Y;
                    else if (paths[i][j].Y > result.Bottom) result.Bottom = paths[i][j].Y;
                }
            return result;
        }

    } //end ClipperBase
    #endregion

    #region Clipper Class
    internal class Clipper : ClipperBase
    {
        //InitOptions that can be passed to the constructor ...
        internal const int IOReverseSolution = 1;
        internal const int IOStrictlySimple = 2;
        internal const int IOPreserveCollinear = 4;

        private List<OutRec> _mPolyOuts;
        private ClipType _mClipType;
        private Scanbeam _mScanbeam;
        private ClipperEdge _mActiveEdges;
        private ClipperEdge _mSortedEdges;
        private List<IntersectNode> _mIntersectList;
        IComparer<IntersectNode> _mIntersectNodeComparer;
        private bool _mExecuteLocked;
        private PolyFillType _mClipFillType;
        private PolyFillType _mSubjFillType;
        private List<Join> _mJoins;
        private List<Join> _mGhostJoins;
        private bool _mUsingPolyTree;

        internal Clipper(int initOptions = 0) //constructor
        {
            _mScanbeam = null;
            _mActiveEdges = null;
            _mSortedEdges = null;
            _mIntersectList = new List<IntersectNode>();
            _mIntersectNodeComparer = new MyIntersectNodeSort();
            _mExecuteLocked = false;
            _mUsingPolyTree = false;
            _mPolyOuts = new List<OutRec>();
            _mJoins = new List<Join>();
            _mGhostJoins = new List<Join>();
            ReverseSolution = (IOReverseSolution & initOptions) != 0;
            StrictlySimple = (IOStrictlySimple & initOptions) != 0;
            PreserveCollinear = (IOPreserveCollinear & initOptions) != 0;
        }
        //------------------------------------------------------------------------------

        protected override void Reset()
        {
            base.Reset();
            _mScanbeam = null;
            _mActiveEdges = null;
            _mSortedEdges = null;
            var lm = MMinimaList;
            while (lm != null)
            {
                InsertScanbeam(lm.Y);
                lm = lm.Next;
            }
        }
        //------------------------------------------------------------------------------

        internal bool ReverseSolution
        {
            get;
            set;
        }
        //------------------------------------------------------------------------------

        internal bool StrictlySimple
        {
            get;
            set;
        }
        //------------------------------------------------------------------------------

        private void InsertScanbeam(double y)
        {
            if (_mScanbeam == null)
            {
                _mScanbeam = new Scanbeam
                {
                    Next = null,
                    Y = y
                };
            }
            else if (y > _mScanbeam.Y)
            {
                var newSb = new Scanbeam
                {
                    Y = y,
                    Next = _mScanbeam
                };
                _mScanbeam = newSb;
            }
            else
            {
                var sb2 = _mScanbeam;
                while (sb2.Next != null && !y.IsGreaterThanNonNegligible(sb2.Next.Y)) sb2 = sb2.Next;
                if (y.IsPracticallySame(sb2.Y)) return; //ie ignores duplicates
                var newSb = new Scanbeam
                {
                    Y = y,
                    Next = sb2.Next
                };
                sb2.Next = newSb;
            }
        }
        //------------------------------------------------------------------------------

        internal bool Execute(ClipType clipType, Paths solution,
            PolyFillType subjFillType, PolyFillType clipFillType)
        {
            if (_mExecuteLocked) return false;
            if (MHasOpenPaths) throw
              new ClipperException("Error: PolyTree struct is need for open path clipping.");

            _mExecuteLocked = true;
            solution.Clear();
            _mSubjFillType = subjFillType;
            _mClipFillType = clipFillType;
            _mClipType = clipType;
            _mUsingPolyTree = false;
            bool succeeded;
            try
            {
                succeeded = ExecuteInternal();
                //build the return polygons ...
                if (succeeded) BuildResult(solution);
            }
            finally
            {
                DisposeAllPolyPts();
                _mExecuteLocked = false;
            }
            return succeeded;
        }
        //------------------------------------------------------------------------------

        internal bool Execute(ClipType clipType, PolyTree polytree,
            PolyFillType subjFillType, PolyFillType clipFillType)
        {
            if (_mExecuteLocked) return false;
            _mExecuteLocked = true;
            _mSubjFillType = subjFillType;
            _mClipFillType = clipFillType;
            _mClipType = clipType;
            _mUsingPolyTree = true;
            bool succeeded;
            try
            {
                succeeded = ExecuteInternal();
                //build the return polygons ...
                if (succeeded) BuildResult2(polytree);
            }
            finally
            {
                DisposeAllPolyPts();
                _mExecuteLocked = false;
            }
            return succeeded;
        }
        //------------------------------------------------------------------------------

        internal bool Execute(ClipType clipType, Paths solution)
        {
            return Execute(clipType, solution,
                PolyFillType.EvenOdd, PolyFillType.EvenOdd);
        }
        //------------------------------------------------------------------------------

        internal bool Execute(ClipType clipType, PolyTree polytree)
        {
            return Execute(clipType, polytree,
                PolyFillType.EvenOdd, PolyFillType.EvenOdd);
        }
        //------------------------------------------------------------------------------

        internal void FixHoleLinkage(OutRec outRec)
        {
            //skip if an outermost polygon or
            //already already points to the correct FirstLeft ...
            if (outRec.FirstLeft == null ||
                  (outRec.IsHole != outRec.FirstLeft.IsHole &&
                  outRec.FirstLeft.Pts != null)) return;

            var orfl = outRec.FirstLeft;
            while (orfl != null && ((orfl.IsHole == outRec.IsHole) || orfl.Pts == null))
                orfl = orfl.FirstLeft;
            outRec.FirstLeft = orfl;
        }
        //------------------------------------------------------------------------------

        private bool ExecuteInternal()
        {
            try
            {
                Reset();
                if (MCurrentLm == null) return false;

                var botY = PopScanbeam();
                do
                {
                    InsertLocalMinimaIntoAEL(botY);
                    _mGhostJoins.Clear();
                    ProcessHorizontals(false);
                    if (_mScanbeam == null) break;
                    var topY = PopScanbeam();
                    if (!ProcessIntersections(topY)) return false;
                    ProcessEdgesAtTopOfScanbeam(topY);
                    botY = topY;
                } while (_mScanbeam != null || MCurrentLm != null);

                //fix orientations ...
                foreach (var outRec in _mPolyOuts)
                {
                    if (outRec.Pts == null || outRec.IsOpen) continue;
                    if ((outRec.IsHole ^ ReverseSolution) == (Area(outRec) > 0))
                        ReversePolyPtLinks(outRec.Pts);
                }

                JoinCommonEdges();

                foreach (var outRec in _mPolyOuts.Where(outRec => outRec.Pts != null && !outRec.IsOpen))
                {
                    FixupOutPolygon(outRec);
                }

                if (StrictlySimple) DoSimplePolygons();
                return true;
            }
            catch { return false; }
            finally
            {
                _mJoins.Clear();
                _mGhostJoins.Clear();
            }
        }
        //------------------------------------------------------------------------------

        private double PopScanbeam()
        {
            var y = _mScanbeam.Y;
            _mScanbeam = _mScanbeam.Next;
            return y;
        }
        //------------------------------------------------------------------------------

        private void DisposeAllPolyPts()
        {
            for (var i = 0; i < _mPolyOuts.Count; ++i) DisposeOutRec(i);
            _mPolyOuts.Clear();
        }
        //------------------------------------------------------------------------------

        private void DisposeOutRec(int index)
        {
            var outRec = _mPolyOuts[index];
            outRec.Pts = null;
            _mPolyOuts[index] = null;
        }
        //------------------------------------------------------------------------------

        private void AddJoin(OutPt outPoint1, OutPt outPoint2, Point offPt)
        {
            var join = new Join();
            join.OutPt1 = outPoint1;
            join.OutPt2 = outPoint2;
            join.OffPt = offPt;
            _mJoins.Add(join);
        }
        //------------------------------------------------------------------------------

        private void AddGhostJoin(OutPt outPoint, Point offPt)
        {
            var join = new Join();
            join.OutPt1 = outPoint;
            join.OffPt = offPt;
            _mGhostJoins.Add(join);
        }
        //------------------------------------------------------------------------------

        private void InsertLocalMinimaIntoAEL(double botY)
        {
            while (MCurrentLm != null && (MCurrentLm.Y.IsPracticallySame(botY)))
            {
                var leftBoundEdge = MCurrentLm.LeftBound;
                var rightBoundEdge = MCurrentLm.RightBound;
                PopLocalMinima();

                OutPt outPoint = null;
                if (leftBoundEdge == null)
                {
                    InsertEdgeIntoAEL(rightBoundEdge, null);
                    SetWindingCount(rightBoundEdge);
                    if (IsContributing(rightBoundEdge))
                        outPoint = AddOutPt(rightBoundEdge, rightBoundEdge.Bot);
                }
                else if (rightBoundEdge == null)
                {
                    InsertEdgeIntoAEL(leftBoundEdge, null);
                    SetWindingCount(leftBoundEdge);
                    if (IsContributing(leftBoundEdge))
                        outPoint = AddOutPt(leftBoundEdge, leftBoundEdge.Bot);
                    InsertScanbeam(leftBoundEdge.Top.Y);
                }
                else
                {
                    InsertEdgeIntoAEL(leftBoundEdge, null);
                    InsertEdgeIntoAEL(rightBoundEdge, leftBoundEdge);
                    SetWindingCount(leftBoundEdge);
                    rightBoundEdge.WindCnt = leftBoundEdge.WindCnt;
                    rightBoundEdge.WindCnt2 = leftBoundEdge.WindCnt2;
                    if (IsContributing(leftBoundEdge))
                        outPoint = AddLocalMinPoly(leftBoundEdge, rightBoundEdge, leftBoundEdge.Bot);
                    InsertScanbeam(leftBoundEdge.Top.Y);
                }

                if (rightBoundEdge != null)
                {
                    if (IsHorizontal(rightBoundEdge))
                        AddEdgeToSEL(rightBoundEdge);
                    else
                        InsertScanbeam(rightBoundEdge.Top.Y);
                }

                if (leftBoundEdge == null || rightBoundEdge == null) continue;

                //if output polygons share an Edge with a horizontal rb, they'll need joining later ...
                if (outPoint != null && IsHorizontal(rightBoundEdge) &&
                  _mGhostJoins.Count > 0 && rightBoundEdge.WindDelta != 0)
                {
                    for (var i = 0; i < _mGhostJoins.Count; i++)
                    {
                        //if the horizontal Rb and a 'ghost' horizontal overlap, then convert
                        //the 'ghost' join to a real join ready for later ...
                        var join = _mGhostJoins[i];
                        if (HorzSegmentsOverlap(join.OutPt1.Pt.X, join.OffPt.X, rightBoundEdge.Bot.X, rightBoundEdge.Top.X))
                            AddJoin(join.OutPt1, outPoint, join.OffPt);
                    }
                }

                if (leftBoundEdge.OutIdx >= 0 && leftBoundEdge.PrevInAEL != null &&
                  leftBoundEdge.PrevInAEL.Curr.X.IsPracticallySame(leftBoundEdge.Bot.X) &&
                  leftBoundEdge.PrevInAEL.OutIdx >= 0 &&
                  SlopesEqual(leftBoundEdge.PrevInAEL, leftBoundEdge) &&
                  leftBoundEdge.WindDelta != 0 && leftBoundEdge.PrevInAEL.WindDelta != 0)
                {
                    var outPoint2 = AddOutPt(leftBoundEdge.PrevInAEL, leftBoundEdge.Bot);
                    AddJoin(outPoint, outPoint2, leftBoundEdge.Top);
                }

                if (leftBoundEdge.NextInAEL == rightBoundEdge) continue;
                if (rightBoundEdge.OutIdx >= 0 && rightBoundEdge.PrevInAEL.OutIdx >= 0 &&
                    SlopesEqual(rightBoundEdge.PrevInAEL, rightBoundEdge) &&
                    rightBoundEdge.WindDelta != 0 && rightBoundEdge.PrevInAEL.WindDelta != 0)
                {
                    var outPoint2 = AddOutPt(rightBoundEdge.PrevInAEL, rightBoundEdge.Bot);
                    AddJoin(outPoint, outPoint2, rightBoundEdge.Top);
                }

                var edge = leftBoundEdge.NextInAEL;
                if (edge == null) continue;
                while (edge != rightBoundEdge)
                {
                    //nb: For calculating winding counts etc, IntersectEdges() assumes
                    //that param1 will be to the right of param2 ABOVE the intersection ...
                    IntersectEdges(rightBoundEdge, edge, leftBoundEdge.Curr); //order important here
                    edge = edge.NextInAEL;
                }
            }
        }
        //------------------------------------------------------------------------------

        private void InsertEdgeIntoAEL(ClipperEdge edge, ClipperEdge startEdge)
        {
            if (_mActiveEdges == null)
            {
                edge.PrevInAEL = null;
                edge.NextInAEL = null;
                _mActiveEdges = edge;
            }
            else if (startEdge == null && E2InsertsBeforeE1(_mActiveEdges, edge))
            {
                edge.PrevInAEL = null;
                edge.NextInAEL = _mActiveEdges;
                _mActiveEdges.PrevInAEL = edge;
                _mActiveEdges = edge;
            }
            else
            {
                if (startEdge == null) startEdge = _mActiveEdges;
                while (startEdge.NextInAEL != null &&
                  !E2InsertsBeforeE1(startEdge.NextInAEL, edge))
                    startEdge = startEdge.NextInAEL;
                edge.NextInAEL = startEdge.NextInAEL;
                if (startEdge.NextInAEL != null) startEdge.NextInAEL.PrevInAEL = edge;
                edge.PrevInAEL = startEdge;
                startEdge.NextInAEL = edge;
            }
        }
        //----------------------------------------------------------------------

        private static bool E2InsertsBeforeE1(ClipperEdge e1, ClipperEdge e2)
        {
            if (e2.Curr.X.IsPracticallySame(e1.Curr.X))
            {
                if (e2.Top.Y > e1.Top.Y)
                    return e2.Top.X < TopX(e1, e2.Top.Y);
                else return e1.Top.X > TopX(e2, e1.Top.Y);
            }
            else return e2.Curr.X < e1.Curr.X;
        }
        //------------------------------------------------------------------------------

        private bool IsEvenOddFillType(ClipperEdge edge)
        {
            if (edge.PolyTyp == PolyType.Subject)
                return _mSubjFillType == PolyFillType.EvenOdd;
            else
                return _mClipFillType == PolyFillType.EvenOdd;
        }
        //------------------------------------------------------------------------------

        private bool IsEvenOddAltFillType(ClipperEdge edge)
        {
            if (edge.PolyTyp == PolyType.Subject)
                return _mClipFillType == PolyFillType.EvenOdd;
            else
                return _mSubjFillType == PolyFillType.EvenOdd;
        }
        //------------------------------------------------------------------------------

        private bool IsContributing(ClipperEdge edge)
        {
            PolyFillType pft, pft2;
            if (edge.PolyTyp == PolyType.Subject)
            {
                pft = _mSubjFillType;
                pft2 = _mClipFillType;
            }
            else
            {
                pft = _mClipFillType;
                pft2 = _mSubjFillType;
            }

            switch (pft)
            {
                case PolyFillType.EvenOdd:
                    //return false if a subj line has been flagged as inside a subj polygon
                    if (edge.WindDelta == 0 && edge.WindCnt != 1) return false;
                    break;
                case PolyFillType.NonZero:
                    if (Math.Abs(edge.WindCnt) != 1) return false;
                    break;
                case PolyFillType.Positive:
                    if (edge.WindCnt != 1) return false;
                    break;
                default: //PolyFillType.pftNegative
                    if (edge.WindCnt != -1) return false;
                    break;
            }

            switch (_mClipType)
            {
                case ClipType.Intersection:
                    switch (pft2)
                    {
                        case PolyFillType.EvenOdd:
                        case PolyFillType.NonZero:
                            return (edge.WindCnt2 != 0);
                        case PolyFillType.Positive:
                            return (edge.WindCnt2 > 0);
                        default:
                            return (edge.WindCnt2 < 0);
                    }
                case ClipType.Union:
                    switch (pft2)
                    {
                        case PolyFillType.EvenOdd:
                        case PolyFillType.NonZero:
                            return (edge.WindCnt2 == 0);
                        case PolyFillType.Positive:
                            return (edge.WindCnt2 <= 0);
                        default:
                            return (edge.WindCnt2 >= 0);
                    }
                case ClipType.Difference:
                    if (edge.PolyTyp == PolyType.Subject)
                        switch (pft2)
                        {
                            case PolyFillType.EvenOdd:
                            case PolyFillType.NonZero:
                                return (edge.WindCnt2 == 0);
                            case PolyFillType.Positive:
                                return (edge.WindCnt2 <= 0);
                            default:
                                return (edge.WindCnt2 >= 0);
                        }
                    else
                        switch (pft2)
                        {
                            case PolyFillType.EvenOdd:
                            case PolyFillType.NonZero:
                                return (edge.WindCnt2 != 0);
                            case PolyFillType.Positive:
                                return (edge.WindCnt2 > 0);
                            default:
                                return (edge.WindCnt2 < 0);
                        }
                case ClipType.Xor:
                    if (edge.WindDelta == 0) //XOr always contributing unless open
                        switch (pft2)
                        {
                            case PolyFillType.EvenOdd:
                            case PolyFillType.NonZero:
                                return (edge.WindCnt2 == 0);
                            case PolyFillType.Positive:
                                return (edge.WindCnt2 <= 0);
                            default:
                                return (edge.WindCnt2 >= 0);
                        }
                    else
                        return true;
            }
            return true;
        }
        //------------------------------------------------------------------------------

        private void SetWindingCount(ClipperEdge edge)
        {
            var previousEdge = edge.PrevInAEL;
            //find the edge of the same polytype that immediately preceeds 'edge' in AEL
            while (previousEdge != null && ((previousEdge.PolyTyp != edge.PolyTyp) || (previousEdge.WindDelta == 0))) previousEdge = previousEdge.PrevInAEL;
            if (previousEdge == null)
            {
                edge.WindCnt = (edge.WindDelta == 0 ? 1 : edge.WindDelta);
                edge.WindCnt2 = 0;
                previousEdge = _mActiveEdges; //ie get ready to calc WindCnt2
            }
            else if (edge.WindDelta == 0 && _mClipType != ClipType.Union)
            {
                edge.WindCnt = 1;
                edge.WindCnt2 = previousEdge.WindCnt2;
                previousEdge = previousEdge.NextInAEL; //ie get ready to calc WindCnt2
            }
            else if (IsEvenOddFillType(edge))
            {
                //EvenOdd filling ...
                if (edge.WindDelta == 0)
                {
                    //are we inside a subj polygon ...
                    var inside = true;
                    var edge2 = previousEdge.PrevInAEL;
                    while (edge2 != null)
                    {
                        if (edge2.PolyTyp == previousEdge.PolyTyp && edge2.WindDelta != 0)
                            inside = !inside;
                        edge2 = edge2.PrevInAEL;
                    }
                    edge.WindCnt = (inside ? 0 : 1);
                }
                else
                {
                    edge.WindCnt = edge.WindDelta;
                }
                edge.WindCnt2 = previousEdge.WindCnt2;
                previousEdge = previousEdge.NextInAEL; //ie get ready to calc WindCnt2
            }
            else
            {
                //nonZero, Positive or Negative filling ...
                if (previousEdge.WindCnt * previousEdge.WindDelta < 0)
                {
                    //prev edge is 'decreasing' WindCount (WC) toward zero
                    //so we're outside the previous polygon ...
                    if (Math.Abs(previousEdge.WindCnt) > 1)
                    {
                        //outside prev poly but still inside another.
                        //when reversing direction of prev poly use the same WC 
                        if (previousEdge.WindDelta * edge.WindDelta < 0) edge.WindCnt = previousEdge.WindCnt;
                        //otherwise continue to 'decrease' WC ...
                        else edge.WindCnt = previousEdge.WindCnt + edge.WindDelta;
                    }
                    else
                        //now outside all polys of same polytype so set own WC ...
                        edge.WindCnt = (edge.WindDelta == 0 ? 1 : edge.WindDelta);
                }
                else
                {
                    //prev edge is 'increasing' WindCount (WC) away from zero
                    //so we're inside the previous polygon ...
                    if (edge.WindDelta == 0)
                        edge.WindCnt = (previousEdge.WindCnt < 0 ? previousEdge.WindCnt - 1 : previousEdge.WindCnt + 1);
                    //if wind direction is reversing prev then use same WC
                    else if (previousEdge.WindDelta * edge.WindDelta < 0)
                        edge.WindCnt = previousEdge.WindCnt;
                    //otherwise add to WC ...
                    else edge.WindCnt = previousEdge.WindCnt + edge.WindDelta;
                }
                edge.WindCnt2 = previousEdge.WindCnt2;
                previousEdge = previousEdge.NextInAEL; //ie get ready to calc WindCnt2
            }

            //update WindCnt2 ...
            if (IsEvenOddAltFillType(edge))
            {
                //EvenOdd filling ...
                while (previousEdge != edge)
                {
                    if (previousEdge.WindDelta != 0)
                        edge.WindCnt2 = (edge.WindCnt2 == 0 ? 1 : 0);
                    previousEdge = previousEdge.NextInAEL;
                }
            }
            else
            {
                //nonZero, Positive or Negative filling ...
                while (previousEdge != edge)
                {
                    edge.WindCnt2 += previousEdge.WindDelta;
                    previousEdge = previousEdge.NextInAEL;
                }
            }
        }
        //------------------------------------------------------------------------------

        private void AddEdgeToSEL(ClipperEdge edge)
        {
            //SEL pointers in PEdge are reused to build a list of horizontal edges.
            //However, we don't need to worry about order with horizontal edge processing.
            if (_mSortedEdges == null)
            {
                _mSortedEdges = edge;
                edge.PrevInSel = null;
                edge.NextInSel = null;
            }
            else
            {
                edge.NextInSel = _mSortedEdges;
                edge.PrevInSel = null;
                _mSortedEdges.PrevInSel = edge;
                _mSortedEdges = edge;
            }
        }
        //------------------------------------------------------------------------------

        private void CopyAELToSEL()
        {
            ClipperEdge e = _mActiveEdges;
            _mSortedEdges = e;
            while (e != null)
            {
                e.PrevInSel = e.PrevInAEL;
                e.NextInSel = e.NextInAEL;
                e = e.NextInAEL;
            }
        }
        //------------------------------------------------------------------------------

        private void SwapPositionsInAEL(ClipperEdge edge1, ClipperEdge edge2)
        {
            //check that one or other edge hasn't already been removed from AEL ...
            if (edge1.NextInAEL == edge1.PrevInAEL ||
              edge2.NextInAEL == edge2.PrevInAEL) return;

            if (edge1.NextInAEL == edge2)
            {
                ClipperEdge next = edge2.NextInAEL;
                if (next != null)
                    next.PrevInAEL = edge1;
                ClipperEdge prev = edge1.PrevInAEL;
                if (prev != null)
                    prev.NextInAEL = edge2;
                edge2.PrevInAEL = prev;
                edge2.NextInAEL = edge1;
                edge1.PrevInAEL = edge2;
                edge1.NextInAEL = next;
            }
            else if (edge2.NextInAEL == edge1)
            {
                ClipperEdge next = edge1.NextInAEL;
                if (next != null)
                    next.PrevInAEL = edge2;
                ClipperEdge prev = edge2.PrevInAEL;
                if (prev != null)
                    prev.NextInAEL = edge1;
                edge1.PrevInAEL = prev;
                edge1.NextInAEL = edge2;
                edge2.PrevInAEL = edge1;
                edge2.NextInAEL = next;
            }
            else
            {
                ClipperEdge next = edge1.NextInAEL;
                ClipperEdge prev = edge1.PrevInAEL;
                edge1.NextInAEL = edge2.NextInAEL;
                if (edge1.NextInAEL != null)
                    edge1.NextInAEL.PrevInAEL = edge1;
                edge1.PrevInAEL = edge2.PrevInAEL;
                if (edge1.PrevInAEL != null)
                    edge1.PrevInAEL.NextInAEL = edge1;
                edge2.NextInAEL = next;
                if (edge2.NextInAEL != null)
                    edge2.NextInAEL.PrevInAEL = edge2;
                edge2.PrevInAEL = prev;
                if (edge2.PrevInAEL != null)
                    edge2.PrevInAEL.NextInAEL = edge2;
            }

            if (edge1.PrevInAEL == null)
                _mActiveEdges = edge1;
            else if (edge2.PrevInAEL == null)
                _mActiveEdges = edge2;
        }
        //------------------------------------------------------------------------------

        private void SwapPositionsInSEL(ClipperEdge edge1, ClipperEdge edge2)
        {
            if (edge1.NextInSel == null && edge1.PrevInSel == null)
                return;
            if (edge2.NextInSel == null && edge2.PrevInSel == null)
                return;

            if (edge1.NextInSel == edge2)
            {
                ClipperEdge next = edge2.NextInSel;
                if (next != null)
                    next.PrevInSel = edge1;
                ClipperEdge prev = edge1.PrevInSel;
                if (prev != null)
                    prev.NextInSel = edge2;
                edge2.PrevInSel = prev;
                edge2.NextInSel = edge1;
                edge1.PrevInSel = edge2;
                edge1.NextInSel = next;
            }
            else if (edge2.NextInSel == edge1)
            {
                ClipperEdge next = edge1.NextInSel;
                if (next != null)
                    next.PrevInSel = edge2;
                ClipperEdge prev = edge2.PrevInSel;
                if (prev != null)
                    prev.NextInSel = edge1;
                edge1.PrevInSel = prev;
                edge1.NextInSel = edge2;
                edge2.PrevInSel = edge1;
                edge2.NextInSel = next;
            }
            else
            {
                ClipperEdge next = edge1.NextInSel;
                ClipperEdge prev = edge1.PrevInSel;
                edge1.NextInSel = edge2.NextInSel;
                if (edge1.NextInSel != null)
                    edge1.NextInSel.PrevInSel = edge1;
                edge1.PrevInSel = edge2.PrevInSel;
                if (edge1.PrevInSel != null)
                    edge1.PrevInSel.NextInSel = edge1;
                edge2.NextInSel = next;
                if (edge2.NextInSel != null)
                    edge2.NextInSel.PrevInSel = edge2;
                edge2.PrevInSel = prev;
                if (edge2.PrevInSel != null)
                    edge2.PrevInSel.NextInSel = edge2;
            }

            if (edge1.PrevInSel == null)
                _mSortedEdges = edge1;
            else if (edge2.PrevInSel == null)
                _mSortedEdges = edge2;
        }
        //------------------------------------------------------------------------------


        private void AddLocalMaxPoly(ClipperEdge e1, ClipperEdge e2, Point pt)
        {
            AddOutPt(e1, pt);
            if (e2.WindDelta == 0) AddOutPt(e2, pt);
            if (e1.OutIdx == e2.OutIdx)
            {
                e1.OutIdx = Unassigned;
                e2.OutIdx = Unassigned;
            }
            else if (e1.OutIdx < e2.OutIdx)
                AppendPolygon(e1, e2);
            else
                AppendPolygon(e2, e1);
        }
        //------------------------------------------------------------------------------

        private OutPt AddLocalMinPoly(ClipperEdge e1, ClipperEdge e2, Point pt)
        {
            OutPt result;
            ClipperEdge e, prevE;
            if (IsHorizontal(e2) || (e1.Dx > e2.Dx))
            {
                result = AddOutPt(e1, pt);
                e2.OutIdx = e1.OutIdx;
                e1.Side = EdgeSide.Left;
                e2.Side = EdgeSide.Right;
                e = e1;
                prevE = e.PrevInAEL == e2 ? e2.PrevInAEL : e.PrevInAEL;
            }
            else
            {
                result = AddOutPt(e2, pt);
                e1.OutIdx = e2.OutIdx;
                e1.Side = EdgeSide.Right;
                e2.Side = EdgeSide.Left;
                e = e2;
                prevE = e.PrevInAEL == e1 ? e1.PrevInAEL : e.PrevInAEL;
            }

            if (prevE != null && prevE.OutIdx >= 0 &&
                TopX(prevE, pt.Y).IsPracticallySame(TopX(e, pt.Y)) &&
                SlopesEqual(e, prevE) &&
                (e.WindDelta != 0) && (prevE.WindDelta != 0))
            {
                var outPt = AddOutPt(prevE, pt);
                AddJoin(result, outPt, e.Top);
            }
            return result;
        }
        //------------------------------------------------------------------------------

        private OutRec CreateOutRec()
        {
            var result = new OutRec
            {
                Idx = Unassigned,
                IsHole = false,
                IsOpen = false,
                FirstLeft = null,
                Pts = null,
                BottomPt = null,
                PolyNode = null
            };
            _mPolyOuts.Add(result);
            result.Idx = _mPolyOuts.Count - 1;
            return result;
        }
        //------------------------------------------------------------------------------

        private OutPt AddOutPt(ClipperEdge e, Point pt)
        {
            var toFront = (e.Side == EdgeSide.Left);
            if (e.OutIdx < 0)
            {
                var outRec = CreateOutRec();
                outRec.IsOpen = (e.WindDelta == 0);
                var newOp = new OutPt();
                outRec.Pts = newOp;
                newOp.Idx = outRec.Idx;
                newOp.Pt = pt;
                newOp.Next = newOp;
                newOp.Prev = newOp;
                if (!outRec.IsOpen)
                    SetHoleState(e, outRec);
                e.OutIdx = outRec.Idx; //nb: do this after SetZ !
                return newOp;
            }
            else
            {
                var outRec = _mPolyOuts[e.OutIdx];
                //OutRec.Pts is the 'Left-most' point & OutRec.Pts.Prev is the 'Right-most'
                var op = outRec.Pts;
                if (toFront && pt == op.Pt) return op;
                if (!toFront && pt == op.Prev.Pt) return op.Prev;

                var newOp = new OutPt
                {
                    Idx = outRec.Idx,
                    Pt = pt,
                    Next = op,
                    Prev = op.Prev
                };
                newOp.Prev.Next = newOp;
                op.Prev = newOp;
                if (toFront) outRec.Pts = newOp;
                return newOp;
            }
        }
        //------------------------------------------------------------------------------

        internal void SwapPoints(ref Point pt1, ref Point pt2)
        {
            var tmp = new Point(pt1);
            pt1 = pt2;
            pt2 = tmp;
        }
        //------------------------------------------------------------------------------

        private bool HorzSegmentsOverlap(double seg1A, double seg1B, double seg2A, double seg2B)
        {
            if (seg1A > seg1B)
            {
                var temp = seg1A;
                seg1A = seg1B;
                seg1B = temp;
            }
            if (seg2A > seg2B)
            {
                var temp = seg2A;
                seg2A = seg2B;
                seg2B = temp;
            }
            return (seg1A < seg2B) && (seg2A < seg1B);
        }
        //------------------------------------------------------------------------------

        private void SetHoleState(ClipperEdge e, OutRec outRec)
        {
            bool isHole = false;
            ClipperEdge e2 = e.PrevInAEL;
            while (e2 != null)
            {
                if (e2.OutIdx >= 0 && e2.WindDelta != 0)
                {
                    isHole = !isHole;
                    if (outRec.FirstLeft == null)
                        outRec.FirstLeft = _mPolyOuts[e2.OutIdx];
                }
                e2 = e2.PrevInAEL;
            }
            if (isHole)
                outRec.IsHole = true;
        }
        //------------------------------------------------------------------------------

        private double GetDx(Point pt1, Point pt2)
        {
            if (pt1.Y.IsPracticallySame(pt2.Y)) return Horizontal;
            else return (pt2.X - pt1.X) / (pt2.Y - pt1.Y);
        }
        //---------------------------------------------------------------------------

        private bool FirstIsBottomPt(OutPt btmPt1, OutPt btmPt2)
        {
            OutPt p = btmPt1.Prev;
            while ((p.Pt == btmPt1.Pt) && (p != btmPt1)) p = p.Prev;
            double dx1P = Math.Abs(GetDx(btmPt1.Pt, p.Pt));
            p = btmPt1.Next;
            while ((p.Pt == btmPt1.Pt) && (p != btmPt1)) p = p.Next;
            double dx1N = Math.Abs(GetDx(btmPt1.Pt, p.Pt));

            p = btmPt2.Prev;
            while ((p.Pt == btmPt2.Pt) && (p != btmPt2)) p = p.Prev;
            double dx2P = Math.Abs(GetDx(btmPt2.Pt, p.Pt));
            p = btmPt2.Next;
            while ((p.Pt == btmPt2.Pt) && (p != btmPt2)) p = p.Next;
            double dx2N = Math.Abs(GetDx(btmPt2.Pt, p.Pt));
            return (dx1P >= dx2P && dx1P >= dx2N) || (dx1N >= dx2P && dx1N >= dx2N);
        }
        //------------------------------------------------------------------------------

        private OutPt GetBottomPt(OutPt pp)
        {

            OutPt dups = null;
            OutPt p = pp.Next;
            while (p != pp)
            {
                if (p.Pt.Y.IsPracticallySame(pp.Pt.Y))
                {
                    if (p.Pt.X < pp.Pt.X)
                    {
                        dups = null;
                        pp = p;
                    }
                    else if(p.Pt.X.IsPracticallySame(pp.Pt.X))
                    {
                        if (p.Next != pp && p.Prev != pp) dups = p;
                    }
                }
                else if (p.Pt.Y > pp.Pt.Y)
                {
                    pp = p;
                    dups = null;
                }
                p = p.Next;
            }
            if (dups != null)
            {
                //there appears to be at least 2 vertices at bottomPt so ...
                while (dups != p)
                {
                    if (!FirstIsBottomPt(p, dups)) pp = dups;
                    dups = dups.Next;
                    while (dups.Pt != pp.Pt) dups = dups.Next;
                }
            }
            return pp;
        }
        //------------------------------------------------------------------------------

        private OutRec GetLowermostRec(OutRec outRec1, OutRec outRec2)
        {
            //work out which polygon fragment has the correct hole state ...
            if (outRec1.BottomPt == null)
                outRec1.BottomPt = GetBottomPt(outRec1.Pts);
            if (outRec2.BottomPt == null)
                outRec2.BottomPt = GetBottomPt(outRec2.Pts);
            OutPt bPt1 = outRec1.BottomPt;
            OutPt bPt2 = outRec2.BottomPt;
            if (bPt1.Pt.Y > bPt2.Pt.Y) return outRec1;
            else if (bPt1.Pt.Y < bPt2.Pt.Y) return outRec2;
            else if (bPt1.Pt.X < bPt2.Pt.X) return outRec1;
            else if (bPt1.Pt.X > bPt2.Pt.X) return outRec2;
            else if (bPt1.Next == bPt1) return outRec2;
            else if (bPt2.Next == bPt2) return outRec1;
            else if (FirstIsBottomPt(bPt1, bPt2)) return outRec1;
            else return outRec2;
        }
        //------------------------------------------------------------------------------

        bool Param1RightOfParam2(OutRec outRec1, OutRec outRec2)
        {
            do
            {
                outRec1 = outRec1.FirstLeft;
                if (outRec1 == outRec2) return true;
            } while (outRec1 != null);
            return false;
        }
        //------------------------------------------------------------------------------

        private OutRec GetOutRec(int idx)
        {
            OutRec outrec = _mPolyOuts[idx];
            while (outrec != _mPolyOuts[outrec.Idx])
                outrec = _mPolyOuts[outrec.Idx];
            return outrec;
        }
        //------------------------------------------------------------------------------

        private void AppendPolygon(ClipperEdge e1, ClipperEdge e2)
        {
            //get the start and ends of both output polygons ...
            OutRec outRec1 = _mPolyOuts[e1.OutIdx];
            OutRec outRec2 = _mPolyOuts[e2.OutIdx];

            OutRec holeStateRec;
            if (Param1RightOfParam2(outRec1, outRec2))
                holeStateRec = outRec2;
            else if (Param1RightOfParam2(outRec2, outRec1))
                holeStateRec = outRec1;
            else
                holeStateRec = GetLowermostRec(outRec1, outRec2);

            OutPt p1Lft = outRec1.Pts;
            OutPt p1Rt = p1Lft.Prev;
            OutPt p2Lft = outRec2.Pts;
            OutPt p2Rt = p2Lft.Prev;

            EdgeSide side;
            //join e2 poly onto e1 poly and delete pointers to e2 ...
            if (e1.Side == EdgeSide.Left)
            {
                if (e2.Side == EdgeSide.Left)
                {
                    //z y x a b c
                    ReversePolyPtLinks(p2Lft);
                    p2Lft.Next = p1Lft;
                    p1Lft.Prev = p2Lft;
                    p1Rt.Next = p2Rt;
                    p2Rt.Prev = p1Rt;
                    outRec1.Pts = p2Rt;
                }
                else
                {
                    //x y z a b c
                    p2Rt.Next = p1Lft;
                    p1Lft.Prev = p2Rt;
                    p2Lft.Prev = p1Rt;
                    p1Rt.Next = p2Lft;
                    outRec1.Pts = p2Lft;
                }
                side = EdgeSide.Left;
            }
            else
            {
                if (e2.Side == EdgeSide.Right)
                {
                    //a b c z y x
                    ReversePolyPtLinks(p2Lft);
                    p1Rt.Next = p2Rt;
                    p2Rt.Prev = p1Rt;
                    p2Lft.Next = p1Lft;
                    p1Lft.Prev = p2Lft;
                }
                else
                {
                    //a b c x y z
                    p1Rt.Next = p2Lft;
                    p2Lft.Prev = p1Rt;
                    p1Lft.Prev = p2Rt;
                    p2Rt.Next = p1Lft;
                }
                side = EdgeSide.Right;
            }

            outRec1.BottomPt = null;
            if (holeStateRec == outRec2)
            {
                if (outRec2.FirstLeft != outRec1)
                    outRec1.FirstLeft = outRec2.FirstLeft;
                outRec1.IsHole = outRec2.IsHole;
            }
            outRec2.Pts = null;
            outRec2.BottomPt = null;

            outRec2.FirstLeft = outRec1;

            int okIdx = e1.OutIdx;
            int obsoleteIdx = e2.OutIdx;

            e1.OutIdx = Unassigned; //nb: safe because we only get here via AddLocalMaxPoly
            e2.OutIdx = Unassigned;

            ClipperEdge e = _mActiveEdges;
            while (e != null)
            {
                if (e.OutIdx == obsoleteIdx)
                {
                    e.OutIdx = okIdx;
                    e.Side = side;
                    break;
                }
                e = e.NextInAEL;
            }
            outRec2.Idx = outRec1.Idx;
        }
        //------------------------------------------------------------------------------

        private static void ReversePolyPtLinks(OutPt pp)
        {
            if (pp == null) return;
            var pp1 = pp;
            do
            {
                var pp2 = pp1.Next;
                pp1.Next = pp1.Prev;
                pp1.Prev = pp2;
                pp1 = pp2;
            } while (pp1 != pp);
        }
        //------------------------------------------------------------------------------

        private static void SwapSides(ClipperEdge edge1, ClipperEdge edge2)
        {
            var side = edge1.Side;
            edge1.Side = edge2.Side;
            edge2.Side = side;
        }
        //------------------------------------------------------------------------------

        private static void SwapPolyIndexes(ClipperEdge edge1, ClipperEdge edge2)
        {
            var outIdx = edge1.OutIdx;
            edge1.OutIdx = edge2.OutIdx;
            edge2.OutIdx = outIdx;
        }
        //------------------------------------------------------------------------------

        private void IntersectEdges(ClipperEdge e1, ClipperEdge e2, Point pt)
        {
            //e1 will be to the left of e2 BELOW the intersection. Therefore e1 is before
            //e2 in AEL except when e1 is being inserted at the intersection point ...

            var e1Contributing = (e1.OutIdx >= 0);
            var e2Contributing = (e2.OutIdx >= 0);

#if use_xyz
          SetZ(ref pt, e1, e2);
#endif

            //if either edge is on an OPEN path ...
            if (e1.WindDelta == 0 || e2.WindDelta == 0)
            {
                //ignore subject-subject open path intersections UNLESS they
                //are both open paths, AND they are both 'contributing maximas' ...
                if (e1.WindDelta == 0 && e2.WindDelta == 0) return;
                //if intersecting a subj line with a subj poly ...
                if (e1.PolyTyp == e2.PolyTyp && e1.WindDelta != e2.WindDelta && _mClipType == ClipType.Union)
                {
                    if (e1.WindDelta == 0)
                    {
                        if (!e2Contributing) return;
                        AddOutPt(e1, pt);
                        if (e1Contributing) e1.OutIdx = Unassigned;
                    }
                    else
                    {
                        if (!e1Contributing) return;
                        AddOutPt(e2, pt);
                        if (e2Contributing) e2.OutIdx = Unassigned;
                    }
                }
                else if (e1.PolyTyp != e2.PolyTyp)
                {
                    if ((e1.WindDelta == 0) && Math.Abs(e2.WindCnt) == 1 &&
                      (_mClipType != ClipType.Union || e2.WindCnt2 == 0))
                    {
                        AddOutPt(e1, pt);
                        if (e1Contributing) e1.OutIdx = Unassigned;
                    }
                    else if ((e2.WindDelta == 0) && (Math.Abs(e1.WindCnt) == 1) &&
                      (_mClipType != ClipType.Union || e1.WindCnt2 == 0))
                    {
                        AddOutPt(e2, pt);
                        if (e2Contributing) e2.OutIdx = Unassigned;
                    }
                }
                return;
            }

            //update winding counts...
            //assumes that e1 will be to the Right of e2 ABOVE the intersection
            if (e1.PolyTyp == e2.PolyTyp)
            {
                if (IsEvenOddFillType(e1))
                {
                    var oldE1WindCnt = e1.WindCnt;
                    e1.WindCnt = e2.WindCnt;
                    e2.WindCnt = oldE1WindCnt;
                }
                else
                {
                    if (e1.WindCnt + e2.WindDelta == 0) e1.WindCnt = -e1.WindCnt;
                    else e1.WindCnt += e2.WindDelta;
                    if (e2.WindCnt - e1.WindDelta == 0) e2.WindCnt = -e2.WindCnt;
                    else e2.WindCnt -= e1.WindDelta;
                }
            }
            else
            {
                if (!IsEvenOddFillType(e2)) e1.WindCnt2 += e2.WindDelta;
                else e1.WindCnt2 = (e1.WindCnt2 == 0) ? 1 : 0;
                if (!IsEvenOddFillType(e1)) e2.WindCnt2 -= e1.WindDelta;
                else e2.WindCnt2 = (e2.WindCnt2 == 0) ? 1 : 0;
            }

            PolyFillType e1FillType, e2FillType, e1FillType2, e2FillType2;
            if (e1.PolyTyp == PolyType.Subject)
            {
                e1FillType = _mSubjFillType;
                e1FillType2 = _mClipFillType;
            }
            else
            {
                e1FillType = _mClipFillType;
                e1FillType2 = _mSubjFillType;
            }
            if (e2.PolyTyp == PolyType.Subject)
            {
                e2FillType = _mSubjFillType;
                e2FillType2 = _mClipFillType;
            }
            else
            {
                e2FillType = _mClipFillType;
                e2FillType2 = _mSubjFillType;
            }

            int e1Wc, e2Wc;
            switch (e1FillType)
            {
                case PolyFillType.Positive: e1Wc = e1.WindCnt; break;
                case PolyFillType.Negative: e1Wc = -e1.WindCnt; break;
                default: e1Wc = Math.Abs(e1.WindCnt); break;
            }
            switch (e2FillType)
            {
                case PolyFillType.Positive: e2Wc = e2.WindCnt; break;
                case PolyFillType.Negative: e2Wc = -e2.WindCnt; break;
                default: e2Wc = Math.Abs(e2.WindCnt); break;
            }

            if (e1Contributing && e2Contributing)
            {
                if ((e1Wc != 0 && e1Wc != 1) || (e2Wc != 0 && e2Wc != 1) ||
                  (e1.PolyTyp != e2.PolyTyp && _mClipType != ClipType.Xor))
                {
                    AddLocalMaxPoly(e1, e2, pt);
                }
                else
                {
                    AddOutPt(e1, pt);
                    AddOutPt(e2, pt);
                    SwapSides(e1, e2);
                    SwapPolyIndexes(e1, e2);
                }
            }
            else if (e1Contributing)
            {
                if (e2Wc != 0 && e2Wc != 1) return;
                AddOutPt(e1, pt);
                SwapSides(e1, e2);
                SwapPolyIndexes(e1, e2);
            }
            else if (e2Contributing)
            {
                if (e1Wc != 0 && e1Wc != 1) return;
                AddOutPt(e2, pt);
                SwapSides(e1, e2);
                SwapPolyIndexes(e1, e2);
            }
            else if ((e1Wc == 0 || e1Wc == 1) && (e2Wc == 0 || e2Wc == 1))
            {
                //neither edge is currently contributing ...
                long e1Wc2, e2Wc2;
                switch (e1FillType2)
                {
                    case PolyFillType.Positive: e1Wc2 = e1.WindCnt2; break;
                    case PolyFillType.Negative: e1Wc2 = -e1.WindCnt2; break;
                    default: e1Wc2 = Math.Abs(e1.WindCnt2); break;
                }
                switch (e2FillType2)
                {
                    case PolyFillType.Positive: e2Wc2 = e2.WindCnt2; break;
                    case PolyFillType.Negative: e2Wc2 = -e2.WindCnt2; break;
                    default: e2Wc2 = Math.Abs(e2.WindCnt2); break;
                }

                if (e1.PolyTyp != e2.PolyTyp)
                {
                    AddLocalMinPoly(e1, e2, pt);
                }
                else if (e1Wc == 1 && e2Wc == 1)
                    switch (_mClipType)
                    {
                        case ClipType.Intersection:
                            if (e1Wc2 > 0 && e2Wc2 > 0)
                                AddLocalMinPoly(e1, e2, pt);
                            break;
                        case ClipType.Union:
                            if (e1Wc2 <= 0 && e2Wc2 <= 0)
                                AddLocalMinPoly(e1, e2, pt);
                            break;
                        case ClipType.Difference:
                            if (((e1.PolyTyp == PolyType.Clip) && (e1Wc2 > 0) && (e2Wc2 > 0)) ||
                                ((e1.PolyTyp == PolyType.Subject) && (e1Wc2 <= 0) && (e2Wc2 <= 0)))
                                AddLocalMinPoly(e1, e2, pt);
                            break;
                        case ClipType.Xor:
                            AddLocalMinPoly(e1, e2, pt);
                            break;
                    }
                else
                    SwapSides(e1, e2);
            }
        }
        //------------------------------------------------------------------------------

        private void DeleteFromAEL(ClipperEdge e)
        {
            var aelPrev = e.PrevInAEL;
            var aelNext = e.NextInAEL;
            if (aelPrev == null && aelNext == null && (e != _mActiveEdges))
                return; //already deleted
            if (aelPrev != null)
                aelPrev.NextInAEL = aelNext;
            else _mActiveEdges = aelNext;
            if (aelNext != null)
                aelNext.PrevInAEL = aelPrev;
            e.NextInAEL = null;
            e.PrevInAEL = null;
        }
        //------------------------------------------------------------------------------

        private void DeleteFromSEL(ClipperEdge e)
        {
            var selPrev = e.PrevInSel;
            var selNext = e.NextInSel;
            if (selPrev == null && selNext == null && (e != _mSortedEdges))
                return; //already deleted
            if (selPrev != null)
                selPrev.NextInSel = selNext;
            else _mSortedEdges = selNext;
            if (selNext != null)
                selNext.PrevInSel = selPrev;
            e.NextInSel = null;
            e.PrevInSel = null;
        }
        //------------------------------------------------------------------------------

        private void UpdateEdgeIntoAEL(ref ClipperEdge e)
        {
            if (e.NextInLml == null)
                throw new ClipperException("UpdateEdgeIntoAEL: invalid call");
            var aelPrev = e.PrevInAEL;
            var aelNext = e.NextInAEL;
            e.NextInLml.OutIdx = e.OutIdx;
            if (aelPrev != null)
                aelPrev.NextInAEL = e.NextInLml;
            else _mActiveEdges = e.NextInLml;
            if (aelNext != null)
                aelNext.PrevInAEL = e.NextInLml;
            e.NextInLml.Side = e.Side;
            e.NextInLml.WindDelta = e.WindDelta;
            e.NextInLml.WindCnt = e.WindCnt;
            e.NextInLml.WindCnt2 = e.WindCnt2;
            e = e.NextInLml;
            e.Curr = e.Bot;
            e.PrevInAEL = aelPrev;
            e.NextInAEL = aelNext;
            if (!IsHorizontal(e)) InsertScanbeam(e.Top.Y);
        }
        //------------------------------------------------------------------------------

        private void ProcessHorizontals(bool isTopOfScanbeam)
        {
            var horzEdge = _mSortedEdges;
            while (horzEdge != null)
            {
                DeleteFromSEL(horzEdge);
                ProcessHorizontal(horzEdge, isTopOfScanbeam);
                horzEdge = _mSortedEdges;
            }
        }
        //------------------------------------------------------------------------------

        void GetHorzDirection(ClipperEdge horzEdge, out Direction dir, out double left, out double right)
        {
            if (horzEdge.Bot.X < horzEdge.Top.X)
            {
                left = horzEdge.Bot.X;
                right = horzEdge.Top.X;
                dir = Direction.LeftToRight;
            }
            else
            {
                left = horzEdge.Top.X;
                right = horzEdge.Bot.X;
                dir = Direction.RightToLeft;
            }
        }
        //------------------------------------------------------------------------

        private void ProcessHorizontal(ClipperEdge horzEdge, bool isTopOfScanbeam)
        {
            Direction dir;
            double horzLeft, horzRight;

            GetHorzDirection(horzEdge, out dir, out horzLeft, out horzRight);

            ClipperEdge eLastHorz = horzEdge, eMaxPair = null;
            while (eLastHorz.NextInLml != null && IsHorizontal(eLastHorz.NextInLml))
                eLastHorz = eLastHorz.NextInLml;
            if (eLastHorz.NextInLml == null)
                eMaxPair = GetMaximaPair(eLastHorz);

            for (;;)
            {
                var isLastHorz = (horzEdge == eLastHorz);
                var e = GetNextInAEL(horzEdge, dir);
                while (e != null)
                {
                    //Break if we've got to the end of an intermediate horizontal edge ...
                    //nb: Smaller Dx's are to the right of larger Dx's ABOVE the horizontal.
                    if (e.Curr.X.IsPracticallySame(horzEdge.Top.X) && horzEdge.NextInLml != null &&
                      e.Dx < horzEdge.NextInLml.Dx) break;

                    var eNext = GetNextInAEL(e, dir); //saves eNext for later

                    if ((dir == Direction.LeftToRight && !e.Curr.X.IsGreaterThanNonNegligible(horzRight)) ||
                      (dir == Direction.RightToLeft && !e.Curr.X.IsLessThanNonNegligible(horzLeft)))
                    {
                        //so far we're still in range of the horizontal Edge  but make sure
                        //we're at the last of consec. horizontals when matching with eMaxPair
                        if (e == eMaxPair && isLastHorz)
                        {
                            if (horzEdge.OutIdx >= 0)
                            {
                                var op1 = AddOutPt(horzEdge, horzEdge.Top);
                                var eNextHorz = _mSortedEdges;
                                while (eNextHorz != null)
                                {
                                    if (eNextHorz.OutIdx >= 0 &&
                                      HorzSegmentsOverlap(horzEdge.Bot.X,
                                      horzEdge.Top.X, eNextHorz.Bot.X, eNextHorz.Top.X))
                                    {
                                        OutPt op2 = AddOutPt(eNextHorz, eNextHorz.Bot);
                                        AddJoin(op2, op1, eNextHorz.Top);
                                    }
                                    eNextHorz = eNextHorz.NextInSel;
                                }
                                AddGhostJoin(op1, horzEdge.Bot);
                                AddLocalMaxPoly(horzEdge, eMaxPair, horzEdge.Top);
                            }
                            DeleteFromAEL(horzEdge);
                            DeleteFromAEL(eMaxPair);
                            return;
                        }
                        if (dir == Direction.LeftToRight)
                        {
                            var pt = new Point(e.Curr.X, horzEdge.Curr.Y);
                            IntersectEdges(horzEdge, e, pt);
                        }
                        else
                        {
                            var pt = new Point(e.Curr.X, horzEdge.Curr.Y);
                            IntersectEdges(e, horzEdge, pt);
                        }
                        SwapPositionsInAEL(horzEdge, e);
                    }
                    else if ((dir == Direction.LeftToRight && !e.Curr.X.IsLessThanNonNegligible(horzRight)) ||
                      (dir == Direction.RightToLeft && !e.Curr.X.IsGreaterThanNonNegligible(horzLeft))) break;
                    e = eNext;
                } //end while

                if (horzEdge.NextInLml != null && IsHorizontal(horzEdge.NextInLml))
                {
                    UpdateEdgeIntoAEL(ref horzEdge);
                    if (horzEdge.OutIdx >= 0) AddOutPt(horzEdge, horzEdge.Bot);
                    GetHorzDirection(horzEdge, out dir, out horzLeft, out horzRight);
                }
                else
                    break;
            } //end for (;;)

            if (horzEdge.NextInLml != null)
            {
                if (horzEdge.OutIdx >= 0)
                {
                    var op1 = AddOutPt(horzEdge, horzEdge.Top);
                    if (isTopOfScanbeam) AddGhostJoin(op1, horzEdge.Bot);

                    UpdateEdgeIntoAEL(ref horzEdge);
                    if (horzEdge.WindDelta == 0) return;
                    //nb: HorzEdge is no longer horizontal here
                    var ePrev = horzEdge.PrevInAEL;
                    var eNext = horzEdge.NextInAEL;
                    if (ePrev != null && ePrev.Curr.X.IsPracticallySame(horzEdge.Bot.X) &&
                      ePrev.Curr.Y.IsPracticallySame(horzEdge.Bot.Y) && ePrev.WindDelta != 0 &&
                      (ePrev.OutIdx >= 0 && ePrev.Curr.Y > ePrev.Top.Y &&
                      SlopesEqual(horzEdge, ePrev)))
                    {
                        var op2 = AddOutPt(ePrev, horzEdge.Bot);
                        AddJoin(op1, op2, horzEdge.Top);
                    }
                    else if (eNext != null && eNext.Curr.X.IsPracticallySame(horzEdge.Bot.X) &&
                      eNext.Curr.Y.IsPracticallySame(horzEdge.Bot.Y) && eNext.WindDelta != 0 &&
                      eNext.OutIdx >= 0 && eNext.Curr.Y > eNext.Top.Y &&
                      SlopesEqual(horzEdge, eNext))
                    {
                        var op2 = AddOutPt(eNext, horzEdge.Bot);
                        AddJoin(op1, op2, horzEdge.Top);
                    }
                }
                else
                    UpdateEdgeIntoAEL(ref horzEdge);
            }
            else
            {
                if (horzEdge.OutIdx >= 0) AddOutPt(horzEdge, horzEdge.Top);
                DeleteFromAEL(horzEdge);
            }
        }
        //------------------------------------------------------------------------------

        private static ClipperEdge GetNextInAEL(ClipperEdge e, Direction direction)
        {
            return direction == Direction.LeftToRight ? e.NextInAEL : e.PrevInAEL;
        }
        //------------------------------------------------------------------------------

        private static bool IsMaxima(ClipperEdge e, double y)
        {
            return (e != null && (e.Top.Y).IsPracticallySame(y) && e.NextInLml == null);
        }
        //------------------------------------------------------------------------------

        private static bool IsIntermediate(ClipperEdge e, double y)
        {
            return ((e.Top.Y).IsPracticallySame(y) && e.NextInLml != null);
        }
        //------------------------------------------------------------------------------

        private static ClipperEdge GetMaximaPair(ClipperEdge e)
        {
            ClipperEdge result = null;
            if ((e.Next.Top == e.Top) && e.Next.NextInLml == null)
                result = e.Next;
            else if ((e.Prev.Top == e.Top) && e.Prev.NextInLml == null)
                result = e.Prev;
            if (result != null && (result.OutIdx == Skip ||
              (result.NextInAEL == result.PrevInAEL && !IsHorizontal(result))))
                return null;
            return result;
        }
        //------------------------------------------------------------------------------

        private bool ProcessIntersections(double topY)
        {
            if (_mActiveEdges == null) return true;
            try
            {
                BuildIntersectList(topY);
                if (_mIntersectList.Count == 0) return true;
                if (_mIntersectList.Count == 1 || FixupIntersectionOrder())
                    ProcessIntersectList();
                else
                    return false;
            }
            catch
            {
                _mSortedEdges = null;
                _mIntersectList.Clear();
                throw new ClipperException("ProcessIntersections error");
            }
            _mSortedEdges = null;
            return true;
        }
        //------------------------------------------------------------------------------

        private void BuildIntersectList(double topY)
        {
            if (_mActiveEdges == null) return;

            //prepare for sorting ...
            var e = _mActiveEdges;
            _mSortedEdges = e;
           //ToDo: it is possible for e to loop back to itself with Next in AEL. This must be fixed.
            var now = DateTime.Now;
            while (e != null)
            {
                e.PrevInSel = e.PrevInAEL;
                e.NextInSel = e.NextInAEL;
                e.Curr = new Point(TopX(e, topY), e.Curr.Y);
                e = e.NextInAEL;
                var duration = DateTime.Now - now;
                if(duration > TimeSpan.FromSeconds(10)) throw new Exception("Infinite Loop Detected");
            }

            //bubblesort ...
            var isModified = true;
            while (isModified && _mSortedEdges != null)
            {
                isModified = false;
                e = _mSortedEdges;
                while (e.NextInSel != null)
                {
                    var eNext = e.NextInSel;
                    if (e.Curr.X > eNext.Curr.X)
                    {
                        Point pt;
                        IntersectPoint(e, eNext, out pt);
                        var newNode = new IntersectNode
                        {
                            Edge1 = e,
                            Edge2 = eNext,
                            Pt = pt
                        };
                        _mIntersectList.Add(newNode);

                        SwapPositionsInSEL(e, eNext);
                        isModified = true;
                    }
                    else
                        e = eNext;
                }
                if (e.PrevInSel != null) e.PrevInSel.NextInSel = null;
                else break;
            }
            _mSortedEdges = null;
        }
        //------------------------------------------------------------------------------

        private bool EdgesAdjacent(IntersectNode inode)
        {
            return (inode.Edge1.NextInSel == inode.Edge2) ||
              (inode.Edge1.PrevInSel == inode.Edge2);
        }
        //------------------------------------------------------------------------------

        private bool FixupIntersectionOrder()
        {
            //pre-condition: intersections are sorted bottom-most first.
            //Now it's crucial that intersections are made only between adjacent edges,
            //so to ensure this the order of intersections may need adjusting ...
            _mIntersectList.Sort(_mIntersectNodeComparer);

            CopyAELToSEL();
            var cnt = _mIntersectList.Count;
            for (var i = 0; i < cnt; i++)
            {
                if (!EdgesAdjacent(_mIntersectList[i]))
                {
                    var j = i + 1;
                    while (j < cnt && !EdgesAdjacent(_mIntersectList[j])) j++;
                    if (j == cnt) return false;

                    var tmp = _mIntersectList[i];
                    _mIntersectList[i] = _mIntersectList[j];
                    _mIntersectList[j] = tmp;

                }
                SwapPositionsInSEL(_mIntersectList[i].Edge1, _mIntersectList[i].Edge2);
            }
            return true;
        }
        //------------------------------------------------------------------------------

        private void ProcessIntersectList()
        {
            foreach (var iNode in _mIntersectList)
            {
                {
                    IntersectEdges(iNode.Edge1, iNode.Edge2, iNode.Pt);
                    SwapPositionsInAEL(iNode.Edge1, iNode.Edge2);
                }
            }
            _mIntersectList.Clear();
        }
        //------------------------------------------------------------------------------

        private static double TopX(ClipperEdge edge, double currentY)
        {
            if (currentY.IsPracticallySame(edge.Top.Y))
                return edge.Top.X;
            return edge.Bot.X + (edge.Dx * (currentY - edge.Bot.Y));
        }
        //------------------------------------------------------------------------------

        private static void IntersectPoint(ClipperEdge edge1, ClipperEdge edge2, out Point ip)
        {
            double b1, b2;
            double ipX;
            double ipY;
            //nb: with very large coordinate values, it's possible for SlopesEqual() to 
            //return false but for the edge.Dx value be equal due to double precision rounding.
            if (edge1.Dx.IsPracticallySame(edge2.Dx))
            {
                ipY = edge1.Curr.Y;
                ipX = TopX(edge1, ipY);
                ip = new Point(ipX, ipY);
                return;
            }

            if (edge1.Delta.X.IsNegligible())
            {

                ipX = edge1.Bot.X;
                if (IsHorizontal(edge2))
                {
                    ipY = edge2.Bot.Y;
                }
                else if (edge2.Dx.IsNegligible()) //Vertical line
                {
                    ipY = edge1.Bot.Y;
                }
                else
                {
                    b2 = edge2.Bot.Y - (edge2.Bot.X / edge2.Dx);
                    ipY = (ipX / edge2.Dx + b2);
                }
            }
            else if (edge2.Delta.X.IsNegligible())
            {
                ipX = edge2.Bot.X;
                if (IsHorizontal(edge1))
                {
                    ipY = edge1.Bot.Y;
                }
                else if (edge1.Dx.IsNegligible()) //Vertical line
                {
                    ipY = edge2.Bot.Y; 
                }
                else
                {
                    b1 = edge1.Bot.Y - (edge1.Bot.X / edge1.Dx);
                    ipY = (ipX / edge1.Dx + b1);
                }
            }
            else
            {
                b1 = edge1.Bot.X - edge1.Bot.Y * edge1.Dx;
                b2 = edge2.Bot.X - edge2.Bot.Y * edge2.Dx;
                if((edge1.Dx - edge2.Dx).IsNegligible()) throw new NotImplementedException();
                var q = (b2 - b1) / (edge1.Dx - edge2.Dx);
                ipY = q;
                ipX = Math.Abs(edge1.Dx) < Math.Abs(edge2.Dx) ? (edge1.Dx * q + b1) : (edge2.Dx * q + b2);
            }

            if (ipY < edge1.Top.Y || ipY < edge2.Top.Y)
            {
                ipY = edge1.Top.Y > edge2.Top.Y ? edge1.Top.Y : edge2.Top.Y;
                ipX = TopX(Math.Abs(edge1.Dx) < Math.Abs(edge2.Dx) ? edge1 : edge2, ipY);
            }
            //finally, don't allow 'ip' to be BELOW curr.Y (ie bottom of scanbeam) ...
            if (ipY > edge1.Curr.Y)
            {
                ipY = edge1.Curr.Y;
                //better to use the more vertical edge to derive X ...
                ipX = TopX(Math.Abs(edge1.Dx) > Math.Abs(edge2.Dx) ? edge2 : edge1, ipY);
            }
            if (double.IsNaN(ipX) || double.IsNaN(ipY)) throw new Exception("Must be a number");
                ip = new Point(ipX, ipY);
        }
        //------------------------------------------------------------------------------

        private void ProcessEdgesAtTopOfScanbeam(double topY)
        {
            var e = _mActiveEdges;
            while (e != null)
            {
                //1. process maxima, treating them as if they're 'bent' horizontal edges,
                //   but exclude maxima with horizontal edges. nb: e can't be a horizontal.
                var isMaximaEdge = IsMaxima(e, topY);

                if (isMaximaEdge)
                {
                    var eMaxPair = GetMaximaPair(e);
                    isMaximaEdge = (eMaxPair == null || !IsHorizontal(eMaxPair));
                }

                if (isMaximaEdge)
                {
                    var ePrev = e.PrevInAEL;
                    DoMaxima(e);
                    e = ePrev == null ? _mActiveEdges : ePrev.NextInAEL;
                }
                else
                {
                    //2. promote horizontal edges, otherwise update Curr.X and Curr.Y ...
                    if (IsIntermediate(e, topY) && IsHorizontal(e.NextInLml))
                    {
                        UpdateEdgeIntoAEL(ref e);
                        if (e.OutIdx >= 0)
                            AddOutPt(e, e.Bot);
                        AddEdgeToSEL(e);
                    }
                    else
                    {
                        e.Curr = new Point(TopX(e, topY), topY);
                    }

                    if (StrictlySimple)
                    {
                        var ePrev = e.PrevInAEL;
                        if ((e.OutIdx >= 0) && (e.WindDelta != 0) && ePrev != null &&
                          (ePrev.OutIdx >= 0) && (ePrev.Curr.X.IsPracticallySame(e.Curr.X)) &&
                          (ePrev.WindDelta != 0))
                        {
                            var ip = new Point(e.Curr);
                            var op = AddOutPt(ePrev, ip);
                            var op2 = AddOutPt(e, ip);
                            AddJoin(op, op2, ip); //StrictlySimple (type-3) join
                        }
                    }

                    e = e.NextInAEL;
                }
            }

            //3. Process horizontals at the Top of the scanbeam ...
            ProcessHorizontals(true);

            //4. Promote intermediate vertices ...
            e = _mActiveEdges;
            while (e != null)
            {
                if (IsIntermediate(e, topY))
                {
                    OutPt op = null;
                    if (e.OutIdx >= 0)
                        op = AddOutPt(e, e.Top);
                    UpdateEdgeIntoAEL(ref e);

                    //if output polygons share an edge, they'll need joining later ...
                    var ePrev = e.PrevInAEL;
                    var eNext = e.NextInAEL;
                    if (ePrev != null && ePrev.Curr.X.IsPracticallySame(e.Bot.X) &&
                      ePrev.Curr.Y.IsPracticallySame(e.Bot.Y) && op != null &&
                      ePrev.OutIdx >= 0 && ePrev.Curr.Y > ePrev.Top.Y &&
                      SlopesEqual(e, ePrev) &&
                      (e.WindDelta != 0) && (ePrev.WindDelta != 0))
                    {
                        var op2 = AddOutPt(ePrev, e.Bot);
                        AddJoin(op, op2, e.Top);
                    }
                    else if (eNext != null && eNext.Curr.X.IsPracticallySame(e.Bot.X) &&
                      eNext.Curr.Y.IsPracticallySame(e.Bot.Y) && op != null &&
                      eNext.OutIdx >= 0 && eNext.Curr.Y > eNext.Top.Y &&
                      SlopesEqual(e, eNext) &&
                      (e.WindDelta != 0) && (eNext.WindDelta != 0))
                    {
                        var op2 = AddOutPt(eNext, e.Bot);
                        AddJoin(op, op2, e.Top);
                    }
                }
                e = e.NextInAEL;
            }
        }
        //------------------------------------------------------------------------------

        private void DoMaxima(ClipperEdge e)
        {
            var eMaxPair = GetMaximaPair(e);
            if (eMaxPair == null)
            {
                if (e.OutIdx >= 0)
                    AddOutPt(e, e.Top);
                DeleteFromAEL(e);
                return;
            }

            var eNext = e.NextInAEL;
            while (eNext != null && eNext != eMaxPair)
            {
                IntersectEdges(e, eNext, e.Top);
                SwapPositionsInAEL(e, eNext);
                eNext = e.NextInAEL;
            }

            if (e.OutIdx == Unassigned && eMaxPair.OutIdx == Unassigned)
            {
                DeleteFromAEL(e);
                DeleteFromAEL(eMaxPair);
            }
            else if (e.OutIdx >= 0 && eMaxPair.OutIdx >= 0)
            {
                if (e.OutIdx >= 0) AddLocalMaxPoly(e, eMaxPair, e.Top);
                DeleteFromAEL(e);
                DeleteFromAEL(eMaxPair);
            }
            else if (e.WindDelta == 0)
            {
                if (e.OutIdx >= 0)
                {
                    AddOutPt(e, e.Top);
                    e.OutIdx = Unassigned;
                }
                DeleteFromAEL(e);

                if (eMaxPair.OutIdx >= 0)
                {
                    AddOutPt(eMaxPair, e.Top);
                    eMaxPair.OutIdx = Unassigned;
                }
                DeleteFromAEL(eMaxPair);
            }
            else throw new ClipperException("DoMaxima error");
        }
        //------------------------------------------------------------------------------

        internal static void ReversePaths(Paths polys)
        {
            foreach (var poly in polys) { poly.Reverse(); }
        }
        //------------------------------------------------------------------------------

        internal static bool Orientation(Path poly)
        {
            return Area(poly) >= 0;
        }
        //------------------------------------------------------------------------------

        private static int PointCount(OutPt pts)
        {
            if (pts == null) return 0;
            var result = 0;
            var p = pts;
            do
            {
                result++;
                p = p.Next;
            }
            while (p != pts);
            return result;
        }
        //------------------------------------------------------------------------------

        private void BuildResult(Paths polyg)
        {
            polyg.Clear();
            polyg.Capacity = _mPolyOuts.Count;
            foreach (var outRec in _mPolyOuts)
            {
                if (outRec.Pts == null) continue;
                var p = outRec.Pts.Prev;
                var cnt = PointCount(p);
                if (cnt < 2) continue;
                var pg = new Path(cnt);
                for (var j = 0; j < cnt; j++)
                {
                    pg.Add(p.Pt);
                    p = p.Prev;
                }
                polyg.Add(pg);
            }
        }
        //------------------------------------------------------------------------------

        private void BuildResult2(PolyTree polytree)
        {
            polytree.Clear();

            //add each output polygon/contour to polytree ...
            polytree.MAllPolys.Capacity = _mPolyOuts.Count;
            foreach (var outRec in _mPolyOuts)
            {
                var cnt = PointCount(outRec.Pts);
                if ((outRec.IsOpen && cnt < 2) ||
                    (!outRec.IsOpen && cnt < 3)) continue;
                FixHoleLinkage(outRec);
                var pn = new PolyNode();
                polytree.MAllPolys.Add(pn);
                outRec.PolyNode = pn;
                pn.MPolygon.Capacity = cnt;
                var op = outRec.Pts.Prev;
                for (var j = 0; j < cnt; j++)
                {
                    pn.MPolygon.Add(op.Pt);
                    op = op.Prev;
                }
            }

            //fixup PolyNode links etc ...
            polytree.MChilds.Capacity = _mPolyOuts.Count;
            foreach (var outRec in _mPolyOuts.Where(outRec => outRec.PolyNode != null))
            {
                if (outRec.IsOpen)
                {
                    outRec.PolyNode.IsOpen = true;
                    polytree.AddChild(outRec.PolyNode);
                }
                else if (outRec.FirstLeft?.PolyNode != null)
                    outRec.FirstLeft.PolyNode.AddChild(outRec.PolyNode);
                else
                    polytree.AddChild(outRec.PolyNode);
            }
        }
        //------------------------------------------------------------------------------

        private void FixupOutPolygon(OutRec outRec)
        {
            //FixupOutPolygon() - removes duplicate points and simplifies consecutive
            //parallel edges by removing the middle vertex.
            OutPt lastOk = null;
            outRec.BottomPt = null;
            var pp = outRec.Pts;
            for (;;)
            {
                if (pp.Prev == pp || pp.Prev == pp.Next)
                {
                    outRec.Pts = null;
                    return;
                }
                //test for duplicate points and collinear edges ...
                if ((pp.Pt == pp.Next.Pt) || (pp.Pt == pp.Prev.Pt) ||
                  (SlopesEqual(pp.Prev.Pt, pp.Pt, pp.Next.Pt) &&
                  (!PreserveCollinear || !Pt2IsBetweenPt1AndPt3(pp.Prev.Pt, pp.Pt, pp.Next.Pt))))
                {
                    lastOk = null;
                    pp.Prev.Next = pp.Next;
                    pp.Next.Prev = pp.Prev;
                    pp = pp.Prev;
                }
                else if (pp == lastOk) break;
                else
                {
                    if (lastOk == null) lastOk = pp;
                    pp = pp.Next;
                }
            }
            outRec.Pts = pp;
        }
        //------------------------------------------------------------------------------

        private static OutPt DupOutPt(OutPt outPt, bool insertAfter)
        {
            var result = new OutPt
            {
                Pt = outPt.Pt,
                Idx = outPt.Idx
            };
            if (insertAfter)
            {
                result.Next = outPt.Next;
                result.Prev = outPt;
                outPt.Next.Prev = result;
                outPt.Next = result;
            }
            else
            {
                result.Prev = outPt.Prev;
                result.Next = outPt;
                outPt.Prev.Next = result;
                outPt.Prev = result;
            }
            return result;
        }
        //------------------------------------------------------------------------------

        private static bool GetOverlap(double a1, double a2, double b1, double b2, out double left, out double right)
        {
            if (a1 < a2)
            {
                if (b1 < b2) { left = Math.Max(a1, b1); right = Math.Min(a2, b2); }
                else { left = Math.Max(a1, b2); right = Math.Min(a2, b1); }
            }
            else
            {
                if (b1 < b2) { left = Math.Max(a2, b1); right = Math.Min(a1, b2); }
                else { left = Math.Max(a2, b2); right = Math.Min(a1, b1); }
            }
            return left < right;
        }
        //------------------------------------------------------------------------------

        private bool JoinHorz(OutPt op1, OutPt op1B, OutPt op2, OutPt op2B,
          Point point, bool discardLeft)
        {
            var dir1 = (op1.Pt.X > op1B.Pt.X ?
              Direction.RightToLeft : Direction.LeftToRight);
            var dir2 = (op2.Pt.X > op2B.Pt.X ?
              Direction.RightToLeft : Direction.LeftToRight);
            if (dir1 == dir2) return false;

            //When DiscardLeft, we want Op1b to be on the Left of outPoint1, otherwise we
            //want Op1b to be on the Right. (And likewise with outPoint2 and Op2b.)
            //So, to facilitate this while inserting Op1b and Op2b ...
            //when DiscardLeft, make sure we're AT or RIGHT of point before adding Op1b,
            //otherwise make sure we're AT or LEFT of point. (Likewise with Op2b.)
            if (dir1 == Direction.LeftToRight)
            {
                while (!op1.Next.Pt.X.IsGreaterThanNonNegligible(point.X) &&
                       !op1.Next.Pt.X.IsLessThanNonNegligible(op1.Pt.X) &&
                       op1.Next.Pt.Y.IsPracticallySame(point.Y))
                {
                    op1 = op1.Next;
                } 
                if (discardLeft && !op1.Pt.X.IsPracticallySame(point.X)) op1 = op1.Next;
                op1B = DupOutPt(op1, !discardLeft);
                if (op1B.Pt != point)
                {
                    op1 = op1B;
                    op1.Pt = point;
                    op1B = DupOutPt(op1, !discardLeft);
                }
            }
            else
            {
                while (!op1.Next.Pt.X.IsLessThanNonNegligible(point.X) &&
                       !op1.Next.Pt.X.IsGreaterThanNonNegligible(op1.Pt.X) &&
                       op1.Next.Pt.Y.IsPracticallySame(point.Y))
                {
                    op1 = op1.Next;
                }  
                if (!discardLeft && !op1.Pt.X.IsPracticallySame(point.X)) op1 = op1.Next;
                op1B = DupOutPt(op1, discardLeft);
                if (op1B.Pt != point)
                {
                    op1 = op1B;
                    op1.Pt = point;
                    op1B = DupOutPt(op1, discardLeft);
                }
            }

            if (dir2 == Direction.LeftToRight)
            {
                while (!op2.Next.Pt.X.IsGreaterThanNonNegligible(point.X) &&
                       !op2.Next.Pt.X.IsLessThanNonNegligible(op2.Pt.X) &&
                       op2.Next.Pt.Y.IsPracticallySame(point.Y))
                {
                    op2 = op2.Next;
                }
                if (discardLeft && !op2.Pt.X.IsPracticallySame(point.X)) op2 = op2.Next;
                op2B = DupOutPt(op2, !discardLeft);
                if (op2B.Pt != point)
                {
                    op2 = op2B;
                    op2.Pt = point;
                    op2B = DupOutPt(op2, !discardLeft);
                }
            }
            else
            {
                while (!op2.Next.Pt.X.IsLessThanNonNegligible(point.X) &&
                       !op2.Next.Pt.X.IsGreaterThanNonNegligible(op2.Pt.X) &&
                       op2.Next.Pt.Y.IsPracticallySame(point.Y))
                {
                    op2 = op2.Next;
                }
                if (!discardLeft && !op2.Pt.X.IsPracticallySame(point.X)) op2 = op2.Next;
                op2B = DupOutPt(op2, discardLeft);
                if (op2B.Pt != point)
                {
                    op2 = op2B;
                    op2.Pt = point;
                    op2B = DupOutPt(op2, discardLeft);
                }
            }

            if ((dir1 == Direction.LeftToRight) == discardLeft)
            {
                op1.Prev = op2;
                op2.Next = op1;
                op1B.Next = op2B;
                op2B.Prev = op1B;
            }
            else
            {
                op1.Next = op2;
                op2.Prev = op1;
                op1B.Prev = op2B;
                op2B.Next = op1B;
            }
            return true;
        }
        //------------------------------------------------------------------------------

        private bool JoinPoints(Join j, OutRec outRec1, OutRec outRec2)
        {
            OutPt op1 = j.OutPt1, op1B;
            OutPt op2 = j.OutPt2, op2B;

            //There are 3 kinds of joins for output polygons ...
            //1. Horizontal joins where Join.OutPt1 & Join.OutPt2 are a vertices anywhere
            //along (horizontal) collinear edges (& Join.OffPt is on the same horizontal).
            //2. Non-horizontal joins where Join.OutPt1 & Join.OutPt2 are at the same
            //location at the Bottom of the overlapping segment (& Join.OffPt is above).
            //3. StrictlySimple joins where edges touch but are not collinear and where
            //Join.OutPt1, Join.OutPt2 & Join.OffPt all share the same point.
            var isHorizontal = (j.OutPt1.Pt.Y.IsPracticallySame(j.OffPt.Y));

            if (isHorizontal && (j.OffPt == j.OutPt1.Pt) && (j.OffPt == j.OutPt2.Pt))
            {
                //Strictly Simple join ...
                if (outRec1 != outRec2) return false;
                op1B = j.OutPt1.Next;
                while (op1B != op1 && (op1B.Pt == j.OffPt))
                    op1B = op1B.Next;
                var reverse1 = (op1B.Pt.Y > j.OffPt.Y);
                op2B = j.OutPt2.Next;
                while (op2B != op2 && (op2B.Pt == j.OffPt))
                    op2B = op2B.Next;
                var reverse2 = (op2B.Pt.Y > j.OffPt.Y);
                if (reverse1 == reverse2) return false;
                if (reverse1)
                {
                    op1B = DupOutPt(op1, false);
                    op2B = DupOutPt(op2, true);
                    op1.Prev = op2;
                    op2.Next = op1;
                    op1B.Next = op2B;
                    op2B.Prev = op1B;
                    j.OutPt1 = op1;
                    j.OutPt2 = op1B;
                    return true;
                }
                //Else
                op1B = DupOutPt(op1, true);
                op2B = DupOutPt(op2, false);
                op1.Next = op2;
                op2.Prev = op1;
                op1B.Prev = op2B;
                op2B.Next = op1B;
                j.OutPt1 = op1;
                j.OutPt2 = op1B;
                return true;
            }
            if (isHorizontal)
            {
                //treat horizontal joins differently to non-horizontal joins since with
                //them we're not yet sure where the overlapping is. OutPt1.point & OutPt2.point
                //may be anywhere along the horizontal edge.
                op1B = op1;
                while ((op1.Prev.Pt.Y).IsPracticallySame(op1.Pt.Y) && op1.Prev != op1B && op1.Prev != op2)
                    op1 = op1.Prev;
                while ((op1B.Next.Pt.Y).IsPracticallySame(op1B.Pt.Y) && op1B.Next != op1 && op1B.Next != op2)
                    op1B = op1B.Next;
                if (op1B.Next == op1 || op1B.Next == op2) return false; //a flat 'polygon'

                op2B = op2;
                while ((op2.Prev.Pt.Y).IsPracticallySame(op2.Pt.Y) && op2.Prev != op2B && op2.Prev != op1B)
                    op2 = op2.Prev;
                while (op2B.Next.Pt.Y.IsPracticallySame(op2B.Pt.Y) && op2B.Next != op2 && op2B.Next != op1)
                    op2B = op2B.Next;
                if (op2B.Next == op2 || op2B.Next == op1) return false; //a flat 'polygon'

                double left, right;
                //outPoint1 -. Op1b & outPoint2 -. Op2b are the extremites of the horizontal edges
                if (!GetOverlap(op1.Pt.X, op1B.Pt.X, op2.Pt.X, op2B.Pt.X, out left, out right))
                    return false;

                //DiscardLeftSide: when overlapping edges are joined, a spike will created
                //which needs to be cleaned up. However, we don't want outPoint1 or outPoint2 caught up
                //on the discard Side as either may still be needed for other joins ...
                Point pt;
                bool discardLeftSide;
                if (!op1.Pt.X.IsLessThanNonNegligible(left) && !op1.Pt.X.IsGreaterThanNonNegligible(right))
                {
                    pt = op1.Pt; discardLeftSide = (op1.Pt.X > op1B.Pt.X);
                }
                else if (!op2.Pt.X.IsLessThanNonNegligible(left) && !op2.Pt.X.IsGreaterThanNonNegligible(right))
                {
                    pt = op2.Pt; discardLeftSide = (op2.Pt.X > op2B.Pt.X);
                }
                else if (!op1B.Pt.X.IsLessThanNonNegligible(left) && !op1B.Pt.X.IsGreaterThanNonNegligible(right))
                {
                    pt = op1B.Pt; discardLeftSide = op1B.Pt.X > op1.Pt.X;
                }
                else
                {
                    pt = op2B.Pt; discardLeftSide = (op2B.Pt.X > op2.Pt.X);
                }
                j.OutPt1 = op1;
                j.OutPt2 = op2;
                return JoinHorz(op1, op1B, op2, op2B, pt, discardLeftSide);
            }
            else
            {
                //nb: For non-horizontal joins ...
                //    1. Jr.OutPt1.point.Y == Jr.OutPt2.point.Y
                //    2. Jr.OutPt1.point > Jr.OffPt.Y

                //make sure the polygons are correctly oriented ...
                op1B = op1.Next;
                while ((op1B.Pt == op1.Pt) && (op1B != op1)) op1B = op1B.Next;
                var reverse1 = ((op1B.Pt.Y > op1.Pt.Y) ||
                  !SlopesEqual(op1.Pt, op1B.Pt, j.OffPt));
                if (reverse1)
                {
                    op1B = op1.Prev;
                    while ((op1B.Pt == op1.Pt) && (op1B != op1)) op1B = op1B.Prev;
                    if ((op1B.Pt.Y > op1.Pt.Y) ||
                      !SlopesEqual(op1.Pt, op1B.Pt, j.OffPt)) return false;
                }
                op2B = op2.Next;
                while ((op2B.Pt == op2.Pt) && (op2B != op2)) op2B = op2B.Next;
                var reverse2 = ((op2B.Pt.Y > op2.Pt.Y) ||
                  !SlopesEqual(op2.Pt, op2B.Pt, j.OffPt));
                if (reverse2)
                {
                    op2B = op2.Prev;
                    while ((op2B.Pt == op2.Pt) && (op2B != op2)) op2B = op2B.Prev;
                    if ((op2B.Pt.Y > op2.Pt.Y) ||
                      !SlopesEqual(op2.Pt, op2B.Pt, j.OffPt)) return false;
                }

                if ((op1B == op1) || (op2B == op2) || (op1B == op2B) ||
                  ((outRec1 == outRec2) && (reverse1 == reverse2))) return false;

                if (reverse1)
                {
                    op1B = DupOutPt(op1, false);
                    op2B = DupOutPt(op2, true);
                    op1.Prev = op2;
                    op2.Next = op1;
                    op1B.Next = op2B;
                    op2B.Prev = op1B;
                    j.OutPt1 = op1;
                    j.OutPt2 = op1B;
                    return true;
                }
                else
                {
                    op1B = DupOutPt(op1, true);
                    op2B = DupOutPt(op2, false);
                    op1.Next = op2;
                    op2.Prev = op1;
                    op1B.Prev = op2B;
                    op2B.Next = op1B;
                    j.OutPt1 = op1;
                    j.OutPt2 = op1B;
                    return true;
                }
            }
        }
        //----------------------------------------------------------------------

        internal static int PointInPolygon(Point pt, Path path)
        {
            //returns 0 if false, +1 if true, -1 if pt ON polygon boundary
            //See "The Point in Polygon Problem for Arbitrary Polygons" by Hormann & Agathos
            //http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.88.5498&rep=rep1&type=pdf
            int result = 0, cnt = path.Count;
            if (cnt < 3) return 0;
            var ip = path[0];
            for (var i = 1; i <= cnt; ++i)
            {
                var ipNext = (i == cnt ? path[0] : path[i]);
                if ((ipNext.Y).IsPracticallySame(pt.Y))
                {
                    if (((ipNext.X).IsPracticallySame(pt.X)) || ((ip.Y).IsPracticallySame(pt.Y) &&
                      (ipNext.X > pt.X == ip.X < pt.X))) return -1;
                }
                if ((ip.Y < pt.Y) != (ipNext.Y < pt.Y))
                {
                    if (ip.X >= pt.X)
                    {
                        if (ipNext.X > pt.X) result = 1 - result;
                        else
                        {
                            var d = (ip.X - pt.X) * (ipNext.Y - pt.Y) -
                              (ipNext.X - pt.X) * (ip.Y - pt.Y);
                            if (d.IsNegligible()) return -1;
                            if ((d > 0) == (ipNext.Y > ip.Y)) result = 1 - result;
                        }
                    }
                    else
                    {
                        if (ipNext.X > pt.X)
                        {
                            var d = (ip.X - pt.X) * (ipNext.Y - pt.Y) -
                              (ipNext.X - pt.X) * (ip.Y - pt.Y);
                            if (d.IsNegligible()) return -1;
                            if ((d > 0) == (ipNext.Y > ip.Y)) result = 1 - result;
                        }
                    }
                }
                ip = ipNext;
            }
            return result;
        }
        //------------------------------------------------------------------------------

        private static int PointInPolygon(Point pt, OutPt op)
        {
            //returns 0 if false, +1 if true, -1 if pt ON polygon boundary
            //See "The Point in Polygon Problem for Arbitrary Polygons" by Hormann & Agathos
            //http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.88.5498&rep=rep1&type=pdf
            var result = 0;
            var startOp = op;
            double ptx = pt.X, pty = pt.Y;
            double poly0X = op.Pt.X, poly0Y = op.Pt.Y;
            do
            {
                op = op.Next;
                double poly1X = op.Pt.X, poly1Y = op.Pt.Y;

                if ((poly1Y).IsPracticallySame(pty))
                {
                    if ((poly1X).IsPracticallySame(ptx) || ((poly0Y).IsPracticallySame(pty) &&
                      (poly1X > ptx == poly0X < ptx))) return -1;
                }
                if ((poly0Y < pty) != (poly1Y < pty))
                {
                    if (poly0X >= ptx)
                    {
                        if (poly1X > ptx) result = 1 - result;
                        else
                        {
                            var d = (poly0X - ptx) * (poly1Y - pty) -
                              (poly1X - ptx) * (poly0Y - pty);
                            if (d.IsNegligible()) return -1;
                            if ((d > 0) == (poly1Y > poly0Y)) result = 1 - result;
                        }
                    }
                    else
                    {
                        if (poly1X > ptx)
                        {
                            var d = (poly0X - ptx) * (poly1Y - pty) -
                              (poly1X - ptx) * (poly0Y - pty);
                            if (d.IsNegligible()) return -1;
                            if ((d > 0) == (poly1Y > poly0Y)) result = 1 - result;
                        }
                    }
                }
                poly0X = poly1X; poly0Y = poly1Y;
            } while (startOp != op);
            return result;
        }
        //------------------------------------------------------------------------------

        private static bool Poly2ContainsPoly1(OutPt outPt1, OutPt outPt2)
        {
            var op = outPt1;
            do
            {
                //nb: PointInPolygon returns 0 if false, +1 if true, -1 if pt on polygon
                var res = PointInPolygon(op.Pt, outPt2);
                if (res >= 0) return res > 0;
                op = op.Next;
            }
            while (op != outPt1);
            return true;
        }
        //----------------------------------------------------------------------

        private void FixupFirstLefts1(OutRec oldOutRec, OutRec newOutRec)
        {
            foreach (var outRec in _mPolyOuts)
            {
                if (outRec.Pts == null || outRec.FirstLeft == null) continue;
                var firstLeft = ParseFirstLeft(outRec.FirstLeft);
                if (firstLeft != oldOutRec) continue;
                if (Poly2ContainsPoly1(outRec.Pts, newOutRec.Pts))
                    outRec.FirstLeft = newOutRec;
            }
        }

        //----------------------------------------------------------------------

        private void FixupFirstLefts2(OutRec oldOutRec, OutRec newOutRec)
        {
            foreach (var outRec in _mPolyOuts.Where(outRec => outRec.FirstLeft == oldOutRec))
                outRec.FirstLeft = newOutRec;
        }

        //----------------------------------------------------------------------

        private static OutRec ParseFirstLeft(OutRec firstLeft)
        {
            while (firstLeft != null && firstLeft.Pts == null)
                firstLeft = firstLeft.FirstLeft;
            return firstLeft;
        }
        //------------------------------------------------------------------------------

        private void JoinCommonEdges()
        {
            foreach (var join1 in _mJoins)
            {
                var outRec1 = GetOutRec(join1.OutPt1.Idx);
                var outRec2 = GetOutRec(join1.OutPt2.Idx);

                if (outRec1.Pts == null || outRec2.Pts == null) continue;

                //get the polygon fragment with the correct hole state (FirstLeft)
                //before calling JoinPoints() ...
                OutRec holeStateRec;
                if (outRec1 == outRec2) holeStateRec = outRec1;
                else if (Param1RightOfParam2(outRec1, outRec2)) holeStateRec = outRec2;
                else if (Param1RightOfParam2(outRec2, outRec1)) holeStateRec = outRec1;
                else holeStateRec = GetLowermostRec(outRec1, outRec2);

                if (!JoinPoints(join1, outRec1, outRec2)) continue;

                if (outRec1 == outRec2)
                {
                    //instead of joining two polygons, we've just created a new one by
                    //splitting one polygon into two.
                    outRec1.Pts = join1.OutPt1;
                    outRec1.BottomPt = null;
                    outRec2 = CreateOutRec();
                    outRec2.Pts = join1.OutPt2;

                    //update all OutRec2.Pts Idx's ...
                    UpdateOutPtIdxs(outRec2);

                    //We now need to check every OutRec.FirstLeft pointer. If it points
                    //to OutRec1 it may need to point to OutRec2 instead ...
                    if (_mUsingPolyTree)
                        for (var j = 0; j < _mPolyOuts.Count - 1; j++)
                        {
                            var oRec = _mPolyOuts[j];
                            if (oRec.Pts == null || ParseFirstLeft(oRec.FirstLeft) != outRec1 ||
                                oRec.IsHole == outRec1.IsHole) continue;
                            if (Poly2ContainsPoly1(oRec.Pts, join1.OutPt2))
                                oRec.FirstLeft = outRec2;
                        }

                    if (Poly2ContainsPoly1(outRec2.Pts, outRec1.Pts))
                    {
                        //outRec2 is contained by outRec1 ...
                        outRec2.IsHole = !outRec1.IsHole;
                        outRec2.FirstLeft = outRec1;

                        //fixup FirstLeft pointers that may need reassigning to OutRec1
                        if (_mUsingPolyTree) FixupFirstLefts2(outRec2, outRec1);

                        if ((outRec2.IsHole ^ ReverseSolution) == (Area(outRec2) > 0))
                            ReversePolyPtLinks(outRec2.Pts);

                    }
                    else if (Poly2ContainsPoly1(outRec1.Pts, outRec2.Pts))
                    {
                        //outRec1 is contained by outRec2 ...
                        outRec2.IsHole = outRec1.IsHole;
                        outRec1.IsHole = !outRec2.IsHole;
                        outRec2.FirstLeft = outRec1.FirstLeft;
                        outRec1.FirstLeft = outRec2;

                        //fixup FirstLeft pointers that may need reassigning to OutRec1
                        if (_mUsingPolyTree) FixupFirstLefts2(outRec1, outRec2);

                        if ((outRec1.IsHole ^ ReverseSolution) == (Area(outRec1) > 0))
                            ReversePolyPtLinks(outRec1.Pts);
                    }
                    else
                    {
                        //the 2 polygons are completely separate ...
                        outRec2.IsHole = outRec1.IsHole;
                        outRec2.FirstLeft = outRec1.FirstLeft;

                        //fixup FirstLeft pointers that may need reassigning to OutRec2
                        if (_mUsingPolyTree) FixupFirstLefts1(outRec1, outRec2);
                    }

                }
                else
                {
                    //joined 2 polygons together ...

                    outRec2.Pts = null;
                    outRec2.BottomPt = null;
                    outRec2.Idx = outRec1.Idx;

                    outRec1.IsHole = holeStateRec.IsHole;
                    if (holeStateRec == outRec2)
                        outRec1.FirstLeft = outRec2.FirstLeft;
                    outRec2.FirstLeft = outRec1;

                    //fixup FirstLeft pointers that may need reassigning to OutRec1
                    if (_mUsingPolyTree) FixupFirstLefts2(outRec2, outRec1);
                }
            }
        }

        //------------------------------------------------------------------------------

        private static void UpdateOutPtIdxs(OutRec outrec)
        {
            var outPoint = outrec.Pts;
            do
            {
                outPoint.Idx = outrec.Idx;
                outPoint = outPoint.Prev;
            }
            while (outPoint != outrec.Pts);
        }
        //------------------------------------------------------------------------------

        private void DoSimplePolygons()
        {
            var i = 0;
            while (i < _mPolyOuts.Count)
            {
                var outrec = _mPolyOuts[i++];
                var op = outrec.Pts;
                if (op == null || outrec.IsOpen) continue;
                do //for each point in Polygon until duplicate found do ...
                {
                    var op2 = op.Next;
                    while (op2 != outrec.Pts)
                    {
                        if ((op.Pt == op2.Pt) && op2.Next != op && op2.Prev != op)
                        {
                            //split the polygon into two ...
                            var op3 = op.Prev;
                            var op4 = op2.Prev;
                            op.Prev = op4;
                            op4.Next = op;
                            op2.Prev = op3;
                            op3.Next = op2;

                            outrec.Pts = op;
                            var outrec2 = CreateOutRec();
                            outrec2.Pts = op2;
                            UpdateOutPtIdxs(outrec2);
                            if (Poly2ContainsPoly1(outrec2.Pts, outrec.Pts))
                            {
                                //OutRec2 is contained by OutRec1 ...
                                outrec2.IsHole = !outrec.IsHole;
                                outrec2.FirstLeft = outrec;
                                if (_mUsingPolyTree) FixupFirstLefts2(outrec2, outrec);
                            }
                            else
                              if (Poly2ContainsPoly1(outrec.Pts, outrec2.Pts))
                            {
                                //OutRec1 is contained by OutRec2 ...
                                outrec2.IsHole = outrec.IsHole;
                                outrec.IsHole = !outrec2.IsHole;
                                outrec2.FirstLeft = outrec.FirstLeft;
                                outrec.FirstLeft = outrec2;
                                if (_mUsingPolyTree) FixupFirstLefts2(outrec, outrec2);
                            }
                            else
                            {
                                //the 2 polygons are separate ...
                                outrec2.IsHole = outrec.IsHole;
                                outrec2.FirstLeft = outrec.FirstLeft;
                                if (_mUsingPolyTree) FixupFirstLefts1(outrec, outrec2);
                            }
                            op2 = op; //ie get ready for the next iteration
                        }
                        op2 = op2.Next;
                    }
                    op = op.Next;
                }
                while (op != outrec.Pts);
            }
        }
        //------------------------------------------------------------------------------

        internal static double Area(Path poly)
        {
            var cnt = poly.Count;
            if (cnt < 3) return 0;
            double a = 0;
            for (int i = 0, j = cnt - 1; i < cnt; ++i)
            {
                a += (poly[j].X + poly[i].X) * (poly[j].Y - poly[i].Y);
                j = i;
            }
            return -a * 0.5;
        }
        //------------------------------------------------------------------------------

        static double Area(OutRec outRec)
        {
            var op = outRec.Pts;
            if (op == null) return 0;
            double a = 0;
            do
            {
                a = a + (op.Prev.Pt.X + op.Pt.X) * (op.Prev.Pt.Y - op.Pt.Y);
                op = op.Next;
            } while (op != outRec.Pts);
            return a * 0.5;
        }

        //------------------------------------------------------------------------------
        // SimplifyPolygon functions ...
        // Convert self-intersecting polygons into simple polygons
        //------------------------------------------------------------------------------

        internal static Paths SimplifyPolygon(Path poly,
              PolyFillType fillType = PolyFillType.EvenOdd)
        {
            var result = new Paths();
            var clipper = new Clipper { StrictlySimple = true };
            clipper.AddPath(poly, PolyType.Subject, true);
            clipper.Execute(ClipType.Union, result, fillType, fillType);
            return result;
        }
        //------------------------------------------------------------------------------

        internal static Paths SimplifyPolygons(Paths polys,
            PolyFillType fillType = PolyFillType.EvenOdd)
        {
            var result = new Paths();
            var clipper = new Clipper { StrictlySimple = true };
            clipper.AddPaths(polys, PolyType.Subject, true);
            clipper.Execute(ClipType.Union, result, fillType, fillType);
            return result;
        }
        //------------------------------------------------------------------------------

        private static double DistanceFromLineSqrd(Point pt, Point ln1, Point ln2)
        {
            //The equation of a line in general form (Ax + By + C = 0)
            //given 2 points (x¹,y¹) & (x²,y²) is ...
            //(y¹ - y²)x + (x² - x¹)y + (y² - y¹)x¹ - (x² - x¹)y¹ = 0
            //A = (y¹ - y²); B = (x² - x¹); C = (y² - y¹)x¹ - (x² - x¹)y¹
            //perpendicular distance of point (x³,y³) = (Ax³ + By³ + C)/Sqrt(A² + B²)
            //see http://en.wikipedia.org/wiki/Perpendicular_distance
            double a = ln1.Y - ln2.Y;
            double b = ln2.X - ln1.X;
            var c = a * ln1.X + b * ln1.Y;
            c = a * pt.X + b * pt.Y - c;
            return (c * c) / (a * a + b * b);
        }
        //---------------------------------------------------------------------------

        private static bool SlopesNearCollinear(Point pt1,
            Point pt2, Point pt3, double distSqrd)
        {
            //this function is more accurate when the point that's GEOMETRICALLY 
            //between the other 2 points is the one that's tested for distance.  
            //nb: with 'spikes', either pt1 or pt3 is geometrically between the other pts                    
            if (Math.Abs(pt1.X - pt2.X) > Math.Abs(pt1.Y - pt2.Y))
            {
                if ((pt1.X > pt2.X) == (pt1.X < pt3.X))
                    return DistanceFromLineSqrd(pt1, pt2, pt3) < distSqrd;
                else if ((pt2.X > pt1.X) == (pt2.X < pt3.X))
                    return DistanceFromLineSqrd(pt2, pt1, pt3) < distSqrd;
                else
                    return DistanceFromLineSqrd(pt3, pt1, pt2) < distSqrd;
            }
            else
            {
                if ((pt1.Y > pt2.Y) == (pt1.Y < pt3.Y))
                    return DistanceFromLineSqrd(pt1, pt2, pt3) < distSqrd;
                else if ((pt2.Y > pt1.Y) == (pt2.Y < pt3.Y))
                    return DistanceFromLineSqrd(pt2, pt1, pt3) < distSqrd;
                else
                    return DistanceFromLineSqrd(pt3, pt1, pt2) < distSqrd;
            }
        }
        //------------------------------------------------------------------------------

        private static bool PointsAreClose(Point pt1, Point pt2, double distSqrd)
        {
            var dx = pt1.X - pt2.X;
            var dy = pt1.Y - pt2.Y;
            var num = (dx*dx) + (dy*dy);
            return num < distSqrd || num.IsPracticallySame(distSqrd);
        }
        //------------------------------------------------------------------------------

        private static OutPt ExcludeOp(OutPt op)
        {
            OutPt result = op.Prev;
            result.Next = op.Next;
            op.Next.Prev = result;
            result.Idx = 0;
            return result;
        }
        //------------------------------------------------------------------------------

        internal static Path CleanPolygon(Path path, double distance = 1.415)
        {
            //distance = proximity in units/pixels below which vertices will be stripped. 
            //Default ~= sqrt(2) so when adjacent vertices or semi-adjacent vertices have 
            //both x & y coords within 1 unit, then the second vertex will be stripped.

            var cnt = path.Count;

            if (cnt == 0) return new Path();

            var outPts = new OutPt[cnt];
            for (var i = 0; i < cnt; ++i) outPts[i] = new OutPt();

            for (var i = 0; i < cnt; ++i)
            {
                outPts[i].Pt = path[i];
                outPts[i].Next = outPts[(i + 1) % cnt];
                outPts[i].Next.Prev = outPts[i];
                outPts[i].Idx = 0;
            }

            var distSqrd = distance * distance;
            var op = outPts[0];
            while (op.Idx == 0 && op.Next != op.Prev)
            {
                if (PointsAreClose(op.Pt, op.Prev.Pt, distSqrd))
                {
                    op = ExcludeOp(op);
                    cnt--;
                }
                else if (PointsAreClose(op.Prev.Pt, op.Next.Pt, distSqrd))
                {
                    ExcludeOp(op.Next);
                    op = ExcludeOp(op);
                    cnt -= 2;
                }
                else if (SlopesNearCollinear(op.Prev.Pt, op.Pt, op.Next.Pt, distSqrd))
                {
                    op = ExcludeOp(op);
                    cnt--;
                }
                else
                {
                    op.Idx = 1;
                    op = op.Next;
                }
            }

            if (cnt < 3) cnt = 0;
            var result = new Path(cnt);
            for (var i = 0; i < cnt; ++i)
            {
                result.Add(op.Pt);
                op = op.Next;
            }
            return result;
        }
        //------------------------------------------------------------------------------

        internal static Paths CleanPolygons(Paths polys,
            double distance = 1.415)
        {
            var result = new Paths(polys.Count);
            result.AddRange(polys.Select(t => CleanPolygon(t, distance)));
            return result;
        }
        //------------------------------------------------------------------------------

        internal static Paths Minkowski(Path pattern, Path path, bool isSum, bool isClosed)
        {
            var delta = (isClosed ? 1 : 0);
            var polyCnt = pattern.Count;
            var pathCnt = path.Count;
            var result = new Paths(pathCnt);
            if (isSum)
                for (var i = 0; i < pathCnt; i++)
                {
                    var p = new Path(polyCnt);
                    var i1 = i;
                    p.AddRange(pattern.Select(ip => new Point(path[i1].X + ip.X, path[i1].Y + ip.Y)));
                    result.Add(p);
                }
            else
                for (var i = 0; i < pathCnt; i++)
                {
                    var p = new Path(polyCnt);
                    var i1 = i;
                    p.AddRange(pattern.Select(ip => new Point(path[i1].X - ip.X, path[i1].Y - ip.Y)));
                    result.Add(p);
                }

            var quads = new Paths((pathCnt + delta) * (polyCnt + 1));
            for (var i = 0; i < pathCnt - 1 + delta; i++)
                for (var j = 0; j < polyCnt; j++)
                {
                    var quad = new Path(4)
                    {
                        result[i%pathCnt][j%polyCnt],
                        result[(i + 1)%pathCnt][j%polyCnt],
                        result[(i + 1)%pathCnt][(j + 1)%polyCnt],
                        result[i%pathCnt][(j + 1)%polyCnt]
                    };
                    if (!Orientation(quad)) quad.Reverse();
                    quads.Add(quad);
                }
            return quads;
        }
        //------------------------------------------------------------------------------

        internal static Paths MinkowskiSum(Path pattern, Path path, bool pathIsClosed)
        {
            var paths = Minkowski(pattern, path, true, pathIsClosed);
            var clipper = new Clipper();
            clipper.AddPaths(paths, PolyType.Subject, true);
            clipper.Execute(ClipType.Union, paths, PolyFillType.NonZero, PolyFillType.NonZero);
            return paths;
        }
        //------------------------------------------------------------------------------

        private static Path TranslatePath(Path path, Point delta)
        {
            var outPath = new Path(path.Count);
            for (var i = 0; i < path.Count; i++)
                outPath.Add(new Point(path[i].X + delta.X, path[i].Y + delta.Y));
            return outPath;
        }
        //------------------------------------------------------------------------------

        internal static Paths MinkowskiSum(Path pattern, Paths paths, bool pathIsClosed)
        {
            var solution = new Paths();
            var clipper = new Clipper();
            foreach (var p in paths)
            {
                var tmp = Minkowski(pattern, p, true, pathIsClosed);
                clipper.AddPaths(tmp, PolyType.Subject, true);
                if (!pathIsClosed) continue;
                var path = TranslatePath(p, pattern[0]);
                clipper.AddPath(path, PolyType.Clip, true);
            }
            clipper.Execute(ClipType.Union, solution,
              PolyFillType.NonZero, PolyFillType.NonZero);
            return solution;
        }
        //------------------------------------------------------------------------------

        internal static Paths MinkowskiDiff(Path poly1, Path poly2)
        {
            var paths = Minkowski(poly1, poly2, false, true);
            var clipper = new Clipper();
            clipper.AddPaths(paths, PolyType.Subject, true);
            clipper.Execute(ClipType.Union, paths, PolyFillType.NonZero, PolyFillType.NonZero);
            return paths;
        }
        //------------------------------------------------------------------------------

        internal enum NodeType { NodeTypeAny, NodeTypeOpen, NodeTypeClosed };

        internal static Paths PolyTreeToPaths(PolyTree polytree)
        {
            var result = new Paths { Capacity = polytree.Total };
            AddPolyNodeToPaths(polytree, NodeType.NodeTypeAny, result);
            return result;
        }
        //------------------------------------------------------------------------------

        internal static void AddPolyNodeToPaths(PolyNode polynode, NodeType nt, Paths paths)
        {
            bool match = true;
            switch (nt)
            {
                case NodeType.NodeTypeOpen: return;
                case NodeType.NodeTypeClosed: match = !polynode.IsOpen; break;
            }

            if (polynode.MPolygon.Count > 0 && match)
                paths.Add(polynode.MPolygon);
            foreach (PolyNode pn in polynode.Childs)
                AddPolyNodeToPaths(pn, nt, paths);
        }
        //------------------------------------------------------------------------------

        internal static Paths OpenPathsFromPolyTree(PolyTree polytree)
        {
            var result = new Paths { Capacity = polytree.ChildCount };
            for (var i = 0; i < polytree.ChildCount; i++)
                if (polytree.Childs[i].IsOpen)
                    result.Add(polytree.Childs[i].MPolygon);
            return result;
        }
        //------------------------------------------------------------------------------

        internal static Paths ClosedPathsFromPolyTree(PolyTree polytree)
        {
            var result = new Paths { Capacity = polytree.Total };
            AddPolyNodeToPaths(polytree, NodeType.NodeTypeClosed, result);
            return result;
        }
        //------------------------------------------------------------------------------

    } //end Clipper

    #endregion 

    #region Clipper Offset
    internal class ClipperOffset
    {
        private Paths _mDestPolys;
        private Path _mSrcPoly;
        private Path _mDestPoly;
        private List<DoublePoint> _mNormals = new List<DoublePoint>();
        private double _mDelta, _mSinA, _mSin, _mCos;
        private double _mMiterLim, _mStepsPerRad;

        private Point _mLowest;
        private PolyNode _mPolyNodes = new PolyNode();

        internal double ArcTolerance { get; set; }
        internal double MiterLimit { get; set; }

        private const double TwoPi = Math.PI * 2;
        //internal double DefArcTolerance;

        internal ClipperOffset(double minLength, double miterLimit = 2.0)
        {
            MiterLimit = miterLimit;
            ArcTolerance = minLength;
            //DefArcTolerance = minLength;
            _mLowest = new Point(-1, 0);
        }
        //------------------------------------------------------------------------------

        internal void Clear()
        {
            _mPolyNodes.Childs.Clear();
            _mLowest = new Point(-1, 0);
        }

        //------------------------------------------------------------------------------

        internal void AddPath(Path path, JoinType joinType, EndType endType)
        {
            path = PolygonOperations.SimplifyFuzzy(path);

            var highI = path.Count - 1;
            if (highI < 0) return;
            var newNode = new PolyNode
            {
                MJointype = joinType,
                MEndtype = endType
            };

            //strip duplicate points from path and also get index to the lowest point ...
            if (endType == EndType.ClosedLine || endType == EndType.ClosedPolygon)
                while (highI > 0 && path[0] == path[highI]) highI--;
            newNode.MPolygon.Capacity = highI + 1;
            newNode.MPolygon.Add(path[0]);
            int j = 0, k = 0;
            for (int i = 1; i <= highI; i++)
                if (newNode.MPolygon[j] != path[i])
                {
                    j++;
                    newNode.MPolygon.Add(path[i]);
                    if (path[i].Y > newNode.MPolygon[k].Y ||
                      (path[i].Y.IsPracticallySame(newNode.MPolygon[k].Y) &&
                      path[i].X < newNode.MPolygon[k].X)) k = j;
                }
            if (endType == EndType.ClosedPolygon && j < 2) return;

            _mPolyNodes.AddChild(newNode);

            //if this path's lowest pt is lower than all the others then update m_lowest
            if (endType != EndType.ClosedPolygon) return;
            if (_mLowest.X < 0)
                _mLowest = new Point(_mPolyNodes.ChildCount - 1, k);
            else
            {
                var ip = _mPolyNodes.Childs[(int)_mLowest.X].MPolygon[(int)_mLowest.Y];
                if (newNode.MPolygon[k].Y > ip.Y ||
                  (newNode.MPolygon[k].Y.IsPracticallySame(ip.Y) &&
                  newNode.MPolygon[k].X < ip.X))
                    _mLowest = new Point(_mPolyNodes.ChildCount - 1, k);
            }
        }
        //------------------------------------------------------------------------------

        internal void AddPaths(IList<Path> paths, JoinType joinType, EndType endType)
        {
            foreach (var path in paths)
                AddPath(path, joinType, endType);
        }
        //------------------------------------------------------------------------------

        private void FixOrientations()
        {
            //fixup orientations of all closed paths if the orientation of the
            //closed path with the lowermost vertex is wrong ...
            if (_mLowest.X >= 0 &&
              !Clipper.Orientation(_mPolyNodes.Childs[(int)_mLowest.X].MPolygon))
            {
                for (var i = 0; i < _mPolyNodes.ChildCount; i++)
                {
                    var node = _mPolyNodes.Childs[i];
                    if (node.MEndtype == EndType.ClosedPolygon ||
                      (node.MEndtype == EndType.ClosedLine &&
                      Clipper.Orientation(node.MPolygon)))
                        node.MPolygon.Reverse();
                }
            }
            else
            {
                for (var i = 0; i < _mPolyNodes.ChildCount; i++)
                {
                    var node = _mPolyNodes.Childs[i];
                    if (node.MEndtype == EndType.ClosedLine &&
                      !Clipper.Orientation(node.MPolygon))
                        node.MPolygon.Reverse();
                }
            }
        }
        //------------------------------------------------------------------------------

        internal static DoublePoint GetUnitNormal(Point pt1, Point pt2)
        {
            var dx = (pt2.X - pt1.X);
            var dy = (pt2.Y - pt1.Y);
            if (dx.IsNegligible() && dy.IsNegligible()) return new DoublePoint();

            var f = 1 * 1.0 / Math.Sqrt(dx * dx + dy * dy);
            dx *= f;
            dy *= f;

            return new DoublePoint(dy, -dx);
        }
        //------------------------------------------------------------------------------

        private void DoOffset(double delta)
        {
            _mDestPolys = new Paths();
            _mDelta = delta;

            //if Zero offset, just copy any CLOSED polygons to m_p and return ...
            if (ClipperBase.near_zero(delta))
            {
                _mDestPolys.Capacity = _mPolyNodes.ChildCount;
                for (var i = 0; i < _mPolyNodes.ChildCount; i++)
                {
                    var node = _mPolyNodes.Childs[i];
                    if (node.MEndtype == EndType.ClosedPolygon)
                        _mDestPolys.Add(node.MPolygon);
                }
                return;
            }

            //see offset_triginometry3.svg in the documentation folder ...
            if (MiterLimit > 2) _mMiterLim = 2 / (MiterLimit * MiterLimit);
            else _mMiterLim = 0.5;

            double y;
            if (ArcTolerance <= 0.0)
                y = Math.Abs(ArcTolerance);
            else if (ArcTolerance > Math.Abs(delta)/2)
                y = Math.Abs(delta)/2;
            else
                y = ArcTolerance;
            //see offset_triginometry2.svg in the documentation folder ...
            var steps = Math.PI / Math.Acos(1 - y / Math.Abs(delta));
            _mSin = Math.Sin(TwoPi / steps);
            _mCos = Math.Cos(TwoPi / steps);
            _mStepsPerRad = steps / TwoPi;
            if (delta < 0.0) _mSin = -_mSin;

            _mDestPolys.Capacity = _mPolyNodes.ChildCount * 2;
            for (var i = 0; i < _mPolyNodes.ChildCount; i++)
            {
                var node = _mPolyNodes.Childs[i];
                _mSrcPoly = node.MPolygon;

                var len = _mSrcPoly.Count;

                if (len == 0 || (delta <= 0 && (len < 3 ||
                  node.MEndtype != EndType.ClosedPolygon)))
                    continue;

                _mDestPoly = new Path();

                if (len == 1)
                {
                    if (node.MJointype == JoinType.Round)
                    {
                        double xval = ArcTolerance, yval = 0.0;
                        for (var j = 1; j <= steps; j++)
                        {
                            _mDestPoly.Add(new Point(
                              (_mSrcPoly[0].X + xval * delta),
                              (_mSrcPoly[0].Y + yval * delta)));
                            var x2 = xval;
                            xval = xval * _mCos - _mSin * yval;
                            yval = x2 * _mSin + yval * _mCos;
                        }
                    }
                    else
                    {
                        double xval = -ArcTolerance, yval = -ArcTolerance;
                        for (var j = 0; j < 4; ++j)
                        {
                            _mDestPoly.Add(new Point(
                              (_mSrcPoly[0].X + xval * delta),
                              (_mSrcPoly[0].Y + yval * delta)));
                            if (xval < 0) xval = 1;
                            else if (yval < 0) yval = 1;
                            else xval = -1;
                        }
                    }
                    _mDestPolys.Add(_mDestPoly);
                    continue;
                }

                //build m_normals ...
                _mNormals.Clear();
                _mNormals.Capacity = len;
                for (var j = 0; j < len - 1; j++)
                    _mNormals.Add(GetUnitNormal(_mSrcPoly[j], _mSrcPoly[j + 1]));
                if (node.MEndtype == EndType.ClosedLine ||
                  node.MEndtype == EndType.ClosedPolygon)
                    _mNormals.Add(GetUnitNormal(_mSrcPoly[len - 1], _mSrcPoly[0]));
                else
                    _mNormals.Add(new DoublePoint(_mNormals[len - 2]));

                switch (node.MEndtype)
                {
                    case EndType.ClosedPolygon:
                        {
                            var k = len - 1;
                            for (var j = 0; j < len; j++)
                                OffsetPoint(j, ref k, node.MJointype);
                            _mDestPolys.Add(_mDestPoly);
                        }
                        break;
                    case EndType.ClosedLine:
                        {
                            var k = len - 1;
                            for (var j = 0; j < len; j++)
                                OffsetPoint(j, ref k, node.MJointype);
                            _mDestPolys.Add(_mDestPoly);
                            _mDestPoly = new Path();
                            //re-build m_normals ...
                            var n = _mNormals[len - 1];
                            for (var j = len - 1; j > 0; j--)
                                _mNormals[j] = new DoublePoint(-_mNormals[j - 1].X, -_mNormals[j - 1].Y);
                            _mNormals[0] = new DoublePoint(-n.X, -n.Y);
                            k = 0;
                            for (var j = len - 1; j >= 0; j--)
                                OffsetPoint(j, ref k, node.MJointype);
                            _mDestPolys.Add(_mDestPoly);
                        }
                        break;
                    default:
                        {
                            var k = 0;
                            for (var j = 1; j < len - 1; ++j)
                                OffsetPoint(j, ref k, node.MJointype);

                            Point pt1;
                            if (node.MEndtype == EndType.OpenButt)
                            {
                                var j = len - 1;
                                pt1 = new Point((_mSrcPoly[j].X + _mNormals[j].X *
                                                         delta), (_mSrcPoly[j].Y + _mNormals[j].Y * delta));
                                _mDestPoly.Add(pt1);
                                pt1 = new Point((_mSrcPoly[j].X - _mNormals[j].X *
                                                         delta), (_mSrcPoly[j].Y - _mNormals[j].Y * delta));
                                _mDestPoly.Add(pt1);
                            }
                            else
                            {
                                var j = len - 1;
                                k = len - 2;
                                _mSinA = 0;
                                _mNormals[j] = new DoublePoint(-_mNormals[j].X, -_mNormals[j].Y);
                                if (node.MEndtype == EndType.OpenSquare)
                                    DoSquare(j, k);
                                else
                                    DoRound(j, k);
                            }

                            //re-build m_normals ...
                            for (var j = len - 1; j > 0; j--)
                                _mNormals[j] = new DoublePoint(-_mNormals[j - 1].X, -_mNormals[j - 1].Y);

                            _mNormals[0] = new DoublePoint(-_mNormals[1].X, -_mNormals[1].Y);

                            k = len - 1;
                            for (var j = k - 1; j > 0; --j)
                                OffsetPoint(j, ref k, node.MJointype);

                            if (node.MEndtype == EndType.OpenButt)
                            {
                                pt1 = new Point((_mSrcPoly[0].X - _mNormals[0].X * delta),
                                    (_mSrcPoly[0].Y - _mNormals[0].Y * delta));
                                _mDestPoly.Add(pt1);
                                pt1 = new Point((_mSrcPoly[0].X + _mNormals[0].X * delta),
                                    (_mSrcPoly[0].Y + _mNormals[0].Y * delta));
                                _mDestPoly.Add(pt1);
                            }
                            else
                            {
                                _mSinA = 0;
                                if (node.MEndtype == EndType.OpenSquare)
                                    DoSquare(0, 1);
                                else
                                    DoRound(0, 1);
                            }
                            _mDestPolys.Add(_mDestPoly);
                        }
                        break;
                }
            }
        }
        //------------------------------------------------------------------------------

        internal void Execute(ref Paths solution, double delta)
        {
            solution.Clear();
            FixOrientations();
            DoOffset(delta);
            //now clean up 'corners' ...
            var clipper = new Clipper();
            clipper.AddPaths(_mDestPolys, PolyType.Subject, true);
            if (delta > 0)
            {
                clipper.Execute(ClipType.Union, solution,
                  PolyFillType.Positive, PolyFillType.Positive);
            }
            else
            {
                var r = ClipperBase.GetBounds(_mDestPolys);
                var outerPath = new Path(4)
                {
                    new Point(r.Left - 10, r.Bottom + 10),
                    new Point(r.Right + 10, r.Bottom + 10),
                    new Point(r.Right + 10, r.Top - 10),
                    new Point(r.Left - 10, r.Top - 10)
                };


                clipper.AddPath(outerPath, PolyType.Subject, true);
                clipper.ReverseSolution = true;
                clipper.Execute(ClipType.Union, solution, PolyFillType.Negative, PolyFillType.Negative);
                if (solution.Count > 0) solution.RemoveAt(0);
            }
        }
        //------------------------------------------------------------------------------

        internal void Execute(ref PolyTree solution, double delta)
        {
            solution.Clear();
            FixOrientations();
            DoOffset(delta);

            //now clean up 'corners' ...
            var clipper = new Clipper();
            clipper.AddPaths(_mDestPolys, PolyType.Subject, true);
            if (delta > 0)
            {
                clipper.Execute(ClipType.Union, solution,
                  PolyFillType.Positive, PolyFillType.Positive);
            }
            else
            {
                var r = ClipperBase.GetBounds(_mDestPolys);
                var outerPath = new Path(4)
                {
                    new Point(r.Left - 10, r.Bottom + 10),
                    new Point(r.Right + 10, r.Bottom + 10),
                    new Point(r.Right + 10, r.Top - 10),
                    new Point(r.Left - 10, r.Top - 10)
                };


                clipper.AddPath(outerPath, PolyType.Subject, true);
                clipper.ReverseSolution = true;
                clipper.Execute(ClipType.Union, solution, PolyFillType.Negative, PolyFillType.Negative);
                //remove the outer PolyNode rectangle ...
                if (solution.ChildCount == 1 && solution.Childs[0].ChildCount > 0)
                {
                    var outerNode = solution.Childs[0];
                    solution.Childs.Capacity = outerNode.ChildCount;
                    solution.Childs[0] = outerNode.Childs[0];
                    solution.Childs[0].MParent = solution;
                    for (var i = 1; i < outerNode.ChildCount; i++)
                        solution.AddChild(outerNode.Childs[i]);
                }
                else
                    solution.Clear();
            }
        }
        //------------------------------------------------------------------------------

        void OffsetPoint(int j, ref int k, JoinType jointype)
        {
            //cross product ...
            _mSinA = (_mNormals[k].X * _mNormals[j].Y - _mNormals[j].X * _mNormals[k].Y);

            if (Math.Abs(_mSinA * _mDelta) < 0.01)
            {
                //dot product ...
                var cosA = (_mNormals[k].X * _mNormals[j].X + _mNormals[j].Y * _mNormals[k].Y);
                if (cosA > 0) // angle ==> 0 degrees
                {
                    _mDestPoly.Add(new Point((_mSrcPoly[j].X + _mNormals[k].X * _mDelta),
                      (_mSrcPoly[j].Y + _mNormals[k].Y * _mDelta)));
                    return;
                }
                //else angle ==> 180 degrees   
            }
            else if (_mSinA > 1.0) _mSinA = 1.0;
            else if (_mSinA < -1.0) _mSinA = -1.0;

            if (_mSinA * _mDelta < 0)
            {
                _mDestPoly.Add(new Point((_mSrcPoly[j].X + _mNormals[k].X * _mDelta),
                  (_mSrcPoly[j].Y + _mNormals[k].Y * _mDelta)));
                _mDestPoly.Add(_mSrcPoly[j]);
                _mDestPoly.Add(new Point((_mSrcPoly[j].X + _mNormals[j].X * _mDelta),
                  (_mSrcPoly[j].Y + _mNormals[j].Y * _mDelta)));
            }
            else
                switch (jointype)
                {
                    case JoinType.Miter:
                        {
                            double r = 1 + (_mNormals[j].X * _mNormals[k].X +
                              _mNormals[j].Y * _mNormals[k].Y);
                            if (r >= _mMiterLim) DoMiter(j, k, r); else DoSquare(j, k);
                            break;
                        }
                    case JoinType.Square: DoSquare(j, k); break;
                    case JoinType.Round: DoRound(j, k); break;
                }
            k = j;
        }
        //------------------------------------------------------------------------------

        internal void DoSquare(int j, int k)
        {
            var dx = Math.Tan(Math.Atan2(_mSinA,
                _mNormals[k].X * _mNormals[j].X + _mNormals[k].Y * _mNormals[j].Y) / 4);
            _mDestPoly.Add(new Point(
                (_mSrcPoly[j].X + _mDelta * (_mNormals[k].X - _mNormals[k].Y * dx)),
                (_mSrcPoly[j].Y + _mDelta * (_mNormals[k].Y + _mNormals[k].X * dx))));
            _mDestPoly.Add(new Point(
                (_mSrcPoly[j].X + _mDelta * (_mNormals[j].X + _mNormals[j].Y * dx)),
                (_mSrcPoly[j].Y + _mDelta * (_mNormals[j].Y - _mNormals[j].X * dx))));
        }
        //------------------------------------------------------------------------------

        internal void DoMiter(int j, int k, double r)
        {
            double q = _mDelta / r;
            _mDestPoly.Add(new Point((_mSrcPoly[j].X + (_mNormals[k].X + _mNormals[j].X) * q),
                (_mSrcPoly[j].Y + (_mNormals[k].Y + _mNormals[j].Y) * q)));
        }
        //------------------------------------------------------------------------------

        internal void DoRound(int j, int k)
        {
            var a = Math.Atan2(_mSinA,
            _mNormals[k].X * _mNormals[j].X + _mNormals[k].Y * _mNormals[j].Y);
            var steps = Math.Max((int)(_mStepsPerRad * Math.Abs(a)), 1);

            double x = _mNormals[k].X, y = _mNormals[k].Y;
            for (var i = 0; i < steps; ++i)
            {
                _mDestPoly.Add(new Point(
                    (_mSrcPoly[j].X + x * _mDelta),
                    (_mSrcPoly[j].Y + y * _mDelta)));
                var x2 = x;
                x = x * _mCos - _mSin * y;
                y = x2 * _mSin + y * _mCos;
            }
            _mDestPoly.Add(new Point(
            (_mSrcPoly[j].X + _mNormals[j].X * _mDelta),
            (_mSrcPoly[j].Y + _mNormals[j].Y * _mDelta)));
        }
        //------------------------------------------------------------------------------
    }
    #endregion

    internal class ClipperException : Exception
    {
        internal ClipperException(string description) : base(description) { }
    }
} //end ClipperLib namespace
