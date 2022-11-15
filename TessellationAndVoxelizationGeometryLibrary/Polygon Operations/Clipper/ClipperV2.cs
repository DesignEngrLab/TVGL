/*******************************************************************************
*                                                                              *
* Author    :  Angus Johnson                                                   *
* Version   :  10.0 (alpha)                                                    *
* Date      :  29 September 2017                                               *
* Website   :  http://www.angusj.com                                           *
* Copyright :  Angus Johnson 2010-2017                                         *
*                                                                              *
* License:                                                                     *
* Use, modification & distribution is subject to Boost Software License Ver 1. *
* http://www.boost.org/LICENSE_1_0.txt                                         *
*                                                                              *
*******************************************************************************/

using System;
using System.Collections.Generic;

namespace ClipperLib2
{
    using Path = List<Point64>;
    using Paths = List<List<Point64>>;

    public struct Point64
    {
        public long X;
        public long Y;
        public Point64(long X, long Y)
        {
            this.X = X; this.Y = Y;
        }
        public Point64(double x, double y)
        {
            X = (long)x; Y = (long)y;
        }

        public Point64(Point64 pt)
        {
            X = pt.X; Y = pt.Y;
        }

        public static bool operator ==(Point64 a, Point64 b)
        {
            return a.X == b.X && a.Y == b.Y;
        }

        public static bool operator !=(Point64 a, Point64 b)
        {
            return a.X != b.X || a.Y != b.Y;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj is Point64 a)
            {
                return (X == a.X) && (Y == a.Y);
            }
            else return false;
        }

        public override int GetHashCode()
        {
            return (X.GetHashCode() ^ Y.GetHashCode());
        }

    } //Point64

    public struct Rect64
    {
        public long left;
        public long top;
        public long right;
        public long bottom;

        public Rect64(long l, long t, long r, long b)
        {
            this.left = l; this.top = t;
            this.right = r; this.bottom = b;
        }
        public Rect64(Rect64 r)
        {
            this.left = r.left; this.top = r.top;
            this.right = r.right; this.bottom = r.bottom;
        }
    } //Rect64

    public enum ClipType { Intersection, Union, Difference, Xor };
    public enum PathType { Subject, Clip };
    //By far the most widely used winding rules for polygon filling are
    //EvenOdd & NonZero (GDI, GDI+, XLib, OpenGL, Cairo, AGG, Quartz, SVG, Gr32)
    //Others rules include Positive, Negative and ABS_GTR_EQ_TWO (only in OpenGL)
    //see http://glprogramming.com/red/chapter11.html
    public enum FillRule { EvenOdd, NonZero, Positive, Negative };

    [Flags]
    internal enum VertexFlags { OpenStart = 1, OpenEnd = 2, LocMax = 4, LocMin = 8 };

    internal class Vertex
    {
        internal Point64 Pt;
        internal Vertex Next;
        internal Vertex Prev;
        internal VertexFlags Flags;
        public Vertex(Point64 ip) { Pt.X = ip.X; Pt.Y = ip.Y; }
    }

    public class LocalMinima
    {
        internal Vertex Vertex;
        internal PathType PolyType;
        internal bool IsOpen;
    };

    internal class Active
    {
        internal Point64 Bot;
        internal Point64 Curr;       //current (updated for every new Scanline)
        internal Point64 Top;
        internal double Dx;
        internal int WindDx;     //wind direction (ascending: +1; descending: -1)
        internal int WindCnt;    //current wind count
        internal int WindCnt2;   //current wind count of opposite TPolyType
        internal OutRec OutRec;
        internal Active NextInAEL;
        internal Active PrevInAEL;
        internal Active NextInSEL;
        internal Active PrevInSEL;
        internal Active MergeJump;
        internal Vertex VertTop;
        internal LocalMinima LocalMin;
    };

    public class ScanLine
    {
        internal long Y;
        internal ScanLine Next;
    };

    internal class OutPt
    {
        internal Point64 Pt;
        internal OutPt Next;
        internal OutPt Prev;
    };

    [Flags]
    internal enum OutrecFlags { Open = 1, Outer = 2 };

    //OutRec: contains a path in the clipping solution. Edges in the AEL will
    //carry a pointer to an OutRec when they are part of the clipping solution.
    internal class OutRec
    {
        internal int IDx;
        internal OutRec Owner;
        internal OutPt Pts;
        internal Active StartE;
        internal Active EndE;
        internal OutrecFlags Flags;
        internal PolyPath PolyPath;
    };

    public class IntersectNode
    {
        internal Active Edge1;
        internal Active Edge2;
        internal Point64 Pt;
    };

    public class MyIntersectNodeSort : IComparer<IntersectNode>
    {
        public int Compare(IntersectNode node1, IntersectNode node2)
        {
            return node2.Pt.Y.CompareTo(node1.Pt.Y); //descending soft
        }
    }

    public class MyLocalMinSort : IComparer<LocalMinima>
    {
        public int Compare(LocalMinima lm1, LocalMinima lm2)
        {
            return lm2.Vertex.Pt.Y.CompareTo(lm1.Vertex.Pt.Y); //descending soft
        }
    }

    //------------------------------------------------------------------------------
    // PolyTree & PolyNode classes
    //------------------------------------------------------------------------------

    public class PolyPath
    {
        internal PolyPath parent;
        internal List<PolyPath> childs = new List<PolyPath>();
        internal Path path = new Path();

        //-----------------------------------------------------
        private bool IsHoleNode()
        {
            bool result = true;
            PolyPath node = parent;
            while (node != null)
            {
                result = !result;
                node = node.parent;
            }
            return result;
        }
        //-----------------------------------------------------

        internal PolyPath AddChild(Path p)
        {
            PolyPath child = new PolyPath();
            child.parent = this;
            child.path = p;
            Childs.Add(child);
            return child;
        }
        //-----------------------------------------------------

        public void Clear() { Childs.Clear(); }

        //the following two methods are really only for debugging ...

        private static void AddPolyNodeToPaths(PolyPath pp, Paths paths)
        {
            int cnt = pp.path.Count;
            if (cnt > 0)
            {
                Path p = new Path(cnt);
                foreach (Point64 ip in pp.path) p.Add(ip);
                paths.Add(p);
            }
            foreach (PolyPath polyp in pp.childs)
                AddPolyNodeToPaths(polyp, paths);
        }
        //-----------------------------------------------------

        public Paths PolyTreeToPaths()
        {
            Paths paths = new Paths();
            AddPolyNodeToPaths(this, paths);
            return paths;
        }
        //-----------------------------------------------------

        public Path Path { get { return path; } }

        public int ChildCount { get { return childs.Count; } }

        public List<PolyPath> Childs { get { return childs; } }

        public PolyPath Parent { get { return parent; } }

        public bool IsHole { get { return IsHoleNode(); } }
    }

    public class PolyTree : PolyPath { };


    //------------------------------------------------------------------------------
    // Clipper 
    //------------------------------------------------------------------------------

    public class Clipper
    {
        internal const double horizontal = double.NegativeInfinity;
        internal ScanLine Scanline;
        internal bool HasOpenPaths;
        internal bool LocMinListSorted;
        internal List<List<Vertex>> VertexList = new List<List<Vertex>>();
        internal List<OutRec> OutRecList = new List<OutRec>();
        internal int CurrentLocMinIdx;
        internal Active Actives;
        private Active SEL;
        private List<LocalMinima> LocMinimaList = new List<LocalMinima>();
        IComparer<LocalMinima> LocalMinimaComparer = new MyLocalMinSort();
        private List<IntersectNode> IntersectList = new List<IntersectNode>();
        IComparer<IntersectNode> IntersectNodeComparer = new MyIntersectNodeSort();
        private bool ExecuteLocked;
        private ClipType ClipType;
        private FillRule FillType;

        private bool IsHotEdge(Active Edge)
        {
            return Edge.OutRec != null;
        }
        //------------------------------------------------------------------------------

        private bool IsStartSide(Active Edge)
        {
            return (Edge == Edge.OutRec.StartE);
        }
        //------------------------------------------------------------------------------

        internal static long Round(double value)
        {
            return value < 0 ? (long)(value - 0.5) : (long)(value + 0.5);
        }
        //------------------------------------------------------------------------------

        private static long TopX(Active edge, long currentY)
        {
            if (currentY == edge.Top.Y)
                return edge.Top.X;
            return edge.Bot.X + Round(edge.Dx * (currentY - edge.Bot.Y));
        }
        //------------------------------------------------------------------------------

        internal static bool IsHorizontal(Active e)
        {
            return e.Dx == horizontal;
        }
        //------------------------------------------------------------------------------

        internal static bool IsOpen(Active e)
        {
            return e.LocalMin.IsOpen;
        }
        //------------------------------------------------------------------------------

        public static double Area(Path poly)
        {
            int cnt = poly.Count;
            if (cnt < 3) return 0;
            double a = 0;
            for (int i = 0, j = cnt - 1; i < cnt; ++i)
            {
                a += ((double)poly[j].X + poly[i].X) * ((double)poly[j].Y - poly[i].Y);
                j = i;
            }
            return -a * 0.5;
        }
        //------------------------------------------------------------------------------

        public static Rect64 GetBounds(Paths paths)
        {
            Rect64 result =
              new Rect64(long.MaxValue, long.MaxValue, long.MinValue, long.MinValue);
            foreach (Path p in paths)
                foreach (Point64 pt in p)
                {
                    if (pt.X < result.left) result.left = pt.X;
                    if (pt.X > result.right) result.right = pt.X;
                    if (pt.Y < result.top) result.top = pt.Y;
                    if (pt.Y > result.bottom) result.bottom = pt.Y;
                }
            return (result.left > result.right ? new Rect64(0, 0, 0, 0) : result);
        }
        //------------------------------------------------------------------------------

        public static int PointInPolygon(Point64 pt, Path path)
        {
            //returns 0 if false, +1 if true, -1 if pt ON polygon boundary
            //See "The Point in Polygon Problem for Arbitrary Polygons" by Hormann & Agathos
            //http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.88.5498&rep=rep1&type=pdf
            int result = 0, cnt = path.Count;
            if (cnt < 3) return 0;
            Point64 ip = path[0];
            for (int i = 1; i <= cnt; ++i)
            {
                Point64 ipNext = (i == cnt ? path[0] : path[i]);
                if (ipNext.Y == pt.Y)
                {
                    if ((ipNext.X == pt.X) || (ip.Y == pt.Y &&
                      ((ipNext.X > pt.X) == (ip.X < pt.X)))) return -1;
                }
                if ((ip.Y < pt.Y) != (ipNext.Y < pt.Y))
                {
                    if (ip.X >= pt.X)
                    {
                        if (ipNext.X > pt.X) result = 1 - result;
                        else
                        {
                            double d = (double)(ip.X - pt.X) * (ipNext.Y - pt.Y) -
                              (double)(ipNext.X - pt.X) * (ip.Y - pt.Y);
                            if (d == 0) return -1;
                            else if ((d > 0) == (ipNext.Y > ip.Y)) result = 1 - result;
                        }
                    }
                    else
                    {
                        if (ipNext.X > pt.X)
                        {
                            double d = (double)(ip.X - pt.X) * (ipNext.Y - pt.Y) -
                              (double)(ipNext.X - pt.X) * (ip.Y - pt.Y);
                            if (d == 0) return -1;
                            else if ((d > 0) == (ipNext.Y > ip.Y)) result = 1 - result;
                        }
                    }
                }
                ip = ipNext;
            }
            return result;
        }
        //------------------------------------------------------------------------------

        internal long GetTopDeltaX(Active e1, Active e2)
        {
            if (e1.Top.Y > e2.Top.Y)
                return TopX(e2, e1.Top.Y) - e1.Top.X;
            else
                return e2.Top.X - TopX(e1, e2.Top.Y);
        }
        //------------------------------------------------------------------------------

        private void SwapActive(ref Active e1, ref Active e2)
        {
            Active e = e1;
            e1 = e2; e2 = e;
        }
        //------------------------------------------------------------------------------

        private bool E2InsertsBeforeE1(Active e1, Active e2, bool PreferLeft)
        {
            if (e2.Curr.X == e1.Curr.X)
            {
                return (PreferLeft ? GetTopDeltaX(e1, e2) <= 0 : GetTopDeltaX(e1, e2) < 0);
            }
            else return e2.Curr.X < e1.Curr.X;
        }
        //------------------------------------------------------------------------------

        private Point64 GetIntersectPoint(Active edge1, Active edge2)
        {
            Point64 ip = new Point64();
            double b1, b2;
            //nb: with very large coordinate values, it's possible for SlopesEqual() to 
            //return false but for the edge.Dx value be equal due to double precision rounding.
            if (edge1.Dx == edge2.Dx)
            {
                ip.Y = edge1.Curr.Y;
                ip.X = TopX(edge1, ip.Y);
                return ip;
            }

            if (edge1.Dx == 0)
            {
                ip.X = edge1.Bot.X;
                if (IsHorizontal(edge2))
                {
                    ip.Y = edge2.Bot.Y;
                }
                else
                {
                    b2 = edge2.Bot.Y - (edge2.Bot.X / edge2.Dx);
                    ip.Y = Round(ip.X / edge2.Dx + b2);
                }
            }
            else if (edge2.Dx == 0)
            {
                ip.X = edge2.Bot.X;
                if (IsHorizontal(edge1))
                {
                    ip.Y = edge1.Bot.Y;
                }
                else
                {
                    b1 = edge1.Bot.Y - (edge1.Bot.X / edge1.Dx);
                    ip.Y = Round(ip.X / edge1.Dx + b1);
                }
            }
            else
            {
                b1 = edge1.Bot.X - edge1.Bot.Y * edge1.Dx;
                b2 = edge2.Bot.X - edge2.Bot.Y * edge2.Dx;
                double q = (b2 - b1) / (edge1.Dx - edge2.Dx);
                ip.Y = Round(q);
                if (Math.Abs(edge1.Dx) < Math.Abs(edge2.Dx))
                    ip.X = Round(edge1.Dx * q + b1);
                else
                    ip.X = Round(edge2.Dx * q + b2);
            }
            return ip;
        }
        //------------------------------------------------------------------------------

        private void SetDx(Active e)
        {
            long dy = (e.Top.Y - e.Bot.Y);
            e.Dx = (dy == 0 ? horizontal : (double)(e.Top.X - e.Bot.X) / dy);
        }
        //---------------------------------------------------------------------------

        private Vertex NextVertex(Active e)
        {
            return (e.WindDx > 0 ? e.VertTop.Next : e.VertTop.Prev);
        }
        //------------------------------------------------------------------------------

        private bool IsMaxima(Active e)
        {
            return (VertexFlags.LocMax & e.VertTop.Flags) != 0;
        }
        //------------------------------------------------------------------------------

        internal Active GetMaximaPair(Active e)
        {
            Active e2;
            if (IsHorizontal(e))
            {
                //we can't be sure whether the MaximaPair is on the left or right, so ...
                e2 = e.PrevInAEL;
                while (e2 != null && e2.Curr.X >= e.Top.X)
                {
                    if (e2.VertTop == e.VertTop) return e2;  //Found!
                    e2 = e2.PrevInAEL;
                }
                e2 = e.NextInAEL;
                while (e2 != null && TopX(e2, e.Top.Y) <= e.Top.X)
                {
                    if (e2.VertTop == e.VertTop) return e2;  //Found!
                    e2 = e2.NextInAEL;
                }
            }
            else
            {
                e2 = e.NextInAEL;
                while (e2 != null)
                {
                    if (e2.VertTop == e.VertTop) return e2; //Found!
                    e2 = e2.NextInAEL;
                }
            }
            return null;
        }
        //------------------------------------------------------------------------------

        public void Clear()
        {
            LocMinimaList.Clear();
            CurrentLocMinIdx = 0;
            VertexList.Clear();
            HasOpenPaths = false;
        }
        //------------------------------------------------------------------------------

        public void CleanUp()
        {
            while (Actives != null) DeleteFromAEL(Actives);
            DisposeScanLineList();
            OutRecList.Clear();
        }
        //------------------------------------------------------------------------------

        private void Reset()
        {
            if (!LocMinListSorted)
            {
                LocMinimaList.Sort(LocalMinimaComparer);
                LocMinListSorted = true;
            }
            foreach (LocalMinima locMin in LocMinimaList)
                InsertScanline(locMin.Vertex.Pt.Y);
            CurrentLocMinIdx = 0;
            Actives = null;
            SEL = null;
        }
        //------------------------------------------------------------------------------

        private void InsertScanline(long Y)
        {
            //single-linked list: sorted descending, ignoring dups.
            if (Scanline == null)
            {
                Scanline = new ScanLine();
                Scanline.Next = null;
                Scanline.Y = Y;
            }
            else if (Y > Scanline.Y)
            {
                ScanLine newSb = new ScanLine();
                newSb.Y = Y;
                newSb.Next = Scanline;
                Scanline = newSb;
            }
            else
            {
                ScanLine sb2 = Scanline;
                while (sb2.Next != null && (Y <= sb2.Next.Y)) sb2 = sb2.Next;
                if (Y == sb2.Y) return; //ie ignores duplicates
                ScanLine newSb = new ScanLine();
                newSb.Y = Y;
                newSb.Next = sb2.Next;
                sb2.Next = newSb;
            }
        }
        //------------------------------------------------------------------------------

        internal bool PopScanline(out long Y)
        {
            if (Scanline == null)
            {
                Y = 0;
                return false;
            }
            Y = Scanline.Y;
            ScanLine tmp = Scanline.Next;
            Scanline = null;
            Scanline = tmp;
            return true;
        }
        //------------------------------------------------------------------------------

        private void DisposeScanLineList()
        {
            while (Scanline != null)
            {
                ScanLine tmp = Scanline.Next;
                Scanline = null;
                Scanline = tmp;
            }
        }
        //------------------------------------------------------------------------------

        private bool PopLocalMinima(long Y, out LocalMinima locMin)
        {
            locMin = null;
            if (CurrentLocMinIdx == LocMinimaList.Count) return false;
            locMin = LocMinimaList[CurrentLocMinIdx];
            if (locMin.Vertex.Pt.Y == Y)
            {
                CurrentLocMinIdx++;
                return true;
            }
            return false;
        }
        //------------------------------------------------------------------------------

        private void AddLocMin(Vertex vert, PathType pt, bool isOpen)
        {
            //make sure the vertex is added only once ...
            if ((VertexFlags.LocMin & vert.Flags) != 0) return;
            vert.Flags |= VertexFlags.LocMin;
            LocalMinima lm = new LocalMinima();
            lm.Vertex = vert;
            lm.PolyType = pt;
            lm.IsOpen = isOpen;
            LocMinimaList.Add(lm);
        }
        //----------------------------------------------------------------------------

        private void AddPathToVertexList(Path p, PathType pt, bool isOpen)
        {
            int pathLen = p.Count;
            while (pathLen > 1 && p[pathLen - 1] == p[0]) pathLen--;
            if (pathLen < 2) return;

            bool P0IsMinima = false;
            bool P0IsMaxima = false;
            bool goingUp = false;
            int i = 1;
            //find the first non-horizontal segment in the path ...
            while (i < pathLen && p[i].Y == p[0].Y) i++;
            if (i == pathLen) //it's a totally flat path
            {
                if (!isOpen) return;       //Ignore closed paths that have ZERO area.
            }
            else
            {
                goingUp = p[i].Y < p[0].Y; //because I'm using an inverted Y-axis display
                if (goingUp)
                {
                    i = pathLen - 1;
                    while (p[i].Y == p[0].Y) i--;
                    P0IsMinima = p[i].Y < p[0].Y; //p[0].Y == a minima
                }
                else
                {
                    i = pathLen - 1;
                    while (p[i].Y == p[0].Y) i--;
                    P0IsMaxima = p[i].Y > p[0].Y; //p[0].Y == a maxima
                }
            }

            List<Vertex> va = new List<Vertex>(pathLen);
            VertexList.Add(va);
            Vertex v = new Vertex(p[0]);
            if (isOpen)
            {
                v.Flags = VertexFlags.OpenStart;
                if (goingUp) AddLocMin(v, pt, isOpen);
                else v.Flags |= VertexFlags.LocMax;
            };
            va.Add(v);
            //nb: polygon orientation is determined later (see InsertLocalMinimaIntoAEL).
            for (int j = 1; j < pathLen; j++)
            {
                if (p[j] == v.Pt) continue; //ie skips duplicates
                Vertex v2 = new Vertex(p[j]);
                v.Next = v2;
                v2.Prev = v;
                if (v2.Pt.Y > v.Pt.Y && goingUp)
                {
                    v.Flags |= VertexFlags.LocMax;
                    goingUp = false;
                }
                else if (v2.Pt.Y < v.Pt.Y && !goingUp)
                {
                    goingUp = true;
                    AddLocMin(v, pt, isOpen);
                }
                va.Add(v2);
                v = v2;
            }
            //i: index of the last vertex in the path.
            v.Next = va[0];
            va[0].Prev = v;

            if (isOpen)
            {
                v.Flags |= VertexFlags.OpenEnd;
                if (goingUp) v.Flags |= VertexFlags.LocMax;
                else AddLocMin(v, pt, isOpen);
            }
            else if (goingUp)
            {
                //going up so find local maxima ...
                while (v.Next.Pt.Y <= v.Pt.Y) v = v.Next;
                v.Flags |= VertexFlags.LocMax;
                if (P0IsMinima) AddLocMin(va[0], pt, isOpen); //ie just turned to going up
            }
            else
            {
                //going down so find local minima ...
                while (v.Next.Pt.Y >= v.Pt.Y) v = v.Next;
                AddLocMin(v, pt, isOpen);
                if (P0IsMaxima) va[0].Flags |= VertexFlags.LocMax;
            }
        }
        //------------------------------------------------------------------------------

        public void AddPath(Path path, PathType pt, bool isOpen = false)
        {
            if (isOpen)
            {
                if (pt == PathType.Clip)
                    throw new ClipperException("AddPath: Only PolyType.Subject paths can be open.");
                HasOpenPaths = true;
            }
            AddPathToVertexList(path, pt, isOpen);
            LocMinListSorted = false;
        }
        //------------------------------------------------------------------------------

        public void AddPaths(Paths paths, PathType pt, bool isOpen = false)
        {
            foreach (Path path in paths) AddPath(path, pt, isOpen);
        }
        //------------------------------------------------------------------------------

        private PathType GetPolyType(Active e)
        {
            return e.LocalMin.PolyType;
        }
        //------------------------------------------------------------------------------

        private bool IsSamePolyType(Active e1, Active e2)
        {
            return (e1.LocalMin.PolyType == e2.LocalMin.PolyType);
        }
        //------------------------------------------------------------------------------

        private bool IsContributingClosed(Active e)
        {
            switch (this.FillType)
            {
                case FillRule.NonZero:
                    if (Math.Abs(e.WindCnt) != 1) return false;
                    break;
                case FillRule.Positive:
                    if (e.WindCnt != 1) return false;
                    break;
                case FillRule.Negative:
                    if (e.WindCnt != -1) return false;
                    break;
            }

            switch (this.ClipType)
            {
                case ClipType.Intersection:
                    switch (this.FillType)
                    {
                        case FillRule.EvenOdd:
                        case FillRule.NonZero:
                            return (e.WindCnt2 != 0);
                        case FillRule.Positive:
                            return (e.WindCnt2 > 0);
                        case FillRule.Negative:
                            return (e.WindCnt2 < 0);
                    }
                    break;
                case ClipType.Union:
                    switch (this.FillType)
                    {
                        case FillRule.EvenOdd:
                        case FillRule.NonZero:
                            return (e.WindCnt2 == 0);
                        case FillRule.Positive:
                            return (e.WindCnt2 <= 0);
                        case FillRule.Negative:
                            return (e.WindCnt2 >= 0);
                    }
                    break;
                case ClipType.Difference:
                    if (GetPolyType(e) == PathType.Subject)
                        switch (this.FillType)
                        {
                            case FillRule.EvenOdd:
                            case FillRule.NonZero:
                                return (e.WindCnt2 == 0);
                            case FillRule.Positive:
                                return (e.WindCnt2 <= 0);
                            case FillRule.Negative:
                                return (e.WindCnt2 >= 0);
                        }
                    else
                        switch (this.FillType)
                        {
                            case FillRule.EvenOdd:
                            case FillRule.NonZero:
                                return (e.WindCnt2 != 0);
                            case FillRule.Positive:
                                return (e.WindCnt2 > 0);
                            case FillRule.Negative:
                                return (e.WindCnt2 < 0);
                        }; break;
                case ClipType.Xor:
                    return true; //XOr is always contributing unless open
            }
            return false; //we never get here but this stops a compiler issue.
        }
        //------------------------------------------------------------------------------

        private bool IsContributingOpen(Active e)
        {
            switch (this.ClipType)
            {
                case ClipType.Intersection: return (e.WindCnt2 != 0);
                case ClipType.Union: return (e.WindCnt == 0 && e.WindCnt2 == 0);
                case ClipType.Difference: return (e.WindCnt2 == 0);
                case ClipType.Xor: return (e.WindCnt != 0) != (e.WindCnt2 != 0);
            }
            return false; //stops compiler error
        }
        //------------------------------------------------------------------------------

        internal static bool IsOdd(int val)
        {
            return (val % 2 != 0);
        }
        //------------------------------------------------------------------------------

        private void SetWindingLeftEdgeOpen(Active e)
        {
            Active e2 = Actives;
            if (FillType == FillRule.EvenOdd)
            {
                int cnt1 = 0, cnt2 = 0;
                while (e2 != e)
                {
                    if (GetPolyType(e2) == PathType.Clip) cnt2++;
                    else if (!IsOpen(e2)) cnt1++;
                    e2 = e2.NextInAEL;
                }
                e.WindCnt = (IsOdd(cnt1) ? 1 : 0);
                e.WindCnt2 = (IsOdd(cnt2) ? 1 : 0);
            }
            else
            {
                //if FClipType in [ctUnion, ctDifference] then e.WindCnt := e.WindDx;
                while (e2 != e)
                {
                    if (GetPolyType(e2) == PathType.Clip) e.WindCnt2 += e2.WindDx;
                    else if (!IsOpen(e2)) e.WindCnt += e2.WindDx;
                    e2 = e2.NextInAEL;
                }
            }
        }
        //------------------------------------------------------------------------------

        private void SetWindingLeftEdgeClosed(Active leftE)
        {
            //Wind counts generally refer to polygon regions not edges, so here an edge's
            //WindCnt indicates the higher of the two wind counts of the regions touching
            //the edge. (Note also that adjacent region wind counts only ever differ
            //by one, and open paths have no meaningful wind directions or counts.)

            Active e = leftE.PrevInAEL;
            //find the nearest closed path edge of the same PathType in AEL (heading left)
            PathType pt = GetPolyType(leftE);
            while (e != null && (GetPolyType(e) != pt || IsOpen(e))) e = e.PrevInAEL;

            if (e == null)
            {
                leftE.WindCnt = leftE.WindDx;
                e = Actives;
            }
            else if (FillType == FillRule.EvenOdd)
            {
                leftE.WindCnt = leftE.WindDx;
                leftE.WindCnt2 = e.WindCnt2;
                e = e.NextInAEL;
            }
            else
            {
                //NonZero, Positive, or Negative filling here ...
                //if e's WindCnt is in the SAME direction as its WindDx, then e is either
                //an outer left or a hole right boundary, so leftE must be inside 'e'.
                //(neither e.WindCnt nor e.WindDx should ever be 0)
                if (e.WindCnt * e.WindDx < 0)
                {
                    //opposite directions so leftE is outside 'e' ...
                    if (Math.Abs(e.WindCnt) > 1)
                    {
                        //outside prev poly but still inside another.
                        if (e.WindDx * leftE.WindDx < 0)
                            //reversing direction so use the same WC
                            leftE.WindCnt = e.WindCnt;
                        else
                            //otherwise keep 'reducing' the WC by 1 (ie towards 0) ...
                            leftE.WindCnt = e.WindCnt + leftE.WindDx;
                    }
                    else
                        //now outside all polys of same polytype so set own WC ...
                        leftE.WindCnt = (IsOpen(leftE) ? 1 : leftE.WindDx);
                }
                else
                {
                    //leftE must be inside 'e'
                    if (e.WindDx * leftE.WindDx < 0)
                        //reversing direction so use the same WC
                        leftE.WindCnt = e.WindCnt;
                    else
                        //otherwise keep 'increasing' the WC by 1 (ie away from 0) ...
                        leftE.WindCnt = e.WindCnt + leftE.WindDx;
                };
                leftE.WindCnt2 = e.WindCnt2;
                e = e.NextInAEL; //ie get ready to calc WindCnt2
            }

            //update WindCnt2 ...
            if (FillType == FillRule.EvenOdd)
                while (e != leftE)
                {
                    if (GetPolyType(e) != pt && !IsOpen(e))
                        leftE.WindCnt2 = (leftE.WindCnt2 == 0 ? 1 : 0);
                    e = e.NextInAEL;
                }
            else
                while (e != leftE)
                {
                    if (GetPolyType(e) != pt && !IsOpen(e))
                        leftE.WindCnt2 += e.WindDx;
                    e = e.NextInAEL;
                }
        }
        //------------------------------------------------------------------------------

        private void InsertEdgeIntoAEL(Active edge, Active startEdge, bool preferLeft)
        {
            if (Actives == null)
            {
                edge.PrevInAEL = null;
                edge.NextInAEL = null;
                Actives = edge;
            }
            else if (startEdge == null &&
              E2InsertsBeforeE1(Actives, edge, preferLeft))
            {
                edge.PrevInAEL = null;
                edge.NextInAEL = Actives;
                Actives.PrevInAEL = edge;
                Actives = edge;
            }
            else
            {
                if (startEdge == null) startEdge = Actives;
                while (startEdge.NextInAEL != null &&
                  !E2InsertsBeforeE1(startEdge.NextInAEL, edge, preferLeft))
                {
                    startEdge = startEdge.NextInAEL;
                    preferLeft = false; //if there's one intervening then allow all
                }
                edge.NextInAEL = startEdge.NextInAEL;
                if (startEdge.NextInAEL != null)
                    startEdge.NextInAEL.PrevInAEL = edge;
                edge.PrevInAEL = startEdge;
                startEdge.NextInAEL = edge;
            }
        }
        //----------------------------------------------------------------------

        private void InsertLocalMinimaIntoAEL(long BotY)
        {
            LocalMinima locMin;
            Active leftB, rightB;
            //Add any local minima at BotY ...
            while (PopLocalMinima(BotY, out locMin))
            {
                if ((locMin.Vertex.Flags & VertexFlags.OpenStart) > 0)
                {
                    leftB = null;
                }
                else
                {
                    leftB = new Active();
                    leftB.Bot = locMin.Vertex.Pt;
                    leftB.Curr = leftB.Bot;
                    leftB.VertTop = locMin.Vertex.Prev; //ie descending
                    leftB.Top = leftB.VertTop.Pt;
                    leftB.WindDx = -1;
                    leftB.LocalMin = locMin;
                    SetDx(leftB);
                }

                if ((locMin.Vertex.Flags & VertexFlags.OpenEnd) > 0)
                {
                    rightB = null;
                }
                else
                {
                    rightB = new Active();
                    rightB.Bot = locMin.Vertex.Pt;
                    rightB.Curr = rightB.Bot;
                    rightB.VertTop = locMin.Vertex.Next; //ie ascending
                    rightB.Top = rightB.VertTop.Pt;
                    rightB.WindDx = 1;
                    rightB.LocalMin = locMin;
                    SetDx(rightB);
                }

                //Currently LeftB is just the descending bound and RightB is the ascending.
                //Now if the LeftB isn't on the left of RightB then we need swap them.
                if (leftB != null && rightB != null)
                {
                    if ((IsHorizontal(leftB) && leftB.Top.X > leftB.Bot.X) ||
                      (!IsHorizontal(leftB) && leftB.Dx < rightB.Dx))
                    {
                        Active tmp = leftB;
                        leftB = rightB;
                        rightB = tmp;
                    }
                }
                else if (leftB == null)
                {
                    leftB = rightB;
                    rightB = null;
                }

                bool contributing;
                InsertEdgeIntoAEL(leftB, null, false);      //insert left edge
                if (IsOpen(leftB))
                {
                    SetWindingLeftEdgeOpen(leftB);
                    contributing = IsContributingOpen(leftB);
                }
                else
                {
                    SetWindingLeftEdgeClosed(leftB);
                    contributing = IsContributingClosed(leftB);
                }

                if (rightB != null)
                {
                    rightB.WindCnt = leftB.WindCnt;
                    rightB.WindCnt2 = leftB.WindCnt2;
                    InsertEdgeIntoAEL(rightB, leftB, false); //insert right edge
                    if (contributing)
                        AddLocalMinPoly(leftB, rightB, leftB.Bot);

                    if (IsHorizontal(rightB))
                        PushHorz(rightB);
                    else
                        InsertScanline(rightB.Top.Y);
                }
                else if (contributing)
                    StartOpenPath(leftB, leftB.Bot);

                if (IsHorizontal(leftB))
                    PushHorz(leftB);
                else
                    InsertScanline(leftB.Top.Y);

                if (rightB != null && leftB.NextInAEL != rightB)
                {
                    //intersect edges that are between left and right bounds ...
                    Active e = leftB.NextInAEL;
                    while (e != rightB)
                    {
                        //nb: For calculating winding counts etc, IntersectEdges() assumes
                        //that rightB will be to the right of e ABOVE the intersection ...
                        IntersectEdges(rightB, e, rightB.Bot);
                        e = e.NextInAEL;
                    }
                }
            }
        }
        //------------------------------------------------------------------------------

        private void SetOutrecClockwise(OutRec outRec, Active e1, Active e2)
        {
            outRec.StartE = e1;
            outRec.EndE = e2;
            e1.OutRec = outRec;
            e2.OutRec = outRec;
        }
        //------------------------------------------------------------------------------

        private void SetOutrecCounterClockwise(OutRec outRec, Active e1, Active e2)
        {
            outRec.StartE = e2;
            outRec.EndE = e1;
            e1.OutRec = outRec;
            e2.OutRec = outRec;
        }
        //------------------------------------------------------------------------------

        private OutRec GetOwner(Active e)
        {
            if (IsHorizontal(e) && e.Top.X < e.Bot.X)
            {
                e = e.NextInAEL;
                while (e != null && (!IsHotEdge(e) || IsOpen(e)))
                    e = e.NextInAEL;
                if (e == null) return null;
                else if (((e.OutRec.Flags & OutrecFlags.Outer) != 0) == (e.OutRec.StartE == e))
                    return e.OutRec.Owner;
                else return e.OutRec;
            }
            else
            {
                e = e.PrevInAEL;
                while (e != null && (!IsHotEdge(e) || IsOpen(e)))
                    e = e.PrevInAEL;
                if (e == null) return null;
                else if (((e.OutRec.Flags & OutrecFlags.Outer) != 0) == (e.OutRec.EndE == e))
                    return e.OutRec.Owner;
                else return e.OutRec;
            }
        }
        //------------------------------------------------------------------------------

        private void AddLocalMinPoly(Active e1, Active e2, Point64 pt)
        {
            OutRec outRec = new OutRec();
            outRec.IDx = OutRecList.Count;
            OutRecList.Add(outRec);
            outRec.Owner = GetOwner(e1);
            if (outRec.Owner != null && (outRec.Owner.Flags & OutrecFlags.Outer) != 0)
                outRec.Flags = 0;
            else
                outRec.Flags |= OutrecFlags.Outer;
            if (IsOpen(e1)) outRec.Flags |= OutrecFlags.Open;
            outRec.PolyPath = null;

            //now set orientation ...
            if (IsHorizontal(e1))
            {
                if (IsHorizontal(e2))
                {
                    if (((outRec.Flags & OutrecFlags.Outer) != 0) == (e1.Bot.X > e2.Bot.X))
                        SetOutrecClockwise(outRec, e1, e2);
                    else SetOutrecCounterClockwise(outRec, e1, e2);
                }
                else if (((outRec.Flags & OutrecFlags.Outer) != 0) == (e1.Top.X < e1.Bot.X))
                    SetOutrecClockwise(outRec, e1, e2);
                else SetOutrecCounterClockwise(outRec, e1, e2);
            }
            else if (IsHorizontal(e2))
            {
                if (((outRec.Flags & OutrecFlags.Outer) != 0) == (e2.Top.X > e2.Bot.X))
                    SetOutrecClockwise(outRec, e1, e2);
                else SetOutrecCounterClockwise(outRec, e1, e2);
            }
            else if (((outRec.Flags & OutrecFlags.Outer) != 0) == (e1.Dx >= e2.Dx))
                SetOutrecClockwise(outRec, e1, e2);
            else
                SetOutrecCounterClockwise(outRec, e1, e2);

            OutPt op = new OutPt();
            op.Pt = pt;
            op.Next = op;
            op.Prev = op;
            outRec.Pts = op;
        }
        //------------------------------------------------------------------------------

        private void EndOutRec(OutRec outRec)
        {
            outRec.StartE.OutRec = null;
            if (outRec.EndE != null) outRec.EndE.OutRec = null;
            outRec.StartE = null;
            outRec.EndE = null;
        }
        //------------------------------------------------------------------------------

        private void AddLocalMaxPoly(Active e1, Active e2, Point64 Pt)
        {
            if (!IsHotEdge(e2))
                throw new ClipperException("Error in AddLocalMaxPoly().");

            AddOutPt(e1, Pt);
            if (e1.OutRec == e2.OutRec) EndOutRec(e1.OutRec);
            //and to preserve the winding orientation of Outrec ...
            else if (e1.OutRec.IDx < e2.OutRec.IDx)
                JoinOutrecPaths(e1, e2);
            else
                JoinOutrecPaths(e2, e1);
        }
        //------------------------------------------------------------------------------

        private void ReversePolyPtLinks(OutPt op)
        {
            if (op.Next == op.Prev) return;
            OutPt pp1 = op, pp2;
            do
            {
                pp2 = pp1.Next;
                pp1.Next = pp1.Prev;
                pp1.Prev = pp2;
                pp1 = pp2;
            }
            while (pp1 != op);
        }
        //------------------------------------------------------------------------------

        private void JoinOutrecPaths(Active e1, Active e2)
        {
            OutPt P1_st, P1_end, P2_st, P2_end;

            //join E2 outrec path onto E1 outrec path and then delete E2 outrec path
            //pointers. (nb: Only very rarely do the joining ends share the same coords.)
            P1_st = e1.OutRec.Pts;
            P2_st = e2.OutRec.Pts;
            P1_end = P1_st.Prev;
            P2_end = P2_st.Prev;
            if (IsStartSide(e1))
            {
                if (IsStartSide(e2))
                {
                    //start-start join
                    ReversePolyPtLinks(P2_st);
                    P2_st.Next = P1_st;
                    P1_st.Prev = P2_st;
                    P1_end.Next = P2_end; //P2 now reversed
                    P2_end.Prev = P1_end;
                    e1.OutRec.Pts = P2_end;
                    e1.OutRec.StartE = e2.OutRec.EndE;
                }
                else
                {
                    //}-start join
                    P2_end.Next = P1_st;
                    P1_st.Prev = P2_end;
                    P2_st.Prev = P1_end;
                    P1_end.Next = P2_st;
                    e1.OutRec.Pts = P2_st;
                    e1.OutRec.StartE = e2.OutRec.StartE;
                }
                if (e1.OutRec.StartE != null) //ie closed path
                    e1.OutRec.StartE.OutRec = e1.OutRec;
            }
            else
            {
                if (IsStartSide(e2))
                {
                    //}-start join (see JoinOutrec3.png)
                    P1_end.Next = P2_st;
                    P2_st.Prev = P1_end;
                    P1_st.Prev = P2_end;
                    P2_end.Next = P1_st;
                    e1.OutRec.EndE = e2.OutRec.EndE;
                }
                else
                {
                    //}-} join (see JoinOutrec4.png)
                    ReversePolyPtLinks(P2_st);
                    P1_end.Next = P2_end; //P2 now reversed
                    P2_end.Prev = P1_end;
                    P2_st.Next = P1_st;
                    P1_st.Prev = P2_st;
                    e1.OutRec.EndE = e2.OutRec.StartE;
                }
                if (e1.OutRec.EndE != null) //ie closed path
                    e1.OutRec.EndE.OutRec = e1.OutRec;
            }

            if (e1.OutRec.Owner == e2.OutRec)
                throw new ClipperException("Clipping error in JoinOuterPaths.");

            //after joining, the E2.OutRec contains not vertices ...
            e2.OutRec.StartE = null;
            e2.OutRec.EndE = null;
            e2.OutRec.Pts = null;
            e2.OutRec.Owner = e1.OutRec; //this may be redundant

            //and e1 and e2 are maxima and are about to be dropped from the Actives list.
            e1.OutRec = null;
            e2.OutRec = null;
        }
        //------------------------------------------------------------------------------

        private void PushHorz(Active e)
        {
            e.NextInSEL = (SEL != null ? SEL : null);
            SEL = e;
        }
        //------------------------------------------------------------------------------

        private bool PopHorz(out Active e)
        {
            e = SEL;
            if (e == null) return false;
            SEL = SEL.NextInSEL;
            return true;
        }
        //------------------------------------------------------------------------------

        private void StartOpenPath(Active e, Point64 pt)
        {
            OutRec outRec = new OutRec();
            outRec.IDx = OutRecList.Count;
            OutRecList.Add(outRec);
            outRec.Flags = OutrecFlags.Open;
            e.OutRec = outRec;

            OutPt op = new OutPt();
            op.Pt = pt;
            op.Next = op;
            op.Prev = op;
            outRec.Pts = op;
        }
        //------------------------------------------------------------------------------

        private void TerminateHotOpen(Active e)
        {
            if (e.OutRec.StartE == e)
                e.OutRec.StartE = null;
            else
                e.OutRec.EndE = null;
            e.OutRec = null;
        }
        //------------------------------------------------------------------------------

        private void SwapOutrecs(Active e1, Active e2)
        {
            OutRec or1 = e1.OutRec;
            OutRec or2 = e2.OutRec;
            if (or1 == or2)
            {
                Active e = or1.StartE;
                or1.StartE = or1.EndE;
                or1.EndE = e;
                return;
            }
            if (or1 != null)
            {
                if (e1 == or1.StartE)
                    or1.StartE = e2;
                else
                    or1.EndE = e2;
            }
            if (or2 != null)
            {
                if (e2 == or2.StartE)
                    or2.StartE = e1;
                else
                    or2.EndE = e1;
            }
            e1.OutRec = or2;
            e2.OutRec = or1;
        }
        //------------------------------------------------------------------------------

        private void AddOutPt(Active e, Point64 pt)
        {

            //Outrec.Pts: a circular double-linked-list of POutPt.
            bool toStart = IsStartSide(e);
            OutPt opStart = e.OutRec.Pts;
            OutPt opEnd = opStart.Prev;
            if (toStart)
            {
                if (pt == opStart.Pt) return;
            }
            else if (pt == opEnd.Pt) return;

            OutPt opNew = new OutPt();
            opNew.Pt = pt;
            opNew.Next = opStart;
            opNew.Prev = opEnd;
            opEnd.Next = opNew;
            opStart.Prev = opNew;
            if (toStart) e.OutRec.Pts = opNew;
        }
        //------------------------------------------------------------------------------

        private void UpdateEdgeIntoAEL(ref Active e)
        {
            e.Bot = e.Top;
            e.VertTop = NextVertex(e);
            e.Top = e.VertTop.Pt;
            e.Curr = e.Bot;
            SetDx(e);
            if (!IsHorizontal(e)) InsertScanline(e.Top.Y);
        }
        //------------------------------------------------------------------------------

        private void IntersectEdges(Active e1, Active e2, Point64 pt)
        {

            e1.Curr = pt;
            e2.Curr = pt;

            //if either edge is an OPEN path ...
            if (HasOpenPaths && (IsOpen(e1) || IsOpen(e2)))
            {
                if (IsOpen(e1) && IsOpen(e2)) return; //ignore lines that intersect
                                                      //the following line just avoids duplicating a whole lot of code ...
                if (IsOpen(e2)) SwapActive(ref e1, ref e2);
                switch (ClipType)
                {
                    case ClipType.Intersection:
                    case ClipType.Difference:
                        if (IsSamePolyType(e1, e2) || (Math.Abs(e2.WindCnt) != 1)) return;
                        break;
                    case ClipType.Union:
                        if (IsHotEdge(e1) != ((Math.Abs(e2.WindCnt) != 1) ||
                          (IsHotEdge(e1) != (e2.WindCnt2 != 0)))) return; //just works!
                        break;
                    case ClipType.Xor:
                        if (Math.Abs(e2.WindCnt) != 1) return;
                        break;
                }
                //toggle contribution ...
                if (IsHotEdge(e1))
                {
                    AddOutPt(e1, pt);
                    TerminateHotOpen(e1);
                }
                else StartOpenPath(e1, pt);
                return;
            }

            //update winding counts...
            //assumes that e1 will be to the right of e2 ABOVE the intersection
            int oldE1WindCnt, oldE2WindCnt;
            if (e1.LocalMin.PolyType == e2.LocalMin.PolyType)
            {
                if (FillType == FillRule.EvenOdd)
                {
                    oldE1WindCnt = e1.WindCnt;
                    e1.WindCnt = e2.WindCnt;
                    e2.WindCnt = oldE1WindCnt;
                }
                else
                {
                    if (e1.WindCnt + e2.WindDx == 0) e1.WindCnt = -e1.WindCnt;
                    else e1.WindCnt += e2.WindDx;
                    if (e2.WindCnt - e1.WindDx == 0) e2.WindCnt = -e2.WindCnt;
                    else e2.WindCnt -= e1.WindDx;
                }
            }
            else
            {
                if (FillType != FillRule.EvenOdd) e1.WindCnt2 += e2.WindDx;
                else e1.WindCnt2 = (e1.WindCnt2 == 0) ? 1 : 0;
                if (FillType != FillRule.EvenOdd) e2.WindCnt2 -= e1.WindDx;
                else e2.WindCnt2 = (e2.WindCnt2 == 0) ? 1 : 0;
            }

            switch (FillType)
            {
                case FillRule.Positive:
                    oldE1WindCnt = e1.WindCnt;
                    oldE2WindCnt = e2.WindCnt;
                    break;
                case FillRule.Negative:
                    oldE1WindCnt = -e1.WindCnt;
                    oldE2WindCnt = -e2.WindCnt;
                    break;
                default:
                    oldE1WindCnt = Math.Abs(e1.WindCnt);
                    oldE2WindCnt = Math.Abs(e2.WindCnt);
                    break;
            }

            if (IsHotEdge(e1) && IsHotEdge(e2))
            {
                if ((oldE1WindCnt != 0 && oldE1WindCnt != 1) || (oldE2WindCnt != 0 && oldE2WindCnt != 1) ||
                  (e1.LocalMin.PolyType != e2.LocalMin.PolyType && ClipType != ClipType.Xor))
                {
                    AddLocalMaxPoly(e1, e2, pt);
                }
                else if (e1.OutRec == e2.OutRec) //optional
                {
                    AddLocalMaxPoly(e1, e2, pt);
                    AddLocalMinPoly(e1, e2, pt);
                }
                else
                {
                    AddOutPt(e1, pt);
                    AddOutPt(e2, pt);
                    SwapOutrecs(e1, e2);
                }
            }
            else if (IsHotEdge(e1))
            {
                if (oldE2WindCnt == 0 || oldE2WindCnt == 1)
                {
                    AddOutPt(e1, pt);
                    SwapOutrecs(e1, e2);
                }
            }
            else if (IsHotEdge(e2))
            {
                if (oldE1WindCnt == 0 || oldE1WindCnt == 1)
                {
                    AddOutPt(e2, pt);
                    SwapOutrecs(e1, e2);
                }
            }
            else if ((oldE1WindCnt == 0 || oldE1WindCnt == 1) &&
              (oldE2WindCnt == 0 || oldE2WindCnt == 1))
            {
                //neither edge is currently contributing ...
                long e1Wc2, e2Wc2;
                switch (FillType)
                {
                    case FillRule.Positive:
                        e1Wc2 = e1.WindCnt2;
                        e2Wc2 = e2.WindCnt2;
                        break;
                    case FillRule.Negative:
                        e1Wc2 = -e1.WindCnt2;
                        e2Wc2 = -e2.WindCnt2;
                        break;
                    default:
                        e1Wc2 = Math.Abs(e1.WindCnt2);
                        e2Wc2 = Math.Abs(e2.WindCnt2);
                        break;
                }

                if (e1.LocalMin.PolyType != e2.LocalMin.PolyType)
                {
                    AddLocalMinPoly(e1, e2, pt);
                }
                else if (oldE1WindCnt == 1 && oldE2WindCnt == 1)
                    switch (ClipType)
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
                            if (((GetPolyType(e1) == PathType.Clip) && (e1Wc2 > 0) && (e2Wc2 > 0)) ||
                                ((GetPolyType(e1) == PathType.Subject) && (e1Wc2 <= 0) && (e2Wc2 <= 0)))
                                AddLocalMinPoly(e1, e2, pt);
                            break;
                        case ClipType.Xor:
                            AddLocalMinPoly(e1, e2, pt);
                            break;
                    }
            }
        }
        //------------------------------------------------------------------------------

        private void DeleteFromAEL(Active e)
        {
            Active AelPrev = e.PrevInAEL;
            Active AelNext = e.NextInAEL;
            if (AelPrev == null && AelNext == null && (e != Actives))
                return; //already deleted
            if (AelPrev != null) AelPrev.NextInAEL = AelNext;
            else Actives = AelNext;
            if (AelNext != null)
                AelNext.PrevInAEL = AelPrev;
            e.NextInAEL = null;
            e.PrevInAEL = null;
        }
        //------------------------------------------------------------------------------

        private void CopyAELToSEL()
        {
            Active e = Actives;
            SEL = e;
            while (e != null)
            {
                e.PrevInSEL = e.PrevInAEL;
                e.NextInSEL = e.NextInAEL;
                e = e.NextInAEL;
            }
        }
        //------------------------------------------------------------------------------

        private void CopyActivesToSELAdjustCurrX(long topY)
        {
            Active e = Actives;
            SEL = e;
            while (e != null)
            {
                e.PrevInSEL = e.PrevInAEL;
                e.NextInSEL = e.NextInAEL;
                e.Curr.X = TopX(e, topY);
                e = e.NextInAEL;
            }
        }
        //------------------------------------------------------------------------------

        private bool ExecuteInternal(ClipType ct, FillRule ft)
        {
            if (ExecuteLocked) return false;
            try
            {
                ExecuteLocked = true;
                FillType = ft;
                ClipType = ct;
                Reset();
                long Y;
                Active e;
                if (!PopScanline(out Y)) return false;

                while (true) /////////////////////////////////////////////
                {
                    InsertLocalMinimaIntoAEL(Y);
                    while (PopHorz(out e)) ProcessHorizontal(e);
                    if (!PopScanline(out Y)) break;   //Y is now at the top of the scanbeam
                    ProcessIntersections(Y);
                    SEL = null;                       //SEL reused to flag horizontals
                    DoTopOfScanbeam(Y);
                } ////////////////////////////////////////////////////////
            }
            finally
            { ExecuteLocked = false; }
            return true;
        }
        //------------------------------------------------------------------------------

        public bool Execute(ClipType clipType, Paths Closed, FillRule ft = FillRule.EvenOdd)
        {
            try
            {
                if (Closed == null) return false;
                Closed.Clear();
                if (!ExecuteInternal(clipType, ft)) return false;
                BuildResult(Closed, null);
                return true;
            }
            finally { CleanUp(); }
        }
        //------------------------------------------------------------------------------

        public bool Execute(ClipType clipType, Paths Closed, Paths Open, FillRule ft = FillRule.EvenOdd)
        {
            try
            {
                if (Closed == null) return false;
                Closed.Clear();
                if (Open != null) Open.Clear();
                if (!ExecuteInternal(clipType, ft)) return false;
                BuildResult(Closed, Open);
                return true;
            }
            finally { CleanUp(); }
        }
        //------------------------------------------------------------------------------

        public bool Execute(ClipType clipType, PolyTree polytree, Paths Open, FillRule ft = FillRule.EvenOdd)
        {
            try
            {
                if (polytree == null) return false;
                polytree.Clear();
                if (Open != null) Open.Clear();
                if (!ExecuteInternal(clipType, ft)) return false;
                BuildResult2(polytree, Open);
                return true;
            }
            finally { CleanUp(); }
        }
        //------------------------------------------------------------------------------

        private void ProcessIntersections(long topY)
        {
            BuildIntersectList(topY);
            if (IntersectList.Count == 0) return;
            try
            {
                FixupIntersectionOrder();
                ProcessIntersectList();
            }
            finally
            {
                IntersectList.Clear(); //clean up only needed if there's been an error
            }
        }
        //------------------------------------------------------------------------------

        private void InsertNewIntersectNode(Active e1, Active e2, long topY)
        {
            Point64 pt = GetIntersectPoint(e1, e2);

            //Rounding errors can occasionally place the calculated intersection
            //point either below or above the scanbeam, so check and correct ...
            if (pt.Y > e1.Curr.Y)
            {
                pt.Y = e1.Curr.Y;      //E.Curr.Y is still the bottom of scanbeam
                                       //use the more vertical of the 2 edges to derive pt.X ...
                if (Math.Abs(e1.Dx) < Math.Abs(e2.Dx))
                    pt.X = TopX(e1, pt.Y);
                else
                    pt.X = TopX(e2, pt.Y);
            }
            else if (pt.Y < topY)
            {
                pt.Y = topY;          //TopY = top of scanbeam

                if (e1.Top.Y == topY) pt.X = e1.Top.X;
                else if (e2.Top.Y == topY) pt.X = e2.Top.X;
                else if (Math.Abs(e1.Dx) < Math.Abs(e2.Dx)) pt.X = e1.Curr.X;
                else pt.X = e2.Curr.X;
            }

            IntersectNode node = new IntersectNode();
            node.Edge1 = e1;
            node.Edge2 = e2;
            node.Pt = pt;
            IntersectList.Add(node);
        }
        //------------------------------------------------------------------------------

        private void BuildIntersectList(long TopY)
        {
            if (Actives == null || Actives.NextInAEL == null) return;

            CopyActivesToSELAdjustCurrX(TopY);

            //Merge sort FActives into their new positions at the top of scanbeam, and
            //create an intersection node every time an edge crosses over another ...

            int mul = 1;
            while (true)
            {
                Active first = SEL, second = null, baseE, prevBase = null, tmp;

                //sort successive larger 'mul' count of nodes ...
                while (first != null)
                {
                    if (mul == 1)
                    {
                        second = first.NextInSEL;
                        if (second == null) break;
                        first.MergeJump = second.NextInSEL;
                    }
                    else
                    {
                        second = first.MergeJump;
                        if (second == null) break;
                        first.MergeJump = second.MergeJump;
                    }
         
                    //now sort first and second groups ...
                    baseE = first;
                    int lCnt = mul, rCnt = mul;
                    while (lCnt > 0 && rCnt > 0)
                    {
                        if (first == null) break;
                        if (second.Curr.X < first.Curr.X)
                        {
                            // create one or more Intersect nodes ///////////
                            tmp = second.PrevInSEL;
                            for (int i = 0; i < lCnt; ++i)
                            {
                                //create a new intersect node...
                                InsertNewIntersectNode(tmp, second, TopY);
                                tmp = tmp.PrevInSEL;
                            }
                            /////////////////////////////////////////////////

                            if (first == baseE)
                            {
                                if (prevBase != null) prevBase.MergeJump = second;
                                baseE = second;
                                baseE.MergeJump = first.MergeJump;
                                if (first.PrevInSEL == null) SEL = second;
                            }
                            tmp = second.NextInSEL;
                            //now move the out of place edge to it's new position in SEL ...
                            Insert2Before1InSel(first, second);
                            second = tmp;
                            if (second == null) break;
                            --rCnt;
                        }
                        else
                        {
                            first = first.NextInSEL;
                            --lCnt;
                        }
                    }
                    first = baseE.MergeJump;
                    if (first == null) break;
                    prevBase = baseE;
                }
                if (SEL.MergeJump == null) break;
                else mul <<= 1;
            }
        }
        //------------------------------------------------------------------------------

        private void ProcessIntersectList()
        {
            foreach (IntersectNode iNode in IntersectList)
            {
                IntersectEdges(iNode.Edge1, iNode.Edge2, iNode.Pt);
                SwapPositionsInAEL(iNode.Edge1, iNode.Edge2);
            }
            IntersectList.Clear();
        }
        //------------------------------------------------------------------------------

        private bool EdgesAdjacentInSEL(IntersectNode inode)
        {
            return (inode.Edge1.NextInSEL == inode.Edge2) ||
              (inode.Edge1.PrevInSEL == inode.Edge2);
        }
        //------------------------------------------------------------------------------

        private static int IntersectNodeSort(IntersectNode node1, IntersectNode node2)
        {
            //the following typecast should be safe because the differences in Pt.Y will
            //be limited to the height of the Scanline ...
            return (int)(node2.Pt.Y - node1.Pt.Y);
        }
        //------------------------------------------------------------------------------

        private void FixupIntersectionOrder()
        {
            int cnt = IntersectList.Count;
            if (cnt < 2) return;
            //It's important that edge intersections are processed from the bottom up,
            //but it's also crucial that intersections only occur between adjacent edges.
            //The first sort here (a quicksort), arranges intersections relative to their
            //vertical positions within the scanbeam ...
            IntersectList.Sort(IntersectNodeComparer);

            //Now we simulate processing these intersections, and as we do, we make sure
            //that the intersecting edges remain adjacent. If they aren't, this simulated
            //intersection is delayed until such time as these edges do become adjacent.
            CopyAELToSEL();
            for (int i = 0; i < cnt; i++)
            {
                if (!EdgesAdjacentInSEL(IntersectList[i]))
                {
                    int j = i + 1;
                    while (!EdgesAdjacentInSEL(IntersectList[j])) j++;
                    IntersectNode tmp = IntersectList[i];
                    IntersectList[i] = IntersectList[j];
                    IntersectList[j] = tmp;
                }
                SwapPositionsInSEL(IntersectList[i].Edge1, IntersectList[i].Edge2);
            }
        }
        //------------------------------------------------------------------------------

        internal void SwapPositionsInAEL(Active e1, Active e2)
        {
            Active next, prev;
            if (e1.NextInAEL == e2)
            {
                next = e2.NextInAEL;
                if (next != null) next.PrevInAEL = e1;
                prev = e1.PrevInAEL;
                if (prev != null) prev.NextInAEL = e2;
                e2.PrevInAEL = prev;
                e2.NextInAEL = e1;
                e1.PrevInAEL = e2;
                e1.NextInAEL = next;
                if (e2.PrevInAEL == null) Actives = e2;
            }
            else if (e2.NextInAEL == e1)
            {
                next = e1.NextInAEL;
                if (next != null) next.PrevInAEL = e2;
                prev = e2.PrevInAEL;
                if (prev != null) prev.NextInAEL = e1;
                e1.PrevInAEL = prev;
                e1.NextInAEL = e2;
                e2.PrevInAEL = e1;
                e2.NextInAEL = next;
                if (e1.PrevInAEL == null) Actives = e1;
            }
            else
                throw new ClipperException("Clipping error in SwapPositionsInAEL");
        }
        //------------------------------------------------------------------------------

        private void SwapPositionsInSEL(Active e1, Active e2)
        {
            Active next, prev;
            if (e1.NextInSEL == e2)
            {
                next = e2.NextInSEL;
                if (next != null) next.PrevInSEL = e1;
                prev = e1.PrevInSEL;
                if (prev != null) prev.NextInSEL = e2;
                e2.PrevInSEL = prev;
                e2.NextInSEL = e1;
                e1.PrevInSEL = e2;
                e1.NextInSEL = next;
                if (e2.PrevInSEL == null) SEL = e2;
            }
            else if (e2.NextInSEL == e1)
            {
                next = e1.NextInSEL;
                if (next != null) next.PrevInSEL = e2;
                prev = e2.PrevInSEL;
                if (prev != null) prev.NextInSEL = e1;
                e1.PrevInSEL = prev;
                e1.NextInSEL = e2;
                e2.PrevInSEL = e1;
                e2.NextInSEL = next;
                if (e1.PrevInSEL == null) SEL = e1;
            }
            else
                throw new ClipperException("Clipping error in SwapPositionsInSEL");
        }
        //------------------------------------------------------------------------------

        private void Insert2Before1InSel(Active first, Active second)
        {
            //remove second from list ...
            Active prev = second.PrevInSEL;
            Active next = second.NextInSEL;
            prev.NextInSEL = next; //always a prev since we're moving from right to left
            if (next != null) next.PrevInSEL = prev;
            //insert back into list ...
            prev = first.PrevInSEL;
            if (prev != null) prev.NextInSEL = second;
            first.PrevInSEL = second;
            second.PrevInSEL = prev;
            second.NextInSEL = first;
        }
        //------------------------------------------------------------------------------

        private bool ResetHorzDirection(Active horz, Active maxPair,
          out long horzLeft, out long horzRight)
        {
            if (horz.Bot.X == horz.Top.X)
            {
                //the horizontal edge is going nowhere ...
                horzLeft = horz.Curr.X;
                horzRight = horz.Curr.X;
                Active e = horz.NextInAEL;
                while (e != null && e != maxPair) e = e.NextInAEL;
                return e != null;
            }
            else if (horz.Curr.X < horz.Top.X)
            {
                horzLeft = horz.Curr.X;
                horzRight = horz.Top.X;
                return true;
            }
            else
            {
                horzLeft = horz.Top.X;
                horzRight = horz.Curr.X;
                return false; //right to left
            }
        }
        //------------------------------------------------------------------------

        private void ProcessHorizontal(Active horz)
        /*******************************************************************************
        * Notes: Horizontal edges (HEs) at scanline intersections (ie at the top or    *
        * bottom of a scanbeam) are processed as if layered.The order in which HEs     *
        * are processed doesn't matter. HEs intersect with the bottom vertices of      *
        * other HEs[#] and with non-horizontal edges [*]. Once these intersections     *
        * are completed, intermediate HEs are 'promoted' to the next edge in their     *
        * bounds, and they in turn may be intersected[%] by other HEs.                 *
        *                                                                              *
        * eg: 3 horizontals at a scanline:    /   |                     /           /  *
        *              |                     /    |     (HE3)o ========%========== o   *
        *              o ======= o(HE2)     /     |         /         /                *
        *          o ============#=========*======*========#=========o (HE1)           *
        *         /              |        /       |       /                            *
        *******************************************************************************/
        {
            Point64 pt;
            //with closed paths, simplify consecutive horizontals into a 'single' edge ...
            if (!IsOpen(horz))
            {
                pt = horz.Bot;
                while (!IsMaxima(horz) && NextVertex(horz).Pt.Y == pt.Y)
                    UpdateEdgeIntoAEL(ref horz);
                horz.Bot = pt;
                horz.Curr = pt;
            };

            Active maxPair = null;
            if (IsMaxima(horz) && (!IsOpen(horz) ||
                ((horz.VertTop.Flags & (VertexFlags.OpenStart | VertexFlags.OpenEnd)) == 0)))
                maxPair = GetMaximaPair(horz);

            long horzLeft, horzRight;
            bool isLeftToRight = ResetHorzDirection(horz, maxPair, out horzLeft, out horzRight);
            if (IsHotEdge(horz)) AddOutPt(horz, horz.Curr);

            while (true) //loops through consec. horizontal edges (if open)
            {
                Active e;
                bool isMax = IsMaxima(horz);
                if (isLeftToRight)
                    e = horz.NextInAEL;
                else
                    e = horz.PrevInAEL;

                while (e != null)
                {
                    //break if we've gone past the } of the horizontal ...
                    if ((isLeftToRight && (e.Curr.X > horzRight)) ||
                      (!isLeftToRight && (e.Curr.X < horzLeft))) break;
                    //or if we've got to the } of an intermediate horizontal edge ...
                    if (e.Curr.X == horz.Top.X && !isMax && !IsHorizontal(e))
                    {
                        pt = NextVertex(horz).Pt;
                        if (isLeftToRight && (TopX(e, pt.Y) >= pt.X) ||
                          (!isLeftToRight && (TopX(e, pt.Y) <= pt.X))) break;
                    };

                    if (e == maxPair)
                    {
                        if (IsHotEdge(horz))
                            AddLocalMaxPoly(horz, e, horz.Top);
                        DeleteFromAEL(e);
                        DeleteFromAEL(horz);
                        return;
                    };

                    if (isLeftToRight)
                    {
                        pt = new Point64(e.Curr.X, horz.Curr.Y);
                        IntersectEdges(horz, e, pt);
                    }
                    else
                    {
                        pt = new Point64(e.Curr.X, horz.Curr.Y);
                        IntersectEdges(e, horz, pt);
                    };

                    Active eNext;
                    if (isLeftToRight)
                        eNext = e.NextInAEL;
                    else
                        eNext = e.PrevInAEL;
                    SwapPositionsInAEL(horz, e);
                    e = eNext;
                }

                //check if we've finished with (consecutive) horizontals ...
                if (isMax || NextVertex(horz).Pt.Y != horz.Top.Y) break;

                //still more horizontals in bound to process ...
                UpdateEdgeIntoAEL(ref horz);
                isLeftToRight = ResetHorzDirection(horz, maxPair, out horzLeft, out horzRight);

                if (IsOpen(horz))
                {
                    if (IsMaxima(horz)) maxPair = GetMaximaPair(horz);
                    if (IsHotEdge(horz)) AddOutPt(horz, horz.Bot);
                }
            }

            if (IsHotEdge(horz)) AddOutPt(horz, horz.Top);

            if (!IsOpen(horz))
                UpdateEdgeIntoAEL(ref horz); //this is the } of an intermediate horiz.      
            else if (!IsMaxima(horz))
                UpdateEdgeIntoAEL(ref horz);
            else if (maxPair == null)      //ie open at top
                DeleteFromAEL(horz);
            else if (IsHotEdge(horz))
                AddLocalMaxPoly(horz, maxPair, horz.Top);
            else { DeleteFromAEL(maxPair); DeleteFromAEL(horz); }

        }
        //------------------------------------------------------------------------------

        private void DoTopOfScanbeam(long Y)
        {
            Active e = Actives;
            while (e != null)
            {
                //nb: E will never be horizontal at this point
                if (e.Top.Y == Y)
                {
                    e.Curr = e.Top; //needed for horizontal processing
                    if (IsMaxima(e))
                    {
                        e = DoMaxima(e); //TOP OF BOUND (MAXIMA)
                        continue;
                    }
                    else
                    {
                        //INTERMEDIATE VERTEX ...
                        UpdateEdgeIntoAEL(ref e);
                        if (IsHotEdge(e)) AddOutPt(e, e.Bot);
                        if (IsHorizontal(e))
                            PushHorz(e); //horizontals are processed later
                    }
                }
                else
                {
                    e.Curr.Y = Y;
                    e.Curr.X = TopX(e, Y);
                }
                e = e.NextInAEL;
            }
        }
        //------------------------------------------------------------------------------

        private Active DoMaxima(Active e)
        {
            Active eNext, ePrev, eMaxPair;
            ePrev = e.PrevInAEL;
            eNext = e.NextInAEL;
            if (IsOpen(e) && ((e.VertTop.Flags & (VertexFlags.OpenStart | VertexFlags.OpenEnd)) != 0))
            {
                if (IsHotEdge(e)) AddOutPt(e, e.Top);
                if (!IsHorizontal(e))
                {
                    if (IsHotEdge(e)) TerminateHotOpen(e);
                    DeleteFromAEL(e);
                }
                return eNext;
            }
            else
            {
                eMaxPair = GetMaximaPair(e);
                if (eMaxPair == null) return eNext; //eMaxPair is horizontal
            }

            //only non-horizontal maxima here.
            //process any edges between maxima pair ...
            while (eNext != eMaxPair)
            {
                IntersectEdges(e, eNext, e.Top);
                SwapPositionsInAEL(e, eNext);
                eNext = e.NextInAEL;
            }

            if (IsOpen(e))
            {
                if (IsHotEdge(e))
                {
                    if (eMaxPair != null)
                        AddLocalMaxPoly(e, eMaxPair, e.Top);
                    else
                        AddOutPt(e, e.Top);
                }
                if (eMaxPair != null)
                    DeleteFromAEL(eMaxPair);
                DeleteFromAEL(e);
                return (ePrev != null ? ePrev.NextInAEL : Actives);
            }
            //here E.NextInAEL == ENext == EMaxPair ...
            if (IsHotEdge(e))
                AddLocalMaxPoly(e, eMaxPair, e.Top);

            DeleteFromAEL(e);
            DeleteFromAEL(eMaxPair);
            return (ePrev != null ? ePrev.NextInAEL : Actives);
        }
        //------------------------------------------------------------------------------

        private int PointCount(OutPt op)
        {
            if (op == null) return 0;
            OutPt p = op;
            int cnt = 0;
            do
            {
                cnt++;
                p = p.Next;
            } while (p != op);
            return cnt;
        }
        //------------------------------------------------------------------------------

        private void BuildResult(Paths closedPaths, Paths openPaths)
        {
            closedPaths.Clear();
            closedPaths.Capacity = OutRecList.Count;
            if (openPaths != null)
            {
                openPaths.Clear();
                openPaths.Capacity = OutRecList.Count;
            }

            foreach (OutRec outrec in OutRecList)
                if (outrec.Pts != null)
                {
                    OutPt op = outrec.Pts.Prev;
                    int cnt = PointCount(op);
                    //fixup for duplicate start and } points ...
                    if (op.Pt == outrec.Pts.Pt) cnt--;

                    if ((outrec.Flags & OutrecFlags.Open) > 0)
                    {
                        if (cnt < 3 || openPaths == null) continue;
                        Path p = new Path(cnt);
                        for (int i = 0; i < cnt; i++) { p.Add(op.Pt); op = op.Prev; }
                        openPaths.Add(p);
                    }
                    else
                    {
                        if (cnt < 3) continue;
                        Path p = new Path(cnt);
                        for (int i = 0; i < cnt; i++) { p.Add(op.Pt); op = op.Prev; }
                        closedPaths.Add(p);
                    }
                }
        }
        //------------------------------------------------------------------------------

        private void BuildResult2(PolyTree pt, Paths openPaths)
        {
            if (pt == null) return;
            if (openPaths != null)
            {
                openPaths.Clear();
                openPaths.Capacity = OutRecList.Count;
            }

            foreach (OutRec outrec in OutRecList)
                if (outrec.Pts != null)
                {
                    OutPt op = outrec.Pts.Prev;
                    int cnt = PointCount(op);
                    //fixup for duplicate start and end points ...
                    if (op.Pt == outrec.Pts.Pt) cnt--;

                    if (cnt < 3)
                    {
                        if ((outrec.Flags & OutrecFlags.Open) == 0 || cnt < 2) continue;
                    }

                    Path p = new Path(cnt);
                    for (int i = 0; i < cnt; i++) { p.Add(op.Pt); op = op.Prev; }
                    if ((outrec.Flags & OutrecFlags.Open) > 0)
                        openPaths.Add(p);
                    else
                    {
                        if (outrec.Owner != null && outrec.Owner.PolyPath != null)
                            outrec.PolyPath = outrec.Owner.PolyPath.AddChild(p);
                        else
                            outrec.PolyPath = pt.AddChild(p);
                    }
                }
        }
        //------------------------------------------------------------------------------

    } //Clipper

    class ClipperException : Exception
    {
        public ClipperException(string description) : base(description) { }
    }
    //------------------------------------------------------------------------------

} //namespace
